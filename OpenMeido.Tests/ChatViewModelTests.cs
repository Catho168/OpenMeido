using System.Threading.Tasks;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class ChatViewModelTests
{
    [Fact]
    public void TryGetPendingMessage_TrimsInput_AndRejectsBlank()
    {
        var viewModel = new ChatViewModel(new FakeChatService(), new FakeChatHistoryService());

        viewModel.InputText = "  你好  ";

        var hasMessage = viewModel.TryGetPendingMessage(out var message);

        Assert.True(hasMessage);
        Assert.Equal("你好", message);

        viewModel.InputText = "   ";

        Assert.False(viewModel.TryGetPendingMessage(out message));
        Assert.Equal(string.Empty, message);
    }

    [Fact]
    public void BeginSend_ClearsInput_AndUpdatesBusyState()
    {
        var viewModel = new ChatViewModel(new FakeChatService(), new FakeChatHistoryService())
        {
            InputText = "hello"
        };

        viewModel.BeginSend();

        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.IsInputEnabled);
        Assert.False(viewModel.CanSend);
        Assert.Equal(string.Empty, viewModel.InputText);
        Assert.Equal("妹抖酱思考ing...", viewModel.StatusText);
        Assert.Equal("processing", viewModel.StatusType);
    }

    [Fact]
    public void AddUserMessage_RefreshesHistoryState_AndSessionTitle()
    {
        var historyService = new FakeChatHistoryService();
        var viewModel = new ChatViewModel(new FakeChatService(), historyService);

        viewModel.AddUserMessage("第一句");
        viewModel.AddAssistantMessage("收到");

        Assert.Equal(2, viewModel.CurrentMessages.Count);
        Assert.Single(viewModel.SavedSessions);
        Assert.True(viewModel.CurrentSession.IsSaved);
        Assert.NotEqual("与妹抖酱的对话", viewModel.CurrentSessionTitle);
    }

    [Fact]
    public async Task InitializeAsync_AppliesServiceInitializationResult()
    {
        var chatService = new FakeChatService
        {
            InitializeResult = ChatServiceInitializationResult.Warning("需要配置API")
        };
        var viewModel = new ChatViewModel(chatService, new FakeChatHistoryService());

        await viewModel.InitializeAsync();

        Assert.Equal("需要配置API", viewModel.StatusText);
        Assert.Equal("warning", viewModel.StatusType);
    }
}