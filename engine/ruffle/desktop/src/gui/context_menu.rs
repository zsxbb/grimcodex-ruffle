use crate::custom_event::RuffleEvent;
use crate::grimcodex::{
    FpsLimit, Graphics, MenuSettings, SCALE_MAX_PERCENT, SCALE_MIN_PERCENT, Setting,
};
use egui::{
    Align, Align2, Area, Button, Checkbox, Color32, CornerRadius, FontId, Frame, Id, Key,
    KeyboardShortcut, Layout, Margin, Modifiers, Order, Painter, Pos2, Rect, Response, Sense,
    Shadow, Stroke, StrokeKind, Style, Ui, Widget, pos2, vec2,
};
use ruffle_core::{ContextMenuItem, PlayerEvent};
use unic_langid::LanguageIdentifier;
use winit::event_loop::EventLoopProxy;

use super::text;

pub struct ContextMenu {
    items: Vec<ContextMenuItem>,
    settings: Option<MenuSettings>,
    position: Option<Pos2>,
    close_event: PlayerEvent,
}

impl ContextMenu {
    pub fn new(
        items: Vec<ContextMenuItem>,
        settings: Option<MenuSettings>,
        close_event: PlayerEvent,
    ) -> Self {
        Self {
            items,
            settings,
            position: None,
            close_event,
        }
    }

    pub fn close_event(self) -> PlayerEvent {
        self.close_event
    }

    pub fn show(
        &mut self,
        locale: &LanguageIdentifier,
        egui_ctx: &egui::Context,
        event_loop: &EventLoopProxy<RuffleEvent>,
        fullscreen: bool,
    ) -> bool {
        if let Some(settings) = self.settings {
            return self.show_grimcodex(egui_ctx, event_loop, settings);
        }
        let mut item_clicked = false;
        self.position = self.position.or(egui_ctx.pointer_latest_pos());

        let area = Area::new(Id::new("context_menu"))
            .order(Order::Foreground)
            .fixed_pos(self.position.unwrap_or_default())
            .constrain_to(egui_ctx.content_rect())
            .interactable(true)
            .show(egui_ctx, |ui| {
                set_menu_style(ui.style_mut());
                Frame::menu(ui.style()).show(ui, |ui| {
                    ui.set_max_width(150.0);
                    ui.with_layout(Layout::top_down_justified(Align::Min), |ui| {
                        for (i, item) in self.items.iter().enumerate() {
                            if i != 0 && item.separator_before {
                                ui.separator();
                            }
                            let clicked = if item.checked {
                                Checkbox::new(&mut true, &item.caption).ui(ui).clicked()
                            } else {
                                let button = Button::new(&item.caption)
                                    .wrap_mode(egui::TextWrapMode::Extend);

                                ui.add_enabled(item.enabled, button).clicked()
                            };
                            if clicked {
                                let _ =
                                    event_loop.send_event(RuffleEvent::ContextMenuItemClicked(i));
                                item_clicked = true;
                            }
                        }

                        if fullscreen {
                            ui.separator();
                            if Button::new(text(locale, "context-menu-exit-fullscreen"))
                                .shortcut_text(ui.ctx().format_shortcut(&KeyboardShortcut::new(
                                    Modifiers::NONE,
                                    Key::Escape,
                                )))
                                .wrap_mode(egui::TextWrapMode::Extend)
                                .ui(ui)
                                .clicked()
                            {
                                let _ = event_loop.send_event(RuffleEvent::ExitFullScreen);
                                item_clicked = true;
                            }
                        }
                    })
                })
            });

        let should_close = item_clicked
            || area.response.clicked_elsewhere()
            || egui_ctx.input_mut(|input| input.consume_key(Modifiers::NONE, Key::Escape));

        !should_close
    }

    /// iOS Control Center Bento Platter panel.
    fn show_grimcodex(
        &mut self,
        egui_ctx: &egui::Context,
        event_loop: &EventLoopProxy<RuffleEvent>,
        settings: MenuSettings,
    ) -> bool {
        let mut item_clicked = false;
        if self.position.is_none() {
            egui_ctx.data_mut(|data| {
                data.remove::<bool>(Id::new(("ios_mem_limit_open", MENU_ID)));
                data.remove::<String>(Id::new(("ios_scale_input", format!("scale_{MENU_ID}"))));
            });
        }
        self.position = self.position.or(egui_ctx.pointer_latest_pos());
        let z = (egui_ctx.content_rect().height() / 1000.0).clamp(0.72, 1.0);
        let send = |event| {
            let _ = event_loop.send_event(event);
        };
        let mut changes = Vec::new();

        let area = Area::new(Id::new("context_menu"))
            .order(Order::Foreground)
            .fixed_pos(self.position.unwrap_or_default())
            .constrain_to(egui_ctx.content_rect())
            .interactable(true)
            .show(egui_ctx, |ui| {
                panel_style(ui.style_mut());
                Frame::new()
                    .fill(PANEL)
                    .stroke(Stroke::new(1.0, GLASS_RIM))
                    .corner_radius(CornerRadius::same((18.0 * z) as u8))
                    .inner_margin(Margin::same((14.0 * z) as i8))
                    .shadow(Shadow {
                        offset: [0, (12.0 * z) as i8],
                        blur: (32.0 * z) as u8,
                        spread: 0,
                        color: Color32::from_black_alpha(165),
                    })
                    .show(ui, |ui| {
                        ui.set_width(WIDTH * z);
                        ui.spacing_mut().item_spacing = vec2(0.0, 8.0 * z);

                        // Clean iOS sheet header
                        let (hdr, _) =
                            ui.allocate_exact_size(vec2(WIDTH * z, 20.0 * z), Sense::hover());
                        ui.painter().text(
                            pos2(hdr.left() + 4.0 * z, hdr.center().y),
                            Align2::LEFT_CENTER,
                            "GrimCodex",
                            FontId::proportional(14.5 * z),
                            TEXT,
                        );
                        ui.painter().text(
                            pos2(hdr.right() - 4.0 * z, hdr.center().y),
                            Align2::RIGHT_CENTER,
                            format!(
                                "{}  ·  {}",
                                crate::grimcodex::active_backend_label(),
                                crate::grimcodex::VERSION
                            ),
                            FontId::proportional(12.0 * z),
                            MUTED,
                        );

                        // Custom SWF context menu items (if any non-AQW movie adds them)
                        if !self.items.is_empty() {
                            let platter_r = CornerRadius::same((12.0 * z) as u8);
                            Frame::new()
                                .fill(PLATTER)
                                .corner_radius(platter_r)
                                .inner_margin(Margin::symmetric((8.0 * z) as i8, (4.0 * z) as i8))
                                .show(ui, |ui| {
                                    for (i, item) in self.items.iter().enumerate() {
                                        if !item.enabled {
                                            continue;
                                        }
                                        if action_row(
                                            ui,
                                            z,
                                            &item.caption,
                                            item.checked.then_some("On"),
                                        )
                                        .clicked()
                                        {
                                            send(RuffleEvent::ContextMenuItemClicked(i));
                                            item_clicked = true;
                                        }
                                    }
                                });
                        }

                        if settings_rows(ui, z, &settings, MENU_ID, &mut changes) {
                            item_clicked = true;
                        }
                    })
            });

        for setting in changes {
            if let Some(shown) = &mut self.settings {
                match setting {
                    Setting::Performance(level) => {
                        shown.performance = level;
                        shown.render_scale_percent = level.percent();
                    }
                    Setting::RenderScale(percent) => shown.render_scale_percent = percent,
                    Setting::Graphics(choice) => shown.graphics = choice,
                    Setting::Interpolation(on) => shown.interpolation = on,
                    Setting::Uncapped(on) => shown.uncapped = on,
                    Setting::FpsLimit(limit) => shown.fps_limit = limit,
                    Setting::MemoryLimit(mb) => shown.memory_limit_mb = mb,
                    Setting::ShowFps(on) => shown.show_fps = on,
                    Setting::Mute(on) => shown.muted = on,
                    _ => {}
                }
            }
            send(RuffleEvent::GrimCodexSetting(setting));
        }

        let should_close = item_clicked
            || area.response.clicked_elsewhere()
            || egui_ctx.input_mut(|input| input.consume_key(Modifiers::NONE, Key::Escape));

        !should_close
    }
}

// --- Apple iOS Dark Mode palette (shared with prompt.rs & grimcodex_menu.rs) -----------------

pub(crate) const WIDTH: f32 = 306.0;
/// Outer Control Center frosted obsidian sheet (`#18181B` @ 95% alpha).
pub(crate) const PANEL: Color32 = Color32::from_rgba_premultiplied(24, 24, 27, 242);
/// Subtle 9% white specular glass rim on outer sheet.
const GLASS_RIM: Color32 = Color32::from_rgba_premultiplied(255, 255, 255, 24);
/// Inactive iOS grouped platter (`#2C2C2E`).
const PLATTER: Color32 = Color32::from_rgb(44, 44, 46);
/// Hovered inactive platter (`#3A3A3C`).
pub(crate) const ROW_HOVER: Color32 = Color32::from_rgb(58, 58, 60);
/// Recessed segmented-control well & slider track (`#1C1C1E`).
pub(crate) const TRACK: Color32 = Color32::from_rgb(28, 28, 30);
/// Hairline inset separator inside platters (`#3D3D41`).
pub(crate) const EDGE: Color32 = Color32::from_rgb(61, 61, 65);
/// iOS active Control Center platter (`#F2F2F7` light ink surface).
const ACTIVE_PLATTER: Color32 = Color32::from_rgb(242, 242, 247);
const ACTIVE_PLATTER_HOVER: Color32 = Color32::from_rgb(255, 255, 255);
const ACTIVE_INK: Color32 = Color32::from_rgb(20, 20, 22);
const ACTIVE_SUBINK: Color32 = Color32::from_rgb(88, 88, 96);
/// iOS dark-mode `UISegmentedControl` elevated thumb (`#636366`).
const SEGMENT_THUMB: Color32 = Color32::from_rgb(99, 99, 102);
/// iOS System Blue (`#0A84FF`) used by prompts & focus rings.
pub(crate) const ACCENT: Color32 = Color32::from_rgb(10, 132, 255);
/// Primary label (`#F5F5F7`) & secondary label (`#98989F`, >=5.2:1 on `#1C1C1E`/`#2C2C2E`).
pub(crate) const TEXT: Color32 = Color32::from_rgb(245, 245, 247);
pub(crate) const MUTED: Color32 = Color32::from_rgb(152, 152, 159);

pub(crate) const MENU_ID: &str = "menu";

fn graphics_options() -> (&'static [&'static str], &'static [Option<Graphics>]) {
    #[cfg(target_os = "linux")]
    return (
        &["Auto", "Vulkan", "OpenGL"],
        &[None, Some(Graphics::Vulkan), Some(Graphics::Gl)],
    );
    #[cfg(target_os = "macos")]
    return (&["Auto", "Metal"], &[None, Some(Graphics::Metal)]);
    #[cfg(not(any(target_os = "linux", target_os = "macos")))]
    return (
        &["Auto", "Vulkan", "DX12"],
        &[None, Some(Graphics::Vulkan), Some(Graphics::Dx12)],
    );
}

/// Renders the iOS Control Center Bento layout.
pub(crate) fn settings_rows(
    ui: &mut Ui,
    z: f32,
    settings: &MenuSettings,
    id: &str,
    changes: &mut Vec<Setting>,
) -> bool {
    let close_menu = false;
    let gap = 8.0 * z;
    let platter_r = CornerRadius::same((12.0 * z) as u8);

    // --- Row 1: Two Paired iOS Toggle Platters (Smooth Motion & Mute Audio) ------------------
    let tile_w = (WIDTH * z - gap) * 0.5;
    let tile_h = 52.0 * z;
    let (row1, _) = ui.allocate_exact_size(vec2(WIDTH * z, tile_h), Sense::hover());

    let smooth_rect = Rect::from_min_size(row1.left_top(), vec2(tile_w, tile_h));
    if ios_toggle_platter(
        ui,
        z,
        Id::new(("ios_smooth", id)),
        smooth_rect,
        "Smooth Motion",
        if settings.interpolation { "On" } else { "Off" },
        settings.interpolation,
        TileGlyph::Motion,
    ) {
        changes.push(Setting::Interpolation(!settings.interpolation));
    }

    let mute_rect = Rect::from_min_size(
        pos2(row1.left() + tile_w + gap, row1.top()),
        vec2(tile_w, tile_h),
    );
    if ios_toggle_platter(
        ui,
        z,
        Id::new(("ios_mute", id)),
        mute_rect,
        "Mute Audio",
        if settings.muted { "Muted" } else { "Off" },
        settings.muted,
        TileGlyph::Speaker {
            muted: settings.muted,
        },
    ) {
        changes.push(Setting::Mute(!settings.muted));
    }

    // --- Row 2: Display & Pacing Platter (Inset Grouped Card with iOS Segmented Controls) ----
    Frame::new()
        .fill(PLATTER)
        .corner_radius(platter_r)
        .inner_margin(Margin::symmetric((10.0 * z) as i8, (8.0 * z) as i8))
        .show(ui, |ui| {
            let inner_w = WIDTH * z - 20.0 * z;
            ui.set_width(inner_w);
            ui.spacing_mut().item_spacing = vec2(0.0, 4.0 * z);

            if let Some(pct) = ios_scale_row(
                ui,
                z,
                inner_w,
                &format!("scale_{id}"),
                settings.render_scale_percent,
            ) {
                changes.push(Setting::RenderScale(pct));
            }

            if settings.graphics_choice {
                inset_hairline(ui, z, inner_w);
                let (gfx_opts, gfx_vals) = graphics_options();
                let gfx_idx = gfx_vals
                    .iter()
                    .position(|&v| v == settings.graphics)
                    .unwrap_or(0);
                if let Some(picked) = ios_segmented_row(
                    ui,
                    z,
                    inner_w,
                    &format!("gfx_{id}"),
                    "Graphics",
                    gfx_opts,
                    gfx_idx,
                ) {
                    changes.push(Setting::Graphics(gfx_vals[picked]));
                }
            }

            if settings.interpolation {
                inset_hairline(ui, z, inner_w);
                let pacing_idx = if settings.uncapped { 1 } else { 0 };
                if let Some(picked) = ios_segmented_row(
                    ui,
                    z,
                    inner_w,
                    &format!("pacing_{id}"),
                    "Pacing",
                    &["V-Sync", "Free"],
                    pacing_idx,
                ) {
                    changes.push(Setting::Uncapped(picked == 1));
                }

                if settings.uncapped {
                    inset_hairline(ui, z, inner_w);
                    if let Some((limit, _)) =
                        fps_limit_section(ui, z, inner_w, &format!("fps_limit_{id}"), settings.fps_limit)
                    {
                        changes.push(Setting::FpsLimit(limit));
                    }
                }
            }
        });

    // --- Row 3: Asymmetric Bento Bottom (Wide Memory Capsule + Stacked FPS / Clean Pills) ----
    let bento_h = 68.0 * z;
    let right_w = 96.0 * z;
    let left_w = WIDTH * z - right_w - gap;
    let (row3, _) = ui.allocate_exact_size(vec2(WIDTH * z, bento_h), Sense::hover());

    let mem_open_key = Id::new(("ios_mem_limit_open", id));
    let mut mem_limit_open = ui.data(|data| data.get_temp::<bool>(mem_open_key)).unwrap_or(false);

    let mem_rect = Rect::from_min_size(row3.left_top(), vec2(left_w, bento_h));
    if memory_bento_platter(
        ui,
        z,
        Id::new(("ios_mem_platter", id)),
        mem_rect,
        settings.memory_limit_mb,
        mem_limit_open,
    ) {
        mem_limit_open = !mem_limit_open;
        ui.data_mut(|data| data.insert_temp(mem_open_key, mem_limit_open));
    }

    let pill_h = (bento_h - gap) * 0.5;
    let fps_rect = Rect::from_min_size(
        pos2(row3.left() + left_w + gap, row3.top()),
        vec2(right_w, pill_h),
    );
    if ios_mini_pill(
        ui,
        z,
        Id::new(("ios_fps_pill", id)),
        fps_rect,
        "Show FPS",
        settings.show_fps,
    ) {
        changes.push(Setting::ShowFps(!settings.show_fps));
    }

    let clean_rect = Rect::from_min_size(
        pos2(row3.left() + left_w + gap, row3.top() + pill_h + gap),
        vec2(right_w, pill_h),
    );
    if ios_mini_pill(
        ui,
        z,
        Id::new(("ios_clean_pill", id)),
        clean_rect,
        "Clean RAM",
        false,
    ) {
        changes.push(Setting::FreeMemory);
    }

    // Expandable iOS Memory Limit picker platter when the "Limit: ... v" chip is opened
    if mem_limit_open {
        Frame::new()
            .fill(PLATTER)
            .corner_radius(platter_r)
            .inner_margin(Margin::symmetric((10.0 * z) as i8, (8.0 * z) as i8))
            .show(ui, |ui| {
                let inner_w = WIDTH * z - 20.0 * z;
                ui.set_width(inner_w);
                ui.spacing_mut().item_spacing = vec2(0.0, 4.0 * z);
                if let Some((next_mb, settled)) = memory_limit_picker(
                    ui,
                    z,
                    inner_w,
                    &format!("mem_limit_{id}"),
                    settings.memory_limit_mb,
                ) {
                    changes.push(Setting::MemoryLimit(next_mb));
                    if settled {
                        ui.data_mut(|data| data.insert_temp(mem_open_key, false));
                    }
                }
            });
    }

    close_menu
}

enum TileGlyph {
    Motion,
    Speaker { muted: bool },
}

#[allow(clippy::too_many_arguments)]
fn ios_toggle_platter(
    ui: &mut Ui,
    z: f32,
    id: Id,
    rect: Rect,
    title: &str,
    state_text: &str,
    active: bool,
    glyph: TileGlyph,
) -> bool {
    let resp = ui.interact(rect, id, Sense::click());
    if resp.hovered() {
        ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
    }
    let t = ui.ctx().animate_bool_responsive(id, active);
    let base_inactive = if resp.hovered() { ROW_HOVER } else { PLATTER };
    let base_active = if resp.hovered() {
        ACTIVE_PLATTER_HOVER
    } else {
        ACTIVE_PLATTER
    };
    let fill = lerp_color(base_inactive, base_active, t);
    let fg = lerp_color(TEXT, ACTIVE_INK, t);
    let sub_fg = lerp_color(MUTED, ACTIVE_SUBINK, t);
    let icon_bg = lerp_color(
        Color32::from_rgba_premultiplied(255, 255, 255, 20),
        Color32::from_rgba_premultiplied(20, 20, 22, 22),
        t,
    );

    let radius = CornerRadius::same((12.0 * z) as u8);
    let painter = ui.painter();
    painter.rect_filled(rect, radius, fill);

    // Circular icon well on the left (authentic Control Center tile)
    let icon_center = pos2(rect.left() + 24.0 * z, rect.center().y);
    painter.circle_filled(icon_center, 14.0 * z, icon_bg);
    match glyph {
        TileGlyph::Motion => draw_motion_glyph(painter, icon_center, z, fg),
        TileGlyph::Speaker { muted } => draw_speaker_glyph(painter, icon_center, z, fg, muted),
    }

    let text_x = rect.left() + 46.0 * z;
    painter.text(
        pos2(text_x, rect.center().y - 7.5 * z),
        Align2::LEFT_CENTER,
        title,
        FontId::proportional(12.5 * z),
        fg,
    );
    painter.text(
        pos2(text_x, rect.center().y + 8.0 * z),
        Align2::LEFT_CENTER,
        state_text,
        FontId::proportional(11.0 * z),
        sub_fg,
    );

    resp.clicked()
}

/// Drawn 1.5px vector motion glyph (overlapping smooth frames, zero emojis).
fn draw_motion_glyph(painter: &Painter, center: Pos2, z: f32, color: Color32) {
    let stroke = Stroke::new(1.5 * z, color);
    let r = CornerRadius::same((2.0 * z) as u8);
    let back = Rect::from_center_size(center + vec2(-2.5 * z, 1.5 * z), vec2(8.0 * z, 8.0 * z));
    let front = Rect::from_center_size(center + vec2(2.0 * z, -1.5 * z), vec2(8.5 * z, 8.5 * z));
    painter.rect_stroke(back, r, stroke, StrokeKind::Inside);
    painter.rect_filled(front, r, color);
}

/// Drawn 1.5px vector speaker / muted-speaker glyph (zero emojis).
fn draw_speaker_glyph(painter: &Painter, center: Pos2, z: f32, color: Color32, muted: bool) {
    let stroke = Stroke::new(1.5 * z, color);
    let body = Rect::from_min_max(
        center + vec2(-6.0 * z, -2.5 * z),
        center + vec2(-3.0 * z, 2.5 * z),
    );
    painter.rect_filled(body, CornerRadius::same(1), color);
    painter.line_segment(
        [center + vec2(-3.0 * z, -2.5 * z), center + vec2(1.0 * z, -5.5 * z)],
        stroke,
    );
    painter.line_segment(
        [center + vec2(1.0 * z, -5.5 * z), center + vec2(1.0 * z, 5.5 * z)],
        stroke,
    );
    painter.line_segment(
        [center + vec2(1.0 * z, 5.5 * z), center + vec2(-3.0 * z, 2.5 * z)],
        stroke,
    );

    if muted {
        painter.line_segment(
            [center + vec2(3.5 * z, -3.0 * z), center + vec2(7.5 * z, 3.0 * z)],
            stroke,
        );
        painter.line_segment(
            [center + vec2(7.5 * z, -3.0 * z), center + vec2(3.5 * z, 3.0 * z)],
            stroke,
        );
    } else {
        painter.line_segment(
            [center + vec2(4.0 * z, -2.5 * z), center + vec2(5.2 * z, 0.0)],
            stroke,
        );
        painter.line_segment(
            [center + vec2(5.2 * z, 0.0), center + vec2(4.0 * z, 2.5 * z)],
            stroke,
        );
    }
}

fn inset_hairline(ui: &mut Ui, z: f32, width: f32) {
    let (rect, _) = ui.allocate_exact_size(vec2(width, 5.0 * z), Sense::hover());
    ui.painter().hline(
        (rect.left() + 4.0 * z)..=rect.right(),
        rect.center().y,
        Stroke::new(1.0, EDGE),
    );
}

fn ios_scale_row(
    ui: &mut Ui,
    z: f32,
    width: f32,
    id: &str,
    current_percent: u8,
) -> Option<u8> {
    let (row, _) = ui.allocate_exact_size(vec2(width, 28.0 * z), Sense::hover());
    ui.painter().text(
        pos2(row.left() + 2.0 * z, row.center().y),
        Align2::LEFT_CENTER,
        "Scale",
        FontId::proportional(13.0 * z),
        TEXT,
    );

    let ctrl_w = 178.0 * z;
    let rect = Rect::from_min_max(
        pos2(row.right() - ctrl_w, row.top() + 1.5 * z),
        pos2(row.right(), row.bottom() - 1.5 * z),
    );
    let radius = CornerRadius::same((7.0 * z) as u8);
    let thumb_r = CornerRadius::same((5.5 * z) as u8);
    ui.painter().rect_filled(rect, radius, TRACK);

    let step_w = 42.0 * z;
    let minus_rect = Rect::from_min_size(rect.left_top(), vec2(step_w, rect.height()));
    let plus_rect = Rect::from_min_size(
        pos2(rect.right() - step_w, rect.top()),
        vec2(step_w, rect.height()),
    );
    let center_rect = Rect::from_min_max(
        pos2(minus_rect.right(), rect.top()),
        pos2(plus_rect.left(), rect.bottom()),
    );

    let can_dec = current_percent > SCALE_MIN_PERCENT;
    let can_inc = current_percent < SCALE_MAX_PERCENT;

    let minus_resp = ui.interact(minus_rect, Id::new((id, "minus")), Sense::click());
    let plus_resp = ui.interact(plus_rect, Id::new((id, "plus")), Sense::click());

    if (minus_resp.hovered() && can_dec) || (plus_resp.hovered() && can_inc) {
        ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
    }

    // Hairline separators for − | 95% | +
    let div_y = (rect.top() + 5.0 * z)..=(rect.bottom() - 5.0 * z);
    ui.painter()
        .vline(minus_rect.right(), div_y.clone(), Stroke::new(1.0, EDGE));
    ui.painter()
        .vline(plus_rect.left(), div_y, Stroke::new(1.0, EDGE));

    if minus_resp.hovered() && can_dec {
        ui.painter().rect_filled(
            minus_rect.shrink(2.0 * z),
            thumb_r,
            Color32::from_rgba_premultiplied(255, 255, 255, 16),
        );
    }
    if plus_resp.hovered() && can_inc {
        ui.painter().rect_filled(
            plus_rect.shrink(2.0 * z),
            thumb_r,
            Color32::from_rgba_premultiplied(255, 255, 255, 16),
        );
    }

    // Vector − and + glyphs
    let minus_col = if can_dec {
        TEXT
    } else {
        Color32::from_rgb(82, 82, 88)
    };
    let plus_col = if can_inc {
        TEXT
    } else {
        Color32::from_rgb(82, 82, 88)
    };
    let glyph_half = 4.0 * z;
    ui.painter().line_segment(
        [
            minus_rect.center() + vec2(-glyph_half, 0.0),
            minus_rect.center() + vec2(glyph_half, 0.0),
        ],
        Stroke::new(1.5 * z, minus_col),
    );
    ui.painter().line_segment(
        [
            plus_rect.center() + vec2(-glyph_half, 0.0),
            plus_rect.center() + vec2(glyph_half, 0.0),
        ],
        Stroke::new(1.5 * z, plus_col),
    );
    ui.painter().line_segment(
        [
            plus_rect.center() + vec2(0.0, -glyph_half),
            plus_rect.center() + vec2(0.0, glyph_half),
        ],
        Stroke::new(1.5 * z, plus_col),
    );

    // Center editable percentage inside the segmented pill
    let key = Id::new(("ios_scale_input", id.to_owned()));
    let mut text = ui
        .data(|data| data.get_temp::<String>(key))
        .unwrap_or_else(|| current_percent.to_string());

    let edit_h = 16.0 * z;
    let edit_top = center_rect.center().y - edit_h * 0.5 + 0.5 * z;
    let edit_rect = Rect::from_min_size(
        pos2(center_rect.left() + 14.0 * z, edit_top),
        vec2(center_rect.width() - 36.0 * z, edit_h),
    );
    let response = ui.put(
        edit_rect,
        egui::TextEdit::singleline(&mut text)
            .frame(Frame::NONE)
            .char_limit(3)
            .font(FontId::proportional(12.0 * z))
            .horizontal_align(Align::Center),
    );

    if response.has_focus() {
        ui.painter().rect_stroke(
            center_rect.shrink(1.5 * z),
            thumb_r,
            Stroke::new(1.0, ACCENT),
            StrokeKind::Inside,
        );
    }

    ui.painter().text(
        pos2(center_rect.right() - 15.0 * z, center_rect.center().y),
        Align2::RIGHT_CENTER,
        "%",
        FontId::proportional(12.0 * z),
        MUTED,
    );

    text.retain(|c| c.is_ascii_digit());

    let mut applied = None;
    if minus_resp.clicked() && can_dec {
        let next = current_percent.saturating_sub(1).max(SCALE_MIN_PERCENT);
        applied = Some(next);
        ui.data_mut(|data| data.insert_temp(key, next.to_string()));
    } else if plus_resp.clicked() && can_inc {
        let next = current_percent.saturating_add(1).min(SCALE_MAX_PERCENT);
        applied = Some(next);
        ui.data_mut(|data| data.insert_temp(key, next.to_string()));
    } else {
        let enter_pressed = response.lost_focus() && ui.input(|i| i.key_pressed(Key::Enter));
        let escaped = ui.input(|i| i.key_pressed(Key::Escape));
        if enter_pressed || (response.lost_focus() && !escaped) {
            if let Some(clamped) = text
                .parse::<u16>()
                .ok()
                .map(|v| (v.min(255) as u8).clamp(SCALE_MIN_PERCENT, SCALE_MAX_PERCENT))
            {
                if clamped != current_percent {
                    applied = Some(clamped);
                }
                ui.data_mut(|data| data.insert_temp(key, clamped.to_string()));
            } else {
                ui.data_mut(|data| data.remove::<String>(key));
            }
        } else if response.has_focus() || response.changed() {
            ui.data_mut(|data| data.insert_temp(key, text));
        } else {
            ui.data_mut(|data| data.remove::<String>(key));
        }
    }

    applied
}

fn ios_segmented_row(
    ui: &mut Ui,
    z: f32,
    width: f32,
    id: &str,
    label: &str,
    options: &[&str],
    selected: usize,
) -> Option<usize> {
    let (row, _) = ui.allocate_exact_size(vec2(width, 28.0 * z), Sense::hover());
    ui.painter().text(
        pos2(row.left() + 2.0 * z, row.center().y),
        Align2::LEFT_CENTER,
        label,
        FontId::proportional(13.0 * z),
        TEXT,
    );

    let seg_w = 178.0 * z;
    let seg_rect = Rect::from_min_max(
        pos2(row.right() - seg_w, row.top() + 1.5 * z),
        pos2(row.right(), row.bottom() - 1.5 * z),
    );
    ios_segmented_control(ui, z, id, seg_rect, options, selected)
}

fn ios_segmented_control(
    ui: &mut Ui,
    z: f32,
    id: &str,
    rect: Rect,
    options: &[&str],
    selected: usize,
) -> Option<usize> {
    let radius = CornerRadius::same((7.0 * z) as u8);
    ui.painter().rect_filled(rect, radius, TRACK);

    let n = options.len().max(1) as f32;
    let cell_w = rect.width() / n;
    let mut chosen = None;

    for (i, option) in options.iter().enumerate() {
        let left = rect.left() + i as f32 * cell_w;
        let cell = Rect::from_min_size(pos2(left, rect.top()), vec2(cell_w, rect.height()));
        let resp = ui.interact(cell, Id::new((id, i)), Sense::click());
        if resp.hovered() && i != selected {
            ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
        }

        // Subtle UISegmentedControl divider between adjacent unselected segments
        if i > 0 && i != selected && i != selected + 1 {
            ui.painter().vline(
                cell.left(),
                (cell.top() + 5.0 * z)..=(cell.bottom() - 5.0 * z),
                Stroke::new(1.0, EDGE),
            );
        }

        let active = ui.ctx().animate_bool_responsive(resp.id, i == selected);
        let thumb = cell.shrink(2.0 * z);
        let thumb_r = CornerRadius::same((5.5 * z) as u8);
        if active > 0.01 {
            ui.painter()
                .rect_filled(thumb, thumb_r, SEGMENT_THUMB.gamma_multiply(active));
        } else if resp.hovered() {
            ui.painter()
                .rect_filled(thumb, thumb_r, Color32::from_rgba_premultiplied(255, 255, 255, 12));
        }

        ui.painter().text(
            cell.center(),
            Align2::CENTER_CENTER,
            *option,
            FontId::proportional(12.0 * z),
            if i == selected { TEXT } else { MUTED },
        );

        if resp.clicked() && i != selected {
            chosen = Some(i);
        }
    }
    chosen
}

/// Draws the wide Memory Bento Platter with a clickable `Limit: ... v` dropdown chip;
/// returns `true` when the chip was clicked to toggle the limit picker platter.
fn memory_bento_platter(
    ui: &mut Ui,
    z: f32,
    id: Id,
    rect: Rect,
    limit_mb: u32,
    open: bool,
) -> bool {
    let radius = CornerRadius::same((12.0 * z) as u8);
    ui.painter().rect_filled(rect, radius, PLATTER);

    let now_mb = crate::grimcodex::memory_now_mb();
    let pad = 10.0 * z;

    let limit_text = match limit_mb {
        0 => "Limit: Off".to_owned(),
        1024 => "Limit: 1 GB".to_owned(),
        2048 => "Limit: 2 GB".to_owned(),
        3072 => "Limit: 3 GB".to_owned(),
        mb => format!("{mb} MB"),
    };
    let chip_w = 90.0 * z;
    let chip_rect = Rect::from_min_size(
        pos2(rect.right() - pad - chip_w, rect.top() + 8.0 * z),
        vec2(chip_w, 21.0 * z),
    );
    let chip_resp = ui.interact(chip_rect, id.with("limit_chip"), Sense::click());
    if chip_resp.hovered() {
        ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
    }
    let chip_fill = if open || chip_resp.hovered() {
        SEGMENT_THUMB
    } else {
        TRACK
    };
    let chip_fg = if open || limit_mb > 0 || chip_resp.hovered() {
        TEXT
    } else {
        MUTED
    };
    ui.painter().rect_filled(
        chip_rect,
        CornerRadius::same((6.0 * z) as u8),
        chip_fill,
    );
    ui.painter().text(
        pos2(chip_rect.left() + 7.5 * z, chip_rect.center().y),
        Align2::LEFT_CENTER,
        limit_text,
        FontId::proportional(11.0 * z),
        chip_fg,
    );

    // Crisp vector downward chevron (flips upward while open)
    let chev_center = pos2(chip_rect.right() - 10.0 * z, chip_rect.center().y + 0.5 * z);
    let (half, tip) = (3.5 * z, if open { -1.8 * z } else { 1.8 * z });
    let chev_stroke = Stroke::new(1.4 * z, chip_fg);
    ui.painter().line_segment(
        [chev_center + vec2(-half, -tip), chev_center + vec2(0.0, tip)],
        chev_stroke,
    );
    ui.painter().line_segment(
        [chev_center + vec2(0.0, tip), chev_center + vec2(half, -tip)],
        chev_stroke,
    );

    ui.painter().text(
        pos2(rect.left() + pad, chip_rect.center().y),
        Align2::LEFT_CENTER,
        format!("RAM {now_mb} MB"),
        FontId::proportional(12.5 * z),
        TEXT,
    );

    // iOS capsule brightness/volume-style smooth bar along the bottom of the platter
    let bar_rect = Rect::from_min_max(
        pos2(rect.left() + pad, rect.bottom() - 22.0 * z),
        pos2(rect.right() - pad, rect.bottom() - 12.0 * z),
    );
    let bar_r = CornerRadius::same((5.0 * z) as u8);
    ui.painter().rect_filled(bar_rect, bar_r, TRACK);

    let denom = if limit_mb > 0 { limit_mb as f32 } else { 2048.0 };
    let frac = (now_mb as f32 / denom).clamp(0.04, 1.0);
    let fill_rect = Rect::from_min_size(
        bar_rect.left_top(),
        vec2(bar_rect.width() * frac, bar_rect.height()),
    );
    let bar_color = if limit_mb > 0 && frac >= 0.88 {
        Color32::from_rgb(255, 159, 10)
    } else {
        ACTIVE_PLATTER
    };
    ui.painter().rect_filled(fill_rect, bar_r, bar_color);

    chip_resp.clicked()
}

fn memory_limit_picker(
    ui: &mut Ui,
    z: f32,
    width: f32,
    id: &str,
    current_mb: u32,
) -> Option<(u32, bool)> {
    const OPTIONS: [&str; 5] = ["Off", "1 GB", "2 GB", "3 GB", "..."];
    const FIRST_CUSTOM_MB: u32 = 1500;
    let selected = match current_mb {
        0 => 0,
        1024 => 1,
        2048 => 2,
        3072 => 3,
        _ => 4,
    };
    let (seg_rect, _) = ui.allocate_exact_size(vec2(width, 26.0 * z), Sense::hover());
    let mut picked = match ios_segmented_control(ui, z, id, seg_rect, &OPTIONS, selected) {
        Some(0) => Some((0, true)),
        Some(1) => Some((1024, true)),
        Some(2) => Some((2048, true)),
        Some(3) => Some((3072, true)),
        Some(_) => {
            focus_number_row(ui, id);
            Some((FIRST_CUSTOM_MB, false))
        }
        None => None,
    };
    if selected == 4
        && let Some(typed) =
            number_row(ui, z, width, id, "Custom (MB)", "Enter", current_mb, 512..=65536)
    {
        picked = Some((typed, true));
    }
    picked
}

fn ios_mini_pill(ui: &mut Ui, z: f32, id: Id, rect: Rect, label: &str, active: bool) -> bool {
    let resp = ui.interact(rect, id, Sense::click());
    if resp.hovered() {
        ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
    }
    let t = ui.ctx().animate_bool_responsive(id, active);
    let inactive_fill = if resp.hovered() { ROW_HOVER } else { PLATTER };
    let active_fill = if resp.hovered() {
        ACTIVE_PLATTER_HOVER
    } else {
        ACTIVE_PLATTER
    };
    let fill = lerp_color(inactive_fill, active_fill, t);
    let fg = lerp_color(TEXT, ACTIVE_INK, t);

    ui.painter()
        .rect_filled(rect, CornerRadius::same((10.0 * z) as u8), fill);
    ui.painter().text(
        rect.center(),
        Align2::CENTER_CENTER,
        label,
        FontId::proportional(12.0 * z),
        fg,
    );

    resp.clicked()
}

fn fps_limit_section(
    ui: &mut Ui,
    z: f32,
    width: f32,
    id: &str,
    current: FpsLimit,
) -> Option<(FpsLimit, bool)> {
    const OPTIONS: [&str; 5] = ["Off", "Scr", "120", "60", "..."];
    const FIRST_CUSTOM: u32 = 144;
    let selected = match current {
        FpsLimit::Off => 0,
        FpsLimit::Screen => 1,
        FpsLimit::Fixed(120) => 2,
        FpsLimit::Fixed(60) => 3,
        FpsLimit::Fixed(_) => 4,
    };
    static TEST_FOCUS: std::sync::Once = std::sync::Once::new();
    TEST_FOCUS.call_once(|| {
        if std::env::var_os("RUFFLE_TEST_MENU_FOCUS").is_some() {
            focus_number_row(ui, id);
        }
    });
    let mut picked = match ios_segmented_row(ui, z, width, id, "FPS Cap", &OPTIONS, selected) {
        Some(0) => Some((FpsLimit::Off, true)),
        Some(1) => Some((FpsLimit::Screen, true)),
        Some(2) => Some((FpsLimit::Fixed(120), true)),
        Some(3) => Some((FpsLimit::Fixed(60), true)),
        Some(_) => {
            focus_number_row(ui, id);
            Some((FpsLimit::Fixed(FIRST_CUSTOM), false))
        }
        None => None,
    };
    if let (4, FpsLimit::Fixed(fps)) = (selected, current)
        && let Some(typed) = number_row(ui, z, width, id, "Custom FPS", "Enter", fps, 45..=1000)
    {
        picked = Some((FpsLimit::Fixed(typed), true));
    }
    picked
}

fn focus_number_row(ui: &Ui, id: &str) {
    ui.data_mut(|data| data.insert_temp(Id::new(("grimcodex_number_focus", id)), true));
}

#[allow(clippy::too_many_arguments)]
fn number_row(
    ui: &mut Ui,
    z: f32,
    width: f32,
    id: &str,
    title: &str,
    hint: &str,
    current: u32,
    range: std::ops::RangeInclusive<u32>,
) -> Option<u32> {
    let (rect, _) = ui.allocate_exact_size(vec2(width, 28.0 * z), Sense::hover());
    ui.painter().text(
        pos2(rect.left() + 2.0 * z, rect.center().y),
        Align2::LEFT_CENTER,
        title,
        FontId::proportional(12.5 * z),
        TEXT,
    );
    let field = Rect::from_min_size(
        pos2(rect.right() - 68.0 * z, rect.top() + 2.0 * z),
        vec2(68.0 * z, 24.0 * z),
    );
    ui.painter()
        .rect_filled(field, CornerRadius::same((6.0 * z) as u8), TRACK);
    ui.painter().text(
        pos2(field.left() - 8.0 * z, rect.center().y),
        Align2::RIGHT_CENTER,
        hint,
        FontId::proportional(11.0 * z),
        MUTED,
    );
    let key = Id::new(("grimcodex_number", id));
    let mut text = ui
        .data(|data| data.get_temp::<String>(key))
        .unwrap_or_else(|| current.to_string());
    let response = ui.put(
        field,
        egui::TextEdit::singleline(&mut text)
            .frame(Frame::NONE)
            .char_limit(5)
            .font(FontId::proportional(12.5 * z))
            .horizontal_align(Align::Center),
    );
    let focus_key = Id::new(("grimcodex_number_focus", id));
    if ui.data(|data| data.get_temp::<bool>(focus_key)).is_some() {
        ui.data_mut(|data| {
            data.remove::<bool>(focus_key);
            data.insert_temp(key, String::new());
        });
        response.request_focus();
        return None;
    }
    text.retain(|c| c.is_ascii_digit());
    let entered = response.lost_focus() && ui.input(|input| input.key_pressed(Key::Enter));
    let typed = text.parse::<u32>().ok().filter(|number| range.contains(number));
    if response.has_focus() {
        ui.data_mut(|data| data.insert_temp(key, text));
    } else {
        ui.data_mut(|data| data.remove::<String>(key));
    }
    if entered { typed } else { None }
}

pub(crate) fn panel_style(style: &mut Style) {
    style.visuals.override_text_color = Some(TEXT);
    style.visuals.selection.bg_fill = ACCENT.gamma_multiply(0.35);
    style.visuals.selection.stroke = Stroke::new(1.0, ACCENT);
    style.visuals.text_cursor.stroke = Stroke::new(1.5, ACCENT);
    style.spacing.button_padding = vec2(0.0, 0.0);
    style.interaction.selectable_labels = false;
}

pub(crate) fn action_row(ui: &mut Ui, z: f32, title: &str, right: Option<&str>) -> Response {
    let w = ui.available_width();
    let (rect, response) = ui.allocate_exact_size(vec2(w, 28.0 * z), Sense::click());
    if response.hovered() {
        ui.painter()
            .rect_filled(rect, CornerRadius::same((6.0 * z) as u8), ROW_HOVER);
        ui.ctx().set_cursor_icon(egui::CursorIcon::PointingHand);
    }
    ui.painter().text(
        pos2(rect.left() + 8.0 * z, rect.center().y),
        Align2::LEFT_CENTER,
        title,
        FontId::proportional(13.0 * z),
        TEXT,
    );
    if let Some(right) = right {
        ui.painter().text(
            pos2(rect.right() - 8.0 * z, rect.center().y),
            Align2::RIGHT_CENTER,
            right,
            FontId::proportional(12.0 * z),
            MUTED,
        );
    }
    response
}

fn lerp_color(a: Color32, b: Color32, t: f32) -> Color32 {
    let mix = |x: u8, y: u8| (x as f32 + (y as f32 - x as f32) * t).round() as u8;
    Color32::from_rgba_premultiplied(
        mix(a.r(), b.r()),
        mix(a.g(), b.g()),
        mix(a.b(), b.b()),
        mix(a.a(), b.a()),
    )
}

fn set_menu_style(style: &mut Style) {
    style.spacing.button_padding = vec2(2.0, 0.0);
    style.visuals.widgets.active.bg_stroke = Stroke::NONE;
    style.visuals.widgets.hovered.bg_stroke = Stroke::NONE;
    style.visuals.widgets.inactive.weak_bg_fill = Color32::TRANSPARENT;
    style.visuals.widgets.inactive.bg_stroke = Stroke::NONE;
}
