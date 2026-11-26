use std::collections::{HashMap, HashSet};
use std::fs::{self, OpenOptions};
use std::io::Write;
use std::path::{Path, PathBuf};
use std::sync::{Mutex, OnceLock};
use std::time::SystemTime;
use url::Url;

#[derive(Default)]
struct OverrideState {
    config_mtime: Option<SystemTime>,
    enabled: bool,
    log_swfs: bool,
    /// Normalized lowercase key (e.g. "mon/frogzard2.swf" or "frogzard2.swf") -> local file path.
    entries: HashMap<String, PathBuf>,
    logged_urls: HashSet<String>,
}

fn mods_dir() -> Option<&'static Path> {
    static DIR: OnceLock<Option<PathBuf>> = OnceLock::new();
    DIR.get_or_init(|| {
        let exe = std::env::current_exe().ok()?;
        let exe_dir = exe.parent()?;
        // In GrimCodex Ruffle, the engine lives at `<app>\runtime\grimcodex-engine.exe`
        // while `Mods\` lives at `<app>\Mods\`.
        let parent_mods = exe_dir.parent().map(|p| p.join("Mods"));
        if let Some(ref dir) = parent_mods
            && exe_dir
                .file_name()
                .is_some_and(|n| n.eq_ignore_ascii_case("runtime"))
        {
            return Some(dir.clone());
        }
        Some(exe_dir.join("Mods"))
    })
    .as_deref()
}

fn state_lock() -> &'static Mutex<OverrideState> {
    static STATE: OnceLock<Mutex<OverrideState>> = OnceLock::new();
    STATE.get_or_init(|| Mutex::new(OverrideState::default()))
}

fn normalize_key(raw: &str) -> String {
    raw.trim()
        .replace('\\', "/")
        .trim_start_matches('/')
        .to_ascii_lowercase()
}

fn refresh_config(state: &mut OverrideState, dir: &Path) {
    let cfg_path = dir.join("overrides.cfg");
    let mtime = fs::metadata(&cfg_path).ok().and_then(|m| m.modified().ok());
    if mtime == state.config_mtime {
        return;
    }

    state.config_mtime = mtime;
    state.enabled = false;
    state.log_swfs = false;
    state.entries.clear();

    let Some(text) = fs::read_to_string(&cfg_path).ok() else {
        return;
    };

    for raw_line in text.trim_start_matches('\u{FEFF}').lines() {
        let line = raw_line.trim();
        if line.is_empty() || line.starts_with('#') || line.starts_with(';') {
            continue;
        }
        let Some((k, v)) = line.split_once('=') else {
            continue;
        };
        let key = k.trim();
        let val = v.trim();
        if key.eq_ignore_ascii_case("enabled") {
            state.enabled = matches!(val, "1" | "true" | "TRUE" | "yes" | "on");
        } else if key.eq_ignore_ascii_case("log_swfs") {
            state.log_swfs = matches!(val, "1" | "true" | "TRUE" | "yes" | "on");
        } else if !key.is_empty() && !val.is_empty() {
            let norm_key = normalize_key(key);
            let target = PathBuf::from(val);
            let resolved = if target.is_absolute() {
                target
            } else {
                dir.join(target)
            };
            state.entries.insert(norm_key, resolved);
        }
    }
}

/// Extracts `(rel_after_gamefiles, basename)` from a fetched SWF URL, ignoring query parameters.
fn extract_swf_keys(url: &Url) -> Option<(String, String)> {
    let path = url.path();
    if !path.to_ascii_lowercase().ends_with(".swf") {
        return None;
    }
    let lower = path.to_ascii_lowercase();
    let rel = if let Some(idx) = lower.find("/gamefiles/") {
        path[idx + "/gamefiles/".len()..].to_string()
    } else {
        path.trim_start_matches('/').to_string()
    };
    let rel_norm = normalize_key(&rel);
    let base = rel_norm
        .rsplit('/')
        .next()
        .unwrap_or(&rel_norm)
        .to_string();
    Some((rel_norm, base))
}

/// Checks whether a remote SWF URL has a local override configured in `<app>\Mods\overrides.cfg`
/// (or dropped directly under `<app>\Mods\<rel_path>` while `enabled=1`).
/// Also records fetched `.swf` URLs into `<app>\Mods\loaded_swfs.log` when `log_swfs=1`.
pub fn resolve_swf_override(url: &Url) -> Option<(PathBuf, Vec<u8>)> {
    if !matches!(url.scheme(), "http" | "https") {
        return None;
    }
    let (rel_key, base_key) = extract_swf_keys(url)?;
    let dir = mods_dir()?;

    let mut state = state_lock().lock().ok()?;
    refresh_config(&mut state, dir);

    if state.log_swfs && state.logged_urls.insert(rel_key.clone()) {
        let log_path = dir.join("loaded_swfs.log");
        if let Ok(mut file) = OpenOptions::new().create(true).append(true).open(&log_path) {
            let mut clean_url = url.clone();
            clean_url.set_query(None);
            let _ = writeln!(file, "{rel_key}\t{clean_url}");
        }
    }

    if !state.enabled {
        return None;
    }

    let candidate = state
        .entries
        .get(&rel_key)
        .or_else(|| state.entries.get(&base_key))
        .cloned()
        .or_else(|| {
            // Direct drop-in folder override: `<app>\Mods\mon\Frogzard2.swf`
            let direct = dir.join(&rel_key);
            direct.is_file().then_some(direct)
        })?;

    drop(state);

    match fs::read(&candidate) {
        Ok(bytes) if !bytes.is_empty() => {
            tracing::info!(
                "GrimCodex SWF mod override: {} -> {}",
                url,
                candidate.display()
            );
            Some((candidate, bytes))
        }
        Ok(_) => None,
        Err(err) => {
            tracing::warn!(
                "GrimCodex SWF mod override failed to read {}: {}",
                candidate.display(),
                err
            );
            None
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn extracts_aqw_gamefiles_relative_and_base_keys() {
        let u = Url::parse("https://game.aq.com/game/gamefiles/mon/Frogzard2.swf?ver=42").unwrap();
        let (rel, base) = extract_swf_keys(&u).unwrap();
        assert_eq!(rel, "mon/frogzard2.swf");
        assert_eq!(base, "frogzard2.swf");
    }

    #[test]
    fn ignores_non_swf_urls() {
        let u = Url::parse("https://game.aq.com/game/api/data/gameversion").unwrap();
        assert!(extract_swf_keys(&u).is_none());
    }
}

