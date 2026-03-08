using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenMeido.Infrastructure;
using OpenMeido.Models;

namespace OpenMeido.Tests;

public sealed class ChatWindowHistoryPanelCoordinatorTests
{
    [Fact]
    public void Initialize_RendersSavedSessions_AndUpdatesCurrentSessionTitle()
    {
        RunInSta(() =>
        {
            var sessions = new List<ChatSession>
            {
                CreateSession("会话一"),
                CreateSession("会话二")
            };
            var titleUpdateCallCount = 0;
            var context = CreateContext(
                sessions,
                updateCurrentSessionTitle: () => titleUpdateCallCount++);

            context.Coordinator.Initialize();

            Assert.Equal(2, context.HistoryItemsPanel.Children.Count);
            Assert.Equal("会话一", GetHistoryItemTitle(context.HistoryItemsPanel, 0));
            Assert.Equal("会话二", GetHistoryItemTitle(context.HistoryItemsPanel, 1));
            Assert.Equal(1, titleUpdateCallCount);
        });
    }

    [Fact]
    public void Toggle_WhenCollapsed_ExpandsPanel_UpdatesIcon_AndRefreshesItems()
    {
        RunInSta(() =>
        {
            var sessions = new List<ChatSession> { CreateSession("会话一") };
            var collapseMcpCallCount = 0;
            var context = CreateContext(
                sessions,
                collapseMcpStatusPanel: () => collapseMcpCallCount++);

            context.Coordinator.Toggle();

            Assert.Equal(200, context.HistoryPanel.Height);
            Assert.Equal("📂", context.HistoryToggleIcon.Text);
            Assert.Single(context.HistoryItemsPanel.Children);
            Assert.Equal(Visibility.Visible, context.HistoryPanelHost.Visibility);
            Assert.True(context.HistoryPanelHost.IsHitTestVisible);
            Assert.Equal(1, collapseMcpCallCount);
        });
    }

    [Fact]
    public void CollapseIfExpanded_WhenExpanded_CollapsesPanel_AndUpdatesIcon()
    {
        RunInSta(() =>
        {
            var context = CreateContext(new List<ChatSession> { CreateSession("会话一") });
            context.Coordinator.Toggle();

            context.Coordinator.CollapseIfExpanded();

            Assert.Equal(0, context.HistoryPanel.Height);
            Assert.Equal("📁", context.HistoryToggleIcon.Text);
            Assert.Equal(Visibility.Collapsed, context.HistoryPanelHost.Visibility);
            Assert.False(context.HistoryPanelHost.IsHitTestVisible);
        });
    }

    [Fact]
    public void Initialize_KeepsHostCollapsed_AndNonHitTestVisible()
    {
        RunInSta(() =>
        {
            var context = CreateContext(new List<ChatSession> { CreateSession("会话一") });

            context.Coordinator.Initialize();

            Assert.Equal(Visibility.Collapsed, context.HistoryPanelHost.Visibility);
            Assert.False(context.HistoryPanelHost.IsHitTestVisible);
            Assert.Equal("📁", context.HistoryToggleIcon.Text);
        });
    }

    [Fact]
    public void DeleteConfirmed_WhenDeleteButtonClicked_DeletesSession_AndRefreshesItems()
    {
        RunInSta(() =>
        {
            var sessions = new List<ChatSession>
            {
                CreateSession("会话一"),
                CreateSession("会话二")
            };
            var deletedSessionIds = new List<string>();
            var context = CreateContext(
                sessions,
                deleteSession: sessionId =>
                {
                    deletedSessionIds.Add(sessionId);
                    sessions.RemoveAll(session => session.SessionId == sessionId);
                },
                confirmDeleteSession: _ => true);

            context.Coordinator.Initialize();
            GetDeleteButton(context.HistoryItemsPanel, 0).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Single(deletedSessionIds);
            Assert.Single(context.HistoryItemsPanel.Children);
            Assert.Equal("会话二", GetHistoryItemTitle(context.HistoryItemsPanel, 0));
        });
    }

    [Fact]
    public void HistoryItemClick_InvokesLoadSessionCallback()
    {
        RunInSta(() =>
        {
            var session = CreateSession("会话一");
            ChatSession? loadedSession = null;
            var context = CreateContext(
                new List<ChatSession> { session },
                loadSession: value => loadedSession = value);

            context.Coordinator.Initialize();
            GetHistoryItemBorder(context.HistoryItemsPanel, 0).RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent
            });

            Assert.Same(session, loadedSession);
        });
    }

    private static TestContext CreateContext(
        List<ChatSession> sessions,
        Action<ChatSession>? loadSession = null,
        Action<string>? deleteSession = null,
        Action? updateCurrentSessionTitle = null,
        Func<ChatSession, bool>? confirmDeleteSession = null,
        Action? collapseMcpStatusPanel = null)
    {
        EnsureApplication();

        var owner = new Window();
        var historyPanelHost = new Grid();
        var historyPanel = new Border { Height = 0 };
        var historyItemsPanel = new StackPanel();
        var historyToggleIcon = new TextBlock { Text = "📁" };

        var coordinator = new ChatWindowHistoryPanelCoordinator(
            owner,
            historyPanel,
            historyItemsPanel,
            historyToggleIcon,
            () => sessions,
            loadSession ?? (_ => { }),
            deleteSession ?? (_ => { }),
            updateCurrentSessionTitle ?? (() => { }),
            confirmDeleteSession,
            (panel, targetHeight, onCompleted) =>
            {
                panel.Height = targetHeight;
                onCompleted?.Invoke();
            },
            historyPanelHost,
            collapseMcpStatusPanel ?? (() => { }));

        return new TestContext(historyPanelHost, historyPanel, historyItemsPanel, historyToggleIcon, coordinator);
    }

    private static ChatSession CreateSession(string title)
    {
        return new ChatSession
        {
            Title = title,
            IsSaved = true
        };
    }

    private static string GetHistoryItemTitle(StackPanel historyItemsPanel, int index)
    {
        var border = GetHistoryItemBorder(historyItemsPanel, index);
        var grid = Assert.IsType<Grid>(border.Child);
        return Assert.IsType<TextBlock>(grid.Children[0]).Text;
    }

    private static Border GetHistoryItemBorder(StackPanel historyItemsPanel, int index)
    {
        return Assert.IsType<Border>(historyItemsPanel.Children[index]);
    }

    private static Button GetDeleteButton(StackPanel historyItemsPanel, int index)
    {
        var border = GetHistoryItemBorder(historyItemsPanel, index);
        var grid = Assert.IsType<Grid>(border.Child);
        return Assert.IsType<Button>(grid.Children[1]);
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

    private sealed class TestContext(
        Grid historyPanelHost,
        Border historyPanel,
        StackPanel historyItemsPanel,
        TextBlock historyToggleIcon,
        ChatWindowHistoryPanelCoordinator coordinator)
    {
        public Grid HistoryPanelHost { get; } = historyPanelHost;
        public Border HistoryPanel { get; } = historyPanel;
        public StackPanel HistoryItemsPanel { get; } = historyItemsPanel;
        public TextBlock HistoryToggleIcon { get; } = historyToggleIcon;
        public ChatWindowHistoryPanelCoordinator Coordinator { get; } = coordinator;
    }
}