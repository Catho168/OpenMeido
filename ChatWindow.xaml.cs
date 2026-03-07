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
using OpenMeido.Models;
using OpenMeido.Services;
using OpenMeido.Helpers;
using OpenMeido.ViewModels;

namespace OpenMeido
{
    /// 主聊天窗口的交互逻辑
    public partial class ChatWindow : Window
    {
        private readonly ChatViewModel _viewModel;

        // 历史面板是否展开
        private bool isHistoryExpanded = false;

        // MCP状态相关
        private bool isMcpStatusPanelVisible = false;
        private System.Windows.Threading.DispatcherTimer mcpStatusUpdateTimer;

        private static readonly string TripleSlash = new string('\\', 3); // fix later
        private const string WelcomeMessageText = "🎀 大人好~妹抖酱在此！有什么需要吗？";

        private ApiService CurrentApiService => _viewModel.CurrentApiService;

        /// 初始化聊天窗口
        public ChatWindow() : this(ResolveViewModel())
        {
        }

        public ChatWindow(ChatViewModel viewModel)
        {
            _viewModel = viewModel ?? ResolveViewModel();

            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ChatViewModel_PropertyChanged;
            ApplyStatusAppearance();

            // 初始化设置和API服务
            InitializeChatService();

            // 初始化历史记录界面
            InitializeHistoryPanel();

            // 初始化MCP状态监控
            InitializeMcpStatusMonitoring();

            AddWelcomeMessage();

            // 设置输入框焦点
            InputTextBox.Focus();

            // 初始化占位符文本显示状态
            UpdatePlaceholderVisibility();

            // 绑定窗口关闭事件
            this.Closing += ChatWindow_Closing;
        }

        private static ChatViewModel ResolveViewModel()
        {
            var appServices = (Application.Current as App)?.Services;
            var viewModel = appServices?.GetService(typeof(ChatViewModel)) as ChatViewModel;
            return viewModel ?? new ChatViewModel(new ChatService(), new ChatHistoryService());
        }

        /// 初始化API服务
        private async void InitializeChatService()
        {
            await _viewModel.InitializeAsync();
            UpdateMcpStatusDisplay();
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
            if (StatusTextBlock == null)
            {
                return;
            }

            StatusTextBlock.Foreground = new SolidColorBrush(GetThemeStatusColor(_viewModel.StatusType)) { Opacity = 0.9 };
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
            if (PlaceholderTextBlock == null) return;
            PlaceholderTextBlock.Visibility = string.IsNullOrWhiteSpace(InputTextBox.Text) ? Visibility.Visible : Visibility.Hidden;
        }

        /// 发送消息的核心方法
        private async Task SendMessage()
        {
            if (_viewModel.IsBusy)
            {
                MessageBox.Show("妹抖酱还在认真思考中呢~请等等再发消息哦♪", "妹抖酱忙碌中",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_viewModel.TryGetPendingMessage(out string userMessage))
            {
                return;
            }

            // 检查API服务是否可用
            if (!_viewModel.HasConfiguredApi)
            {
                MessageBox.Show("需要先设置API信息，妹抖酱才能和你聊天哦~", "设置缺失",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                await OpenSettingsAsync();
                return;
            }

            _viewModel.BeginSend();

            try
            {
                // 添加用户消息到界面和历史记录
                AddUserMessage(userMessage);
                _viewModel.AddUserMessage(userMessage);

                // 更新当前会话标题
                UpdateCurrentSessionTitle();

                // 发送消息到AI并获取回复（带上下文）
                var historyMessages = new List<ChatMessage>(_viewModel.CurrentMessages);
                string aiResponse = await _viewModel.SendMessageAsync(historyMessages);

                // 检查是否是错误响应
                if (aiResponse.StartsWith("网络请求错误") ||
                    aiResponse.StartsWith("API请求失败") ||
                    aiResponse.StartsWith("JSON解析失败") ||
                    aiResponse.StartsWith("响应解析") ||
                    aiResponse.StartsWith("解析响应时出错"))
                {
                    // 显示详细错误信息
                    AddAiMessage($"❌ 请求失败\n\n{aiResponse}");
                    _viewModel.MarkRequestFailed();
                }
                else
                {
                    // 添加正常的AI回复到界面（支持多句分割和延时显示）
                    await AddAiMessageWithDelay(aiResponse);

                    // 添加AI回复到历史记录
                    _viewModel.AddAssistantMessage(aiResponse);

                    // 更新历史记录面板
                    UpdateHistoryPanel();

                    _viewModel.MarkReady();
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                AddAiMessage($"抱歉，发生了错误: {ex.Message}");
                _viewModel.MarkSendFailed();
            }
            finally
            {
                // 恢复界面状态
                _viewModel.CompleteSend();
                InputTextBox.Focus();
            }
        }

        /// 添加用户消息到聊天界面
        /// <param name="message">用户消息内容</param>
        private void AddUserMessage(string message)
        {
            MessagesPanel.Children.Add(ChatMessageElementFactory.CreateUserMessage(
                message,
                GetChatStyle("UserMessageStyle"),
                GetChatStyle("UserMessageTextStyle")));
            
            // 滚动到底部
            ScrollToBottom();
        }

        /// 添加消息到聊天界面
        /// <param name="message">消息内容</param>
        private void AddAiMessage(string message)
        {
            // 检查是否包含工具执行相关的特殊标记
            if (IsToolExecutionMessage(message))
            {
                AddMcpToolCallBar(message, true); // 主聊天界面支持详细视图
            }
            else
            {
                MessagesPanel.Children.Add(ChatMessageElementFactory.CreateAiMessage(
                    message,
                    GetChatStyle("AiMessageStyle"),
                    GetChatStyle("AiMessageTextStyle")));
            }

            // 滚动到底部
            ScrollToBottom();
        }

        /// 检查消息是否包含工具执行相关内容
        /// <param name="message">消息内容</param>
        /// <returns>是否为工具执行消息</returns>
        private bool IsToolExecutionMessage(string message)
        {
            return message.Contains("TOOL_CALL_START:") ||
                   message.Contains("TOOL_PARAMS:") ||
                   message.Contains("TOOL_RESULT_SUCCESS:") ||
                   message.Contains("TOOL_RESULT_FAILED:") ||
                   message.Contains("TOOL_CALL_END");
        }

        /// 添加工具调用信息条（类似聊天软件的系统消息）
        /// <param name="message">工具调用消息</param>
        /// <param name="isDetailedView">是否为详细视图（主聊天界面）</param>
        private void AddMcpToolCallBar(string message, bool isDetailedView = true)
        {
            var toolCallData = ToolCallMessageParser.Parse(message);
            if (toolCallData == null) return;

            MessagesPanel.Children.Add(ChatMessageElementFactory.CreateToolCallBar(
                toolCallData.ToolName,
                isDetailedView,
                isDetailedView ? () => ShowToolCallDetails(toolCallData) : null));
        }

        /// 显示工具调用详情对话框
        /// <param name="toolCallData">工具调用数据</param>
        private void ShowToolCallDetails(ToolCallMessageData toolCallData)
        {
            var detailsWindow = ToolCallDetailsWindowFactory.Create(
                this,
                toolCallData.ToolName,
                toolCallData.Parameters,
                toolCallData.Result,
                toolCallData.IsSuccess);
            detailsWindow.Show();
        }



        /// 添加消息到聊天界面，支持多句分割和延时显示
        /// <param name="fullMessage">完整的回复消息</param>
        private async Task AddAiMessageWithDelay(string fullMessage)
        {
            // 检查是否包含分割符 (三个反斜杠)
            if (fullMessage.Contains(TripleSlash))
            {
                // 分割消息
                var sentences = SplitAiMessage(fullMessage);

                // 逐句显示，每句之间有延时
                foreach (var sentence in sentences)
                {
                    if (!string.IsNullOrWhiteSpace(sentence))
                    {
                        // 添加单句消息
                        AddAiMessage(sentence.Trim());

                        // 根据句子长度计算延时
                        int delay = CalculateDelay(sentence);
                        await Task.Delay(delay);
                    }
                }
            }
            else
            {
                // 没有分割符，直接显示整条消息
                AddAiMessage(fullMessage);
            }
        }

        /// 分割消息为多个句子
        /// <param name="message">原始消息</param>
        /// <returns>分割后的句子列表</returns>
        private List<string> SplitAiMessage(string message)
        {
            // 使用 \\\ 作为分割符 (三个反斜杠)
            var sentences = message.Split(new string[] { TripleSlash }, StringSplitOptions.RemoveEmptyEntries);
            return sentences.ToList();
        }

        /// 根据句子长度计算延时时间
        /// <param name="sentence">句子内容</param>
        /// <returns>延时毫秒数</returns>
        private int CalculateDelay(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return 500; // 默认延时

            int length = sentence.Trim().Length;

            // 基础延时 + 根据长度的额外延时
            // 短句子(1-20字符): 800-1200ms
            // 中等句子(21-50字符): 1200-2000ms
            // 长句子(51+字符): 2000-3500ms

            int baseDelay = 800;
            int extraDelay = 0;

            if (length <= 20)
            {
                extraDelay = length * 20; // 每字符20ms
            }
            else if (length <= 50)
            {
                extraDelay = 400 + (length - 20) * 25; // 前20字符400ms + 后续每字符25ms
            }
            else
            {
                extraDelay = 1150 + (length - 50) * 30; // 前50字符1150ms + 后续每字符30ms
            }

            int totalDelay = baseDelay + extraDelay;

            // 限制最大延时为3500ms，最小延时为800ms
            return Math.Max(800, Math.Min(3500, totalDelay));
        }

        /// 滚动聊天区域到底部
        private void ScrollToBottom()
        {
            ChatScrollViewer.ScrollToEnd();
        }

        /// 添加欢迎消息
        private void AddWelcomeMessage()
        {
            MessagesPanel.Children.Add(ChatMessageElementFactory.CreateWelcomeMessage(
                WelcomeMessageText,
                GetChatStyle("AiMessageStyle"),
                GetChatStyle("AiMessageTextStyle")));
        }

        private Style GetChatStyle(string resourceKey)
        {
            return (Style)FindResource(resourceKey);
        }

        /// 设置按钮点击事件处理器
        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenSettingsAsync();
        }

        private async Task OpenSettingsAsync()
        {
            var appServices = (Application.Current as App)?.Services;
            var settingsWindow = appServices?.GetService(typeof(SettingsWindow)) as SettingsWindow ?? new SettingsWindow();
            settingsWindow.Owner = this;
            
            // 显示设置窗口
            bool? result = settingsWindow.ShowDialog();
            
            // 如果设置已更改，重新初始化API服务
            if (result == true)
            {
                await _viewModel.ReinitializeAsync();
                UpdateMcpStatusDisplay();
            }
        }

        /// 窗口关闭事件处理器
        private void ChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.PropertyChanged -= ChatViewModel_PropertyChanged;
            mcpStatusUpdateTimer?.Stop();
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
            if (ConfirmClearCurrentConversation())
            {
                StartNewConversation(collapseHistoryPanel: false);
            }
        }

        /// 历史记录切换按钮点击事件处理器
        private void HistoryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleHistoryPanel();
        }

        /// 新建对话按钮点击事件处理器
        private void NewChatButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewConversation(collapseHistoryPanel: true);
        }

        private bool ConfirmClearCurrentConversation()
        {
            var result = MessageBox.Show("确定要清空当前对话吗？", "确认清空",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private void StartNewConversation(bool collapseHistoryPanel)
        {
            ResetConversationView();
            _viewModel.StartNewSession();
            UpdateCurrentSessionTitle();

            if (collapseHistoryPanel)
            {
                CollapseHistoryPanelIfExpanded();
            }
        }

        private void CollapseHistoryPanelIfExpanded()
        {
            if (isHistoryExpanded)
            {
                CollapseHistoryPanel();
            }
        }

        /// 初始化历史记录面板
        private void InitializeHistoryPanel()
        {
            UpdateHistoryPanel();
            UpdateCurrentSessionTitle();
        }

        /// 切换历史记录面板的展开/折叠状态
        private void ToggleHistoryPanel()
        {
            if (isHistoryExpanded)
            {
                CollapseHistoryPanel();
            }
            else
            {
                ExpandHistoryPanel();
            }
        }

        private void ExpandHistoryPanel()
        {
            SetHistoryPanelExpanded(true);
            AnimateHistoryPanel(200, refreshWhenExpanded: true);
        }

        private void CollapseHistoryPanel()
        {
            SetHistoryPanelExpanded(false);
            AnimateHistoryPanel(0, refreshWhenExpanded: false);
        }

        private void SetHistoryPanelExpanded(bool isExpanded)
        {
            isHistoryExpanded = isExpanded;
            HistoryToggleIcon.Text = isExpanded ? "📂" : "📁";
        }

        private void AnimateHistoryPanel(double targetHeight, bool refreshWhenExpanded)
        {
            double currentHeight = HistoryPanel.Height;
            if (double.IsNaN(currentHeight)) currentHeight = 0;

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseInOut }
            };

            EventHandler renderingHandler = null;
            renderingHandler = (sender, e) =>
            {
                if (HistoryPanel.Height == targetHeight)
                {
                    CompositionTarget.Rendering -= renderingHandler;

                    if (refreshWhenExpanded)
                    {
                        UpdateHistoryPanel();
                    }
                }
            };
            CompositionTarget.Rendering += renderingHandler;

            HistoryPanel.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// 更新历史记录面板
        private void UpdateHistoryPanel()
        {
            HistoryItemsPanel.Children.Clear();

            foreach (var session in _viewModel.SavedSessions)
            {
                var historyItem = CreateHistoryItem(session);
                HistoryItemsPanel.Children.Add(historyItem);
            }
        }

        /// 创建历史记录项
        /// <param name="session">聊天会话</param>
        /// <returns>历史记录项控件</returns>
        private Border CreateHistoryItem(ChatSession session)
        {
            return ChatHistoryItemElementFactory.Create(
                session.Title,
                () => LoadHistorySession(session),
                () => ConfirmDeleteSession(session));
        }

        private void ConfirmDeleteSession(ChatSession session)
        {
            var result = MessageBox.Show($"确定要删除对话 \"{session.Title}\" 吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.DeleteSession(session.SessionId);
                UpdateHistoryPanel();
            }
        }

        /// 加载历史会话
        /// <param name="session">要加载的会话</param>
        private void LoadHistorySession(ChatSession session)
        {
            ClearMessagesPanel();
            ReplayMessages(session.Messages, syncToViewModel: false);

            // 设置当前会话
            _viewModel.LoadSession(session);
            UpdateCurrentSessionTitle();

            // 折叠历史面板
            CollapseHistoryPanelIfExpanded();
        }

        /// 更新当前会话标题
        private void UpdateCurrentSessionTitle()
        {
            _viewModel.UpdateCurrentSessionTitle(_viewModel.CurrentSession);
        }

        /// 供外部窗口注入迷你聊天历史，需在Show()后调用
        public void AppendMiniChatHistory(IEnumerable<ChatMessage> messages)
        {
            if (messages == null || !messages.Any()) return;

            ClearMessagesPanel();
            ReplayMessages(messages, syncToViewModel: true);

            // 更新会话标题、滚动到底部
            UpdateCurrentSessionTitle();
            ScrollToBottom();
        }

        private void ResetConversationView()
        {
            ClearMessagesPanel();
            AddWelcomeMessage();
        }

        private void ClearMessagesPanel()
        {
            MessagesPanel.Children.Clear();
        }

        private void ReplayMessages(IEnumerable<ChatMessage> messages, bool syncToViewModel)
        {
            foreach (var message in messages ?? Enumerable.Empty<ChatMessage>())
            {
                if (string.IsNullOrWhiteSpace(message?.Content))
                {
                    continue;
                }

                if (message.Role == "user")
                {
                    AddUserMessage(message.Content);
                    if (syncToViewModel)
                    {
                        _viewModel.AddUserMessage(message.Content);
                    }
                }
                else if (message.Role == "assistant")
                {
                    ReplayAssistantMessage(message.Content);
                    if (syncToViewModel)
                    {
                        _viewModel.AddAssistantMessage(message.Content);
                    }
                }
            }
        }

        private void ReplayAssistantMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            if (!content.Contains(TripleSlash))
            {
                AddAiMessage(content);
                return;
            }

            foreach (var part in SplitAiMessage(content))
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    AddAiMessage(part.Trim());
                }
            }
        }

        #region MCP状态管理

        /// 初始化MCP状态监控
        private void InitializeMcpStatusMonitoring()
        {
            try
            {
                // 启动MCP状态更新定时器
                mcpStatusUpdateTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5) // 每5秒更新一次
                };
                mcpStatusUpdateTimer.Tick += (sender, e) => UpdateMcpStatusDisplay();
                mcpStatusUpdateTimer.Start();

                // 初始更新
                UpdateMcpStatusDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化MCP状态监控失败: {ex.Message}");
            }
        }

        /// MCP状态按钮点击事件
        private void McpStatusButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMcpStatusPanel();
        }

        /// 刷新MCP按钮点击事件
        private void RefreshMcpButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateMcpStatusDisplay();
        }

        /// 清空MCP日志按钮点击事件
        private void ClearMcpLogButton_Click(object sender, RoutedEventArgs e)
        {
            ClearMcpActivityLog();
        }

        private void ClearMcpActivityLog()
        {
            try
            {
                if (CurrentApiService != null)
                {
                    CurrentApiService.ClearMcpActivities();
                    UpdateMcpActivityDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清空MCP日志失败: {ex.Message}");
            }
        }



        /// 切换MCP状态面板显示
        private void ToggleMcpStatusPanel()
        {
            if (isMcpStatusPanelVisible)
            {
                HideMcpStatusPanel();
            }
            else
            {
                ShowMcpStatusPanel();
            }
        }

        private void ShowMcpStatusPanel()
        {
            HideHistoryPanelForMcpStatus();
            AnimatePanel(McpStatusPanel, 200);
            isMcpStatusPanelVisible = true;
            UpdateMcpStatusDisplay();
        }

        private void HideMcpStatusPanel()
        {
            AnimatePanel(McpStatusPanel, 0);
            isMcpStatusPanelVisible = false;
        }

        private void HideHistoryPanelForMcpStatus()
        {
            CollapseHistoryPanelIfExpanded();
        }

        /// 面板动画方法
        /// <param name="panel">要动画的面板</param>
        /// <param name="targetHeight">目标高度</param>
        private void AnimatePanel(Border panel, double targetHeight)
        {
            if (panel == null) return;

            double currentHeight = panel.Height;
            if (double.IsNaN(currentHeight)) currentHeight = 0;

            // 创建动画
            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseInOut }
            };

            // 启动动画
            panel.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        /// 更新MCP状态显示
        private async void UpdateMcpStatusDisplay()
        {
            try
            {
                if (CurrentApiService == null)
                {
                    DisplayMcpNotAvailable();
                    return;
                }

                // 清空现有内容
                McpServersPanel.Children.Clear();
                McpToolsPanel.Children.Clear();

                // 显示服务器状态
                var serverStatuses = await CurrentApiService.GetMcpServerStatusesAsync();
                await DisplayMcpServersStatus(serverStatuses);

                // 显示可用工具
                var availableTools = await CurrentApiService.GetAvailableMcpToolsAsync();
                DisplayMcpTools(availableTools);

                // 显示活动日志
                UpdateMcpActivityDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新MCP状态显示失败: {ex.Message}");
                DisplayMcpError(ex.Message);
            }
        }

        /// 显示MCP服务器状态
        private Task DisplayMcpServersStatus(List<(string Id, string Name, bool IsConnected, int ToolCount)> serverStatuses)
        {
            if (serverStatuses == null || serverStatuses.Count == 0)
            {
                McpServersPanel.Children.Add(McpPanelElementFactory.CreateNoServersText());
                return Task.CompletedTask;
            }

            foreach (var server in serverStatuses)
            {
                McpServersPanel.Children.Add(McpPanelElementFactory.CreateServerStatusItem(
                    server.Name,
                    server.IsConnected,
                    server.ToolCount));
            }

            return Task.CompletedTask;
        }

        /// 显示MCP工具列表
        private void DisplayMcpTools(IList<McpClientTool> tools)
        {
            if (tools == null || tools.Count == 0)
            {
                McpToolsPanel.Children.Add(McpPanelElementFactory.CreateNoToolsText());
                return;
            }

            foreach (var tool in tools)
            {
                McpToolsPanel.Children.Add(McpPanelElementFactory.CreateToolItem(
                    tool.Name,
                    tool.Description));
            }
        }

        /// 显示MCP不可用状态
        private void DisplayMcpNotAvailable()
        {
            McpServersPanel.Children.Add(McpPanelElementFactory.CreateNotAvailableText());
        }

        /// 显示MCP错误状态
        private void DisplayMcpError(string errorMessage)
        {
            McpServersPanel.Children.Add(McpPanelElementFactory.CreateErrorText(errorMessage));
        }

        /// 更新MCP活动日志显示
        private void UpdateMcpActivityDisplay()
        {
            try
            {
                McpActivityPanel.Children.Clear();

                if (CurrentApiService == null)
                {
                    McpActivityPanel.Children.Add(McpPanelElementFactory.CreateNoActivityText());
                    return;
                }

                // 获取最近的活动记录
                var recentActivities = CurrentApiService.GetRecentMcpActivities(20);
                if (recentActivities.Count == 0)
                {
                    McpActivityPanel.Children.Add(McpPanelElementFactory.CreateEmptyActivityText());
                    return;
                }

                // 显示统计信息
                var stats = CurrentApiService.GetMcpActivityStatistics();
                if (stats.TotalToolCalls > 0)
                {
                    McpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivitySummary(stats));
                    McpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivitySummarySeparator());
                }

                // 显示活动记录（最新的在前）
                for (int i = recentActivities.Count - 1; i >= 0; i--)
                {
                    var activity = recentActivities[i];
                    DisplayMcpActivityRecord(activity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新MCP活动显示失败: {ex.Message}");
            }
        }

        /// 显示单个MCP活动记录
        private void DisplayMcpActivityRecord(McpActivityRecord activity)
        {
            McpActivityPanel.Children.Add(McpPanelElementFactory.CreateActivityRecordItem(activity));
        }

        #endregion
    }
}
