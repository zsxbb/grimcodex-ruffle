# Panduan Build, Update & GitHub Actions (`release.yml`)

Dokumen internal untuk pemeliharaan, pengembangan lokal, dan rilis otomatis **GrimCodex Ruffle**.

---

## 1. Cara Kerja GitHub Actions (`release.yml`)

Workflow [`.github/workflows/release.yml`](release.yml) berjalan otomatis setiap kali tag berawalan `v*` (misalnya `v1.0.2`) di-push ke GitHub, atau dipicu manual lewat **Actions → workflow_dispatch**:

1. **Sinkronisasi Versi Tunggal:** Membaca nomor versi dari nama tag (`v1.0.2` → `1.0.2`) lalu menjalankan `build.ps1 -Version 1.0.2 -SyncReleaseDate` untuk menyamakan versi di:
   - `VERSION`
   - `grim/Properties/AssemblyInfo.cs`
   - `grim/Grimoire/Tools/CodexBankManager.cs` (`CURRENT_APP_VERSION`)
   - `grim/Grimoire/Program.cs` (`Version` & `ReleaseDate`)
   - `engine/ruffle/desktop/src/grimcodex.rs` (`VERSION`)
   - `engine/ruffle/desktop/Cargo.toml` (`ProductVersion` & `FileVersion`)
   - `grim/CodexBank/manifest.json` (`app_version`)
2. **Kompilasi Optimal (`-Profile dist -UsePgo`):**
   - Mengompilasi engine Rust x64 menggunakan cache `Swatinem/rust-cache` + profil PGO (`engine/pgo/aqw.profdata.zip`).
   - Mengompilasi aplikasi C# x86 (`grim/Project Grimlite.sln`).
3. **Pemaketan & Publikasi GitHub Release (`-PackageRelease`):**
   Menerbitkan 3 file ke halaman **GitHub Releases**:
   - **`GrimCodex-Ruffle-vX.Y.Z-win64.zip`** — paket 1 folder `GrimCodex Ruffle\` bersih untuk pengguna baru (termasuk `Bots\CodexBank\`, bebas dari file sesi lokal seperti `CharSelect.cfg`).
   - **`GrimCodex.Ruffle.exe`** — dipakai oleh fitur **About → Update App** pengguna lama.
   - **`grimcodex-engine.zip`** — dipakai oleh **Update App** hanya apabila hash SHA-256 engine berubah dibanding versi lokal pengguna.
4. **Auto-Sync ke `main`:**
   Melakukan commit otomatis (`[skip ci]`) ke branch `main` untuk memperbarui `VERSION` serta `app_version` dan `engine_sha256` di `grim/CodexBank/manifest.json` (teks `changelog` custom yang sudah mengandung `vX.Y.Z` tetap dipertahankan).

### Pengaturan Repo GitHub yang Wajib Aktif
Di `Settings → Actions → General`:
- **Actions permissions:** *Allow all actions and reusable workflows*
- **Workflow permissions:** *Read and write permissions*

---

## 2. Skenario Update Sehari-hari

> [!IMPORTANT]
> Selalu jalankan **`git pull --rebase origin main`** sebelum mulai mengedit atau merilis, karena GitHub Actions menambahkan commit otomatis ke `main` setelah setiap rilis tag.

### A. Update Aplikasi C# / UI Saja (`grim/`)
Compile sangat cepat (~6 detik) tanpa mengompilasi ulang Rust:
```powershell
git pull --rebase origin main

# Setelah selesai edit file di grim/:
powershell -ExecutionPolicy Bypass -File build.ps1 -SkipEngine

# Uji lokal:
.\dist\"GrimCodex Ruffle.exe"
```

### B. Update Engine Rust (`engine/ruffle/`)
Saat mengubah rendering, IPC, performa, atau menu klik kanan engine:
```powershell
git pull --rebase origin main

# Compile engine Rust saja (tanpa compile ulang C#):
powershell -ExecutionPolicy Bypass -File build.ps1 -SkipApp

# Atau compile penuh keduanya (Rust + C#):
powershell -ExecutionPolicy Bypass -File build.ps1
```

### C. Update Koleksi Skrip `CodexBank` Saja (Tanpa Rilis Aplikasi Baru)
Jika hanya menambah/mengubah skrip `.gbot` di `grim/CodexBank/`:
1. Taruh/perbarui file `.gbot` di `grim/CodexBank/` dan sesuaikan daftar `scripts` di `grim/CodexBank/manifest.json` (biarkan `app_version` tetap).
2. Push langsung ke `main` tanpa membuat tag:
   ```powershell
   git pull --rebase origin main
   git add grim/CodexBank
   git commit -m "chore(bank): update scripts"
   git push origin main
   ```

---

## 3. Prosedur Rilis Versi Baru (`vX.Y.Z`)

Ikuti urutan ini setiap kali merilis versi baru (contoh `1.0.2`):

```powershell
# 1. Tarik commit terbaru dari GitHub
git pull --rebase origin main

# 2. Sinkronkan nomor versi di seluruh file & verifikasi compile C#
powershell -ExecutionPolicy Bypass -File build.ps1 -Version 1.0.2 -SyncReleaseDate -SkipEngine

# 3. (Opsional) Tulis catatan rilis di field "changelog" pada grim/CodexBank/manifest.json
#    Pastikan mengandung teks "v1.0.2" (mis. "GrimCodex Ruffle v1.0.2 - Deskripsi perubahan")

# 4. Commit, buat tag, dan push ke GitHub
git add .
git commit -m "release: GrimCodex Ruffle v1.0.2"
git tag v1.0.2
git push origin main --tags
```

---

## 4. Troubleshooting & Build Manual

### Memicu Ulang Tag yang Sama
Jika perlu menjalankan ulang rilis pada tag yang sama setelah perbaikan kode:
```powershell
git tag -f v1.0.2
git push origin :refs/tags/v1.0.2
git push origin v1.0.2
```

### Membuat Paket Release `.zip` Secara Lokal di PC
```powershell
powershell -ExecutionPolicy Bypass -File build.ps1 -Profile dist -UsePgo -PackageRelease
```
Hasil paket siap upload akan berada di folder `release-artifacts\` (diabaikan oleh `.gitignore`).

### Prasyarat Lingkungan Lokal (Jika Pindah PC Baru)
1. **Visual Studio 2022 / Build Tools** dengan workload *.NET desktop development* + NuGet (`nuget restore "grim\Project Grimlite.sln"`).
2. **Rust (`rustup`)** sesuai versi pada `engine/RUST_VERSION` (`rustup toolchain install <versi>`).
3. **JDK 21** (Eclipse Temurin 21 di `PATH` / `JAVA_HOME`, atau portable di `%LOCALAPPDATA%\Programs\temurin-21`) untuk kompilasi `playerglobal` pada engine Rust.

