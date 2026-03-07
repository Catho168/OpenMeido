using System;
using System.IO;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class ChatHistoryServiceTests
{
    [Fact]
    public void StartNewSession_CreatesANewCurrentSession()
    {
        RunIsolated(service =>
        {
            var originalSessionId = service.CurrentSession.SessionId;

            service.StartNewSession();

            Assert.NotEqual(originalSessionId, service.CurrentSession.SessionId);
            Assert.Empty(service.CurrentMessages);
        });
    }

    [Fact]
    public void AddAssistantMessage_UpdatesCurrentMessages_WithoutSavingCurrentSession()
    {
        RunIsolated(service =>
        {
            service.AddMessage("assistant", "你好");

            Assert.Single(service.CurrentMessages);
            Assert.Equal("assistant", service.CurrentMessages[0].Role);
            Assert.Equal("你好", service.CurrentMessages[0].Content);
            Assert.False(service.CurrentSession.IsSaved);
        });
    }

    [Fact]
    public void SetCurrentSession_Null_FallsBackToNewSession()
    {
        RunIsolated(service =>
        {
            service.SetCurrentSession(null);

            Assert.NotNull(service.CurrentSession);
            Assert.False(string.IsNullOrWhiteSpace(service.CurrentSession.SessionId));
            Assert.Empty(service.CurrentMessages);
        });
    }

    private static void RunIsolated(Action<ChatHistoryService> assertion)
    {
        var originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        var tempAppData = Path.Combine(Path.GetTempPath(), "OpenMeido.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempAppData);

        try
        {
            Environment.SetEnvironmentVariable("APPDATA", tempAppData);
            assertion(new ChatHistoryService());
        }
        finally
        {
            Environment.SetEnvironmentVariable("APPDATA", originalAppData);
            if (Directory.Exists(tempAppData))
            {
                Directory.Delete(tempAppData, true);
            }
        }
    }
}