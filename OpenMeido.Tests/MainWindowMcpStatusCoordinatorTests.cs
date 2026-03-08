using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class MainWindowMcpStatusCoordinatorTests
{
    [Fact]
    public async Task StartAsync_WhenMcpDisabled_DoesNotCreateService_AndKeepsIndicatorCollapsed()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings { EnableMcp = false } };
        var mcpServiceFactory = new FakeMcpServiceFactory();
        var viewModel = CreateViewModel(settingsService);

        using var coordinator = new MainWindowMcpStatusCoordinator(viewModel, settingsService, mcpServiceFactory);
        await coordinator.StartAsync();

        Assert.Equal(0, mcpServiceFactory.CreateCallCount);
        Assert.Equal("MCP: 0/0", viewModel.McpStatusText);
        Assert.Equal("MCP服务器状态", viewModel.McpStatusToolTip);
        Assert.Equal(Visibility.Collapsed, viewModel.McpStatusVisibility);
    }

    [Fact]
    public async Task StartAsync_WhenAllServersConnected_UpdatesBoundState()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings { EnableMcp = true } };
        var fakeMcpService = new FakeMcpService
        {
            ServerStatusesResult = new()
            {
                ("1", "文件", true, 2),
                ("2", "浏览器", true, 3)
            }
        };
        var mcpServiceFactory = new FakeMcpServiceFactory();
        mcpServiceFactory.Enqueue(fakeMcpService);
        var viewModel = CreateViewModel(settingsService);

        using var coordinator = new MainWindowMcpStatusCoordinator(viewModel, settingsService, mcpServiceFactory);
        await coordinator.StartAsync();

        Assert.True(fakeMcpService.InitializeCalled);
        Assert.Equal(1, fakeMcpService.GetServerStatusCallCount);
        Assert.Equal("MCP: 2/2 (5工具)", viewModel.McpStatusText);
        Assert.Equal("MCP: 全部连接 (5 个工具可用)", viewModel.McpStatusToolTip);
        Assert.Equal(Visibility.Visible, viewModel.McpStatusVisibility);
        Assert.Equal(Color.FromRgb(0xE8, 0x74, 0x75), Assert.IsType<SolidColorBrush>(viewModel.McpStatusDotBrush).Color);
    }

    [Fact]
    public async Task StartAsync_WhenPartiallyConnected_UsesPartialDisplayState()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings { EnableMcp = true } };
        var fakeMcpService = new FakeMcpService
        {
            ServerStatusesResult = new()
            {
                ("1", "文件", true, 4),
                ("2", "浏览器", false, 0)
            }
        };
        var mcpServiceFactory = new FakeMcpServiceFactory();
        mcpServiceFactory.Enqueue(fakeMcpService);
        var viewModel = CreateViewModel(settingsService);

        using var coordinator = new MainWindowMcpStatusCoordinator(viewModel, settingsService, mcpServiceFactory);
        await coordinator.StartAsync();

        Assert.Equal("MCP: 1/2 (4工具)", viewModel.McpStatusText);
        Assert.Equal("MCP: 部分连接 (1/2 服务器, 4 个工具)", viewModel.McpStatusToolTip);
        Assert.Equal(Visibility.Visible, viewModel.McpStatusVisibility);
        Assert.Equal(Color.FromRgb(0xF0, 0xA0, 0xA1), Assert.IsType<SolidColorBrush>(viewModel.McpStatusDotBrush).Color);
    }

    [Fact]
    public async Task StartAsync_WhenStatusRefreshFails_ResetsUnavailableState()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings { EnableMcp = true } };
        var fakeMcpService = new FakeMcpService
        {
            ServerStatusException = new InvalidOperationException("boom")
        };
        var mcpServiceFactory = new FakeMcpServiceFactory();
        mcpServiceFactory.Enqueue(fakeMcpService);
        var viewModel = CreateViewModel(settingsService);

        using var coordinator = new MainWindowMcpStatusCoordinator(viewModel, settingsService, mcpServiceFactory);
        await coordinator.StartAsync();

        Assert.Equal(Visibility.Collapsed, viewModel.McpStatusVisibility);
        Assert.Equal("MCP: 0/0", viewModel.McpStatusText);
        Assert.Equal("MCP服务器状态", viewModel.McpStatusToolTip);
    }

    [Fact]
    public async Task Dispose_DisposesUnderlyingMcpService()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings { EnableMcp = true } };
        var fakeMcpService = new FakeMcpService();
        var mcpServiceFactory = new FakeMcpServiceFactory();
        mcpServiceFactory.Enqueue(fakeMcpService);
        var viewModel = CreateViewModel(settingsService);

        var coordinator = new MainWindowMcpStatusCoordinator(viewModel, settingsService, mcpServiceFactory);
        await coordinator.StartAsync();

        coordinator.Dispose();

        Assert.True(fakeMcpService.DisposeCalled);
    }

    private static MainViewModel CreateViewModel(FakeSettingsService settingsService)
        => new(settingsService, new FakeApiServiceFactory());
}