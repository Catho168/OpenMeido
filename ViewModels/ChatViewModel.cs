using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public class ChatViewModel : ViewModelBase
    {
        private const string DefaultSessionTitle = "与妹抖酱的对话";
        private readonly IChatService _chatService;
        private readonly IChatHistoryService _chatHistoryService;
        private string _statusText = "待命";
        private string _statusType = "ready";
        private string _inputText = string.Empty;
        private bool _isBusy;
        private string _currentSessionTitle = DefaultSessionTitle;

        public ChatViewModel(IChatService chatService, IChatHistoryService chatHistoryService)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _chatHistoryService = chatHistoryService ?? throw new ArgumentNullException(nameof(chatHistoryService));
            UpdateCurrentSessionTitle(_chatHistoryService.CurrentSession);
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public string StatusType
        {
            get => _statusType;
            private set => SetProperty(ref _statusType, value);
        }

        public string InputText
        {
            get => _inputText;
            set
            {
                if (SetProperty(ref _inputText, value))
                {
                    OnPropertyChanged(nameof(CanSend));
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsInputEnabled));
                    OnPropertyChanged(nameof(CanSend));
                }
            }
        }

        public bool IsInputEnabled => !IsBusy;

        public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

        public bool HasConfiguredApi => CurrentApiService != null;

        public string CurrentSessionTitle
        {
            get => _currentSessionTitle;
            private set => SetProperty(ref _currentSessionTitle, value);
        }

        public ApiService CurrentApiService => _chatService.CurrentApiService;

        public ChatSession CurrentSession => _chatHistoryService.CurrentSession;

        public IReadOnlyList<ChatSession> SavedSessions => _chatHistoryService.SavedSessions;

        public IReadOnlyList<ChatMessage> CurrentMessages => _chatHistoryService.CurrentMessages;

        public async Task InitializeAsync()
        {
            SetStatus("初始化聊天服务...", "processing");
            ApplyInitializationResult(await _chatService.InitializeAsync());
        }

        public async Task ReinitializeAsync()
        {
            SetStatus("重新加载聊天配置...", "processing");
            ApplyInitializationResult(await _chatService.ReinitializeAsync());
        }

        public bool TryGetPendingMessage(out string userMessage)
        {
            userMessage = InputText?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(userMessage);
        }

        public void BeginSend()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            InputText = string.Empty;
            SetStatus("妹抖酱思考ing...", "processing");
        }

        public void StartNewSession()
        {
            _chatHistoryService.StartNewSession();
            RefreshHistoryState();
        }

        public void AddUserMessage(string message)
        {
            _chatHistoryService.AddMessage("user", message);
            RefreshHistoryState();
        }

        public void AddAssistantMessage(string message)
        {
            _chatHistoryService.AddMessage("assistant", message);
            RefreshHistoryState();
        }

        public void DeleteSession(string sessionId)
        {
            _chatHistoryService.DeleteSession(sessionId);
            RefreshHistoryState();
        }

        public void LoadSession(ChatSession session)
        {
            _chatHistoryService.SetCurrentSession(session);
            RefreshHistoryState();
        }

        public async Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
        {
            SetStatus("正在发送请求...", "processing");
            return await _chatService.SendMessageAsync(messagesHistory);
        }

        public void MarkReady()
        {
            SetStatus("就绪", "ready");
        }

        public void MarkRequestFailed()
        {
            SetStatus("请求失败", "error");
        }

        public void MarkSendFailed()
        {
            SetStatus("发送失败", "error");
        }

        public void CompleteSend()
        {
            IsBusy = false;
        }

        public void UpdateCurrentSessionTitle(ChatSession session)
        {
            CurrentSessionTitle = session?.IsSaved == true ? session.Title : DefaultSessionTitle;
        }

        public void DisposeChatService()
        {
            _chatService.Dispose();
            OnPropertyChanged(nameof(CurrentApiService));
        }

        private void ApplyInitializationResult(ChatServiceInitializationResult result)
        {
            SetStatus(result.StatusText, result.StatusType);
            OnPropertyChanged(nameof(CurrentApiService));
        }

        private void RefreshHistoryState()
        {
            OnPropertyChanged(nameof(CurrentSession));
            OnPropertyChanged(nameof(CurrentMessages));
            OnPropertyChanged(nameof(SavedSessions));
            UpdateCurrentSessionTitle(_chatHistoryService.CurrentSession);
        }

        private void SetStatus(string statusText, string statusType)
        {
            StatusText = statusText;
            StatusType = statusType;
        }
    }
}