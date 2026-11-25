# GrimCodex Ruffle

**GrimCodex Ruffle** adalah aplikasi AQW client desktop (.NET Framework 4.7.2) yang berjalan langsung di atas **Ruffle engine** khusus AQW (Rust x64, DirectX 12 / Vulkan) tanpa membutuhkan Adobe Flash Player / ActiveX.

## Struktur Folder

- `VERSION` — Sumber versi tunggal (saat ini `1.0.0`). Otomatis disinkronkan oleh `build.ps1` ke C#, Rust engine, metadata PE, dan `manifest.json`.
- `grim/` — Source codebase aplikasi **GrimCodex Ruffle** (C# WinForms .NET 4.7.2, `Loader/grimoire.swf`, `RuffleFlashBridge`).
- `engine/` — Source **GrimCodex Ruffle Engine** berbasis Ruffle (Rust x64, DX12/Vulkan, Named Pipe `ExternalInterface` bridge, Win32 `WS_CHILD` HWND embedding, PGO profile).
- `dist/` — Output aplikasi siap pakai (1 folder tunggal, di-ignore Git):
  - `GrimCodex Ruffle.exe`
  - `runtime\grimcodex-engine.exe`
  - `Loader\grimoire.swf` & `Loader\bg.jpg`
  - `ClientConfig.cfg`, `config.cfg`, `Resources\`
- `build.ps1` — Skrip build terpadu (auto-sync `VERSION` + compile engine Rust + aplikasi C# → `dist\`).
- `ARCHITECTURE.md` — Dokumentasi arsitektur IPC & HWND embedding.

## Cara Build

```powershell
# Build penuh memakai versi di file VERSION (engine + aplikasi -> dist\)
powershell -ExecutionPolicy Bypass -File build.ps1

# Build & otomatis sinkronkan ke versi baru (cocok untuk rilis / CI Action)
powershell -ExecutionPolicy Bypass -File build.ps1 -Version 1.0.1

# Rebuild cepat sisi C# saja (tanpa recompile Rust)
powershell -ExecutionPolicy Bypass -File build.ps1 -SkipEngine

# Build final release dengan PGO profile AQW
powershell -ExecutionPolicy Bypass -File build.ps1 -Profile dist -UsePgo
```

## Cara Menjalankan

Cukup jalankan langsung (tanpa argumen/flag):

```powershell
.\dist\"GrimCodex Ruffle.exe"
```

## Lisensi

Komponen engine berbasis Ruffle berlisensi **GPL-3.0** (`engine/LICENSE`).
