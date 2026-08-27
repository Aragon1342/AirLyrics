using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AirLyrics.App.Native
{
    [Flags]
    public enum KeyModifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8,
        NoRepeat = 0x4000
    }

    /// <summary>
    /// Administrador de atajos de teclado globales (System-wide HotKeys) mediante Win32 API.
    /// </summary>
    public sealed class HotKeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly IntPtr _hwnd;
        private HwndSource? _source;
        private int _currentId = 0;
        private readonly System.Collections.Generic.Dictionary<int, Action> _hotkeyActions = new();

        public HotKeyManager(Window window)
        {
            var helper = new WindowInteropHelper(window);
            _hwnd = helper.EnsureHandle();
            _source = HwndSource.FromHwnd(_hwnd);
            _source?.AddHook(HwndHook);
        }

        public int Register(KeyModifiers modifiers, uint virtualKey, Action callback)
        {
            _currentId++;
            var success = RegisterHotKey(_hwnd, _currentId, (uint)modifiers, virtualKey);
            if (success)
            {
                _hotkeyActions[_currentId] = callback;
                return _currentId;
            }
            return -1;
        }

        public void Unregister(int hotKeyId)
        {
            if (_hotkeyActions.ContainsKey(hotKeyId))
            {
                UnregisterHotKey(_hwnd, hotKeyId);
                _hotkeyActions.Remove(hotKeyId);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (_hotkeyActions.TryGetValue(id, out var action))
                {
                    action.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(_hwnd, id);
            }
            _hotkeyActions.Clear();

            if (_source != null)
            {
                _source.RemoveHook(HwndHook);
                _source = null;
            }
        }
    }
}
