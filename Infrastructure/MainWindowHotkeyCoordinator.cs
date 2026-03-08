using System;
using System.Windows.Interop;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowHotkeyCoordinator : IDisposable
    {
        private const int WmHotkey = 0x0312;
        private readonly IMainWindowHotkeyPlatform _platform;
        private readonly int _hotkeyId;
        private readonly uint _modifier;
        private readonly uint _virtualKey;
        private HwndSourceHook _hook;
        private Action _onHotkeyPressed;
        private IntPtr _hwnd;
        private bool _isAttached;

        public MainWindowHotkeyCoordinator(
            IMainWindowHotkeyPlatform platform = null,
            int hotkeyId = 9000,
            uint modifier = 0x0001,
            uint virtualKey = 0x52)
        {
            _platform = platform ?? new MainWindowHotkeyPlatform();
            _hotkeyId = hotkeyId;
            _modifier = modifier;
            _virtualKey = virtualKey;
        }

        public void Attach(IntPtr hwnd, Action onHotkeyPressed)
        {
            ArgumentNullException.ThrowIfNull(onHotkeyPressed);

            if (_isAttached || hwnd == IntPtr.Zero)
            {
                return;
            }

            _hwnd = hwnd;
            _onHotkeyPressed = onHotkeyPressed;
            _hook = HandleWindowMessage;

            _platform.AddHook(_hwnd, _hook);
            _platform.RegisterHotKey(_hwnd, _hotkeyId, _modifier, _virtualKey);
            _isAttached = true;
        }

        public void Dispose()
        {
            if (!_isAttached)
            {
                return;
            }

            if (_hook != null)
            {
                _platform.RemoveHook(_hwnd, _hook);
            }

            _platform.UnregisterHotKey(_hwnd, _hotkeyId);
            _hook = null;
            _onHotkeyPressed = null;
            _hwnd = IntPtr.Zero;
            _isAttached = false;
        }

        private IntPtr HandleWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotkey && wParam.ToInt32() == _hotkeyId)
            {
                _onHotkeyPressed?.Invoke();
                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}