using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class ChatWindowMessageDisplayCoordinatorTests
{
    [Fact]
    public void AddUserMessage_AddsUserMessageElement()
    {
        RunInSta(() =>
        {
            var context = CreateContext();

            context.Coordinator.AddUserMessage("你好");

            var item = Assert.Single(context.DisplayMessages);
            Assert.Equal("你好", item.Text);
            Assert.True(item.IsUser);
            Assert.False(item.IsToolCall);
        });
    }

    [Fact]
    public void AddAiMessage_WithToolCall_AddsInteractiveBar_AndInvokesDetailsCallback()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            const string toolCallMessage = "TOOL_CALL_START: web_search\nTOOL_PARAMS: {\"query\":\"天气\"}\nTOOL_RESULT_SUCCESS: 晴天\nTOOL_CALL_END";

            context.Coordinator.AddAiMessage(toolCallMessage);

            var item = Assert.Single(context.DisplayMessages);
            Assert.True(item.IsToolCall);
            Assert.True(item.CanShowDetails);
            Assert.Contains("web_search", item.DisplayText);

            Assert.NotNull(item.ShowDetailsCommand);
            item.ShowDetailsCommand.Execute(null);

            Assert.Equal(1, context.ShowDetailsCallCount);
            Assert.Equal("web_search", context.LastToolName);
            Assert.Equal("{\"query\":\"天气\"}", context.LastParameters);
            Assert.Equal("晴天", context.LastResult);
            Assert.True(context.LastIsSuccess);
        });
    }

    [Fact]
    public void ResetConversationView_ClearsExistingMessages_AndAddsWelcomeMessage()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            context.Coordinator.AddUserMessage("旧消息");

            context.Coordinator.ResetConversationView("欢迎回来");

            var item = Assert.Single(context.DisplayMessages);
            Assert.Equal("欢迎回来", item.Text);
            Assert.True(item.IsWelcome);
        });
    }

    [Fact]
    public void ReplayMessages_RendersConversation_AndInvokesSyncCallbacksOncePerSourceMessage()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            var replayedUsers = new List<string>();
            var replayedAssistants = new List<string>();
            var separator = new string('\\', 3);

            context.Coordinator.ReplayMessages(
                new[]
                {
                    new ChatMessage("user", "你好"),
                    new ChatMessage("assistant", $"第一句{separator}第二句"),
                    new ChatMessage("assistant", string.Empty)
                },
                replayedUsers.Add,
                replayedAssistants.Add);

            Assert.Equal(3, context.DisplayMessages.Count);
            Assert.Equal("你好", context.DisplayMessages[0].Text);
            Assert.Equal("第一句", context.DisplayMessages[1].Text);
            Assert.Equal("第二句", context.DisplayMessages[2].Text);
            Assert.Single(replayedUsers);
            Assert.Single(replayedAssistants);
            Assert.Equal("你好", replayedUsers[0]);
            Assert.Equal($"第一句{separator}第二句", replayedAssistants[0]);
        });
    }

    [Fact]
    public void AddAiMessageWithDelayAsync_SplitsSentencesIntoMultipleMessages()
    {
        RunInStaAsync(async () =>
        {
            var context = CreateContext();
            var separator = new string('\\', 3);

            await context.Coordinator.AddAiMessageWithDelayAsync($"第一句{separator}第二句");

            Assert.Equal(2, context.DisplayMessages.Count);
            Assert.Equal("第一句", context.DisplayMessages[0].Text);
            Assert.Equal("第二句", context.DisplayMessages[1].Text);
        });
    }

    private static TestContext CreateContext()
    {
        EnsureApplication();

        var owner = new Window();
        var displayMessages = new ObservableCollection<ChatMessageDisplayItemViewModel>();
        var scrollViewer = new ScrollViewer
        {
            Content = new ItemsControl { ItemsSource = displayMessages }
        };
        var showDetailsCallCount = 0;
        string? lastToolName = null;
        string? lastParameters = null;
        string? lastResult = null;
        var lastIsSuccess = false;

        var coordinator = new ChatWindowMessageDisplayCoordinator(
            owner,
            displayMessages,
            scrollViewer,
            (toolName, parameters, result, isSuccess) =>
            {
                showDetailsCallCount++;
                lastToolName = toolName;
                lastParameters = parameters;
                lastResult = result;
                lastIsSuccess = isSuccess;
            });

        return new TestContext(
            displayMessages,
            coordinator,
            () => showDetailsCallCount,
            () => lastToolName,
            () => lastParameters,
            () => lastResult,
            () => lastIsSuccess);
    }

    private static void EnsureApplication()
    {
        _ = Application.Current ?? new Application();
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private static void RunInStaAsync(Func<Task> action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            try
            {
                var task = action();
                task.ContinueWith(
                    _ => dispatcher.BeginInvokeShutdown(DispatcherPriority.Background),
                    TaskScheduler.Default);
                Dispatcher.Run();
                task.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private sealed class TestContext(
        ObservableCollection<ChatMessageDisplayItemViewModel> displayMessages,
        ChatWindowMessageDisplayCoordinator coordinator,
        Func<int> getShowDetailsCallCount,
        Func<string?> getLastToolName,
        Func<string?> getLastParameters,
        Func<string?> getLastResult,
        Func<bool> getLastIsSuccess)
    {
        public ObservableCollection<ChatMessageDisplayItemViewModel> DisplayMessages { get; } = displayMessages;
        public ChatWindowMessageDisplayCoordinator Coordinator { get; } = coordinator;
        public int ShowDetailsCallCount => getShowDetailsCallCount();
        public string? LastToolName => getLastToolName();
        public string? LastParameters => getLastParameters();
        public string? LastResult => getLastResult();
        public bool LastIsSuccess => getLastIsSuccess();
    }
}