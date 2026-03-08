using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class MiniChatViewModelTests
{
    [Fact]
    public async Task SendAsync_IgnoresBlankInput()
    {
        var viewModel = CreateViewModel();

        viewModel.InputText = "   ";
        await viewModel.SendAsync();

        Assert.Empty(viewModel.Messages);
        Assert.Empty(viewModel.GetHistorySnapshot());
    }

    [Fact]
    public async Task SendAsync_WithValidSettings_AddsUserAndSplitAssistantMessages()
    {
        var apiService = new FakeApiService { SendMessageResult = @"第一句\\\第二句" };
        var viewModel = CreateViewModel(apiService: apiService);

        viewModel.InputText = "  你好  ";
        await viewModel.SendAsync();

        Assert.Collection(viewModel.Messages,
            message => Assert.Equal("你好", message.Text),
            message => Assert.Equal("第一句", message.Text),
            message => Assert.Equal("第二句", message.Text));

        Assert.Collection(viewModel.GetHistorySnapshot(),
            message => Assert.Equal(("user", "你好"), (message.Role, message.Content)),
            message => Assert.Equal(("assistant", "第一句"), (message.Role, message.Content)),
            message => Assert.Equal(("assistant", "第二句"), (message.Role, message.Content)));
    }

    [Fact]
    public async Task SendAsync_WhenReplyIsToolCall_AddsToolCallPresentationItem()
    {
        var apiService = new FakeApiService
        {
            SendMessageResult = "TOOL_CALL_START: search\nTOOL_RESULT_SUCCESS: ok\nTOOL_CALL_END"
        };
        var viewModel = CreateViewModel(apiService: apiService);

        viewModel.InputText = "查一下";
        await viewModel.SendAsync();

        Assert.Equal(2, viewModel.Messages.Count);
        Assert.False(viewModel.Messages[1].IsUser);
        Assert.True(viewModel.Messages[1].IsToolCall);
        Assert.Equal("search", viewModel.Messages[1].ToolName);
        Assert.Equal("TOOL_CALL_START: search\nTOOL_RESULT_SUCCESS: ok\nTOOL_CALL_END", viewModel.GetHistorySnapshot()[1].Content);
    }

    [Fact]
    public async Task SendAsync_WhenSettingsInvalid_ShowsConfigurationPromptWithoutCreatingApiService()
    {
        var settingsService = new FakeSettingsService();
        var factory = new FakeApiServiceFactory();
        var viewModel = new MiniChatViewModel(settingsService, factory);

        viewModel.InputText = "你好";
        await viewModel.SendAsync();

        Assert.Equal(0, factory.CreateCallCount);
        Assert.Equal(2, viewModel.Messages.Count);
        Assert.Equal("你好", viewModel.Messages[0].Text);
        Assert.Equal("需要先配置API，才能与妹抖酱聊天哦~", viewModel.Messages[1].Text);
    }

    [Fact]
    public async Task SendAsync_OnThirdRound_RaisesEscalationRequestedWithHistory()
    {
        var apiService = new FakeApiService { SendMessageResult = "收到" };
        var viewModel = CreateViewModel(apiService: apiService);
        IReadOnlyList<ChatMessage>? escalatedHistory = null;
        var eventCount = 0;

        viewModel.EscalationRequested += (_, args) =>
        {
            eventCount++;
            escalatedHistory = args.History;
        };

        for (var i = 1; i <= 3; i++)
        {
            viewModel.InputText = $"问题{i}";
            await viewModel.SendAsync();
        }

        Assert.Equal(1, eventCount);
        Assert.NotNull(escalatedHistory);
        Assert.Equal(6, escalatedHistory!.Count);
        Assert.Equal("问题3", escalatedHistory[4].Content);
        Assert.Equal("收到", escalatedHistory[5].Content);
    }

    [Fact]
    public async Task SendAsync_KeepsOnlyLastSevenVisibleMessages()
    {
        var apiService = new FakeApiService();
        var viewModel = CreateViewModel(apiService: apiService);

        for (var i = 1; i <= 4; i++)
        {
            apiService.SendMessageResult = $"回复{i}";
            viewModel.InputText = $"问题{i}";
            await viewModel.SendAsync();
        }

        Assert.Equal(7, viewModel.Messages.Count);
        Assert.Equal("回复1", viewModel.Messages[0].Text);
        Assert.DoesNotContain(viewModel.Messages, message => message.Text == "问题1");
        Assert.Equal("回复4", viewModel.Messages[^1].Text);
    }

    private static MiniChatViewModel CreateViewModel(FakeSettingsService? settingsService = null, FakeApiService? apiService = null)
    {
        settingsService ??= new FakeSettingsService { LoadResult = CreateValidSettings() };
        var factory = new FakeApiServiceFactory();
        factory.Enqueue(apiService ?? new FakeApiService());
        return new MiniChatViewModel(settingsService, factory);
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