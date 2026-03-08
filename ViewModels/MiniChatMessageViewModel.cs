namespace OpenMeido.ViewModels
{
    public sealed class MiniChatMessageViewModel
    {
        private MiniChatMessageViewModel(string text, bool isUser, bool isToolCall, string toolName)
        {
            Text = text;
            IsUser = isUser;
            IsToolCall = isToolCall;
            ToolName = toolName;
        }

        public string Text { get; }

        public bool IsUser { get; }

        public bool IsToolCall { get; }

        public string ToolName { get; }

        public string DisplayText => IsToolCall ? $"🤖 妹抖酱调用了 {ToolName} 工具" : Text;

        public static MiniChatMessageViewModel CreateUser(string text)
            => new MiniChatMessageViewModel(text, true, false, string.Empty);

        public static MiniChatMessageViewModel CreateAssistant(string text)
            => new MiniChatMessageViewModel(text, false, false, string.Empty);

        public static MiniChatMessageViewModel CreateToolCall(string toolName)
            => new MiniChatMessageViewModel(string.Empty, false, true, toolName);
    }
}