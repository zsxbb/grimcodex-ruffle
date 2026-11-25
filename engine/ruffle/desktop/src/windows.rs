use std::ptr::NonNull;
use std::sync::atomic::AtomicBool;

use windows_sys::Win32::Storage::FileSystem::{FILE_TYPE_DISK, FILE_TYPE_PIPE, GetFileType};
use windows_sys::Win32::System::Console::{
    ATTACH_PARENT_PROCESS, AttachConsole, FreeConsole, GetStdHandle, STD_OUTPUT_HANDLE,
};

/// RAII guard for the attached parent console.
/// Frees the console on drop if it was successfully attached.
pub(super) struct Console {
    attached: bool,
}

impl Console {
    // When linked with the windows subsystem windows won't automatically attach
    // to the console of the parent process, so we do it explicitly. This fails
    // silently if the parent has no console.
    //
    // However, if stdout/stderr are already redirected (e.g., `ruffle.exe > file.txt`),
    // we should NOT attach to the console as that would bypass the redirection.
    // See: https://github.com/ruffle-rs/ruffle/issues/9145
    pub(super) fn attach() -> Self {
        // Check if stdout is already redirected to a file or pipe
        // SAFETY: STD_OUTPUT_HANDLE is a valid standard device constant.
        let stdout_handle = NonNull::new(unsafe { GetStdHandle(STD_OUTPUT_HANDLE) });

        let should_attach = stdout_handle.is_none_or(|mut handle| {
            // SAFETY: GetFileType accepts any handle value including INVALID_HANDLE_VALUE,
            // returning FILE_TYPE_UNKNOWN in that case.
            let file_type = unsafe { GetFileType(handle.as_mut()) };

            // If output is redirected to a file or pipe, don't attach to console
            // as that would bypass the redirection
            !matches!(file_type, FILE_TYPE_DISK | FILE_TYPE_PIPE)
        });

        let attached = if should_attach {
            // Otherwise, attach to parent console for interactive use
            // SAFETY: ATTACH_PARENT_PROCESS is a valid constant for AttachConsole.
            // This call fails silently if the parent has no console.
            (unsafe { AttachConsole(ATTACH_PARENT_PROCESS) }) != 0
        } else {
            false
        };

        Self { attached }
    }
}

impl Drop for Console {
    fn drop(&mut self) {
        if self.attached {
            // Without explicitly detaching, cmd won't redraw its prompt.
            // SAFETY: We only call FreeConsole if it was previously successfully attached.
            unsafe { FreeConsole() };
        }
    }
}

/// AQW build (GrimCodex): runs at high priority and opts out of Windows 11's "EcoQoS" power
/// throttling, which otherwise lowers the clock speed of a process Windows considers in the
/// background (e.g. while another window has focus) and caused frame drops. The old .NET
/// launcher did this from outside; doing it here keeps the launcher trivial.
pub(super) fn aqw_tune_process() {
    use windows_sys::Win32::System::Threading::{
        GetCurrentProcess, HIGH_PRIORITY_CLASS, PROCESS_POWER_THROTTLING_CURRENT_VERSION,
        PROCESS_POWER_THROTTLING_EXECUTION_SPEED, PROCESS_POWER_THROTTLING_STATE,
        ProcessPowerThrottling, SetPriorityClass, SetProcessInformation,
    };
    // SAFETY: GetCurrentProcess returns a pseudo-handle that needs no closing; the struct
    // pointer and size describe a valid PROCESS_POWER_THROTTLING_STATE. Failures are ignored.
    unsafe {
        let process = GetCurrentProcess();
        SetPriorityClass(process, HIGH_PRIORITY_CLASS);
        let state = PROCESS_POWER_THROTTLING_STATE {
            Version: PROCESS_POWER_THROTTLING_CURRENT_VERSION,
            ControlMask: PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
            StateMask: 0,
        };
        SetProcessInformation(
            process,
            ProcessPowerThrottling,
            (&raw const state).cast(),
            size_of::<PROCESS_POWER_THROTTLING_STATE>() as u32,
        );
    }
}

/// AQW build (GrimCodex): a crash in native code (almost always a graphics driver, e.g. Intel's
/// Vulkan driver igvk64.dll) used to make GrimCodex vanish without a word: Rust's panic hook only
/// sees Rust panics. This catches it, writes where it happened to the log and says what
/// happened instead. If the Vulkan driver crashed while GrimCodex was starting, it remembers that
/// (see `grimcodex::save_vulkan_crash`) and offers to start GrimCodex again, which then uses
/// DirectX 12.
pub(super) fn install_crash_handler() {
    use windows_sys::Win32::System::Diagnostics::Debug::SetUnhandledExceptionFilter;
    // SAFETY: `crash_filter` has the signature Windows expects and stays valid for the whole
    // process lifetime.
    unsafe { SetUnhandledExceptionFilter(Some(crash_filter)) };
}

/// Set while a native crash is being handled, and once its report is in crash.txt.
static CRASH_HANDLING: AtomicBool = AtomicBool::new(false);
static CRASH_REPORTED: AtomicBool = AtomicBool::new(false);

/// Called before the process ends normally: if another thread is handling a native crash right
/// now, waits (up to 3 s) until its report is in crash.txt. Ending the process stops every
/// thread, so a report from another thread was cut off after its first line: the DirectX 12
/// crash while closing happened on wgpu's drop thread while the main thread finished closing,
/// and its crash.txt never had a stack.
pub(super) fn wait_for_crash_report() {
    use std::sync::atomic::Ordering;
    let started = std::time::Instant::now();
    while CRASH_HANDLING.load(Ordering::SeqCst)
        && !CRASH_REPORTED.load(Ordering::SeqCst)
        && started.elapsed() < std::time::Duration::from_secs(3)
    {
        std::thread::sleep(std::time::Duration::from_millis(5));
    }
}

unsafe extern "system" fn crash_filter(
    info: *const windows_sys::Win32::System::Diagnostics::Debug::EXCEPTION_POINTERS,
) -> i32 {
    use std::sync::atomic::Ordering;
    const EXCEPTION_EXECUTE_HANDLER: i32 = 1;
    const EXCEPTION_ILLEGAL_INSTRUCTION: u32 = 0xC000_001D;

    if CRASH_HANDLING.swap(true, Ordering::SeqCst) {
        // Another thread crashed too (or this one crashed again inside the handler): leave the
        // first report on screen; the process ends when it is closed.
        loop {
            std::thread::sleep(std::time::Duration::from_secs(3600));
        }
    }

    // SAFETY: Windows passes a valid EXCEPTION_POINTERS with a valid exception record.
    let (code, address) = unsafe {
        let record = &*(*info).ExceptionRecord;
        (record.ExceptionCode as u32, record.ExceptionAddress as usize)
    };
    let module = module_at(address);
    let in_grimcodex = module.as_ref().is_none_or(|module| module.is_main_exe);
    let file_name = module.as_ref().map_or_else(
        || "an unknown module".to_string(),
        |module| {
            module.path.file_name().map_or_else(
                || module.path.display().to_string(),
                |name| name.to_string_lossy().into_owned(),
            )
        },
    );
    let vulkan_starting = crate::grimcodex::vulkan_starting();
    let dx12_starting = crate::grimcodex::dx12_starting();
    // SAFETY: plain query.
    let thread = unsafe { windows_sys::Win32::System::Threading::GetCurrentThreadId() };
    let record = format!(
        "fatal native crash: exception 0x{code:08X} in {} at offset 0x{:X} (thread {thread}, \
        {}, Vulkan starting: {vulkan_starting}, DirectX 12 starting: {dx12_starting})",
        module
            .as_ref()
            .map_or_else(|| "an unknown module".to_string(), |m| m.path.display().to_string()),
        module.as_ref().map_or(0, |m| m.offset),
        crate::grimcodex::exit_phase()
    );
    // The record is written to crash.txt right here, before anything that could hang: the
    // log is written by another thread, which doesn't get to it when the process ends right
    // after, or is gone already when the crash is during shutdown. Then the stack as module
    // and offset (safe at any time), then the log, with the symbolized stack when not
    // shutting down (looking symbols up while Windows unloads DLLs can hang).
    let mut crash_file = CrashFile::create(&record);
    let stack = raw_stack();
    crash_file.add(&stack);
    drop(crash_file);
    CRASH_REPORTED.store(true, Ordering::SeqCst);
    // While closing to start again with another graphics API: the new GrimCodex is already
    // starting (see `grimcodex::start_again_now`), or is started here if closing didn't get that
    // far, so this one just ends, with no message about it.
    if crate::grimcodex::exiting() && crate::grimcodex::should_start_again() {
        use windows_sys::Win32::System::Threading::{GetCurrentProcess, TerminateProcess};
        if !crate::grimcodex::started_again() {
            start_again();
        }
        // SAFETY: ends this process right away, as the crash would have.
        unsafe { TerminateProcess(GetCurrentProcess(), 1) };
    }
    if crate::grimcodex::exiting() {
        tracing::error!("{record}\n{stack}");
    } else {
        tracing::error!("{record}\n{}", std::backtrace::Backtrace::force_capture());
    }
    let where_saved = crate::grimcodex::log_folder().map_or_else(
        || "%LOCALAPPDATA%\\ruffle\\log".to_string(),
        |folder| folder.display().to_string(),
    );

    if code == EXCEPTION_ILLEGAL_INSTRUCTION && in_grimcodex {
        message_box(
            &format!(
                "GrimCodex Ruffle has to close: it ran into an instruction this PC's processor doesn't \
                have. Details were saved to crash.txt in:\n{where_saved}"
            ),
            false,
        );
    } else if vulkan_starting
        && let Some(module) = module.as_ref().filter(|module| module.is_graphics_driver())
    {
        crate::grimcodex::save_vulkan_crash(&module.path);
        let restart = message_box(
            &format!(
                "The graphics driver crashed while GrimCodex Ruffle was starting Vulkan (in {file_name}).\n\n\
                GrimCodex Ruffle will use DirectX 12 on this PC instead.\n\n\
                Click OK to start again."
            ),
            true,
        );
        if restart {
            start_again();
        }
    } else if dx12_starting
        && !crate::grimcodex::exiting()
        && crate::grimcodex::vulkan_crashed_before().is_none()
        && let Some(module) = module
            .as_ref()
            .filter(|module| module.is_graphics_driver() || module.is_direct3d())
    {
        crate::grimcodex::save_dx12_crash(&module.path);
        let restart = message_box(
            &format!(
                "The graphics driver crashed while GrimCodex Ruffle was starting DirectX 12 (in {file_name}).\n\n\
                GrimCodex Ruffle will use Vulkan on this PC instead.\n\n\
                Click OK to start again."
            ),
            true,
        );
        if restart {
            start_again();
        }
    } else {
        let hint = if in_grimcodex {
            String::new()
        } else {
            format!(
                "It happened inside {file_name}, which isn't part of GrimCodex Ruffle: usually the \
                graphics driver, or a program that hooks into games (overlays, recorders). \
                Updating your graphics driver often fixes it.\n\n"
            )
        };
        message_box(
            &format!(
                "GrimCodex Ruffle crashed (error 0x{code:08X} in {file_name}) and has to close.\n\n{hint}\
                Details were saved to crash.txt and ruffle.log in {where_saved}."
            ),
            false,
        );
    }
    // End right here: a normal exit runs every DLL's shutdown code, and a crashed graphics
    // driver can hang in its own.
    // SAFETY: terminating the current process (pseudo-handle, never closed).
    unsafe {
        use windows_sys::Win32::System::Threading::{GetCurrentProcess, TerminateProcess};
        TerminateProcess(GetCurrentProcess(), code);
    }
    EXCEPTION_EXECUTE_HANDLER
}

/// crash.txt in the log folder (what the crash dialog says was saved), replaced by this crash's
/// record and then its stack. Each part is on disk before the next step; the file stays open
/// in between, so adding the stack can't lose the record.
struct CrashFile(Option<std::fs::File>);

impl CrashFile {
    fn create(record: &str) -> Self {
        let file = crate::grimcodex::log_folder()
            .and_then(|folder| std::fs::File::create(folder.join("crash.txt")).ok());
        let mut crash_file = Self(file);
        crash_file.add(&format!("GrimCodex Ruffle {}\n{record}\n", crate::grimcodex::VERSION));
        crash_file
    }

    fn add(&mut self, text: &str) {
        use std::io::Write as _;
        if let Some(file) = &mut self.0 {
            let _ = file.write_all(text.replace('\n', "\r\n").as_bytes());
            let _ = file.sync_all();
        }
    }
}

/// The calling thread's stack as module and offset per frame, which needs no symbols (safe
/// while Windows unloads DLLs, unlike `Backtrace`).
fn raw_stack() -> String {
    use windows_sys::Win32::System::Diagnostics::Debug::RtlCaptureStackBackTrace;
    let mut frames = [std::ptr::null_mut::<std::ffi::c_void>(); 62];
    // SAFETY: the buffer and its length match; the hash is optional.
    let count = unsafe {
        RtlCaptureStackBackTrace(0, frames.len() as u32, frames.as_mut_ptr(), std::ptr::null_mut())
    };
    let mut text = String::from("stack (module+offset):\n");
    for frame in &frames[..count as usize] {
        let address = *frame as usize;
        match module_at(address) {
            Some(module) => {
                let name = module.path.file_name().map_or_else(
                    || module.path.display().to_string(),
                    |name| name.to_string_lossy().into_owned(),
                );
                text.push_str(&format!("  {name}+0x{:X}\n", module.offset));
            }
            None => text.push_str(&format!("  0x{address:X}\n")),
        }
    }
    text
}

/// AQW build: `RUFFLE_TEST_NATIVE_CRASH=driver` crashes inside a Windows DLL (counts as a
/// graphics driver crash when `RUFFLE_TEST_DRIVER_FILE` names that DLL, e.g. ntdll.dll),
/// `=own` inside GrimCodex itself, to test `install_crash_handler`.
pub(super) fn test_native_crash() {
    use windows_sys::Win32::System::LibraryLoader::GetModuleFileNameW;
    match std::env::var("RUFFLE_TEST_NATIVE_CRASH").as_deref() {
        // SAFETY: deliberately invalid: makes kernelbase.dll write to address 8.
        Ok("driver") => unsafe {
            GetModuleFileNameW(std::ptr::null_mut(), 8 as *mut u16, 260);
        },
        // SAFETY: deliberately invalid: writes to address 8.
        Ok("own") => unsafe { std::ptr::write_volatile(8 as *mut u32, 0) },
        _ => {}
    }
}

struct CrashModule {
    path: std::path::PathBuf,
    offset: usize,
    is_main_exe: bool,
}

impl CrashModule {
    /// Graphics drivers are installed in Windows' driver store (a new folder per driver
    /// version); older ones put their Vulkan driver (Intel's igvk64.dll, NVIDIA's nvoglv64.dll,
    /// AMD's amdvlk64.dll) in System32. Only a crash in one of those switches GrimCodex to
    /// DirectX 12; one in an overlay, antivirus or other Windows file doesn't.
    fn is_graphics_driver(&self) -> bool {
        let path = self.path.to_string_lossy().to_ascii_lowercase();
        let file = path.rsplit('\\').next().unwrap_or_default();
        path.contains(r"\driverstore\filerepository\")
            || ["igvk64.dll", "nvoglv64.dll", "amdvlk64.dll"].contains(&file)
            || std::env::var("RUFFLE_TEST_DRIVER_FILE")
                .is_ok_and(|test| test.eq_ignore_ascii_case(file))
    }

    /// Windows' own Direct3D 12 files: a crash in one of them while DirectX 12 is starting
    /// counts as DirectX 12 not working on this PC (see `grimcodex::save_dx12_crash`).
    fn is_direct3d(&self) -> bool {
        let path = self.path.to_string_lossy().to_ascii_lowercase();
        let file = path.rsplit('\\').next().unwrap_or_default();
        ["d3d12core.dll", "d3d12.dll"].contains(&file)
    }
}

/// The loaded module (exe or DLL) that contains `address`.
fn module_at(address: usize) -> Option<CrashModule> {
    use std::os::windows::ffi::OsStringExt;
    use windows_sys::Win32::Foundation::HMODULE;
    use windows_sys::Win32::System::LibraryLoader::{
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        GetModuleFileNameW, GetModuleHandleExW, GetModuleHandleW,
    };

    let mut module: HMODULE = std::ptr::null_mut();
    let mut buffer = [0u16; 1024];
    // SAFETY: with FROM_ADDRESS the "name" is an address inside the module; UNCHANGED_REFCOUNT
    // means nothing must be released. The buffer pointer and length match.
    let length = unsafe {
        if GetModuleHandleExW(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            address as *const u16,
            &mut module,
        ) == 0
        {
            return None;
        }
        GetModuleFileNameW(module, buffer.as_mut_ptr(), buffer.len() as u32) as usize
    };
    // SAFETY: a null name returns the handle of the process's exe.
    let main_exe = unsafe { GetModuleHandleW(std::ptr::null()) };
    Some(CrashModule {
        path: std::ffi::OsString::from_wide(&buffer[..length.min(buffer.len())]).into(),
        offset: address.wrapping_sub(module as usize),
        is_main_exe: module == main_exe,
    })
}

/// Shows an error message box; with `ok_cancel`, returns whether OK was clicked.
fn message_box(text: &str, ok_cancel: bool) -> bool {
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        IDOK, MB_ICONERROR, MB_OK, MB_OKCANCEL, MB_SETFOREGROUND, MB_TOPMOST, MessageBoxW,
    };
    let wide = |s: &str| s.encode_utf16().chain(Some(0)).collect::<Vec<u16>>();
    let (text, title) = (wide(text), wide("GrimCodex Ruffle"));
    let buttons = if ok_cancel { MB_OKCANCEL } else { MB_OK };
    // SAFETY: both strings are NUL-terminated UTF-16 that outlive the call.
    let result = unsafe {
        MessageBoxW(
            std::ptr::null_mut(),
            text.as_ptr(),
            title.as_ptr(),
            buttons | MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST,
        )
    };
    result == IDOK
}

/// Waits up to `timeout` for the process `pid` to end (see `grimcodex::wait_for_restarted_from`).
pub fn wait_for_process(pid: u32, timeout: std::time::Duration) {
    use windows_sys::Win32::Foundation::CloseHandle;
    use windows_sys::Win32::System::Threading::{
        OpenProcess, PROCESS_SYNCHRONIZE, WaitForSingleObject,
    };
    // SAFETY: plain calls; the handle, if any, is closed.
    unsafe {
        let process = OpenProcess(PROCESS_SYNCHRONIZE, 0, pid);
        if !process.is_null() {
            WaitForSingleObject(process, timeout.as_millis() as u32);
            CloseHandle(process);
        }
    }
}

/// Starts GrimCodex again with the same command line (after the Vulkan driver crashed while
/// starting; a graphics change uses `grimcodex::start_again_now`).
pub fn start_again() {
    use windows_sys::Win32::Foundation::CloseHandle;
    use windows_sys::Win32::System::Environment::GetCommandLineW;
    use windows_sys::Win32::System::Threading::{
        CreateProcessW, PROCESS_INFORMATION, STARTUPINFOW,
    };
    // SAFETY: GetCommandLineW returns this process's NUL-terminated command line, copied (with
    // its terminator) because CreateProcessW may modify the buffer. The structs are plain data
    // for which all-zero is valid, and the returned handles are closed.
    unsafe {
        let command_line = GetCommandLineW();
        let mut length = 0;
        while *command_line.add(length) != 0 {
            length += 1;
        }
        let mut buffer = std::slice::from_raw_parts(command_line, length + 1).to_vec();
        let mut startup: STARTUPINFOW = std::mem::zeroed();
        startup.cb = size_of::<STARTUPINFOW>() as u32;
        let mut process: PROCESS_INFORMATION = std::mem::zeroed();
        if CreateProcessW(
            std::ptr::null(),
            buffer.as_mut_ptr(),
            std::ptr::null(),
            std::ptr::null(),
            0,
            0,
            std::ptr::null(),
            std::ptr::null(),
            &startup,
            &mut process,
        ) != 0
        {
            CloseHandle(process.hProcess);
            CloseHandle(process.hThread);
        }
    }
}

/// GrimCodex: "Graphics" choices in the window's title-bar menu (right-click the title bar). A
/// different choice is saved and GrimCodex restarts with it. The title bar is drawn by Windows, so
/// this also works when the game itself shows nothing (e.g. a black screen on Vulkan); the other
/// settings are in the game's right-click menu (`gui::context_menu`).
mod graphics_menu {
    use crate::custom_event::RuffleEvent;
    use crate::grimcodex::{self, Graphics, Setting};
    use std::cell::RefCell;
    use std::sync::atomic::{AtomicIsize, Ordering};
    use windows_sys::Win32::Foundation::{HWND, LPARAM, LRESULT, WPARAM};
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        AppendMenuW, CallWindowProcW, CheckMenuItem, GWLP_WNDPROC, GetSystemMenu, MF_BYCOMMAND,
        MF_CHECKED, MF_GRAYED, MF_SEPARATOR, MF_STRING, MF_UNCHECKED, SetWindowLongPtrW,
        WM_SYSCOMMAND, WNDPROC,
    };
    use winit::event_loop::EventLoopProxy;

    // System-menu command ids must keep their low 4 bits clear and stay below 0xF000.
    const AUTOMATIC: usize = 0x1000;
    const VULKAN: usize = 0x1010;
    const DX12: usize = 0x1020;
    const MORE: usize = 0x1040;

    static PREVIOUS_PROC: AtomicIsize = AtomicIsize::new(0);

    thread_local! {
        /// Where a choice in the menu goes: asked about in the game window, like one from the
        /// game's right-click menu (see `gui::prompt`). The window procedure runs on the
        /// window's thread, where this is set.
        static EVENTS: RefCell<Option<EventLoopProxy<RuffleEvent>>> = const { RefCell::new(None) };
    }

    fn wide(s: &str) -> Vec<u16> {
        s.encode_utf16().chain(Some(0)).collect()
    }

    pub fn install(hwnd: HWND, events: EventLoopProxy<RuffleEvent>) {
        if PREVIOUS_PROC.load(Ordering::Relaxed) != 0 {
            return;
        }
        EVENTS.with(|e| *e.borrow_mut() = Some(events));
        let current = grimcodex::saved_graphics();
        let items = [
            (AUTOMATIC, "Graphics: Automatic (recommended)", current.is_none()),
            (VULKAN, "Graphics: Vulkan", current == Some(Graphics::Vulkan)),
            (DX12, "Graphics: DirectX 12", current == Some(Graphics::Dx12)),
        ];
        // SAFETY: `hwnd` is our live window, on its own thread; the strings outlive the calls.
        // The replaced window procedure is kept and called for everything else.
        unsafe {
            let menu = GetSystemMenu(hwnd, 0);
            if menu.is_null() {
                return;
            }
            AppendMenuW(menu, MF_SEPARATOR, 0, std::ptr::null());
            for (id, text, checked) in items {
                let text = wide(text);
                let flags = MF_STRING | if checked { MF_CHECKED } else { 0 };
                AppendMenuW(menu, flags, id, text.as_ptr());
            }
            let text = wide("More settings: right-click the game");
            AppendMenuW(menu, MF_STRING | MF_GRAYED, MORE, text.as_ptr());
            let previous = SetWindowLongPtrW(hwnd, GWLP_WNDPROC, window_proc as *const () as isize);
            PREVIOUS_PROC.store(previous, Ordering::Relaxed);
        }
    }

    unsafe extern "system" fn window_proc(
        hwnd: HWND,
        message: u32,
        wparam: WPARAM,
        lparam: LPARAM,
    ) -> LRESULT {
        if message == WM_SYSCOMMAND {
            let choice = match wparam & 0xFFF0 {
                AUTOMATIC => Some(None),
                VULKAN => Some(Some(Graphics::Vulkan)),
                DX12 => Some(Some(Graphics::Dx12)),
                _ => None,
            };
            if let Some(choice) = choice {
                EVENTS.with(|e| {
                    if let Some(events) = &*e.borrow() {
                        let _ = events.send_event(RuffleEvent::GrimCodexSetting(Setting::Graphics(choice)));
                    }
                });
                return 0;
            }
        }
        // SAFETY: the previous procedure of this window, as `SetWindowLongPtrW` returned it.
        unsafe {
            let previous: WNDPROC = std::mem::transmute(PREVIOUS_PROC.load(Ordering::Relaxed));
            CallWindowProcW(previous, hwnd, message, wparam, lparam)
        }
    }

    /// The menu's check marks as the settings are now (also changed in the game's menu).
    pub(super) fn sync_checks(hwnd: HWND) {
        let current = grimcodex::saved_graphics();
        // SAFETY: `hwnd` is our live window; its system menu exists while it does.
        unsafe {
            let menu = GetSystemMenu(hwnd, 0);
            if !menu.is_null() {
                for (id, checked) in [
                    (AUTOMATIC, current.is_none()),
                    (VULKAN, current == Some(Graphics::Vulkan)),
                    (DX12, current == Some(Graphics::Dx12)),
                ] {
                    let state = if checked { MF_CHECKED } else { MF_UNCHECKED };
                    CheckMenuItem(menu, id as u32, MF_BYCOMMAND | state);
                }
            }
        }
    }

}

fn hwnd_of(window: &winit::window::Window) -> Option<windows_sys::Win32::Foundation::HWND> {
    use winit::raw_window_handle::{HasWindowHandle, RawWindowHandle};
    match window.window_handle().ok()?.as_raw() {
        RawWindowHandle::Win32(handle) => Some(handle.hwnd.get() as _),
        _ => None,
    }
}

pub fn add_graphics_menu(
    window: &winit::window::Window,
    events: winit::event_loop::EventLoopProxy<crate::custom_event::RuffleEvent>,
) {
    if let Some(hwnd) = hwnd_of(window) {
        graphics_menu::install(hwnd, events);
    }
}

/// GrimCodex: the title-bar menu's check marks after a change in the game's right-click menu.
pub fn sync_menu_checks(window: &winit::window::Window) {
    if let Some(hwnd) = hwnd_of(window) {
        graphics_menu::sync_checks(hwnd);
    }
}

/// Grim integration: reparents the winit window into the host WinForms panel HWND as a borderless
/// WS_CHILD window sized to the parent's client area, returning the child window HWND as u64.
pub fn embed_in_parent(window: &winit::window::Window, parent_hwnd: u64) -> Option<u64> {
    use windows_sys::Win32::Foundation::{GetLastError, HWND, RECT};
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GWL_EXSTYLE, GWL_STYLE, GetClientRect, GetParent, GetWindowLongPtrW, HWND_TOP,
        SWP_FRAMECHANGED, SWP_NOACTIVATE, SWP_NOZORDER, SWP_SHOWWINDOW, SetParent,
        SetWindowLongPtrW, SetWindowPos, WS_CHILD, WS_CLIPCHILDREN, WS_CLIPSIBLINGS,
        WS_EX_APPWINDOW, WS_EX_CLIENTEDGE, WS_EX_TOPMOST, WS_EX_WINDOWEDGE, WS_VISIBLE,
    };

    let child = hwnd_of(window)?;
    let parent = parent_hwnd as usize as HWND;
    if parent.is_null() {
        return None;
    }

    // SAFETY: `child` is our live winit HWND and `parent` is the host WinForms container HWND.
    unsafe {
        let new_style = WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN;
        SetWindowLongPtrW(child, GWL_STYLE, new_style as isize);

        let ex_style = GetWindowLongPtrW(child, GWL_EXSTYLE) as u32;
        let new_ex_style =
            ex_style & !(WS_EX_APPWINDOW | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_TOPMOST);
        SetWindowLongPtrW(child, GWL_EXSTYLE, new_ex_style as isize);

        let prev_parent = SetParent(child, parent);
        let err = GetLastError();
        let actual_parent = GetParent(child);

        let mut rect = RECT {
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
        };
        let (width, height) = if GetClientRect(parent, &mut rect) != 0 {
            ((rect.right - rect.left).max(1), (rect.bottom - rect.top).max(1))
        } else {
            (960, 550)
        };

        SetWindowPos(
            child,
            HWND_TOP,
            0,
            0,
            width,
            height,
            SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED | SWP_SHOWWINDOW,
        );

        tracing::info!(
            "embed_in_parent: child=0x{:X} target_parent=0x{:X} actual_parent=0x{:X} prev_parent=0x{:X} err={} size={}x{}",
            child as usize,
            parent as usize,
            actual_parent as usize,
            prev_parent as usize,
            err,
            width,
            height
        );
    }

    Some(child as usize as u64)
}

/// Gives Win32 keyboard focus to the embedded child HWND so typing works inside the host WinForms
/// window. Called on mouse press inside the embedded view.
pub fn focus_embedded(window: &winit::window::Window) {
    use windows_sys::Win32::Foundation::HWND;
    use windows_sys::Win32::System::Threading::GetCurrentThreadId;
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GA_ROOT, GetAncestor, GetForegroundWindow, GetWindowThreadProcessId, SetForegroundWindow,
    };

    #[link(name = "user32")]
    unsafe extern "system" {
        fn SetFocus(hWnd: HWND) -> HWND;
        fn GetFocus() -> HWND;
        fn AttachThreadInput(idAttach: u32, idAttachTo: u32, fAttach: i32) -> i32;
    }

    let Some(child) = hwnd_of(window) else {
        return;
    };

    // SAFETY: `child` is our live WS_CHILD HWND embedded inside the host WinForms window.
    unsafe {
        if GetFocus() == child {
            return;
        }
        let root = GetAncestor(child, GA_ROOT);
        let target_win = if !root.is_null() { root } else { child };
        let fg = GetForegroundWindow();
        if fg != target_win && !root.is_null() {
            SetForegroundWindow(root);
        }

        let prev = SetFocus(child);
        let mut now = GetFocus();
        if now != child {
            let our_tid = GetCurrentThreadId();
            let root_tid = GetWindowThreadProcessId(target_win, std::ptr::null_mut());
            if root_tid != 0 && root_tid != our_tid {
                AttachThreadInput(our_tid, root_tid, 1);
                SetFocus(child);
                now = GetFocus();
            }
        }
        tracing::info!(
            "focus_embedded: child=0x{:X} root=0x{:X} prev=0x{:X} now=0x{:X}",
            child as usize,
            root as usize,
            prev as usize,
            now as usize
        );
    }
}

/// Returns the Win32 HWND of the given winit window as a u64.
pub fn window_hwnd_u64(window: &winit::window::Window) -> Option<u64> {
    hwnd_of(window).map(|h| h as usize as u64)
}

/// Queries the parent HWND's physical client rect (in this DPI-aware process's coordinate space),
/// resizes the embedded child window to fill it completely, and returns `(width, height)`.
pub fn sync_embedded_size(window: &winit::window::Window, parent_hwnd: u64) -> Option<(u32, u32)> {
    use windows_sys::Win32::Foundation::{HWND, RECT};
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GetClientRect, HWND_TOP, SWP_NOACTIVATE, SWP_NOZORDER, SetWindowPos,
    };

    let child = hwnd_of(window)?;
    let parent = parent_hwnd as usize as HWND;
    if parent.is_null() {
        return None;
    }

    // SAFETY: `child` and `parent` are live HWNDs.
    unsafe {
        let mut rect = RECT {
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
        };
        if GetClientRect(parent, &mut rect) == 0 {
            return None;
        }
        let width = (rect.right - rect.left).max(1);
        let height = (rect.bottom - rect.top).max(1);

        let mut child_rect = RECT {
            left: 0,
            top: 0,
            right: 0,
            bottom: 0,
        };
        if GetClientRect(child, &mut child_rect) != 0 {
            let cw = child_rect.right - child_rect.left;
            let ch = child_rect.bottom - child_rect.top;
            if cw == width && ch == height {
                return None;
            }
        }

        SetWindowPos(
            child,
            HWND_TOP,
            0,
            0,
            width,
            height,
            SWP_NOZORDER | SWP_NOACTIVATE,
        );
        Some((width as u32, height as u32))
    }
}
