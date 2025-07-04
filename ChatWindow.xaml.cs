using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido
{
    /// 主聊天窗口的交互逻辑
    public partial class ChatWindow : Window
    {
        // API服务实例，用于与AI进行通信
        private ApiService apiService;
        
        // 应用程序设置
        private AppSettings settings;
        
        // 标记是否正在等待AI回复
        private bool isWaitingForResponse = false;

        // 聊天历史管理器
        private ChatHistoryManager historyManager;

        // 历史面板是否展开
        private bool isHistoryExpanded = false;

        private static readonly string TripleSlash = new string('\\', 3); // fix later

        /// 初始化聊天窗口
        public ChatWindow()
        {
            InitializeComponent();

            // 初始化聊天历史管理器
            historyManager = new ChatHistoryManager();

            // 初始化设置和API服务
            InitializeApiService();

            // 初始化历史记录界面
            InitializeHistoryPanel();

            // 添加欢迎消息
            AddWelcomeMessage();

            // 设置输入框焦点
            InputTextBox.Focus();

            // 初始化占位符文本显示状态
            UpdatePlaceholderVisibility();

            // 绑定窗口关闭事件
            this.Closing += ChatWindow_Closing;
        }

        /// 初始化API服务
        private void InitializeApiService()
        {
            try
            {
                // 加载应用程序设置
                settings = AppSettings.Load();
                
                // 检查设置是否有效
                if (settings?.IsValid() == true)
                {
                    // 创建API服务实例
                    apiService = new ApiService(settings);
                    UpdateStatus("就绪", Colors.Green);
                }
                else
                {
                    UpdateStatus("需要配置API", Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("初始化失败", Colors.Red);
                System.Diagnostics.Debug.WriteLine($"初始化API服务失败: {ex.Message}");
            }
        }

        /// 更新状态显示
        /// <param name="status">状态文本</param>
        /// <param name="color">状态颜色</param>
        private void UpdateStatus(string status, Color color)
        {
            // 更新标题栏状态
            StatusTextBlock.Text = status;
            StatusTextBlock.Foreground = new SolidColorBrush(color) { Opacity = 0.9 };
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
            // 获取用户输入的消息
            string userMessage = InputTextBox.Text?.Trim();

            // 检查消息是否为空
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return;
            }

            // 检查是否正在等待回复
            if (isWaitingForResponse)
            {
                MessageBox.Show("妹抖酱还在认真思考中呢~请等等再发消息哦♪", "妹抖酱忙碌中",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 检查API服务是否可用
            if (apiService == null)
            {
                MessageBox.Show("需要先设置API信息，妹抖酱才能和你聊天哦~", "设置缺失",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SettingsButton_Click(this, new RoutedEventArgs());
                return;
            }

            try
            {
                // 设置等待状态
                isWaitingForResponse = true;
                UpdateStatus("妹抖酱思考ing...", Colors.Blue);

                // 禁用发送按钮和输入框
                SendButton.IsEnabled = false;
                InputTextBox.IsEnabled = false;

                // 清空输入框
                InputTextBox.Text = "";

                // 添加用户消息到界面和历史记录
                AddUserMessage(userMessage);
                historyManager.AddMessage("user", userMessage);

                // 更新当前会话标题
                UpdateCurrentSessionTitle();

                // 发送消息到AI并获取回复（带上下文）
                UpdateStatus("正在发送请求...", Colors.Blue);
                var historyMessages = historyManager.CurrentSession.Messages;
                string aiResponse = await apiService.SendMessageAsync(historyMessages);

                // 检查是否是错误响应
                if (aiResponse.StartsWith("网络请求错误") ||
                    aiResponse.StartsWith("API请求失败") ||
                    aiResponse.StartsWith("JSON解析失败") ||
                    aiResponse.StartsWith("响应解析") ||
                    aiResponse.StartsWith("解析响应时出错"))
                {
                    // 显示详细错误信息
                    AddAiMessage($"❌ 请求失败\n\n{aiResponse}");
                    UpdateStatus("请求失败", Colors.Red);
                }
                else
                {
                    // 添加正常的AI回复到界面（支持多句分割和延时显示）
                    await AddAiMessageWithDelay(aiResponse);

                    // 添加AI回复到历史记录
                    historyManager.AddMessage("assistant", aiResponse);

                    // 更新历史记录面板
                    UpdateHistoryPanel();

                    UpdateStatus("就绪", Colors.Green);
                }
                
                UpdateStatus("就绪", Colors.Green);
            }
            catch (Exception ex)
            {
                // 处理异常
                AddAiMessage($"抱歉，发生了错误: {ex.Message}");
                UpdateStatus("发送失败", Colors.Red);
            }
            finally
            {
                // 恢复界面状态
                isWaitingForResponse = false;
                SendButton.IsEnabled = true;
                InputTextBox.IsEnabled = true;
                InputTextBox.Focus();
            }
        }

        /// 添加用户消息到聊天界面
        /// <param name="message">用户消息内容</param>
        private void AddUserMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("UserMessageStyle")
            };
            
            var textBlock = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("UserMessageTextStyle")
            };
            
            border.Child = textBlock;
            MessagesPanel.Children.Add(border);
            
            // 滚动到底部
            ScrollToBottom();
        }

        /// 添加消息到聊天界面
        /// <param name="message">消息内容</param>
        private void AddAiMessage(string message)
        {
            var border = new Border
            {
                Style = (Style)FindResource("AiMessageStyle")
            };

            var textBlock = new TextBlock
            {
                Text = message,
                Style = (Style)FindResource("AiMessageTextStyle")
            };

            border.Child = textBlock;
            MessagesPanel.Children.Add(border);

            // 滚动到底部
            ScrollToBottom();
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
            var border = new Border
            {
                Style = (Style)FindResource("AiMessageStyle"),
                Margin = new Thickness(10, 20, 50, 10)
            };

            var textBlock = new TextBlock
            {
                Style = (Style)FindResource("AiMessageTextStyle"),
                Text = "🎀 大人好~妹抖酱在此！有什么需要吗？"
            };

            border.Child = textBlock;
            MessagesPanel.Children.Add(border);
        }

        /// 设置按钮点击事件处理器
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            
            // 显示设置窗口
            bool? result = settingsWindow.ShowDialog();
            
            // 如果设置已更改，重新初始化API服务
            if (result == true)
            {
                // 释放旧的API服务
                apiService?.Dispose();
                
                // 重新初始化
                InitializeApiService();
            }
        }

        /// 窗口关闭事件处理器
        private void ChatWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 释放API服务资源
            apiService?.Dispose();
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
            var result = MessageBox.Show("确定要清空当前对话吗？", "确认清空",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 清空消息面板
                MessagesPanel.Children.Clear();

                // 添加欢迎消息
                AddWelcomeMessage();

                // 开始新的会话
                historyManager.StartNewSession();
                UpdateCurrentSessionTitle();
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
            // 清空当前对话
            MessagesPanel.Children.Clear();
            AddWelcomeMessage();

            // 开始新会话
            historyManager.StartNewSession();
            UpdateCurrentSessionTitle();

            // 折叠历史面板
            if (isHistoryExpanded)
            {
                ToggleHistoryPanel();
            }
        }

        /// 初始化历史记录面板
        private void InitializeHistoryPanel()
        {
            UpdateHistoryPanel();
            UpdateCurrentSessionTitle();
        }

        /// 切换历史记录面板的展开/折叠状态
        private async void ToggleHistoryPanel()
        {
            isHistoryExpanded = !isHistoryExpanded;

            // 更新图标
            HistoryToggleIcon.Text = isHistoryExpanded ? "📂" : "📁";

            // 动画展开/折叠
            double currentHeight = HistoryPanel.Height;
            if (double.IsNaN(currentHeight)) currentHeight = 0;

            double targetHeight = isHistoryExpanded ? 200 : 0;
            const int steps = 10;
            double stepSize = (targetHeight - currentHeight) / steps;

            for (int i = 0; i < steps; i++)
            {
                currentHeight += stepSize;
                HistoryPanel.Height = currentHeight;
                await Task.Delay(20);
            }

            HistoryPanel.Height = targetHeight;

            // 如果展开，更新历史记录列表
            if (isHistoryExpanded)
            {
                UpdateHistoryPanel();
            }
        }

        /// 更新历史记录面板
        private void UpdateHistoryPanel()
        {
            HistoryItemsPanel.Children.Clear();

            foreach (var session in historyManager.Sessions.Where(s => s.IsSaved))
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
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(10, 8, 10, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = session.Title,
                FontSize = 11,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);

            var deleteButton = new Button
            {
                Content = "❌",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Padding = new Thickness(2),
                ToolTip = "删除此对话"
            };
            Grid.SetColumn(deleteButton, 1);

            // 删除按钮事件
            deleteButton.Click += (_, __) =>
            {
                var result = MessageBox.Show($"确定要删除对话 \"{session.Title}\" 吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    historyManager.DeleteSession(session.SessionId);
                    UpdateHistoryPanel();
                }
            };

            // 历史项点击事件
            border.MouseLeftButtonDown += (_, __) => LoadHistorySession(session);

            grid.Children.Add(titleBlock);
            grid.Children.Add(deleteButton);
            border.Child = grid;

            return border;
        }

        /// 加载历史会话
        /// <param name="session">要加载的会话</param>
        private void LoadHistorySession(ChatSession session)
        {
            // 清空当前消息
            MessagesPanel.Children.Clear();

            // 加载历史消息（不添加欢迎消息，直接显示历史对话）
            foreach (var message in session.Messages)
            {
                if (message.Role == "user")
                {
                    AddUserMessage(message.Content);
                }
                else if (message.Role == "assistant")
                {
                    // 当历史消息包含分割符（三个反斜杠）时，按句拆分显示
                    if (message.Content.Contains(TripleSlash))
                    {
                        var sentences = SplitAiMessage(message.Content);
                        foreach (var sentence in sentences)
                        {
                            if (!string.IsNullOrWhiteSpace(sentence))
                            {
                                AddAiMessage(sentence.Trim());
                            }
                        }
                    }
                    else
                    {
                        AddAiMessage(message.Content);
                    }
                }
            }

            // 设置当前会话
            historyManager.CurrentSession = session;
            UpdateCurrentSessionTitle();

            // 折叠历史面板
            if (isHistoryExpanded)
            {
                ToggleHistoryPanel();
            }
        }

        /// 更新当前会话标题
        private void UpdateCurrentSessionTitle()
        {
            if (historyManager.CurrentSession.IsSaved)
            {
                CurrentSessionTitle.Text = historyManager.CurrentSession.Title;
            }
            else
            {
                CurrentSessionTitle.Text = "与妹抖酱的对话";
            }
        }

        /// 供外部窗口注入迷你聊天历史，需在Show()后调用
        public void AppendMiniChatHistory(IEnumerable<ChatMessage> messages)
        {
            if (messages == null) return;

            // 清空默认欢迎信息
            MessagesPanel.Children.Clear();

            foreach (var msg in messages)
            {
                if (string.IsNullOrWhiteSpace(msg?.Content)) continue;

                string role = msg.Role;
                string content = msg.Content;

                if (role == "user")
                {
                    AddUserMessage(content);
                    historyManager.AddMessage("user", content);
                }
                else
                {
                    // 消息可能包含分句
                    if (content.Contains(TripleSlash))
                    {
                        var parts = SplitAiMessage(content);
                        foreach (var p in parts)
                        {
                            AddAiMessage(p.Trim());
                        }
                    }
                    else
                    {
                        AddAiMessage(content);
                    }
                    historyManager.AddMessage("assistant", content);
                }
            }

            // 更新会话标题、滚动到底部
            UpdateCurrentSessionTitle();
            ScrollToBottom();
        }
    }
}
