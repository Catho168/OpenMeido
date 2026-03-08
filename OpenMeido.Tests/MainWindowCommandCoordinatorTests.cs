using System;
using System.Collections.Generic;
using System.Windows.Input;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class MainWindowCommandCoordinatorTests
{
    [Fact]
    public void Execute_WhenOpenNotepadCommand_UsesPlatformAndHidesMainWindow()
    {
        var platform = new FakeMainWindowCommandPlatform();
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideWindowCalls = 0;

        coordinator.Execute(MenuCommands.OpenNotepad, [], () => { }, () => { }, () => hideWindowCalls++);

        Assert.Equal(1, platform.OpenNotepadCallCount);
        Assert.Equal(0, platform.LockWorkstationCallCount);
        Assert.Equal(1, hideWindowCalls);
    }

    [Fact]
    public void Execute_WhenLockWorkstationCommand_UsesPlatformAndHidesMainWindow()
    {
        var platform = new FakeMainWindowCommandPlatform();
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideWindowCalls = 0;

        coordinator.Execute(MenuCommands.LockWorkstation, [], () => { }, () => { }, () => hideWindowCalls++);

        Assert.Equal(1, platform.LockWorkstationCallCount);
        Assert.Equal(1, hideWindowCalls);
    }

    [Fact]
    public void Execute_WhenOpenAiChatCommand_HidesContent_OpensChat_AndRestoresOnClose()
    {
        var platform = new FakeMainWindowCommandPlatform();
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideContentCalls = 0;
        var restoreCalls = 0;
        var hideWindowCalls = 0;
        List<ChatMessage> initialMessages = [new("user", "hello")];

        coordinator.Execute(
            MenuCommands.OpenAiChat,
            initialMessages,
            () => hideContentCalls++,
            () => restoreCalls++,
            () => hideWindowCalls++);

        Assert.Equal(1, hideContentCalls);
        Assert.Equal(1, platform.OpenChatWindowCallCount);
        Assert.Same(initialMessages, platform.ChatInitialMessages);
        Assert.Equal(0, restoreCalls);
        Assert.Equal(1, hideWindowCalls);

        platform.RaiseChatClosed();
        Assert.Equal(1, restoreCalls);
    }

    [Fact]
    public void Execute_WhenOpenAiChatFails_ShowsError_RestoresContent_AndStillHidesMainWindow()
    {
        var platform = new FakeMainWindowCommandPlatform
        {
            OpenChatWindowException = new InvalidOperationException("boom")
        };
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideContentCalls = 0;
        var restoreCalls = 0;
        var hideWindowCalls = 0;

        coordinator.Execute(
            MenuCommands.OpenAiChat,
            [],
            () => hideContentCalls++,
            () => restoreCalls++,
            () => hideWindowCalls++);

        Assert.Equal(1, hideContentCalls);
        Assert.Equal(1, restoreCalls);
        Assert.Equal(1, hideWindowCalls);
        Assert.Equal("错误", platform.LastErrorTitle);
        Assert.Equal("无法打开妹抖酱的聊天窗口: boom", platform.LastErrorMessage);
    }

    [Fact]
    public void Execute_WhenOpenSettingsCommand_HidesContent_RestoresContent_AndHidesMainWindow()
    {
        var platform = new FakeMainWindowCommandPlatform();
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideContentCalls = 0;
        var restoreCalls = 0;
        var hideWindowCalls = 0;

        coordinator.Execute(
            MenuCommands.OpenSettings,
            [],
            () => hideContentCalls++,
            () => restoreCalls++,
            () => hideWindowCalls++);

        Assert.Equal(1, hideContentCalls);
        Assert.Equal(1, platform.OpenSettingsWindowCallCount);
        Assert.Equal(1, restoreCalls);
        Assert.Equal(1, hideWindowCalls);
    }

    [Fact]
    public void Execute_WhenCustomCommand_ExecutesCommandAndHidesMainWindow()
    {
        var platform = new FakeMainWindowCommandPlatform();
        var coordinator = new MainWindowCommandCoordinator(null!, platform);
        var hideWindowCalls = 0;
        var command = new RecordingCommand();

        coordinator.Execute(command, [], () => { }, () => { }, () => hideWindowCalls++);

        Assert.Equal(1, command.ExecuteCallCount);
        Assert.Equal(1, hideWindowCalls);
    }

    private sealed class RecordingCommand : ICommand
    {
        public int ExecuteCallCount { get; private set; }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            ExecuteCallCount++;
        }
    }
}