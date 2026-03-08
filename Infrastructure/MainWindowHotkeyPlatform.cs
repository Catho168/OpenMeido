using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowHotkeyPlatform : IMainWindowHotkeyPlatform
    {
        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
        private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
        private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

        public void AddHook(IntPtr hwnd, HwndSourceHook hook)
        {
            HwndSource.FromHwnd(hwnd)?.AddHook(hook);
        }

        public void RemoveHook(IntPtr hwnd, HwndSourceHook hook)
        {
            HwndSource.FromHwnd(hwnd)?.RemoveHook(hook);
        }

        public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey)
            => RegisterHotKeyNative(hwnd, id, modifiers, virtualKey);

        public bool UnregisterHotKey(IntPtr hwnd, int id)
            => UnregisterHotKeyNative(hwnd, id);
    }
}