using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using OpenMeido.Helpers;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public sealed class MiniChatEscalationRequestedEventArgs : EventArgs
    {
        public MiniChatEscalationRequestedEventArgs(IReadOnlyList<ChatMessage> history)
        {
            History = history ?? Array.Empty<ChatMessage>();
        }

        public IReadOnlyList<ChatMessage> History { get; }
    }

    public sealed class MiniChatViewModel : ViewModelBase, IDisposable
    {
        private const int MaxVisibleMessages = 7;

        private readonly ISettingsService _settingsService;
        private readonly IApiServiceFactory _apiServiceFactory;
        private readonly AsyncRelayCommand _sendCommand;
        private readonly List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private IApiService _apiService;
        private string _inputText = string.Empty;
        private bool _isBusy;
        private bool _hasInitializedMcp;
        private int _roundCount;

        public MiniChatViewModel(ISettingsService settingsService, IApiServiceFactory apiServiceFactory)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _apiServiceFactory = apiServiceFactory ?? throw new ArgumentNullException(nameof(apiServiceFactory));
            _sendCommand = new AsyncRelayCommand(SendAsync, () => CanSend);
            Messages = new ObservableCollection<MiniChatMessageViewModel>();
        }

        public ObservableCollection<MiniChatMessageViewModel> Messages { get; }

        public string InputText
        {
            get => _inputText;
            set
            {
                if (SetProperty(ref _inputText, value))
                {
                    OnPropertyChanged(nameof(CanSend));
                    _sendCommand.RaiseCanExecuteChanged();
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
                    _sendCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsInputEnabled => !IsBusy;

        public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(InputText);

        public ICommand SendCommand => _sendCommand;

        public event EventHandler<MiniChatEscalationRequestedEventArgs> EscalationRequested;

        public void Open()
        {
            EnsureApiServiceCreated();
        }

        public void Close()
        {
            _roundCount = 0;
            InputText = string.Empty;
            _chatHistory.Clear();
            Messages.Clear();
            IsBusy = false;
        }

        public List<ChatMessage> GetHistorySnapshot()
        {
            return new List<ChatMessage>(_chatHistory);
        }

        public async Task SendAsync()
        {
            string userMessage = InputText?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return;
            }

            AddVisibleMessage(MiniChatMessageViewModel.CreateUser(userMessage));
            _chatHistory.Add(new ChatMessage("user", userMessage));
            InputText = string.Empty;

            if (!EnsureApiServiceCreated())
            {
                AddVisibleMessage(MiniChatMessageViewModel.CreateAssistant("需要先配置API，才能与妹抖酱聊天哦~"));
                return;
            }

            IsBusy = true;

            try
            {
                string reply = await _apiService.SendMessageAsync(new List<ChatMessage>(_chatHistory));
                foreach (var sentence in AiMessageDisplayHelper.SplitMessage(reply))
                {
                    string trimmedSentence = sentence.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedSentence))
                    {
                        continue;
                    }

                    AddAssistantMessage(trimmedSentence);
                    _chatHistory.Add(new ChatMessage("assistant", trimmedSentence));
                }

                _roundCount++;
                if (_roundCount >= 3)
                {
                    EscalationRequested?.Invoke(this, new MiniChatEscalationRequestedEventArgs(GetHistorySnapshot()));
                }
            }
            catch (Exception ex)
            {
                AddVisibleMessage(MiniChatMessageViewModel.CreateAssistant($"抱歉，发生了错误: {ex.Message}"));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void Dispose()
        {
            _apiService?.Dispose();
            _apiService = null;
        }

        private void AddAssistantMessage(string message)
        {
            if (ToolCallMessageMarkers.ContainsAny(message))
            {
                var toolCallData = ToolCallMessageParser.Parse(message);
                if (toolCallData != null)
                {
                    AddVisibleMessage(MiniChatMessageViewModel.CreateToolCall(toolCallData.ToolName));
                    return;
                }
            }

            AddVisibleMessage(MiniChatMessageViewModel.CreateAssistant(message));
        }

        private void AddVisibleMessage(MiniChatMessageViewModel message)
        {
            Messages.Add(message);
            while (Messages.Count > MaxVisibleMessages)
            {
                Messages.RemoveAt(0);
            }
        }

        private bool EnsureApiServiceCreated()
        {
            if (_apiService != null)
            {
                return true;
            }

            var settings = _settingsService.Load();
            if (!settings.IsValid())
            {
                return false;
            }

            _apiService = _apiServiceFactory.Create(settings);
            if (settings.EnableMcp && !_hasInitializedMcp)
            {
                _hasInitializedMcp = true;
                _ = InitializeMcpAsync();
            }

            return true;
        }

        private async Task InitializeMcpAsync()
        {
            try
            {
                await _apiService.InitializeMcpAsync();
                Debug.WriteLine("迷你聊天MCP服务初始化完成");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"迷你聊天MCP服务初始化失败: {ex.Message}");
            }
        }
    }
}