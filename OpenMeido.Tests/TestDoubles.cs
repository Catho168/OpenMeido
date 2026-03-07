using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Tests;

internal sealed class FakeChatService : IChatService
{
    public ApiService CurrentApiService { get; set; } = null!;
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