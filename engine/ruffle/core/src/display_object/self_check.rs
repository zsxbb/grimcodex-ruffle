//! AQW build (GrimCodex): self-checks of the pictures the shortcuts keep, off unless switched on
//! (for tests, and for a player to send a log with). Both cost nothing while off: a switch
//! read once, and a flag the trackers ask (`interp::dry`).
//!
//! `RUFFLE_CACHE_VERIFY` (a `cache-verify` file next to the exe; `=panic` panics, for tests):
//! a cached object (cacheAsBitmap, filters) keeps its picture until something invalidates it.
//! Whenever its picture is drawn, what it was drawn from is kept: the commands of its contents
//! (with the transform they were drawn inside), the filters it was drawn with, its background,
//! and the version of every cached object's picture drawn into it. Once per SWF frame, when the
//! object is drawn from its kept picture (not dirty, not waiting for shapes), its contents are
//! walked again inside that same transform without drawing or changing anything (a dry walk:
//! the trackers are read through a copy, no cache is drawn or marked, no flag is cleared), and
//! the commands compared as lean render's check compares them (the same tessellation or one of
//! the same shape, a stand-in included; the same transform, bitmap and blend). The picture is
//! stale when they differ, when its filters or background do, when a cached object drawn into
//! it was drawn again since, or would be now (it's dirty). A difference that lasts for 3 checks
//! in 3 SWF frames is reported ("CACHE VERIFY: stale picture") with the object's path, its
//! movie, what differs and for how long; a shorter one is not (a change between two frames is
//! drawn with the next frame's picture at the latest). Pictures kept on purpose are not
//! checked: an object waiting for its shapes keeps its last picture until they're done (see
//! `deferred_shapes`), and one whose contents (or a cached object in them) can't be told is
//! skipped (drawn for the first time, too big to cache). Not compared: a bitmap's pixels (only
//! which bitmap), and the scroll rects of the objects inside, which a walk takes up in
//! `pre_render` (left alone by the dry walk). Cost while on: one dry walk per cached object
//! drawn from its picture per SWF frame, and a copy of the commands of every picture drawn.
//!
//! `RUFFLE_REPLAY_VERIFY` (a `replay-verify` file; `=panic`): every picture replayed from a
//! recording (frame interpolation's in-between pictures, see `replay`) is walked as well (dry),
//! and the two lists of commands compared. A replay promises the picture as it was recorded,
//! with the moving objects' transforms blended for the picture's time, and the islands (text,
//! cached and masked objects, shape tweens, 9-slice shapes) drawn as they are now. So:
//! - while nothing has changed between any two of its pictures (the recording's and the
//!   replays'), the whole picture must be what a walk draws;
//! - after a change between two frames without a redraw request (left for the next frame on
//!   purpose, see `Saved::plan`), every branch of the recording whose objects didn't change
//!   since it was begun (lean render's marks tell; without lean render nothing is compared
//!   then) must be what a walk of that branch draws inside its parent's transform in the
//!   replay. A changed branch is looked into for unchanged branches below it.
//! A difference is reported ("REPLAY VERIFY") with the deepest branch that differs. Cost while
//! on: a dry walk of the stage per replayed picture.
//!
//! `RUFFLE_CACHE_VERIFY_FAULT` and `RUFFLE_REPLAY_VERIFY_FAULT` break what each check is
//! about on purpose, to see that it reports it (testing the tests).

use crate::display_object::{DisplayObject, TDisplayObject};
use ruffle_render::bitmap::{BitmapHandle, BitmapHandleImpl};
use ruffle_render::commands::Command;
use ruffle_render::filters::Filter;
use ruffle_render::transform::Transform;
use std::cell::{Cell, RefCell};
use std::collections::HashMap;
use std::sync::OnceLock;
use std::sync::Weak;
use swf::Color;
use web_time::Instant;

fn switch(cell: &'static OnceLock<bool>, name: &str) -> bool {
    *cell.get_or_init(|| std::env::var_os(name).is_some())
}

fn panics(name: &str) -> bool {
    std::env::var_os(name).is_some_and(|v| v == "panic")
}

pub fn cache_verify() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    switch(&ON, "RUFFLE_CACHE_VERIFY")
}

pub fn replay_verify() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    switch(&ON, "RUFFLE_REPLAY_VERIFY")
}

/// `RUFFLE_CACHE_VERIFY_FAULT` (testing the test): a replay draws its islands as 2.0.1 did,
/// without clearing their "cache invalidated" flag, so a change of a cached one can be lost.
/// Unless it's `=flag`, the rest is as in 2.0.1 too (`fault_as_201`): islands hidden since
/// the recording are drawn, and cached pictures drawn in between two frames are not drawn
/// again. Only with `RUFFLE_CACHE_VERIFY`, to see that it's reported.
pub fn cache_fault() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    cache_verify() && switch(&ON, "RUFFLE_CACHE_VERIFY_FAULT")
}

/// See `cache_fault`.
pub fn fault_as_201() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    cache_fault()
        && *ON.get_or_init(|| {
            std::env::var_os("RUFFLE_CACHE_VERIFY_FAULT").is_some_and(|v| v != "flag")
        })
}

/// `RUFFLE_REPLAY_VERIFY_FAULT` (testing the test): a replay draws the moving objects where
/// they were recorded instead of blending them. Only with `RUFFLE_REPLAY_VERIFY`.
pub fn replay_fault() -> bool {
    static ON: OnceLock<bool> = OnceLock::new();
    replay_verify() && switch(&ON, "RUFFLE_REPLAY_VERIFY_FAULT")
}

/// Either check (they need to know which tessellations are of one shape, see
/// `tessellation_cache::made_alike`).
pub fn any() -> bool {
    cache_verify() || replay_verify()
}

/// Checks, then reports: once per SWF frame per object; reported when stale this long.
const STALE_CHECKS: u32 = 3;
const STALE_FRAMES: u64 = 2;
/// Reports logged in full (the counts go on).
const LOGGED: u64 = 40;

/// What a cached object's picture was drawn from (see the module docs).
pub struct Picture {
    /// The transform its contents were drawn inside.
    root: Transform,
    commands: Vec<Command>,
    /// The versions of the cached objects' pictures it drew, in the order of its bitmaps.
    nested: Vec<Option<u64>>,
    filters: Vec<Filter>,
    clear: Color,
    /// The SWF frame it was last checked in.
    checked: u64,
    /// Since when it's been stale: the frame, the time, and how many checks found it so.
    stale: Option<(u64, Instant, u32)>,
    reported: bool,
}

/// Kept with a cached object's `BitmapCache` (empty while the check is off; a copy starts
/// empty).
#[derive(Default)]
pub struct Slot(Option<Box<Picture>>);

impl Clone for Slot {
    fn clone(&self) -> Self {
        Self::default()
    }
}

impl std::fmt::Debug for Slot {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(if self.0.is_some() {
            "Slot(kept)"
        } else {
            "Slot"
        })
    }
}

thread_local! {
    /// The version of each cached object's picture (by its texture's address): how many times
    /// it was drawn. (The weak pointer keeps the address from being given to another texture
    /// while its entry is here.)
    static VERSIONS: RefCell<HashMap<usize, (Weak<dyn BitmapHandleImpl>, u64)>> =
        RefCell::new(HashMap::new());
    static NEXT_PRUNE: Cell<usize> = const { Cell::new(4096) };
    /// During a dry walk: the cached objects in it that a walk would draw again now.
    static WOULD_REDRAW: RefCell<HashMap<usize, String>> = RefCell::new(HashMap::new());
    /// During a dry walk: why it can't tell what a walk would draw.
    static UNSURE: Cell<Option<&'static str>> = const { Cell::new(None) };
    static STATS: Cell<Stats> = const { Cell::new(Stats { checks: 0, stored: 0, unsure: 0, waiting: 0, found: 0, replays: 0, whole: 0, by_branch: 0, branches: 0, replay_found: 0, frames: 0 }) };
}

#[derive(Clone, Copy)]
struct Stats {
    checks: u64,
    stored: u64,
    unsure: u64,
    waiting: u64,
    found: u64,
    replays: u64,
    whole: u64,
    by_branch: u64,
    branches: u64,
    replay_found: u64,
    frames: u64,
}

fn stats(change: impl FnOnce(&mut Stats)) -> Stats {
    STATS.with(|s| {
        let mut stats = s.get();
        change(&mut stats);
        s.set(stats);
        stats
    })
}

fn address(handle: &BitmapHandle) -> usize {
    std::sync::Arc::as_ptr(&handle.0) as *const () as usize
}

fn version(handle: &BitmapHandle) -> Option<u64> {
    VERSIONS.with_borrow(|v| v.get(&address(handle)).map(|entry| entry.1))
}

fn bitmaps<'a>(commands: &'a [Command], out: &mut Vec<&'a BitmapHandle>) {
    for command in commands {
        match command {
            Command::RenderBitmap { bitmap, .. } | Command::RenderStage3D { bitmap, .. } => {
                out.push(bitmap)
            }
            Command::RenderAlphaMask {
                maskee_commands,
                mask_commands,
            } => {
                bitmaps(&maskee_commands.commands, out);
                bitmaps(&mask_commands.commands, out);
            }
            Command::Blend(list, _) => bitmaps(&list.commands, out),
            _ => {}
        }
    }
}

/// A cached object's picture is drawn (into `handle`) from `commands`, inside `root`, with
/// `filters`, on `clear`: it's kept, and the picture's version goes up.
pub(crate) fn drawn(
    slot: &mut Slot,
    handle: &BitmapHandle,
    root: Transform,
    commands: &[Command],
    filters: &[Filter],
    clear: Color,
) {
    let mut drawn_into = Vec::new();
    bitmaps(commands, &mut drawn_into);
    let nested = drawn_into.into_iter().map(version).collect();
    VERSIONS.with_borrow_mut(|versions| {
        if versions.len() >= NEXT_PRUNE.with(Cell::get) {
            versions.retain(|_, entry| entry.0.strong_count() > 0);
            NEXT_PRUNE.with(|next| next.set((versions.len() * 2).max(4096)));
        }
        let entry = versions
            .entry(address(handle))
            .or_insert_with(|| (std::sync::Arc::downgrade(&handle.0), 0));
        entry.1 += 1;
    });
    stats(|s| s.stored += 1);
    slot.0 = Some(Box::new(Picture {
        root,
        commands: commands.to_vec(),
        nested,
        filters: filters.to_vec(),
        clear,
        checked: 0,
        stale: None,
        reported: false,
    }));
}

/// The cached object is drawn from its kept picture: whether it's checked now (once per SWF
/// frame), and the transform to walk its contents inside.
pub(crate) fn due(slot: &mut Slot) -> Option<Transform> {
    let picture = slot.0.as_mut()?;
    let frame = crate::display_object::interp::frame();
    if picture.checked == frame {
        return None;
    }
    picture.checked = frame;
    Some(picture.root.clone())
}

/// The cached object is waiting for shapes: it keeps its last picture on purpose.
pub(crate) fn waiting() {
    stats(|s| s.waiting += 1);
}

/// A dry walk of a cached object's contents begins (`dry_result` ends it).
pub(crate) fn dry_scope() {
    WOULD_REDRAW.with_borrow_mut(HashMap::clear);
    UNSURE.with(|u| u.set(None));
}

/// In a dry walk: a cached object (`object`, drawn into `handle`) would be drawn again now, and
/// why; `None`: it would get its first picture, or a new texture.
pub(crate) fn would_redraw(object: DisplayObject<'_>, handle: Option<&BitmapHandle>, why: &str) {
    match handle {
        Some(handle) => {
            let what = format!("{} ({why})", describe(object));
            WOULD_REDRAW.with_borrow_mut(|map| map.insert(address(handle), what));
        }
        None => unsure("a cached object in it would be drawn for the first time"),
    }
}

/// In a dry walk: something it can't tell.
pub(crate) fn unsure(why: &'static str) {
    UNSURE.with(|u| {
        if u.get().is_none() {
            u.set(Some(why));
        }
    });
}

fn describe(object: DisplayObject<'_>) -> String {
    let kind = match object {
        DisplayObject::Stage(_) => "stage",
        DisplayObject::Bitmap(_) => "bitmap",
        DisplayObject::Avm1Button(_) => "AVM1 button",
        DisplayObject::Avm2Button(_) => "button",
        DisplayObject::EditText(_) => "text field",
        DisplayObject::TextLine(_) => "text line",
        DisplayObject::Graphic(_) => "shape",
        DisplayObject::MorphShape(_) => "shape tween",
        DisplayObject::MovieClip(_) => "clip",
        DisplayObject::Text(_) => "text",
        DisplayObject::Video(_) => "video",
        DisplayObject::LoaderDisplay(_) => "loader",
    };
    let name = object.name().map(|n| n.to_string()).unwrap_or_default();
    format!("{kind} {} {name:?}", object.id())
}

/// Where an object is: its ancestors and itself, from the stage down.
pub(crate) fn path_of(object: DisplayObject<'_>) -> String {
    let mut path = String::new();
    let mut at = Some(object);
    let mut depth = 0;
    while let Some(o) = at {
        path = format!("/{}{path}", describe(o));
        at = o.parent();
        depth += 1;
        if depth > 64 {
            path = format!("...{path}");
            break;
        }
    }
    path
}

fn url_of(object: DisplayObject<'_>) -> String {
    object.movie().url().to_string()
}

/// `path_of`, with the object's movie.
pub(crate) fn where_is(object: DisplayObject<'_>) -> String {
    format!("{} (in {})", path_of(object), url_of(object))
}

/// The dry walk of the contents of `object` (kept in `slot`) gave `now`, with `filters` and
/// `clear` as they are now: compared with what its picture was drawn from.
pub(crate) fn judge(
    object: DisplayObject<'_>,
    slot: &mut Slot,
    now: &[Command],
    filters: &[Filter],
    clear: Color,
) {
    let unsure_why = UNSURE.with(|u| u.take());
    let would_redraw = WOULD_REDRAW.with_borrow_mut(std::mem::take);
    let Some(picture) = slot.0.as_mut() else {
        return;
    };
    if unsure_why.is_some() {
        stats(|s| s.unsure += 1);
        return;
    }
    stats(|s| s.checks += 1);
    let mut what = super::replay::difference(&picture.commands, now);
    // (A shader filter's image inputs compare by where the copy is: told apart by how they
    // print.)
    if what.is_none()
        && picture.filters != filters
        && format!("{:?}", picture.filters) != format!("{filters:?}")
    {
        let short = |filters: &[Filter]| -> String {
            let text = format!("{filters:?}");
            text.chars().take(300).collect()
        };
        what = Some(format!(
            "its filters were {} and are {} now",
            short(&picture.filters),
            short(filters)
        ));
    }
    if what.is_none() && picture.clear != clear {
        what = Some("its background colour changed".to_string());
    }
    if what.is_none() {
        let mut drawn_into = Vec::new();
        bitmaps(now, &mut drawn_into);
        for (index, (handle, then)) in drawn_into.iter().zip(&picture.nested).enumerate() {
            let current = version(handle);
            if *then != current {
                what = Some(format!(
                    "the cached object drawn as its bitmap {index} was drawn again since (picture {then:?}, now {current:?})"
                ));
                break;
            }
            if let Some(which) = would_redraw.get(&address(handle)) {
                what = Some(format!(
                    "the cached object drawn as its bitmap {index}, {which}, would be drawn again now: its own picture is out of date"
                ));
                break;
            }
        }
    }
    let frame = crate::display_object::interp::frame();
    let Some(what) = what else {
        if picture.reported {
            tracing::warn!(
                target: "ruffle_aqw_perf",
                "CACHE VERIFY: the picture of {} is right again",
                path_of(object)
            );
        }
        picture.stale = None;
        picture.reported = false;
        return;
    };
    let (since, at, checks) = picture.stale.get_or_insert((frame, Instant::now(), 0));
    *checks += 1;
    if picture.reported || *checks < STALE_CHECKS || frame - *since < STALE_FRAMES {
        return;
    }
    picture.reported = true;
    let message = format!(
        "CACHE VERIFY: stale picture: {} (in {}) was drawn from its kept picture, but {what}; so for {} checks, {} SWF frames, {:.0} ms",
        path_of(object),
        url_of(object),
        checks,
        frame - *since,
        at.elapsed().as_secs_f64() * 1000.0
    );
    let found = stats(|s| s.found += 1).found;
    if found <= LOGGED {
        tracing::warn!(target: "ruffle_aqw_perf", "{message}");
    }
    if panics("RUFFLE_CACHE_VERIFY") {
        panic!("{message}");
    }
}

/// The replay check: a replay compared with a walk, as a whole (`whole`) or by its unchanged
/// branches (`branches` of them).
pub(crate) fn replay_compared(whole: bool, branches: u64) {
    stats(|s| {
        s.replays += 1;
        if whole {
            s.whole += 1;
        } else {
            s.by_branch += 1;
        }
        s.branches += branches;
    });
}

pub(crate) fn replay_found(message: String) {
    let found = stats(|s| s.replay_found += 1).replay_found;
    let message = format!("REPLAY VERIFY: {message}");
    if found <= LOGGED {
        tracing::warn!(target: "ruffle_aqw_perf", "{message}");
    }
    if panics("RUFFLE_REPLAY_VERIFY") {
        panic!("{message}");
    }
}

/// Called when a frame's walks are done: a line in the log now and then.
pub fn frame_done() {
    if !any() {
        return;
    }
    let s = stats(|s| s.frames += 1);
    if s.frames < 150 {
        return;
    }
    if cache_verify() {
        tracing::info!(
            target: "ruffle_aqw_perf",
            "cache verify: {} pictures kept, {} checks, {} not told (a cached object inside drawn for the first time), {} waiting for shapes, in {} frames; {} stale found in all",
            s.stored, s.checks, s.unsure, s.waiting, s.frames, s.found
        );
    }
    if replay_verify() {
        tracing::info!(
            target: "ruffle_aqw_perf",
            "replay verify: {} replays compared in {} frames ({} whole, {} by {} unchanged branches); {} found in all",
            s.replays, s.frames, s.whole, s.by_branch, s.branches, s.replay_found
        );
    }
    STATS.with(|st| {
        st.set(Stats {
            found: s.found,
            replay_found: s.replay_found,
            checks: 0,
            stored: 0,
            unsure: 0,
            waiting: 0,
            replays: 0,
            whole: 0,
            by_branch: 0,
            branches: 0,
            frames: 0,
        })
    });
}
