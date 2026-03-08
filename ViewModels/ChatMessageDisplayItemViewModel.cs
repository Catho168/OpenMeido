using System;
using System.Windows.Input;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public sealed class ChatMessageDisplayItemViewModel
    {
        private ChatMessageDisplayItemViewModel(
            string text,
            bool isUser,
            bool isToolCall,
            bool isWelcome,
            string toolName,
            Action showDetails)
        {
            Text = text;
            IsUser = isUser;
            IsToolCall = isToolCall;
            IsWelcome = isWelcome;
            ToolName = toolName;
            CanShowDetails = showDetails != null;
            ShowDetailsCommand = showDetails == null ? null : new RelayCommand(showDetails);
        }

        public string Text { get; }

        public bool IsUser { get; }

        public bool IsToolCall { get; }

        public bool IsWelcome { get; }

        public string ToolName { get; }

        public bool CanShowDetails { get; }

        public ICommand ShowDetailsCommand { get; }

        public string DisplayText => IsToolCall ? $"妹抖酱调用了 {ToolName} 工具" : Text;

        public string ToolCallToolTip => CanShowDetails ? "点击查看详情" : null;

        public static ChatMessageDisplayItemViewModel CreateUser(string text)
            => new ChatMessageDisplayItemViewModel(text, true, false, false, string.Empty, null);

        public static ChatMessageDisplayItemViewModel CreateAssistant(string text)
            => new ChatMessageDisplayItemViewModel(text, false, false, false, string.Empty, null);

        public static ChatMessageDisplayItemViewModel CreateWelcome(string text)
            => new ChatMessageDisplayItemViewModel(text, false, false, true, string.Empty, null);

        public static ChatMessageDisplayItemViewModel CreateToolCall(string toolName, Action showDetails = null)
            => new ChatMessageDisplayItemViewModel(string.Empty, false, true, false, toolName, showDetails);
    }
}