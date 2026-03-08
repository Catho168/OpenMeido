using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ModelContextProtocol.Client;
using OpenMeido.Helpers;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Infrastructure
{
    public sealed class ChatWindowMcpStatusPanelCoordinator : IDisposable
    {
        private const double ExpandedHeight = 200;

        private readonly FrameworkElement _mcpStatusPanelHost;
        private readonly Border _mcpStatusPanel;
        private readonly Panel _mcpServersPanel;
        private readonly Panel _mcpToolsPanel;
        private readonly Panel _mcpActivityPanel;
        private readonly Func<IApiService> _getCurrentApiService;
        private readonly Action _collapseHistoryPanel;
        private readonly Action<Border, double, Action> _animatePanel;
        private readonly DispatcherTimer _refreshTimer;

        private bool _isVisible;
        private bool _isRefreshing;
        private bool _isDisposed;

        public ChatWindowMcpStatusPanelCoordinator(
            Border mcpStatusPanel,
            Panel mcpServersPanel,
            Panel mcpToolsPanel,
            Panel mcpActivityPanel,
            Func<IApiService> getCurrentApiService,
            Action collapseHistoryPanel,
            TimeSpan? refreshInterval = null,
            Action<Border, double, Action> animatePanel = null,
            FrameworkElement mcpStatusPanelHost = null)
        {
            ArgumentNullException.ThrowIfNull(mcpStatusPanel);
            ArgumentNullException.ThrowIfNull(mcpServersPanel);
            ArgumentNullException.ThrowIfNull(mcpToolsPanel);
            ArgumentNullException.ThrowIfNull(mcpActivityPanel);
            ArgumentNullException.ThrowIfNull(getCurrentApiService);
            ArgumentNullException.ThrowIfNull(collapseHistoryPanel);

            _mcpStatusPanelHost = mcpStatusPanelHost ?? mcpStatusPanel;
            _mcpStatusPanel = mcpStatusPanel;
            _mcpServersPanel = mcpServersPanel;
            _mcpToolsPanel = mcpToolsPanel;
            _mcpActivityPanel = mcpActivityPanel;
            _getCurrentApiService = getCurrentApiService;
            _collapseHistoryPanel = collapseHistoryPanel;
            _animatePanel = animatePanel ?? AnimatePanel;
            _refreshTimer = new DispatcherTimer
            {
                Interval = refreshInterval ?? TimeSpan.FromSeconds(5)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            ApplyHiddenHostState();
        }

        public void Initialize()
        {
            if (_isDisposed)
            {
                return;
            }

            ApplyHiddenHostState();
            _refreshTimer.Start();
            _ = RefreshAsync();
        }

        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public async Task RefreshAsync()
        {
            if (_isDisposed || _isRefreshing)
            {
                return;
            }

            _isRefreshing = true;

            try
            {
                var apiService = _getCurrentApiService();
                if (apiService == null)
                {
                    RenderUnavailableState();
                    return;
                }

                ClearPanels();

                var serverStatuses = await apiService.GetMcpServerStatusesAsync();
                RenderServerStatuses(serverStatuses);

                var availableTools = await apiService.GetAvailableMcpToolsAsync();
                RenderTools(availableTools);

                RenderActivityDisplay(apiService);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新MCP状态显示失败: {ex.Message}");
                RenderErrorState(ex.Message);
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void HideIfVisible()
        {
            if (_isVisible)
            {
                Hide();
                return;
            }

            ApplyHiddenHostState();
        }

        public void ClearActivityLog()
        {
            try
            {
                var apiService = _getCurrentApiService();
                if (apiService == null)
                {
                    _mcpActivityPanel.Children.Clear();
                    _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateNoActivityText());
                    return;
                }

                apiService.ClearMcpActivities();
                RenderActivityDisplay(apiService);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清空MCP日志失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _refreshTimer.Stop();
            _refreshTimer.Tick -= RefreshTimer_Tick;
        }

        private void Show()
        {
            _collapseHistoryPanel();
            _isVisible = true;
            ApplyVisibleHostState();
            _animatePanel(_mcpStatusPanel, ExpandedHeight, null);
            _ = RefreshAsync();
        }

        private void Hide()
        {
            _isVisible = false;
            _mcpStatusPanelHost.IsHitTestVisible = false;
            _animatePanel(_mcpStatusPanel, 0, ApplyHiddenHostState);
        }

        private void ApplyVisibleHostState()
        {
            _mcpStatusPanelHost.Visibility = Visibility.Visible;
            _mcpStatusPanelHost.IsHitTestVisible = true;
        }

        private void ApplyHiddenHostState()
        {
            _mcpStatusPanelHost.Visibility = Visibility.Collapsed;
            _mcpStatusPanelHost.IsHitTestVisible = false;
        }

        private void ClearPanels()
        {
            _mcpServersPanel.Children.Clear();
            _mcpToolsPanel.Children.Clear();
            _mcpActivityPanel.Children.Clear();
        }

        private void RenderUnavailableState()
        {
            ClearPanels();
            _mcpServersPanel.Children.Add(McpPanelElementFactory.CreateNotAvailableText());
            _mcpToolsPanel.Children.Add(McpPanelElementFactory.CreateNoToolsText());
            _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateNoActivityText());
        }

        private void RenderErrorState(string errorMessage)
        {
            ClearPanels();
            _mcpServersPanel.Children.Add(McpPanelElementFactory.CreateErrorText(errorMessage));
            _mcpToolsPanel.Children.Add(McpPanelElementFactory.CreateNoToolsText());
            _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateNoActivityText());
        }

        private void RenderServerStatuses(List<(string Id, string Name, bool IsConnected, int ToolCount)> serverStatuses)
        {
            if (serverStatuses == null || serverStatuses.Count == 0)
            {
                _mcpServersPanel.Children.Add(McpPanelElementFactory.CreateNoServersText());
                return;
            }

            foreach (var server in serverStatuses)
            {
                _mcpServersPanel.Children.Add(McpPanelElementFactory.CreateServerStatusItem(
                    server.Name,
                    server.IsConnected,
                    server.ToolCount));
            }
        }

        private void RenderTools(IList<McpClientTool> tools)
        {
            if (tools == null || tools.Count == 0)
            {
                _mcpToolsPanel.Children.Add(McpPanelElementFactory.CreateNoToolsText());
                return;
            }

            foreach (var tool in tools)
            {
                _mcpToolsPanel.Children.Add(McpPanelElementFactory.CreateToolItem(
                    tool.Name,
                    tool.Description));
            }
        }

        private void RenderActivityDisplay(IApiService apiService)
        {
            _mcpActivityPanel.Children.Clear();

            var recentActivities = apiService.GetRecentMcpActivities(20) ?? new List<McpActivityRecord>();
            if (recentActivities.Count == 0)
            {
                _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateEmptyActivityText());
                return;
            }

            var stats = apiService.GetMcpActivityStatistics() ?? new McpActivityStatistics();
            if (stats.TotalToolCalls > 0)
            {
                _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivitySummary(stats));
                _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivitySummarySeparator());
            }

            for (int i = recentActivities.Count - 1; i >= 0; i--)
            {
                _mcpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivityRecordItem(recentActivities[i]));
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _ = RefreshAsync();
        }

        private static void AnimatePanel(Border panel, double targetHeight, Action onCompleted)
        {
            double currentHeight = panel.Height;
            if (double.IsNaN(currentHeight))
            {
                currentHeight = 0;
            }

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseInOut }
            };

            if (onCompleted != null)
            {
                EventHandler renderingHandler = null;
                renderingHandler = (_, __) =>
                {
                    if (Math.Abs(panel.Height - targetHeight) < 0.5)
                    {
                        System.Windows.Media.CompositionTarget.Rendering -= renderingHandler;
                        onCompleted();
                    }
                };

                System.Windows.Media.CompositionTarget.Rendering += renderingHandler;
            }

            panel.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }
    }
}