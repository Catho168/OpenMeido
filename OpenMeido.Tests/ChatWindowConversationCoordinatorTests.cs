using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class ChatWindowConversationCoordinatorTests
{
    [Fact]
    public void InitializeChatServiceAsync_InitializesViewModel_AndRefreshesMcpStatus()
    {
        RunInStaAsync(async () =>
        {
            var chatService = new FakeChatService
            {
                InitializeResult = ChatServiceInitializationResult.Warning("需要配置API")
            };
            var context = CreateContext(chatService: chatService);

            await context.Coordinator.InitializeChatServiceAsync();

            Assert.Equal("需要配置API", context.ViewModel.StatusText);
            Assert.Equal(1, context.GetRefreshMcpStatusCallCount());
        });
    }

    [Fact]
    public void OpenSettingsAsync_WhenDialogAccepted_ReinitializesViewModel_AndRefreshesMcpStatus()
    {
        RunInStaAsync(async () =>
        {
            var chatService = new FakeChatService
            {
                ReinitializeResult = ChatServiceInitializationResult.Ready("已重新加载")
            };
            var platform = new FakeChatWindowConversationPlatform
            {
                OpenSettingsDialogResult = true
            };
            var context = CreateContext(chatService: chatService, platform: platform);

            await context.Coordinator.OpenSettingsAsync();

            Assert.Equal(1, platform.OpenSettingsDialogCallCount);
            Assert.Same(context.Owner, platform.SettingsDialogOwner);
            Assert.Equal("已重新加载", context.ViewModel.StatusText);
            Assert.Equal(1, context.GetRefreshMcpStatusCallCount());
        });
    }

    [Fact]
    public void SendMessageAsync_WhenApiNotConfigured_PromptsAndOpensSettingsWithoutAddingMessages()
    {
        RunInStaAsync(async () =>
        {
            var platform = new FakeChatWindowConversationPlatform
            {
                OpenSettingsDialogResult = false
            };
            var context = CreateContext(platform: platform);
            context.ViewModel.InputText = "你好";

            await context.Coordinator.SendMessageAsync();

            Assert.Equal(1, platform.ShowMessageCallCount);
            Assert.Equal("设置缺失", platform.LastTitle);
            Assert.Equal("需要先设置API信息，妹抖酱才能和你聊天哦~", platform.LastMessage);
            Assert.Equal(1, platform.OpenSettingsDialogCallCount);
            Assert.Empty(context.ViewModel.CurrentMessages);
            Assert.Empty(context.DisplayMessages);
            Assert.Equal(0, context.GetFocusInputCallCount());
        });
    }

    [Fact]
    public void SendMessageAsync_WhenResponseSuccessful_RendersMessages_RefreshesHistory_AndFocusesInput()
    {
        RunInStaAsync(async () =>
        {
            var chatService = new FakeChatService
            {
                CurrentApiService = new FakeApiService(),
                SendMessageResult = "收到"
            };
            var context = CreateContext(chatService: chatService);
            context.ViewModel.InputText = "  你好  ";

            await context.Coordinator.SendMessageAsync();

            Assert.Equal(2, context.DisplayMessages.Count);
            Assert.Equal("你好", context.DisplayMessages[0].Text);
            Assert.Equal("收到", context.DisplayMessages[1].Text);
            Assert.Equal(2, context.ViewModel.CurrentMessages.Count);
            Assert.Single(chatService.LastMessagesHistory);
            Assert.Equal("你好", chatService.LastMessagesHistory[0].Content);
            Assert.Equal(1, context.GetRefreshHistoryCallCount());
            Assert.Equal(1, context.GetFocusInputCallCount());
            Assert.Equal("就绪", context.ViewModel.StatusText);
            Assert.False(context.ViewModel.IsBusy);
        });
    }

    [Fact]
    public void SendMessageAsync_WhenResponseIsRequestFailure_RendersErrorMessage_AndMarksRequestFailed()
    {
        RunInStaAsync(async () =>
        {
            var chatService = new FakeChatService
            {
                CurrentApiService = new FakeApiService(),
                SendMessageResult = "API请求失败: boom"
            };
            var context = CreateContext(chatService: chatService);
            context.ViewModel.InputText = "你好";

            await context.Coordinator.SendMessageAsync();

            Assert.Equal(2, context.DisplayMessages.Count);
            Assert.Contains("❌ 请求失败", context.DisplayMessages[1].Text);
            Assert.Single(context.ViewModel.CurrentMessages);
            Assert.Equal("请求失败", context.ViewModel.StatusText);
            Assert.Equal(0, context.GetRefreshHistoryCallCount());
            Assert.Equal(1, context.GetFocusInputCallCount());
        });
    }

    [Fact]
    public void ClearCurrentConversation_WhenConfirmed_ResetsView_AndStartsNewSession()
    {
        RunInSta(() =>
        {
            var platform = new FakeChatWindowConversationPlatform
            {
                ShowMessageResult = MessageBoxResult.Yes
            };
            var context = CreateContext(platform: platform);
            context.ViewModel.AddUserMessage("旧消息");
            context.MessageDisplayCoordinator.AddUserMessage("旧消息");

            context.Coordinator.ClearCurrentConversation();

            Assert.Equal("确认清空", platform.LastTitle);
            Assert.Equal("与妹抖酱的对话", context.ViewModel.CurrentSessionTitle);
            var item = Assert.Single(context.DisplayMessages);
            Assert.Equal("欢迎回来", item.Text);
            Assert.True(item.IsWelcome);
            Assert.Empty(context.ViewModel.CurrentMessages);
            Assert.Equal(0, context.GetCollapseHistoryCallCount());
        });
    }

    [Fact]
    public void LoadHistorySession_ReplaysMessages_LoadsSession_AndCollapsesHistory()
    {
        RunInSta(() =>
        {
            var session = new ChatSession();
            session.AddMessage("user", "第一句");
            session.AddMessage("assistant", "收到");
            var context = CreateContext();

            context.Coordinator.LoadHistorySession(session);

            Assert.Equal(2, context.DisplayMessages.Count);
            Assert.Equal("第一句", context.DisplayMessages[0].Text);
            Assert.Equal("收到", context.DisplayMessages[1].Text);
            Assert.Same(session, context.ViewModel.CurrentSession);
            Assert.Equal(1, context.GetCollapseHistoryCallCount());
            Assert.Equal(session.Title, context.ViewModel.CurrentSessionTitle);
        });
    }

    [Fact]
    public void AppendMiniChatHistory_ReplaysMessagesIntoViewModel_AndMessagePanel()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            List<ChatMessage> history =
            [
                new("user", "你好"),
                new("assistant", "欢迎")
            ];

            context.Coordinator.AppendMiniChatHistory(history);

            Assert.Equal(2, context.DisplayMessages.Count);
            Assert.Equal(2, context.ViewModel.CurrentMessages.Count);
            Assert.Equal("你好", context.DisplayMessages[0].Text);
            Assert.Equal("欢迎", context.DisplayMessages[1].Text);
        });
    }

    private static TestContext CreateContext(FakeChatService? chatService = null, FakeChatWindowConversationPlatform? platform = null)
    {
        EnsureApplication();

        var owner = new Window();
        var viewModel = new ChatViewModel(chatService ?? new FakeChatService(), new FakeChatHistoryService());
        var scrollViewer = new ScrollViewer
        {
            Content = new ItemsControl { ItemsSource = viewModel.DisplayMessages }
        };
        var messageDisplayCoordinator = new ChatWindowMessageDisplayCoordinator(owner, viewModel.DisplayMessages, scrollViewer);
        platform ??= new FakeChatWindowConversationPlatform();
        var refreshMcpStatusCallCount = 0;
        var refreshHistoryCallCount = 0;
        var collapseHistoryCallCount = 0;
        var focusInputCallCount = 0;

        var coordinator = new ChatWindowConversationCoordinator(
            owner,
            viewModel,
            messageDisplayCoordinator,
            () =>
            {
                refreshMcpStatusCallCount++;
                return Task.CompletedTask;
            },
            () => refreshHistoryCallCount++,
            () => collapseHistoryCallCount++,
            () => focusInputCallCount++,
            "欢迎回来",
            platform);

        return new TestContext(
            owner,
            viewModel,
            messageDisplayCoordinator,
            coordinator,
            platform,
            () => refreshMcpStatusCallCount,
            () => refreshHistoryCallCount,
            () => collapseHistoryCallCount,
            () => focusInputCallCount);
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

    private sealed class FakeChatWindowConversationPlatform : IChatWindowConversationPlatform
    {
        public int ShowMessageCallCount { get; private set; }

        public string? LastMessage { get; private set; }

        public string? LastTitle { get; private set; }

        public MessageBoxButton LastButtons { get; private set; }

        public MessageBoxImage LastImage { get; private set; }

        public MessageBoxResult ShowMessageResult { get; set; } = MessageBoxResult.OK;

        public int OpenSettingsDialogCallCount { get; private set; }

        public bool? OpenSettingsDialogResult { get; set; }

        public Window? SettingsDialogOwner { get; private set; }

        public MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            ShowMessageCallCount++;
            LastMessage = message;
            LastTitle = title;
            LastButtons = buttons;
            LastImage = image;
            return ShowMessageResult;
        }

        public bool? OpenSettingsDialog(Window owner)
        {
            OpenSettingsDialogCallCount++;
            SettingsDialogOwner = owner;
            return OpenSettingsDialogResult;
        }
    }

    private sealed class TestContext(
        Window owner,
        ChatViewModel viewModel,
        ChatWindowMessageDisplayCoordinator messageDisplayCoordinator,
        ChatWindowConversationCoordinator coordinator,
        FakeChatWindowConversationPlatform platform,
        Func<int> getRefreshMcpStatusCallCount,
        Func<int> getRefreshHistoryCallCount,
        Func<int> getCollapseHistoryCallCount,
        Func<int> getFocusInputCallCount)
    {
        public Window Owner { get; } = owner;

        public ChatViewModel ViewModel { get; } = viewModel;

        public ObservableCollection<ChatMessageDisplayItemViewModel> DisplayMessages => ViewModel.DisplayMessages;

        public ChatWindowMessageDisplayCoordinator MessageDisplayCoordinator { get; } = messageDisplayCoordinator;

        public ChatWindowConversationCoordinator Coordinator { get; } = coordinator;

        public FakeChatWindowConversationPlatform Platform { get; } = platform;

        public Func<int> GetRefreshMcpStatusCallCount { get; } = getRefreshMcpStatusCallCount;

        public Func<int> GetRefreshHistoryCallCount { get; } = getRefreshHistoryCallCount;

        public Func<int> GetCollapseHistoryCallCount { get; } = getCollapseHistoryCallCount;

        public Func<int> GetFocusInputCallCount { get; } = getFocusInputCallCount;
    }
}