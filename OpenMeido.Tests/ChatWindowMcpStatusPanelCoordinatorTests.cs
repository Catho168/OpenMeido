using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using OpenMeido.Infrastructure;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Tests;

public sealed class ChatWindowMcpStatusPanelCoordinatorTests
{
    [Fact]
    public void Initialize_WhenApiServiceUnavailable_RendersUnavailableState()
    {
        RunInSta(() =>
        {
            IApiService? currentApiService = null;
            var context = CreateContext(() => currentApiService);

            context.Coordinator.Initialize();

            Assert.Equal("MCP功能未启用或未配置", GetText(context.McpServersPanel, 0));
            Assert.Equal("无可用工具", GetText(context.McpToolsPanel, 0));
            Assert.Equal("无活动记录", GetText(context.McpActivityPanel, 0));

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void Toggle_WhenCollapsed_ExpandsPanel_CollapsesHistory_AndRefreshes()
    {
        RunInSta(() =>
        {
            var apiService = new FakeApiService
            {
                McpServerStatusesResult = new List<(string Id, string Name, bool IsConnected, int ToolCount)>
                {
                    ("1", "文件", true, 2)
                }
            };
            var collapseHistoryCallCount = 0;
            var context = CreateContext(() => apiService, () => collapseHistoryCallCount++);

            context.Coordinator.Toggle();

            Assert.Equal(200, context.McpStatusPanel.Height);
            Assert.Equal(1, collapseHistoryCallCount);
            Assert.Contains("文件", GetAllText(context.McpServersPanel.Children[0]));
            Assert.Equal(1, apiService.GetMcpServerStatusesCallCount);
            Assert.Equal(Visibility.Visible, context.McpStatusPanelHost.Visibility);
            Assert.True(context.McpStatusPanelHost.IsHitTestVisible);

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void HideIfVisible_WhenExpanded_CollapsesHost_AndDisablesHitTesting()
    {
        RunInSta(() =>
        {
            var apiService = new FakeApiService();
            var context = CreateContext(() => apiService);

            context.Coordinator.Toggle();
            context.Coordinator.HideIfVisible();

            Assert.Equal(0, context.McpStatusPanel.Height);
            Assert.Equal(Visibility.Collapsed, context.McpStatusPanelHost.Visibility);
            Assert.False(context.McpStatusPanelHost.IsHitTestVisible);

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void Initialize_KeepsHostCollapsed_AndNonHitTestVisible()
    {
        RunInSta(() =>
        {
            var context = CreateContext(() => null);

            context.Coordinator.Initialize();

            Assert.Equal(Visibility.Collapsed, context.McpStatusPanelHost.Visibility);
            Assert.False(context.McpStatusPanelHost.IsHitTestVisible);

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void RefreshAsync_WhenApiServiceHasData_RendersServersToolsAndActivities()
    {
        RunInStaAsync(async () =>
        {
            var apiService = new FakeApiService
            {
                McpServerStatusesResult = new List<(string Id, string Name, bool IsConnected, int ToolCount)>
                {
                    ("1", "文件", true, 2)
                },
                RecentMcpActivitiesResult = new List<McpActivityRecord>
                {
                    new()
                    {
                        Timestamp = new DateTime(2024, 1, 1, 10, 0, 0),
                        ActivityType = "ToolCallComplete",
                        ToolName = "search",
                        IsSuccess = true,
                        ExecutionTimeMs = 12
                    }
                },
                McpActivityStatisticsResult = new McpActivityStatistics
                {
                    TotalToolCalls = 1,
                    SuccessfulCalls = 1,
                    FailedCalls = 0,
                    AverageExecutionTime = 12
                }
            };
            var context = CreateContext(() => apiService);

            await context.Coordinator.RefreshAsync();

            Assert.Contains("文件", GetAllText(context.McpServersPanel.Children[0]));
            Assert.Equal("无可用工具", GetText(context.McpToolsPanel, 0));
            Assert.Equal(3, context.McpActivityPanel.Children.Count);
            Assert.Contains("总调用: 1", GetAllText(context.McpActivityPanel.Children[0]));
            Assert.Contains("search 执行完成", GetAllText(context.McpActivityPanel.Children[2]));

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void ClearActivityLog_WhenApiServiceAvailable_ClearsActivitiesAndRendersEmptyState()
    {
        RunInSta(() =>
        {
            var apiService = new FakeApiService
            {
                RecentMcpActivitiesResult = new List<McpActivityRecord>
                {
                    new() { Timestamp = DateTime.Now, ActivityType = "ServerConnection", ServerName = "文件", IsSuccess = true }
                },
                McpActivityStatisticsResult = new McpActivityStatistics { TotalToolCalls = 1, SuccessfulCalls = 1 }
            };
            var context = CreateContext(() => apiService);

            context.Coordinator.ClearActivityLog();

            Assert.Equal(1, apiService.ClearMcpActivitiesCallCount);
            Assert.Equal("暂无活动记录", GetText(context.McpActivityPanel, 0));

            context.Coordinator.Dispose();
        });
    }

    [Fact]
    public void RefreshAsync_WhenStatusRefreshFails_RendersErrorState()
    {
        RunInStaAsync(async () =>
        {
            var apiService = new FakeApiService
            {
                McpServerStatusesException = new InvalidOperationException("boom")
            };
            var context = CreateContext(() => apiService);

            await context.Coordinator.RefreshAsync();

            Assert.Equal("MCP状态获取失败: boom", GetText(context.McpServersPanel, 0));
            Assert.Equal("无可用工具", GetText(context.McpToolsPanel, 0));
            Assert.Equal("无活动记录", GetText(context.McpActivityPanel, 0));

            context.Coordinator.Dispose();
        });
    }

    private static TestContext CreateContext(Func<IApiService?> getCurrentApiService, Action? collapseHistoryPanel = null)
    {
        EnsureApplication();

        var mcpStatusPanelHost = new Grid();
        var mcpStatusPanel = new Border { Height = 0 };
        var mcpServersPanel = new StackPanel();
        var mcpToolsPanel = new StackPanel();
        var mcpActivityPanel = new StackPanel();

        var coordinator = new ChatWindowMcpStatusPanelCoordinator(
            mcpStatusPanel,
            mcpServersPanel,
            mcpToolsPanel,
            mcpActivityPanel,
            () => getCurrentApiService(),
            collapseHistoryPanel ?? (() => { }),
            animatePanel: (panel, targetHeight, onCompleted) =>
            {
                panel.Height = targetHeight;
                onCompleted?.Invoke();
            },
            mcpStatusPanelHost: mcpStatusPanelHost);

        return new TestContext(mcpStatusPanelHost, mcpStatusPanel, mcpServersPanel, mcpToolsPanel, mcpActivityPanel, coordinator);
    }

    private static string GetText(StackPanel panel, int index)
    {
        return Assert.IsType<TextBlock>(panel.Children[index]).Text;
    }

    private static string GetAllText(object element)
    {
        return element switch
        {
            TextBlock textBlock => textBlock.Text,
            Border border when border.Child != null => GetAllText(border.Child),
            Panel panel => string.Join(" | ", GetPanelTexts(panel)),
            _ => throw new InvalidOperationException($"Unsupported element type: {element.GetType().Name}")
        };
    }

    private static IEnumerable<string> GetPanelTexts(Panel panel)
    {
        foreach (UIElement child in panel.Children)
        {
            yield return GetAllText(child);
        }
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
        Grid mcpStatusPanelHost,
        Border mcpStatusPanel,
        StackPanel mcpServersPanel,
        StackPanel mcpToolsPanel,
        StackPanel mcpActivityPanel,
        ChatWindowMcpStatusPanelCoordinator coordinator)
    {
        public Grid McpStatusPanelHost { get; } = mcpStatusPanelHost;
        public Border McpStatusPanel { get; } = mcpStatusPanel;
        public StackPanel McpServersPanel { get; } = mcpServersPanel;
        public StackPanel McpToolsPanel { get; } = mcpToolsPanel;
        public StackPanel McpActivityPanel { get; } = mcpActivityPanel;
        public ChatWindowMcpStatusPanelCoordinator Coordinator { get; } = coordinator;
    }
}