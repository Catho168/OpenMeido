using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using OpenMeido.Helpers;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure
{
    public sealed class ChatWindowMessageDisplayCoordinator
    {
        private readonly Window _owner;
        private readonly ICollection<ChatMessageDisplayItemViewModel> _displayMessages;
        private readonly ScrollViewer _chatScrollViewer;
        private readonly Action<string, string, string, bool> _showToolCallDetails;

        public ChatWindowMessageDisplayCoordinator(
            Window owner,
            ICollection<ChatMessageDisplayItemViewModel> displayMessages,
            ScrollViewer chatScrollViewer,
            Action<string, string, string, bool> showToolCallDetails = null)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(displayMessages);
            ArgumentNullException.ThrowIfNull(chatScrollViewer);

            _owner = owner;
            _displayMessages = displayMessages;
            _chatScrollViewer = chatScrollViewer;
            _showToolCallDetails = showToolCallDetails ?? ShowToolCallDetails;
        }

        public void AddUserMessage(string message)
        {
            _displayMessages.Add(ChatMessageDisplayItemViewModel.CreateUser(message));
            ScrollToBottom();
        }

        public void AddAiMessage(string message, bool isDetailedView = true)
        {
            if (ToolCallMessageMarkers.ContainsAny(message))
            {
                AddToolCallBar(message, isDetailedView);
            }
            else
            {
                _displayMessages.Add(ChatMessageDisplayItemViewModel.CreateAssistant(message));
            }

            ScrollToBottom();
        }

        public async Task AddAiMessageWithDelayAsync(string fullMessage)
        {
            if (AiMessageDisplayHelper.ContainsSentenceSeparator(fullMessage))
            {
                var sentences = AiMessageDisplayHelper.SplitMessage(fullMessage);

                foreach (var sentence in sentences)
                {
                    if (!string.IsNullOrWhiteSpace(sentence))
                    {
                        AddAiMessage(sentence.Trim());

                        var delay = AiMessageDisplayHelper.CalculateDelay(sentence);
                        await Task.Delay(delay);
                    }
                }

                return;
            }

            AddAiMessage(fullMessage);
        }

        public void AddWelcomeMessage(string message)
        {
            _displayMessages.Add(ChatMessageDisplayItemViewModel.CreateWelcome(message));
        }

        public void ResetConversationView(string welcomeMessage)
        {
            ClearMessages();
            AddWelcomeMessage(welcomeMessage);
        }

        public void ClearMessages()
        {
            _displayMessages.Clear();
        }

        public void ReplayMessages(
            IEnumerable<ChatMessage> messages,
            Action<string> onUserMessageReplayed = null,
            Action<string> onAssistantMessageReplayed = null)
        {
            foreach (var message in messages ?? Array.Empty<ChatMessage>())
            {
                if (string.IsNullOrWhiteSpace(message?.Content))
                {
                    continue;
                }

                if (message.Role == "user")
                {
                    AddUserMessage(message.Content);
                    onUserMessageReplayed?.Invoke(message.Content);
                }
                else if (message.Role == "assistant")
                {
                    ReplayAssistantMessage(message.Content);
                    onAssistantMessageReplayed?.Invoke(message.Content);
                }
            }
        }

        public void ScrollToBottom()
        {
            _chatScrollViewer.ScrollToEnd();
        }

        private void AddToolCallBar(string message, bool isDetailedView)
        {
            var toolCallData = ToolCallMessageParser.Parse(message);
            if (toolCallData == null)
            {
                return;
            }

            _displayMessages.Add(ChatMessageDisplayItemViewModel.CreateToolCall(
                toolCallData.ToolName,
                isDetailedView
                    ? () => _showToolCallDetails(
                        toolCallData.ToolName,
                        toolCallData.Parameters,
                        toolCallData.Result,
                        toolCallData.IsSuccess)
                    : null));
        }

        private void ReplayAssistantMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            if (!AiMessageDisplayHelper.ContainsSentenceSeparator(content))
            {
                AddAiMessage(content);
                return;
            }

            foreach (var part in AiMessageDisplayHelper.SplitMessage(content))
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    AddAiMessage(part.Trim());
                }
            }
        }

        private void ShowToolCallDetails(string toolName, string parameters, string result, bool isSuccess)
        {
            var detailsWindow = ToolCallDetailsWindowFactory.Create(
                _owner,
                toolName,
                parameters,
                result,
                isSuccess);

            detailsWindow.Show();
        }
    }
}