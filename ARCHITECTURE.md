# Arsitektur: GrimCodex Ruffle

Dokumen teknis arsitektur **GrimCodex Ruffle** — aplikasi tunggal yang menggabungkan UI WinForms **Grimlite Li x CODEX** (`grim/`) dengan engine **Ruffle** khusus AQW (`engine/`) tanpa Adobe Flash ActiveX.

---

## 1. Gambaran Besar

```text
+-------------------------------------------------------------------+
| GrimCodex Ruffle.exe (C# WinForms, .NET 4.7.2, x86)               |
|   Bot, Maid, Codex, Packet Sniffer/Spammer, Plugins, CharSelect   |
|   +-----------------------------------------------------------+   |
|   | Root.gameContainer -> ruffleHost (Panel, HWND)            |   |
|   |   +---------------------------------------------------+   |   |
|   |   | runtime\grimcodex-engine.exe (Rust, x64, DX12/Vulkan) |   |   |
|   |   |  - WS_CHILD borderless embedded via HWND          |   |   |
|   |   |  - Load Loader\grimoire.swf (--base exeDir)       |   |   |
|   |   |  - Spoof URL: https://game.aq.com/game/           |   |   |
|   |   +------------------------^--------------------------+   |   |
|   +----------------------------|------------------------------+   |
|                                |                                  |
|            2-Channel Windows Named Pipe (JSON Lines)              |
|        \\.\pipe\grim-ruffle-<pid>-<id>-c2r  (C# -> Rust)          |
|        \\.\pipe\grim-ruffle-<pid>-<id>-r2c  (Rust -> C#)          |
|                                |                                  |
|            IFlashBridge -> RuffleFlashBridge                      |
+-------------------------------------------------------------------+
```

### Keunggulan Arsitektur Out-of-Process (Embedded Child HWND)
1. **Bitness aman (x86 ↔ x64):** UI Grim & ekosistem .NET berjalan di x86, sedangkan engine Ruffle DX12/Vulkan berjalan di x64.
2. **Isolasi crash & Job Object:** Dilengkapi Win32 Job Object (`JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`) sehingga `runtime\grimcodex-engine.exe` otomatis mati saat `GrimCodex Ruffle.exe` ditutup.
3. **HiDPI & Swapchain-safe Resize:** Resize diproses asinkron melalui IPC → `render_thread::settle` + `gui.settle_present()` → `SetWindowPos` berdasarkan physical `GetClientRect` parent, menghindari crash swapchain DX12 saat Maximize/Restore.
4. **Cross-Process Focus:** Klik mouse pada area game otomatis memanggil `focus_embedded()` (`SetForegroundWindow` + `SetFocus`) agar input keyboard & chat AQW langsung aktif.

---

## 2. Protokol IPC (JSON Lines UTF-8)

Dua kanal searah (`-c2r` dan `-r2c`) mencegah blocking sinkron pada handle Windows Named Pipe:

1. **Engine Ready (Rust → C#):**
   ```json
   {"type": "ready", "hwnd": 123456}
   ```
2. **Pemanggilan Callback SWF (C# → Rust → C#):**
   ```json
   {"id": 101, "type": "call", "name": "Health", "args": []}
   {"id": 101, "type": "result", "ok": true, "value": "3450"}
   ```
3. **ExternalInterface dari SWF (Rust → C#):**
   ```json
   {"id": 202, "type": "ext_call", "name": "packetFromServer", "args": [["%xt%..."]], "needs_return": false}
   ```
   Untuk `modifyServers` (`needs_return: true`), C# mengembalikan:
   ```json
   {"id": 202, "type": "ext_result", "value": "..."}
   ```
4. **Kontrol Engine (C# → Rust):**
   ```json
   {"type": "resize", "width": 962, "height": 552}
   {"type": "set_quality", "quality": "high"}
   {"type": "shutdown"}
   ```
