# GrimCodex Ruffle

**GrimCodex Ruffle** adalah aplikasi desktop client yang berjalan langsung di atas **Ruffle Engine** (Rust x64, DirectX 12 / Vulkan) tanpa membutuhkan instalasi Adobe Flash Player maupun komponen ActiveX lama.

---

## Cara Kerja GrimCodex Ruffle

Aplikasi berjalan sebagai satu paket terpadu dengan menggabungkan antarmuka desktop dan engine grafis modern:

1. **GrimCodex Ruffle (`GrimCodex Ruffle.exe` — .NET Framework 4.7.2)**
   Menangani seluruh antarmuka utama, manajemen sesi, jaringan lokal, koleksi skrip **CodexBank**, plugin, dan pembaruan otomatis.
2. **GrimCodex Ruffle Engine (`runtime\grimcodex-engine.exe` — Rust x64)**
   Menjalankan konten SWF secara native menggunakan akselerasi GPU **DirectX 12 / Vulkan** (dengan optimasi performa PGO, frame pacing, dan manajemen memori).
3. **Integrasi Langsung (HWND Embedding & Named Pipe IPC)**
   Jendela engine ditanam langsung ke dalam panel aplikasi utama (`WS_CHILD`) dan berkomunikasi dua arah secara real-time melalui **2-Channel Windows Named Pipe**, serta dilindungi Win32 Job Object agar proses engine selalu tertutup bersih bersama aplikasi utama.

*(Detail teknis selengkapnya tersedia di [`ARCHITECTURE.md`](ARCHITECTURE.md).)*

---

## Instalasi & Pembaruan

### Pengguna Baru
1. Unduh **`GrimCodex-Ruffle-vX.Y.Z-win64.zip`** dari halaman [**Releases**](https://github.com/zsxbb/grimcodex-ruffle/releases/latest).
2. Ekstrak folder **`GrimCodex Ruffle`** ke lokasi pilihan Anda.
3. Jalankan **`GrimCodex Ruffle.exe`**.

### Pengguna Lama (Auto-Update)
Tidak perlu mengunduh ulang file `.zip`: cukup buka menu **About → Update App** di dalam aplikasi untuk mengunduh pembaruan terbaru secara otomatis tanpa mengubah konfigurasi lokal maupun koleksi skrip Anda.

> **Persyaratan Sistem:** Windows 10 / 11 (64-bit), .NET Framework 4.7.2+, dan GPU dengan dukungan DirectX 12 / Vulkan / OpenGL.

---

## Lisensi

Komponen engine berbasis [Ruffle](https://ruffle.rs/) dilisensikan di bawah **GNU General Public License v3.0 (GPL-3.0)** — lihat [`engine/LICENSE`](engine/LICENSE).
