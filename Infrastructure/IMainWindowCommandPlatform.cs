using System;
using System.Collections.Generic;
using System.Windows;
using OpenMeido.Models;

namespace OpenMeido.Infrastructure
{
    public interface IMainWindowCommandPlatform
    {
        void OpenNotepad();

        void LockWorkstation();

        void OpenChatWindow(Window owner, List<ChatMessage> initialMessages, Action onClosed);

        void OpenSettingsWindow(Window owner);

        void ShowError(string message, string title);
    }
}