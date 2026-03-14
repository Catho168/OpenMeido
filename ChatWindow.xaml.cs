using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Data;
using ModelContextProtocol.Client;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.Helpers;
using OpenMeido.ViewModels;

namespace OpenMeido
{
    /// 主聊天窗口的交互逻辑
    public partial class ChatWindow : Window
    {
        private readonly ChatViewModel _viewModel;
        private readonly ChatWindowMessageDisplayCoordinator _messageDisplayCoordinator;
        private readonly ChatWindowHistoryPanelCoordinator _historyPanelCoordinator;
        private readonly ChatWindowMcpStatusPanelCoordinator _mcpStatusPanelCoordinator;
        private readonly ChatWindowConversationCoordinator _conversationCoordinator;

        private const string WelcomeMessageText = "您好~有什么需要吗？";

        private IApiService CurrentApiService => _viewModel.CurrentApiService;

        /// 初始化聊天窗口
        public ChatWindow() : this(UiDependencyResolver.ResolveChatViewModel())
        {
        }

        public ChatWindow(ChatViewModel viewModel)
        {
            _viewModel = viewModel ?? UiDependencyResolver.ResolveChatViewModel();

            InitializeComponent();
            TitleBarHost.DragRequested += TitleBar_MouseLeftButtonDown;
            TitleBarHost.MinimizeRequested += MinimizeButton_Click;
            TitleBarHost.CloseRequested += CloseButton_Click;
            ToolbarHost.HistoryToggleRequested += HistoryToggleButton_Click;
            ToolbarHost.McpStatusRequested += McpStatusButton_Click;
            ToolbarHost.ClearRequested += ClearButton_Click;
            ToolbarHost.SettingsRequested += SettingsButton_Click;
            HistoryPanelHost.NewChatRequested += NewChatButton_Click;
            McpStatusPanelHost.RefreshRequested += RefreshMcpButton_Click;
            McpStatusPanelHost.ClearLogRequested += ClearMcpLogButton_Click;
            InputPanelHost.SendRequested += SendButton_Click;
            InputPanelHost.InputKeyDown += InputTextBox_KeyDown;
            InputPanelHost.InputTextChanged += InputTextBox_TextChanged;
            _messageDisplayCoordinator = new ChatWindowMessageDisplayCoordinator(
                this,
                _viewModel.DisplayMessages,
                MessageListHost.ScrollViewer);
            _historyPanelCoordinator = new ChatWindowHistoryPanelCoordinator(
                this,
                HistoryPanelHost.PanelBorder,
                HistoryPanelHost.ItemsPanel,
                ToolbarHost.HistoryToggleIcon,
                () => _viewModel.SavedSessions,
                LoadHistorySession,
                sessionId => _viewModel.DeleteSession(sessionId),
                UpdateCurrentSessionTitle,
                historyPanelHost: HistoryPanelHost,
                collapseMcpStatusPanel: () => _mcpStatusPanelCoordinator?.HideIfVisible());
            _mcpStatusPanelCoordinator = new ChatWindowMcpStatusPanelCoordinator(
                McpStatusPanelHost.PanelBorder,
                McpStatusPanelHost.ServersPanel,
                McpStatusPanelHost.ToolsPanel,
                McpStatusPanelHost.ActivityPanel,
                () => CurrentApiService,
                _historyPanelCoordinator.CollapseIfExpanded,
                mcpStatusPanelHost: McpStatusPanelHost);
            _conversationCoordinator = new ChatWindowConversationCoordinator(
                this,
                _viewModel,
                _messageDisplayCoordinator,
                () => _mcpStatusPanelCoordinator.RefreshAsync(),
                _historyPanelCoordinator.Refresh,
                _historyPanelCoordinator.CollapseIfExpanded,
                () => InputPanelHost.InputTextBox.Focus(),
                WelcomeMessageText);
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ChatViewModel_PropertyChanged;
            ApplyStatusAppearance();

            // 初始化设置和API服务
            InitializeChatService();

            // 初始化历史记录界面
            _historyPanelCoordinator.Initialize();

            // 初始化MCP状态监控
            InitializeMcpStatusMonitoring();

            _messageDisplayCoordinator.AddWelcomeMessage(WelcomeMessageText);

            // 设置输入框焦点
            InputPanelHost.InputTextBox.Focus();

            // 初始化占位符文本显示状态
            UpdatePlaceholderVisibility();

            // 绑定窗口关闭事件
            this.Closing += ChatWindow_Closing;
        }

        /// 初始化API服务
        private async void InitializeChatService()
        {
            await _conversationCoordinator.InitializeChatServiceAsync();
        }

        private void ChatViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatViewModel.StatusType))
            {
                ApplyStatusAppearance();
            }
        }

        private void ApplyStatusAppearance()
        {
            var statusTextBlock = TitleBarHost?.StatusTextBlock;
            if (statusTextBlock == null)
            {
                return;
            }

            statusTextBlock.Foreground = new SolidColorBrush(GetThemeStatusColor(_viewModel.StatusType)) { Opacity = 0.9 };
        }

        /// 获取主题色相关的状态颜色
        private Color GetThemeStatusColor(string statusType)
        {
            return ThemeColors.GetStatusColor(statusType);
        }

        /// 获取主题色相关的UI颜色
        private Color GetThemeUIColor(string colorType)
        {
            return ThemeColors.GetUiColor(colorType);
        }

        /// 发送按钮点击事件处理器
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        /// 输入框按键事件处理器
        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否按下了Ctrl+Enter组合键
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        /// 输入框文本变化事件处理器
        /// 用于控制占位符文本的显示和隐藏
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePlaceholderVisibility();
        }

        /// 更新占位符文本的可见性
        /// 当输入框为空时显示占位符，有内容时隐藏
        private void UpdatePlaceholderVisibility()
        {
            var placeholderTextBlock = InputPanelHost?.PlaceholderTextBlock;
            var inputTextBox = InputPanelHost?.InputTextBox;
            if (placeholderTextBlock == null || inputTextBox == null)
            {
                return;
            }

            placeholderTextBlock.Visibility = string.IsNullOrWhiteSpace(inputTextBox.Text)
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        /// 发送消息的核心方法
        private async Task SendMessage()
        {
            await _conversationCoordinator.SendMessageAsync();
        }

        /// 设置按钮点击事件处理器
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenSettingsAsync();
        }

        private async Task OpenSettingsAsync()
        {
            await _conversationCoordinator.OpenSettingsAsync();
        }

        /// 窗口关闭事件处理器
        private void ChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.PropertyChanged -= ChatViewModel_PropertyChanged;
            _mcpStatusPanelCoordinator.Dispose();
            _viewModel.DisposeChatService();
        }

        /// 标题栏鼠标按下事件处理器 - 实现窗口拖拽
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// 最小化按钮点击事件处理器
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// 关闭按钮点击事件处理器
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// 清空对话按钮点击事件处理器
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _conversationCoordinator.ClearCurrentConversation();
        }

        /// 历史记录切换按钮点击事件处理器
        private void HistoryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _historyPanelCoordinator.Toggle();
        }

        /// 新建对话按钮点击事件处理器
        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            _conversationCoordinator.StartNewConversation(collapseHistoryPanel: true);
        }

        /// 加载历史会话
        /// <param name="session">要加载的会话</param>
        private void LoadHistorySession(ChatSession session)
        {
            _conversationCoordinator.LoadHistorySession(session);
        }

        /// 更新当前会话标题
        private void UpdateCurrentSessionTitle()
        {
            _conversationCoordinator.UpdateCurrentSessionTitle();
        }

        /// 供外部窗口注入迷你聊天历史，需在Show()后调用
        public void AppendMiniChatHistory(IEnumerable<ChatMessage> messages)
        {
            _conversationCoordinator.AppendMiniChatHistory(messages);
        }

        private void ResetConversationView()
        {
            _conversationCoordinator.ResetConversationView();
        }

        #region MCP状态管理

        /// 初始化MCP状态监控
        private void InitializeMcpStatusMonitoring()
        {
            _mcpStatusPanelCoordinator.Initialize();
        }

        /// MCP状态按钮点击事件
        private void McpStatusButton_Click(object sender, RoutedEventArgs e)
        {
            _mcpStatusPanelCoordinator.Toggle();
        }

        /// 刷新MCP按钮点击事件
        private async void RefreshMcpButton_Click(object sender, RoutedEventArgs e)
        {
            await _mcpStatusPanelCoordinator.RefreshAsync();
        }

        /// 清空MCP日志按钮点击事件
        private void ClearMcpLogButton_Click(object sender, RoutedEventArgs e)
        {
            _mcpStatusPanelCoordinator.ClearActivityLog();
        }

        #endregion

        private void InputPanelHost_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
