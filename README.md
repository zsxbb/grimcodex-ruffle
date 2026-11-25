# GrimCodex Ruffle

**GrimCodex Ruffle** adalah aplikasi desktop client berbasis **.NET Framework 4.7.2** yang terintegrasi langsung dengan **GrimCodex Ruffle Engine** (Rust x64, DirectX 12 / Vulkan) — berjalan mandiri di Windows 64-bit tanpa memerlukan instalasi Adobe Flash Player atau komponen ActiveX lama.

---

## Cara Pakai (Untuk Pengguna)

### 1. Instalasi Baru
1. Buka halaman [**Releases**](https://github.com/zsxbb/grimcodex-ruffle/releases/latest).
2. Unduh file **`GrimCodex-Ruffle-vX.Y.Z-win64.zip`**.
3. Ekstrak folder **`GrimCodex Ruffle`** ke lokasi yang diinginkan (hindari `C:\Program Files` agar aplikasi leluasa menyimpan konfigurasi lokal).
4. Jalankan **`GrimCodex Ruffle.exe`**.

> **Persyaratan Sistem:** Windows 10 / 11 (64-bit), .NET Framework 4.7.2+ (sudah bawaan Windows 10/11), serta GPU dengan dukungan DirectX 12 / Vulkan / OpenGL.

### 2. Cara Update Versi Berikutnya
Pengguna yang sudah memasang aplikasi **tidak perlu mengunduh ulang file `.zip`**:
- Klik menu **About → Update App** di dalam aplikasi.
- Aplikasi otomatis mengunduh pembaruan aplikasi (dan ikut memperbarui `runtime\grimcodex-engine.exe` apabila engine berubah), lalu restart sendiri tanpa menghapus konfigurasi lokal (`ClientConfig.cfg`, `config.cfg`, `Bots\`, `Plugins\`).

---

## Struktur Proyek

```text
.
├── VERSION                        # Sumber versi tunggal (otomatis disinkronkan oleh build.ps1)
├── build.ps1                      # Skrip build, sinkronisasi versi, dan pemaket rilis
├── ARCHITECTURE.md                # Dokumentasi teknis HWND embedding & Named Pipe IPC
├── .github/workflows/release.yml  # GitHub Actions: otomatis build PGO + publish Release saat push tag v*
├── grim/                          # Source aplikasi utama (C# WinForms .NET 4.7.2, x86)
│   ├── Grimoire/                  # UI, networking, bridge IPC, dan fitur client
│   ├── CodexBank/                 # Koleksi skrip bawaan + manifest.json (update & sync)
│   ├── Loader/                    # grimoire.swf & bg.jpg
│   └── Resources/                 # Aset ikon & gambar UI
└── engine/                        # Source GrimCodex Ruffle Engine (Rust, x64)
    ├── RUST_VERSION               # Versi toolchain Rust yang dikunci
    ├── pgo/aqw.profdata.zip       # Profil PGO untuk build performa maksimal
    └── ruffle/                    # Source engine berbasis Ruffle (DX12/Vulkan + Grim IPC)
```

> **Keamanan Repo:** Folder output hasil kompilasi (`dist/`, `release-artifacts/`, `engine/target/`, `grim/bin/`, `grim/obj/`) serta berkas sesi/akun lokal pengguna (`CharSelect.cfg`, `*.log`, dll.) dikecualikan lewat `.gitignore` dan tidak pernah ikut dikemas oleh `-PackageRelease`.

---

## Panduan Update & Pengembangan (Developer Cheat Sheet)

> [!IMPORTANT]
> Karena GitHub Actions otomatis melakukan commit sinkronisasi `VERSION` & `manifest.json` ke branch `main` setiap kali rilis tag selesai, **selalu jalankan `git pull --rebase origin main` terlebih dahulu** sebelum membuat perubahan atau rilis baru di PC lokal.

### Skenario 1 — Update Aplikasi C# / UI Saja (`grim/`)
Cocok saat memperbaiki bug UI, form, atau fitur C# tanpa menyentuh Rust (waktu compile ~6 detik):
```powershell
git pull --rebase origin main

# Setelah selesai edit file di grim/, tes build cepat (tanpa compile ulang Rust):
powershell -ExecutionPolicy Bypass -File build.ps1 -SkipEngine

# Uji jalankan hasil build lokal:
.\dist\"GrimCodex Ruffle.exe"
```

### Skenario 2 — Update Engine Rust (`engine/ruffle/`)
Cocok saat mengubah rendering, performa, IPC, atau menu klik kanan engine:
```powershell
git pull --rebase origin main

# Compile engine Rust saja (tanpa compile ulang C#):
powershell -ExecutionPolicy Bypass -File build.ps1 -SkipApp

# Atau compile keduanya sekaligus (Rust + C#):
powershell -ExecutionPolicy Bypass -File build.ps1
```

### Skenario 3 — Update Koleksi Skrip `CodexBank` Saja (Tanpa Rilis Aplikasi Baru)
Jika hanya menambah/mengubah file `.gbot` di `grim/CodexBank/`:
1. Tambahkan/perbarui file `.gbot` di dalam `grim/CodexBank/` dan sesuaikan daftar `scripts` di `grim/CodexBank/manifest.json` (jangan ubah `app_version` jika aplikasi tidak rilis baru).
2. Push langsung ke `main` (tanpa membuat tag):
   ```powershell
   git pull --rebase origin main
   git add grim/CodexBank
   git commit -m "chore(bank): update scripts"
   git push origin main
   ```
   Pengguna langsung mendapatkan skrip terbaru melalui menu **CodexBank** di dalam aplikasi.

---

## Prosedur Rilis Versi Baru (`vX.Y.Z`) — Otomatis via GitHub Actions

Cukup ikuti urutan aman ini setiap kali ingin merilis versi baru (misalnya `1.0.2`):

```powershell
# 1. Tarik commit terbaru dari GitHub (termasuk commit otomatis dari rilis sebelumnya)
git pull --rebase origin main

# 2. Sinkronkan nomor versi ke seluruh file proyek & pastikan C# lulus compile
powershell -ExecutionPolicy Bypass -File build.ps1 -Version 1.0.2 -SyncReleaseDate -SkipEngine

# 3. (Opsional) Ubah teks "changelog" di grim/CodexBank/manifest.json sesuai catatan rilis

# 4. Commit, buat tag vX.Y.Z, lalu push ke GitHub
git add .
git commit -m "release: GrimCodex Ruffle v1.0.2"
git tag v1.0.2
git push origin main --tags
```

Setelah `git push origin main --tags`, **GitHub Actions** otomatis mengerjakan sisanya:
1. Mengompilasi engine Rust dengan optimasi penuh (`-Profile dist -UsePgo`) + aplikasi C# sesuai versi tag.
2. Menerbitkan halaman **GitHub Release `v1.0.2`** lengkap dengan 3 asset:
   - `GrimCodex-Ruffle-v1.0.2-win64.zip` *(paket 1 folder lengkap untuk pengguna baru)*
   - `GrimCodex.Ruffle.exe` *(untuk fitur Update App pengguna lama)*
   - `grimcodex-engine.zip` *(otomatis ikut terunduh pada Update App hanya bila hash SHA-256 engine berubah)*
3. Menyinkronkan `app_version` dan `engine_sha256` terbaru ke `grim/CodexBank/manifest.json` di branch `main`.

### Tips Troubleshooting Rilis
- **Memicu ulang tag yang sama (jika sempat batal/diubah):**
  ```powershell
  git tag -f v1.0.2
  git push origin :refs/tags/v1.0.2
  git push origin v1.0.2
  ```
- **Membuat paket `.zip` rilis secara manual di PC lokal:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File build.ps1 -Profile dist -UsePgo -PackageRelease
  ```
  Hasil paket siap upload tersedia di folder `release-artifacts\`.

---

## Prasyarat Build Lokal (Dari Nol)

Jika suatu saat pindah PC baru dan ingin build penuh dari nol:
1. **Visual Studio 2022** (atau **VS 2022 Build Tools**) dengan workload *.NET desktop development* + NuGet (`nuget restore "grim\Project Grimlite.sln"`).
2. **Rust (`rustup`)** sesuai versi di `engine/RUST_VERSION` (`rustup toolchain install <versi>`).
3. **JDK 21** (misalnya Eclipse Temurin 21 di `PATH` / `JAVA_HOME`, atau portable di `%LOCALAPPDATA%\Programs\temurin-21`) untuk kompilasi `playerglobal` pada engine Rust.

---

## Lisensi & Kredit

- Komponen engine berbasis [Ruffle](https://ruffle.rs/) dilisensikan di bawah **GNU General Public License v3.0 (GPL-3.0)** — lihat [`engine/LICENSE`](engine/LICENSE).
- Antarmuka desktop dikembangkan dari ekosistem komunitas Grimoire / Grimlite Li.
