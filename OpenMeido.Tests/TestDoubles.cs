using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Tests;

internal sealed class FakeChatService : IChatService
{
    public IApiService CurrentApiService { get; set; } = null!;
    public ChatServiceInitializationResult InitializeResult { get; set; } = ChatServiceInitializationResult.Ready();
    public ChatServiceInitializationResult ReinitializeResult { get; set; } = ChatServiceInitializationResult.Ready();
    public string SendMessageResult { get; set; } = string.Empty;
    public List<ChatMessage> LastMessagesHistory { get; private set; } = null!;
    public bool DisposeCalled { get; private set; }

    public Task<ChatServiceInitializationResult> InitializeAsync() => Task.FromResult(InitializeResult);

    public Task<ChatServiceInitializationResult> ReinitializeAsync() => Task.FromResult(ReinitializeResult);

    public Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
    {
        LastMessagesHistory = messagesHistory;
        return Task.FromResult(SendMessageResult);
    }

    public void Dispose()
    {
        DisposeCalled = true;
    }
}

internal sealed class FakeApiService : IApiService
{
    public string SendMessageResult { get; set; } = string.Empty;
    public Exception? SendMessageException { get; set; }
    public List<(string Id, string Name, bool IsConnected, int ToolCount)> McpServerStatusesResult { get; set; } = new();
    public Exception? McpServerStatusesException { get; set; }
    public int GetMcpServerStatusesCallCount { get; private set; }
    public IList<ModelContextProtocol.Client.McpClientTool> AvailableMcpToolsResult { get; set; } = new List<ModelContextProtocol.Client.McpClientTool>();
    public Exception? AvailableMcpToolsException { get; set; }
    public int GetAvailableMcpToolsCallCount { get; private set; }
    public List<McpActivityRecord> RecentMcpActivitiesResult { get; set; } = new();
    public McpActivityStatistics McpActivityStatisticsResult { get; set; } = new();
    public int ClearMcpActivitiesCallCount { get; private set; }
    public bool InitializeMcpCalled { get; private set; }
    public Exception? InitializeMcpException { get; set; }
    public bool TestConnectionCalled { get; private set; }
    public bool TestConnectionResult { get; set; }
    public Exception? TestConnectionException { get; set; }
    public bool DisposeCalled { get; private set; }

    public Task InitializeMcpAsync()
    {
        InitializeMcpCalled = true;
        if (InitializeMcpException != null)
        {
            throw InitializeMcpException;
        }

        return Task.CompletedTask;
    }

    public Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
    {
        if (SendMessageException != null)
        {
            throw SendMessageException;
        }

        return Task.FromResult(SendMessageResult);
    }

    public Task<List<(string Id, string Name, bool IsConnected, int ToolCount)>> GetMcpServerStatusesAsync()
    {
        GetMcpServerStatusesCallCount++;
        if (McpServerStatusesException != null)
        {
            throw McpServerStatusesException;
        }

        return Task.FromResult(McpServerStatusesResult);
    }

    public Task<IList<ModelContextProtocol.Client.McpClientTool>> GetAvailableMcpToolsAsync()
    {
        GetAvailableMcpToolsCallCount++;
        if (AvailableMcpToolsException != null)
        {
            throw AvailableMcpToolsException;
        }

        return Task.FromResult(AvailableMcpToolsResult);
    }

    public List<McpActivityRecord> GetRecentMcpActivities(int count = 20)
        => RecentMcpActivitiesResult.Count <= count ? new List<McpActivityRecord>(RecentMcpActivitiesResult) : RecentMcpActivitiesResult.GetRange(RecentMcpActivitiesResult.Count - count, count);

    public McpActivityStatistics GetMcpActivityStatistics() => McpActivityStatisticsResult;

    public void ClearMcpActivities()
    {
        ClearMcpActivitiesCallCount++;
        RecentMcpActivitiesResult.Clear();
        McpActivityStatisticsResult = new McpActivityStatistics();
    }

    public Task<bool> TestConnectionAsync()
    {
        TestConnectionCalled = true;
        if (TestConnectionException != null)
        {
            throw TestConnectionException;
        }

        return Task.FromResult(TestConnectionResult);
    }

    public void Dispose()
    {
        DisposeCalled = true;
    }
}

internal sealed class FakeApiServiceFactory : IApiServiceFactory
{
    private readonly Queue<IApiService> _services = new();

    public AppSettings? LastSettings { get; private set; }
    public int CreateCallCount { get; private set; }
    public Exception? CreateException { get; set; }

    public void Enqueue(IApiService apiService)
    {
        _services.Enqueue(apiService);
    }

    public IApiService Create(AppSettings settings)
    {
        LastSettings = settings;
        CreateCallCount++;

        if (CreateException != null)
        {
            throw CreateException;
        }

        if (_services.Count == 0)
        {
            throw new InvalidOperationException("No fake api service configured.");
        }

        return _services.Dequeue();
    }
}

internal sealed class FakeMcpService : IMcpService
{
    public bool InitializeCalled { get; private set; }
    public Exception? InitializeException { get; set; }
    public Exception? ServerStatusException { get; set; }
    public bool IsAvailableResult { get; set; }
    public int ConnectedServerCount { get; set; }
    public bool ReconnectServerResult { get; set; }
    public bool DisposeCalled { get; private set; }
    public int GetServerStatusCallCount { get; private set; }
    public IList<ModelContextProtocol.Client.McpClientTool> AvailableToolsResult { get; set; }
        = new List<ModelContextProtocol.Client.McpClientTool>();
    public IList<ModelContextProtocol.Client.McpClientTool> ServerToolsResult { get; set; }
        = new List<ModelContextProtocol.Client.McpClientTool>();
    public List<(string Id, string Name, bool IsConnected, int ToolCount)> ServerStatusesResult { get; set; }
        = new();

    public Task InitializeAsync()
    {
        InitializeCalled = true;
        if (InitializeException != null)
        {
            throw InitializeException;
        }

        return Task.CompletedTask;
    }

    public Task<IList<ModelContextProtocol.Client.McpClientTool>> GetAvailableToolsAsync()
        => Task.FromResult(AvailableToolsResult);

    public Task<(bool Success, string Message)> TestConnectionAsync(McpServerConfig serverConfig)
        => Task.FromResult((true, string.Empty));

    public Task<IList<ModelContextProtocol.Client.McpClientTool>> GetServerToolsAsync(string serverId)
        => Task.FromResult(ServerToolsResult);

    public bool IsAvailable() => IsAvailableResult;

    public int GetConnectedServerCount() => ConnectedServerCount;

    public Task<List<(string Id, string Name, bool IsConnected, int ToolCount)>> GetServerStatusAsync()
    {
        GetServerStatusCallCount++;
        if (ServerStatusException != null)
        {
            throw ServerStatusException;
        }

        return Task.FromResult(ServerStatusesResult);
    }

    public Task<bool> ReconnectServerAsync(string serverId)
        => Task.FromResult(ReconnectServerResult);

    public void Dispose()
    {
        DisposeCalled = true;
    }
}

internal sealed class FakeMcpServiceFactory : IMcpServiceFactory
{
    private readonly Queue<IMcpService> _services = new();

    public AppSettings? LastSettings { get; private set; }
    public McpActivityLogger? LastLogger { get; private set; }
    public int CreateCallCount { get; private set; }

    public void Enqueue(IMcpService mcpService)
    {
        _services.Enqueue(mcpService);
    }

    public IMcpService Create(AppSettings settings, McpActivityLogger? logger = null)
    {
        LastSettings = settings;
        LastLogger = logger;
        CreateCallCount++;

        if (_services.Count == 0)
        {
            throw new InvalidOperationException("No fake mcp service configured.");
        }

        return _services.Dequeue();
    }
}

internal sealed class FakeChatHistoryService : IChatHistoryService
{
    private readonly List<ChatSession> _savedSessions = new();

    public ChatSession CurrentSession { get; private set; } = new();

    public IReadOnlyList<ChatSession> SavedSessions => _savedSessions;

    public IReadOnlyList<ChatMessage> CurrentMessages => CurrentSession.Messages;

    public void StartNewSession()
    {
        CurrentSession = new ChatSession();
    }

    public void AddMessage(string role, string content)
    {
        CurrentSession.AddMessage(role, content);
        if (CurrentSession.IsSaved && !_savedSessions.Contains(CurrentSession))
        {
            _savedSessions.Insert(0, CurrentSession);
        }
    }

    public void DeleteSession(string sessionId)
    {
        _savedSessions.RemoveAll(session => session.SessionId == sessionId);
        if (CurrentSession.SessionId == sessionId)
        {
            CurrentSession = new ChatSession();
        }
    }

    public void SetCurrentSession(ChatSession session)
    {
        CurrentSession = session ?? new ChatSession();
    }
}

internal sealed class FakeSettingsService : ISettingsService
{
    public AppSettings LoadResult { get; set; } = new();
    public Exception? LoadException { get; set; }
    public AppSettings? SavedSettings { get; private set; }
    public Exception? SaveException { get; set; }
    public AppSettings? ConnectionTestSettings { get; private set; }
    public bool ConnectionTestResult { get; set; }
    public Exception? ConnectionTestException { get; set; }

    public AppSettings Load()
    {
        if (LoadException != null)
        {
            throw LoadException;
        }

        return LoadResult;
    }

    public Task SaveAsync(AppSettings settings)
    {
        if (SaveException != null)
        {
            throw SaveException;
        }

        SavedSettings = settings;
        return Task.CompletedTask;
    }

    public Task<bool> TestConnectionAsync(AppSettings settings)
    {
        if (ConnectionTestException != null)
        {
            throw ConnectionTestException;
        }

        ConnectionTestSettings = settings;
        return Task.FromResult(ConnectionTestResult);
    }
}

internal sealed class FakeMainWindowHotkeyPlatform : IMainWindowHotkeyPlatform
{
    public IntPtr? AddedHookHwnd { get; private set; }
    public IntPtr? RemovedHookHwnd { get; private set; }
    public IntPtr? RegisteredHotkeyHwnd { get; private set; }
    public IntPtr? UnregisteredHotkeyHwnd { get; private set; }
    public HwndSourceHook? AddedHook { get; private set; }
    public HwndSourceHook? RemovedHook { get; private set; }
    public int RegisterHotKeyId { get; private set; }
    public uint RegisterModifiers { get; private set; }
    public uint RegisterVirtualKey { get; private set; }
    public int UnregisterHotKeyId { get; private set; }
    public int AddHookCallCount { get; private set; }
    public int RemoveHookCallCount { get; private set; }
    public int RegisterHotKeyCallCount { get; private set; }
    public int UnregisterHotKeyCallCount { get; private set; }
    public bool RegisterHotKeyResult { get; set; } = true;
    public bool UnregisterHotKeyResult { get; set; } = true;

    public void AddHook(IntPtr hwnd, HwndSourceHook hook)
    {
        AddedHookHwnd = hwnd;
        AddedHook = hook;
        AddHookCallCount++;
    }

    public void RemoveHook(IntPtr hwnd, HwndSourceHook hook)
    {
        RemovedHookHwnd = hwnd;
        RemovedHook = hook;
        RemoveHookCallCount++;
    }

    public bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint virtualKey)
    {
        RegisteredHotkeyHwnd = hwnd;
        RegisterHotKeyId = id;
        RegisterModifiers = modifiers;
        RegisterVirtualKey = virtualKey;
        RegisterHotKeyCallCount++;
        return RegisterHotKeyResult;
    }

    public bool UnregisterHotKey(IntPtr hwnd, int id)
    {
        UnregisteredHotkeyHwnd = hwnd;
        UnregisterHotKeyId = id;
        UnregisterHotKeyCallCount++;
        return UnregisterHotKeyResult;
    }

    public void RaiseWindowMessage(int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (AddedHook is null)
        {
            throw new InvalidOperationException("No hook has been added.");
        }

        AddedHook(IntPtr.Zero, msg, wParam, lParam, ref handled);
    }
}

internal sealed class FakeMainWindowCommandPlatform : IMainWindowCommandPlatform
{
    public int OpenNotepadCallCount { get; private set; }
    public int LockWorkstationCallCount { get; private set; }
    public int OpenChatWindowCallCount { get; private set; }
    public int OpenSettingsWindowCallCount { get; private set; }
    public Window? ChatWindowOwner { get; private set; }
    public Window? SettingsWindowOwner { get; private set; }
    public List<ChatMessage>? ChatInitialMessages { get; private set; }
    public Action? ChatClosedCallback { get; private set; }
    public string? LastErrorMessage { get; private set; }
    public string? LastErrorTitle { get; private set; }
    public Exception? OpenChatWindowException { get; set; }
    public Exception? OpenSettingsWindowException { get; set; }

    public void OpenNotepad()
    {
        OpenNotepadCallCount++;
    }

    public void LockWorkstation()
    {
        LockWorkstationCallCount++;
    }

    public void OpenChatWindow(Window owner, List<ChatMessage> initialMessages, Action onClosed)
    {
        if (OpenChatWindowException != null)
        {
            throw OpenChatWindowException;
        }

        OpenChatWindowCallCount++;
        ChatWindowOwner = owner;
        ChatInitialMessages = initialMessages;
        ChatClosedCallback = onClosed;
    }

    public void OpenSettingsWindow(Window owner)
    {
        if (OpenSettingsWindowException != null)
        {
            throw OpenSettingsWindowException;
        }

        OpenSettingsWindowCallCount++;
        SettingsWindowOwner = owner;
    }

    public void ShowError(string message, string title)
    {
        LastErrorMessage = message;
        LastErrorTitle = title;
    }

    public void RaiseChatClosed()
    {
        ChatClosedCallback?.Invoke();
    }
}