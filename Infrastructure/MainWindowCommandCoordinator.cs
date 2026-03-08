using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowCommandCoordinator
    {
        private readonly Window _owner;
        private readonly IMainWindowCommandPlatform _platform;

        public MainWindowCommandCoordinator(Window owner, IMainWindowCommandPlatform platform = null)
        {
            _owner = owner;
            _platform = platform ?? new MainWindowCommandPlatform();
        }

        public void Execute(
            ICommand command,
            List<ChatMessage> initialMessages,
            Action hideContent,
            Action restoreContent,
            Action hideMainWindow)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(initialMessages);
            ArgumentNullException.ThrowIfNull(hideContent);
            ArgumentNullException.ThrowIfNull(restoreContent);
            ArgumentNullException.ThrowIfNull(hideMainWindow);

            if (command == MenuCommands.OpenNotepad)
            {
                _platform.OpenNotepad();
            }
            else if (command == MenuCommands.LockWorkstation)
            {
                _platform.LockWorkstation();
            }
            else if (command == MenuCommands.OpenAiChat)
            {
                OpenChatWindow(initialMessages, hideContent, restoreContent);
            }
            else if (command == MenuCommands.OpenSettings)
            {
                OpenSettingsWindow(hideContent, restoreContent);
            }
            else
            {
                command.Execute(null);
            }

            hideMainWindow();
        }

        private void OpenChatWindow(List<ChatMessage> initialMessages, Action hideContent, Action restoreContent)
        {
            try
            {
                hideContent();
                _platform.OpenChatWindow(_owner, initialMessages, restoreContent);
            }
            catch (Exception ex)
            {
                _platform.ShowError($"无法打开妹抖酱的聊天窗口: {ex.Message}", "错误");
                restoreContent();
            }
        }

        private void OpenSettingsWindow(Action hideContent, Action restoreContent)
        {
            try
            {
                hideContent();
                _platform.OpenSettingsWindow(_owner);
                restoreContent();
            }
            catch (Exception ex)
            {
                _platform.ShowError($"无法打开设置窗口: {ex.Message}", "错误");
                restoreContent();
            }
        }
    }
}