using OpenMeido.Models;
using OpenMeido.Helpers;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class ChatServiceTests
{
    [Fact]
    public async Task InitializeAsync_WhenSettingsInvalid_ReturnsWarning_WithoutCreatingApiService()
    {
        var settingsService = new FakeSettingsService { LoadResult = new AppSettings() };
        var factory = new FakeApiServiceFactory();
        var service = new ChatService(settingsService, factory);

        var result = await service.InitializeAsync();

        Assert.Equal("需要配置API", result.StatusText);
        Assert.Equal(ChatStatusTypes.Warning, result.StatusType);
        Assert.Equal(0, factory.CreateCallCount);
        Assert.Null(service.CurrentApiService);
    }

    [Fact]
    public async Task InitializeAsync_WhenSettingsValid_CreatesApiService_AndInitializesMcp()
    {
        var settings = CreateValidSettings(enableMcp: true);
        var settingsService = new FakeSettingsService { LoadResult = settings };
        var factory = new FakeApiServiceFactory();
        var apiService = new FakeApiService();
        factory.Enqueue(apiService);
        var service = new ChatService(settingsService, factory);

        var result = await service.InitializeAsync();

        Assert.Equal("就绪", result.StatusText);
        Assert.Equal(ChatStatusTypes.Ready, result.StatusType);
        Assert.Same(settings, factory.LastSettings);
        Assert.Same(apiService, service.CurrentApiService);
        Assert.True(apiService.InitializeMcpCalled);
    }

    [Fact]
    public async Task InitializeAsync_WhenSettingsLoadFails_ReturnsError()
    {
        var settingsService = new FakeSettingsService { LoadException = new InvalidOperationException("boom") };
        var factory = new FakeApiServiceFactory();
        var service = new ChatService(settingsService, factory);

        var result = await service.InitializeAsync();

        Assert.Equal("初始化失败", result.StatusText);
        Assert.Equal(ChatStatusTypes.Error, result.StatusType);
        Assert.Null(service.CurrentApiService);
    }

    [Fact]
    public async Task ReinitializeAsync_DisposesPreviousApiService_BeforeReplacingIt()
    {
        var settingsService = new FakeSettingsService { LoadResult = CreateValidSettings() };
        var factory = new FakeApiServiceFactory();
        var firstApiService = new FakeApiService();
        var secondApiService = new FakeApiService();
        factory.Enqueue(firstApiService);
        factory.Enqueue(secondApiService);
        var service = new ChatService(settingsService, factory);

        await service.InitializeAsync();
        await service.ReinitializeAsync();

        Assert.True(firstApiService.DisposeCalled);
        Assert.Same(secondApiService, service.CurrentApiService);
    }

    [Fact]
    public async Task SendMessageAsync_WhenInitialized_DelegatesToCurrentApiService()
    {
        var settingsService = new FakeSettingsService { LoadResult = CreateValidSettings() };
        var factory = new FakeApiServiceFactory();
        var apiService = new FakeApiService { SendMessageResult = "收到" };
        factory.Enqueue(apiService);
        var service = new ChatService(settingsService, factory);

        await service.InitializeAsync();
        var result = await service.SendMessageAsync(new List<ChatMessage> { new("user", "你好") });

        Assert.Equal("收到", result);
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
