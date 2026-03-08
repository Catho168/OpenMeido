using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowMcpStatusCoordinator : IDisposable
    {
        private static readonly SolidColorBrush ConnectedBrush = CreateBrush(0xE8, 0x74, 0x75);
        private static readonly SolidColorBrush PartiallyConnectedBrush = CreateBrush(0xF0, 0xA0, 0xA1);
        private static readonly Brush DisconnectedBrush = Brushes.Gray;

        private readonly MainViewModel _viewModel;
        private readonly ISettingsService _settingsService;
        private readonly IMcpServiceFactory _mcpServiceFactory;
        private readonly DispatcherTimer _refreshTimer;

        private IMcpService _mcpService;
        private bool _isDisposed;
        private bool _isRefreshing;

        public MainWindowMcpStatusCoordinator(
            MainViewModel viewModel,
            ISettingsService settingsService,
            IMcpServiceFactory mcpServiceFactory,
            TimeSpan? refreshInterval = null)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _mcpServiceFactory = mcpServiceFactory ?? throw new ArgumentNullException(nameof(mcpServiceFactory));

            _refreshTimer = new DispatcherTimer
            {
                Interval = refreshInterval ?? TimeSpan.FromSeconds(10)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;

            ApplyUnavailableState();
        }

        public async Task StartAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var settings = _settingsService.Load();
                if (!settings.EnableMcp)
                {
                    ApplyUnavailableState();
                    return;
                }

                _mcpService = _mcpServiceFactory.Create(settings);
                await _mcpService.InitializeAsync();
                await RefreshAsync();

                if (!_isDisposed)
                {
                    _refreshTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"初始化MCP状态监控失败: {ex.Message}");
                ApplyUnavailableState();
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
                if (_mcpService == null)
                {
                    ApplyUnavailableState();
                    return;
                }

                var serverStatuses = await _mcpService.GetServerStatusAsync();
                var connectedCount = serverStatuses.Count(s => s.IsConnected);
                var totalCount = serverStatuses.Count;
                var totalTools = serverStatuses.Where(s => s.IsConnected).Sum(s => s.ToolCount);

                _viewModel.McpStatusText = $"MCP: {connectedCount}/{totalCount} ({totalTools}工具)";
                _viewModel.McpStatusVisibility = Visibility.Visible;

                if (connectedCount == 0)
                {
                    _viewModel.McpStatusDotBrush = DisconnectedBrush;
                    _viewModel.McpStatusToolTip = "MCP: 无连接";
                }
                else if (connectedCount == totalCount)
                {
                    _viewModel.McpStatusDotBrush = ConnectedBrush;
                    _viewModel.McpStatusToolTip = $"MCP: 全部连接 ({totalTools} 个工具可用)";
                }
                else
                {
                    _viewModel.McpStatusDotBrush = PartiallyConnectedBrush;
                    _viewModel.McpStatusToolTip = $"MCP: 部分连接 ({connectedCount}/{totalCount} 服务器, {totalTools} 个工具)";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"更新MCP状态显示失败: {ex.Message}");
                ApplyUnavailableState();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            try
            {
                _refreshTimer.Stop();
                _refreshTimer.Tick -= RefreshTimer_Tick;

                _mcpService?.Dispose();
                _mcpService = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"清理MCP资源失败: {ex.Message}");
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            _ = RefreshAsync();
        }

        private void ApplyUnavailableState()
        {
            _viewModel.McpStatusText = "MCP: 0/0";
            _viewModel.McpStatusToolTip = "MCP服务器状态";
            _viewModel.McpStatusDotBrush = DisconnectedBrush;
            _viewModel.McpStatusVisibility = Visibility.Collapsed;
        }

        private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}