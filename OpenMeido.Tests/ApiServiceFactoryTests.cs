using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class ApiServiceFactoryTests
{
    [Fact]
    public async Task Create_WhenMcpFactoryInjected_UsesItInsideApiService()
    {
        var settings = CreateValidSettings(enableMcp: true);
        var mcpFactory = new FakeMcpServiceFactory();
        var mcpService = new FakeMcpService();
        mcpFactory.Enqueue(mcpService);
        var apiFactory = new ApiServiceFactory(mcpFactory);

        var apiService = apiFactory.Create(settings);
        await apiService.InitializeMcpAsync();
        apiService.Dispose();

        Assert.Equal(1, mcpFactory.CreateCallCount);
        Assert.Same(settings, mcpFactory.LastSettings);
        Assert.NotNull(mcpFactory.LastLogger);
        Assert.True(mcpService.InitializeCalled);
        Assert.True(mcpService.DisposeCalled);
    }

    private static AppSettings CreateValidSettings(bool enableMcp = false) => new()
    {
        ApiBaseUrl = "https://example.com/v1",
        ApiKey = "key",
        ModelName = "model",
        MaxTokens = 1000,
        Temperature = 0.7,
        EnableMcp = enableMcp
    };
}