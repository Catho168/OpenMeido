using System;
using System.Windows.Interop;

namespace OpenMeido.Infrastructure
{
    public interface IMainWindowHotkeyPlatform
    {
        void AddHook(IntPtr hwnd, HwndSourceHook hook);

        void RemoveHook(IntPtr hwnd, HwndSourceHook hook);

        bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey);

        bool UnregisterHotKey(IntPtr hwnd, int id);
    }
}