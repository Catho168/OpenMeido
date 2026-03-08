using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task TestConnectionAsync_UsesFactoryCreatedApiService_AndDisposesIt()
    {
        var factory = new FakeApiServiceFactory();
        var apiService = new FakeApiService { TestConnectionResult = true };
        factory.Enqueue(apiService);
        var service = new SettingsService(factory);
        var settings = CreateValidSettings();

        var result = await service.TestConnectionAsync(settings);

        Assert.True(result);
        Assert.Same(settings, factory.LastSettings);
        Assert.True(apiService.TestConnectionCalled);
        Assert.True(apiService.DisposeCalled);
    }

    [Fact]
    public async Task TestConnectionAsync_WhenApiServiceThrows_StillDisposesInstance()
    {
        var factory = new FakeApiServiceFactory();
        var apiService = new FakeApiService { TestConnectionException = new InvalidOperationException("boom") };
        factory.Enqueue(apiService);
        var service = new SettingsService(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TestConnectionAsync(CreateValidSettings()));

        Assert.True(apiService.DisposeCalled);
    }

    private static AppSettings CreateValidSettings() => new()
    {
        ApiBaseUrl = "https://example.com/v1",
        ApiKey = "key",
        ModelName = "model",
        MaxTokens = 1000,
        Temperature = 0.7
    };
}