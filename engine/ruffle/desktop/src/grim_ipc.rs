use crate::custom_event::RuffleEvent;
use ruffle_core::context::UpdateContext;
use ruffle_core::external::{ExternalInterfaceProvider, Value as ExternalValue};
use ruffle_render::quality::StageQuality;
use std::collections::{BTreeMap, HashMap};
use std::fs::{File, OpenOptions};
use std::io::{BufRead, BufReader, BufWriter, Write};
use std::sync::atomic::{AtomicI64, Ordering};
use std::sync::{Arc, Mutex, mpsc};
use std::time::Duration;
use url::Url;
use winit::event_loop::EventLoopProxy;

pub enum GrimIpcCommand {
    Call {
        id: Option<i64>,
        name: String,
        args: Vec<ExternalValue>,
    },
    Resize {
        width: u32,
        height: u32,
    },
    SetQuality {
        quality: StageQuality,
    },
    Shutdown,
}

struct GrimIpcInner {
    writer: Mutex<BufWriter<File>>,
    next_ext_id: AtomicI64,
    pending_ext: Mutex<HashMap<i64, mpsc::Sender<ExternalValue>>>,
}

#[derive(Clone)]
pub struct GrimIpcBridge {
    inner: Arc<GrimIpcInner>,
}

impl std::fmt::Debug for GrimIpcBridge {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.debug_struct("GrimIpcBridge").finish()
    }
}

impl GrimIpcBridge {
    pub fn connect(pipe_base: &str, event_loop: EventLoopProxy<RuffleEvent>) -> anyhow::Result<Self> {
        let c2r_path = format!("{pipe_base}-c2r");
        let r2c_path = format!("{pipe_base}-r2c");

        let read_file = open_pipe_with_retry(&c2r_path, false)?;
        let write_file = open_pipe_with_retry(&r2c_path, true)?;

        let inner = Arc::new(GrimIpcInner {
            writer: Mutex::new(BufWriter::new(write_file)),
            next_ext_id: AtomicI64::new(1),
            pending_ext: Mutex::new(HashMap::new()),
        });

        let reader_inner = inner.clone();
        std::thread::Builder::new()
            .name("grim-ipc-reader".to_string())
            .spawn(move || {
                reader_loop(read_file, reader_inner, event_loop);
            })?;

        tracing::info!("Connected to Grim IPC pipes: {pipe_base}");
        Ok(Self { inner })
    }

    pub fn send_ready(&self, hwnd: u64) {
        let msg = serde_json::json!({
            "type": "ready",
            "hwnd": hwnd,
        });
        self.write_json(&msg);
    }

    pub fn send_call_result(&self, id: i64, ok: bool, value: &ExternalValue) {
        let msg = serde_json::json!({
            "id": id,
            "type": "result",
            "ok": ok,
            "value": external_value_to_json(value),
        });
        self.write_json(&msg);
    }

    pub fn call_host(&self, name: &str, args: &[ExternalValue]) -> ExternalValue {
        let needs_return = matches!(name, "modifyServers");
        let id = self.inner.next_ext_id.fetch_add(1, Ordering::Relaxed);
        let json_args: Vec<serde_json::Value> = args.iter().map(external_value_to_json).collect();

        let rx = if needs_return {
            let (tx, rx) = mpsc::channel();
            if let Ok(mut map) = self.inner.pending_ext.lock() {
                map.insert(id, tx);
            }
            Some(rx)
        } else {
            None
        };

        let msg = serde_json::json!({
            "id": id,
            "type": "ext_call",
            "name": name,
            "args": json_args,
            "needs_return": needs_return,
        });
        self.write_json(&msg);

        if let Some(rx) = rx {
            match rx.recv_timeout(Duration::from_secs(10)) {
                Ok(val) => val,
                Err(_) => {
                    if let Ok(mut map) = self.inner.pending_ext.lock() {
                        map.remove(&id);
                    }
                    tracing::warn!("Grim IPC timed out waiting for ext_result on '{name}' (id={id})");
                    ExternalValue::Null
                }
            }
        } else {
            ExternalValue::Null
        }
    }

    fn write_json(&self, value: &serde_json::Value) {
        let Ok(serialized) = serde_json::to_string(value) else {
            return;
        };
        if let Ok(mut writer) = self.inner.writer.lock() {
            let _ = writer.write_all(serialized.as_bytes());
            let _ = writer.write_all(b"\n");
            let _ = writer.flush();
        }
    }
}

fn open_pipe_with_retry(path: &str, write: bool) -> anyhow::Result<File> {
    let mut last_err = None;
    for _ in 0..250 {
        let res = if write {
            OpenOptions::new().write(true).open(path)
        } else {
            OpenOptions::new().read(true).open(path)
        };
        match res {
            Ok(file) => return Ok(file),
            Err(err) => {
                last_err = Some(err);
                std::thread::sleep(Duration::from_millis(20));
            }
        }
    }
    Err(anyhow::anyhow!(
        "Failed to open Grim IPC pipe '{path}': {:?}",
        last_err
    ))
}

fn reader_loop(file: File, inner: Arc<GrimIpcInner>, event_loop: EventLoopProxy<RuffleEvent>) {
    let mut reader = BufReader::new(file);
    let mut line = String::new();

    loop {
        line.clear();
        match reader.read_line(&mut line) {
            Ok(0) => {
                tracing::info!("Grim IPC pipe closed by host; exiting.");
                let _ = event_loop.send_event(RuffleEvent::ExitRequested);
                break;
            }
            Ok(_) => {
                let trimmed = line.trim();
                if trimmed.is_empty() {
                    continue;
                }
                handle_host_message(trimmed, &inner, &event_loop);
            }
            Err(err) => {
                tracing::warn!("Grim IPC read error: {err}; exiting.");
                let _ = event_loop.send_event(RuffleEvent::ExitRequested);
                break;
            }
        }
    }
}

fn handle_host_message(
    line: &str,
    inner: &Arc<GrimIpcInner>,
    event_loop: &EventLoopProxy<RuffleEvent>,
) {
    let Ok(msg) = serde_json::from_str::<serde_json::Value>(line) else {
        tracing::warn!("Ignoring malformed Grim IPC message");
        return;
    };

    let Some(msg_type) = msg.get("type").and_then(|v| v.as_str()) else {
        return;
    };

    match msg_type {
        "call" => {
            let no_reply = msg
                .get("no_reply")
                .and_then(|v| v.as_bool())
                .unwrap_or(false);
            let id = if no_reply {
                None
            } else {
                Some(msg.get("id").and_then(|v| v.as_i64()).unwrap_or(0))
            };
            let Some(name) = msg.get("name").and_then(|v| v.as_str()).map(str::to_owned) else {
                return;
            };
            let args = msg
                .get("args")
                .and_then(|v| v.as_array())
                .map(|arr| arr.iter().map(json_to_external_value).collect())
                .unwrap_or_default();
            let _ = event_loop.send_event(RuffleEvent::GrimIpc(GrimIpcCommand::Call {
                id,
                name,
                args,
            }));
        }
        "ext_result" => {
            let id = msg.get("id").and_then(|v| v.as_i64()).unwrap_or(0);
            let value = msg
                .get("value")
                .map(json_to_external_value)
                .unwrap_or(ExternalValue::Null);
            let sender = inner
                .pending_ext
                .lock()
                .ok()
                .and_then(|mut map| map.remove(&id));
            if let Some(tx) = sender {
                let _ = tx.send(value);
            }
        }
        "resize" => {
            let width = msg.get("width").and_then(|v| v.as_u64()).unwrap_or(0) as u32;
            let height = msg.get("height").and_then(|v| v.as_u64()).unwrap_or(0) as u32;
            if width > 0 && height > 0 {
                let _ = event_loop.send_event(RuffleEvent::GrimIpc(GrimIpcCommand::Resize {
                    width,
                    height,
                }));
            }
        }
        "set_quality" => {
            if let Some(q) = msg.get("quality").and_then(|v| v.as_str()) {
                let quality = match q.to_ascii_lowercase().as_str() {
                    "low" => StageQuality::Low,
                    "medium" => StageQuality::Medium,
                    "high" => StageQuality::High,
                    "best" => StageQuality::Best,
                    _ => StageQuality::High,
                };
                let _ = event_loop.send_event(RuffleEvent::GrimIpc(GrimIpcCommand::SetQuality {
                    quality,
                }));
            }
        }
        "shutdown" => {
            let _ = event_loop.send_event(RuffleEvent::GrimIpc(GrimIpcCommand::Shutdown));
        }
        _ => {}
    }
}

pub struct GrimExternalInterfaceProvider {
    pub ipc: GrimIpcBridge,
    pub spoof_url: Option<Url>,
}

fn is_location_href(code: &str) -> bool {
    matches!(
        code,
        "document.location.href" | "window.location.href" | "top.location.href"
    )
}

impl ExternalInterfaceProvider for GrimExternalInterfaceProvider {
    fn call_method(
        &self,
        _context: &mut UpdateContext<'_>,
        name: &str,
        args: &[ExternalValue],
    ) -> ExternalValue {
        if let Some(ref url) = self.spoof_url
            && let Some(stripped) = name.strip_suffix(".toString")
            && is_location_href(stripped)
        {
            return url.to_string().into();
        }

        if name == "eval"
            && let Some(ref url) = self.spoof_url
            && let [ExternalValue::String(code)] = args
            && is_location_href(code)
        {
            return ExternalValue::String(url.to_string());
        }

        self.ipc.call_host(name, args)
    }

    fn on_callback_available(&self, _name: &str) {}

    fn get_id(&self) -> Option<String> {
        Some("Grimoire".to_string())
    }
}

pub fn external_value_to_json(val: &ExternalValue) -> serde_json::Value {
    match val {
        ExternalValue::Undefined | ExternalValue::Null => serde_json::Value::Null,
        ExternalValue::Bool(b) => serde_json::Value::Bool(*b),
        ExternalValue::Number(n) => {
            if n.is_finite() {
                if n.fract() == 0.0 && *n >= (i64::MIN as f64) && *n <= (i64::MAX as f64) {
                    serde_json::Value::Number((*n as i64).into())
                } else {
                    serde_json::Number::from_f64(*n)
                        .map(serde_json::Value::Number)
                        .unwrap_or(serde_json::Value::Null)
                }
            } else {
                serde_json::Value::Null
            }
        }
        ExternalValue::String(s) => serde_json::Value::String(s.clone()),
        ExternalValue::List(items) => {
            serde_json::Value::Array(items.iter().map(external_value_to_json).collect())
        }
        ExternalValue::Object(map) => {
            let mut obj = serde_json::Map::with_capacity(map.len());
            for (k, v) in map {
                obj.insert(k.clone(), external_value_to_json(v));
            }
            serde_json::Value::Object(obj)
        }
    }
}

pub fn json_to_external_value(val: &serde_json::Value) -> ExternalValue {
    match val {
        serde_json::Value::Null => ExternalValue::Null,
        serde_json::Value::Bool(b) => ExternalValue::Bool(*b),
        serde_json::Value::Number(n) => ExternalValue::Number(n.as_f64().unwrap_or(0.0)),
        serde_json::Value::String(s) => ExternalValue::String(s.clone()),
        serde_json::Value::Array(arr) => {
            ExternalValue::List(arr.iter().map(json_to_external_value).collect())
        }
        serde_json::Value::Object(obj) => {
            let mut map = BTreeMap::new();
            for (k, v) in obj {
                map.insert(k.clone(), json_to_external_value(v));
            }
            ExternalValue::Object(map)
        }
    }
}

