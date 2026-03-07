using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class SettingsViewModelTests
{
    [Fact]
    public void CreateSettingsSnapshot_TrimsValues_AndClonesServers()
    {
        var viewModel = new SettingsViewModel(new FakeSettingsService())
        {
            ApiBaseUrl = " https://example.com/v1 ",
            ApiKey = " secret ",
            ModelName = " model-a ",
            SystemPrompt = " prompt ",
            MaxTokens = 123,
            Temperature = 0.8,
            EnableMcp = true,
            SelectedCategory = SettingsCategory.MCP
        };
        viewModel.McpServers.Add(new McpServerConfig { Id = "1", Name = "server", Command = "cmd" });

        var snapshot = viewModel.CreateSettingsSnapshot();
        viewModel.McpServers[0].Name = "changed";

        Assert.Equal("https://example.com/v1", snapshot.ApiBaseUrl);
        Assert.Equal("secret", snapshot.ApiKey);
        Assert.Equal("model-a", snapshot.ModelName);
        Assert.Equal("prompt", snapshot.SystemPrompt);
        Assert.True(snapshot.EnableMcp);
        Assert.Equal(SettingsCategory.MCP, snapshot.SelectedCategory);
        Assert.Single(snapshot.McpServers);
        Assert.Equal("server", snapshot.McpServers[0].Name);
        Assert.NotSame(viewModel.McpServers[0], snapshot.McpServers[0]);
    }

    [Fact]
    public async Task SaveAsync_WhenSettingsInvalid_ReturnsValidationError_WithoutSaving()
    {
        var settingsService = new FakeSettingsService();
        var viewModel = new SettingsViewModel(settingsService);

        var result = await viewModel.SaveAsync();

        Assert.Equal(SettingsOperationStatus.ValidationError, result.Status);
        Assert.Equal("请填写完整且正确的配置信息", result.Message);
        Assert.Null(settingsService.SavedSettings);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenSettingsValid_UsesServiceAndReturnsSuccess()
    {
        var settingsService = new FakeSettingsService { ConnectionTestResult = true };
        var viewModel = new SettingsViewModel(settingsService)
        {
            ApiBaseUrl = " https://example.com/v1 ",
            ApiKey = " key ",
            ModelName = " model ",
            MaxTokens = 1000,
            Temperature = 0.7
        };

        var result = await viewModel.TestConnectionAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal("妹抖酱连接成功！可以开始聊天了♪", result.Message);
        Assert.NotNull(settingsService.ConnectionTestSettings);
        Assert.Equal("https://example.com/v1", settingsService.ConnectionTestSettings.ApiBaseUrl);
        Assert.Equal("key", settingsService.ConnectionTestSettings.ApiKey);
        Assert.Equal("model", settingsService.ConnectionTestSettings.ModelName);
    }
}