using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure
{
    public sealed class ChatWindowConversationCoordinator
    {
        private static readonly string[] RequestFailurePrefixes =
        [
            "网络请求错误",
            "API请求失败",
            "JSON解析失败",
            "响应解析",
            "解析响应时出错"
        ];

        private readonly Window _owner;
        private readonly ChatViewModel _viewModel;
        private readonly ChatWindowMessageDisplayCoordinator _messageDisplayCoordinator;
        private readonly Func<Task> _refreshMcpStatusAsync;
        private readonly Action _refreshHistoryPanel;
        private readonly Action _collapseHistoryPanel;
        private readonly Action _focusInput;
        private readonly IChatWindowConversationPlatform _platform;
        private readonly string _welcomeMessageText;

        public ChatWindowConversationCoordinator(
            Window owner,
            ChatViewModel viewModel,
            ChatWindowMessageDisplayCoordinator messageDisplayCoordinator,
            Func<Task> refreshMcpStatusAsync,
            Action refreshHistoryPanel,
            Action collapseHistoryPanel,
            Action focusInput,
            string welcomeMessageText,
            IChatWindowConversationPlatform platform = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(messageDisplayCoordinator);
            ArgumentNullException.ThrowIfNull(refreshMcpStatusAsync);
            ArgumentNullException.ThrowIfNull(refreshHistoryPanel);
            ArgumentNullException.ThrowIfNull(collapseHistoryPanel);
            ArgumentNullException.ThrowIfNull(focusInput);
            ArgumentException.ThrowIfNullOrWhiteSpace(welcomeMessageText);

            _owner = owner;
            _viewModel = viewModel;
            _messageDisplayCoordinator = messageDisplayCoordinator;
            _refreshMcpStatusAsync = refreshMcpStatusAsync;
            _refreshHistoryPanel = refreshHistoryPanel;
            _collapseHistoryPanel = collapseHistoryPanel;
            _focusInput = focusInput;
            _welcomeMessageText = welcomeMessageText;
            _platform = platform ?? new ChatWindowConversationPlatform();
        }

        public async Task InitializeChatServiceAsync()
        {
            await _viewModel.InitializeAsync();
            await _refreshMcpStatusAsync();
        }

        public async Task OpenSettingsAsync()
        {
            if (_platform.OpenSettingsDialog(_owner) == true)
            {
                await _viewModel.ReinitializeAsync();
                await _refreshMcpStatusAsync();
            }
        }

        public async Task SendMessageAsync()
        {
            if (_viewModel.IsBusy)
            {
                _platform.ShowMessage("妹抖酱还在认真思考中呢~请等等再发消息哦♪", "妹抖酱忙碌中", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_viewModel.TryGetPendingMessage(out string userMessage))
            {
                return;
            }

            if (!_viewModel.HasConfiguredApi)
            {
                _platform.ShowMessage("需要先设置API信息，妹抖酱才能和你聊天哦~", "设置缺失", MessageBoxButton.OK, MessageBoxImage.Warning);
                await OpenSettingsAsync();
                return;
            }

            _viewModel.BeginSend();

            try
            {
                _messageDisplayCoordinator.AddUserMessage(userMessage);
                _viewModel.AddUserMessage(userMessage);
                UpdateCurrentSessionTitle();

                var historyMessages = new List<ChatMessage>(_viewModel.CurrentMessages);
                string aiResponse = await _viewModel.SendMessageAsync(historyMessages);

                if (RequestFailurePrefixes.Any(prefix => aiResponse.StartsWith(prefix, StringComparison.Ordinal)))
                {
                    _messageDisplayCoordinator.AddAiMessage($"❌ 请求失败\n\n{aiResponse}");
                    _viewModel.MarkRequestFailed();
                }
                else
                {
                    await _messageDisplayCoordinator.AddAiMessageWithDelayAsync(aiResponse);
                    _viewModel.AddAssistantMessage(aiResponse);
                    _refreshHistoryPanel();
                    _viewModel.MarkReady();
                }
            }
            catch (Exception ex)
            {
                _messageDisplayCoordinator.AddAiMessage($"抱歉，发生了错误: {ex.Message}");
                _viewModel.MarkSendFailed();
            }
            finally
            {
                _viewModel.CompleteSend();
                _focusInput();
            }
        }

        public void ClearCurrentConversation()
        {
            var result = _platform.ShowMessage("确定要清空当前对话吗？", "确认清空", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                StartNewConversation(collapseHistoryPanel: false);
            }
        }

        public void StartNewConversation(bool collapseHistoryPanel)
        {
            ResetConversationView();
            _viewModel.StartNewSession();
            UpdateCurrentSessionTitle();

            if (collapseHistoryPanel)
            {
                _collapseHistoryPanel();
            }
        }

        public void LoadHistorySession(ChatSession session)
        {
            ArgumentNullException.ThrowIfNull(session);

            _messageDisplayCoordinator.ClearMessages();
            _messageDisplayCoordinator.ReplayMessages(session.Messages);
            _viewModel.LoadSession(session);
            UpdateCurrentSessionTitle();
            _collapseHistoryPanel();
        }

        public void UpdateCurrentSessionTitle()
        {
            _viewModel.UpdateCurrentSessionTitle(_viewModel.CurrentSession);
        }

        public void AppendMiniChatHistory(IEnumerable<ChatMessage> messages)
        {
            if (messages == null)
            {
                return;
            }

            var history = messages.Where(message => !string.IsNullOrWhiteSpace(message?.Content)).ToList();
            if (history.Count == 0)
            {
                return;
            }

            _messageDisplayCoordinator.ClearMessages();
            _messageDisplayCoordinator.ReplayMessages(
                history,
                onUserMessageReplayed: _viewModel.AddUserMessage,
                onAssistantMessageReplayed: _viewModel.AddAssistantMessage);
            UpdateCurrentSessionTitle();
            _messageDisplayCoordinator.ScrollToBottom();
        }

        public void ResetConversationView()
        {
            _messageDisplayCoordinator.ResetConversationView(_welcomeMessageText);
        }
    }
}