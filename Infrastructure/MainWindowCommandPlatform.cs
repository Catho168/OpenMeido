using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using OpenMeido.Models;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowCommandPlatform : IMainWindowCommandPlatform
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void LockWorkStation();

        public void OpenNotepad()
        {
            Process.Start("notepad.exe");
        }

        public void LockWorkstation()
        {
            LockWorkStation();
        }

        public void OpenChatWindow(Window owner, List<ChatMessage> initialMessages, Action onClosed)
        {
            var appServices = (Application.Current as App)?.Services;
            var chatWindow = appServices?.GetService(typeof(ChatWindow)) as ChatWindow ?? new ChatWindow();
            chatWindow.Show();
            chatWindow.AppendMiniChatHistory(initialMessages);
            chatWindow.Activate();
            chatWindow.Closed += (_, __) => onClosed();
        }

        public void OpenSettingsWindow(Window owner)
        {
            var appServices = (Application.Current as App)?.Services;
            var settingsWindow = appServices?.GetService(typeof(SettingsWindow)) as SettingsWindow ?? new SettingsWindow();
            settingsWindow.Owner = owner;
            settingsWindow.ShowDialog();
        }

        public void ShowError(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}