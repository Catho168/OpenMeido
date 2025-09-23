using System;
using System.Collections.Generic;
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

        // MCP状态相关
        private bool isMcpStatusPanelVisible = false;
        private System.Windows.Threading.DispatcherTimer mcpStatusUpdateTimer;

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

        /// 初始化API服务
        private async void InitializeApiService()
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

                    // 初始化MCP服务
                    if (settings.EnableMcp)
                    {
                        UpdateStatus("初始化MCP服务...", GetThemeStatusColor("processing"));
                        await apiService.InitializeMcpAsync();
                    }

                    UpdateStatus("就绪", GetThemeStatusColor("ready"));
                }
                else
                {
                    UpdateStatus("需要配置API", GetThemeStatusColor("warning"));
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("初始化失败", GetThemeStatusColor("error"));
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

        /// 获取主题色相关的状态颜色
        private Color GetThemeStatusColor(string statusType)
        {
            return statusType switch
            {
                "ready" => Color.FromRgb(0xE8, 0x74, 0x75), // 主题色 - 就绪状态
                "processing" => Color.FromRgb(0xF0, 0xA0, 0xA1), // 主题色浅色 - 处理中
                "error" => Color.FromRgb(0xD6, 0x58, 0x59), // 主题色深色 - 错误状态
                "warning" => Color.FromRgb(0xF0, 0xA0, 0xA1), // 主题色浅色 - 警告状态
                _ => Color.FromRgb(0xE8, 0x74, 0x75) // 默认主题色
            };
        }

        /// 获取主题色相关的UI颜色
        private Color GetThemeUIColor(string colorType)
        {
            return colorType switch
            {
                "success" => Color.FromRgb(0xE8, 0x74, 0x75), // 成功状态使用主题色
                "error" => Color.FromRgb(0xD6, 0x58, 0x59), // 错误状态使用主题色深色
                "warning" => Color.FromRgb(0xF0, 0xA0, 0xA1), // 警告状态使用主题色浅色
                "processing" => Color.FromRgb(0xF0, 0xA0, 0xA1), // 处理中状态
                "muted" => Color.FromRgb(0xE8, 0xC4, 0xC5), // 弱化文本(主题色+灰色混合)
                "background_success" => Color.FromRgb(0xF8, 0xF0, 0xF0), // 成功背景色
                "background_error" => Color.FromRgb(0xF5, 0xE8, 0xE8), // 错误背景色
                "border_success" => Color.FromRgb(0xF0, 0xA0, 0xA1), // 成功边框色
                "border_error" => Color.FromRgb(0xD6, 0x58, 0x59), // 错误边框色
                _ => Color.FromRgb(0xE8, 0x74, 0x75) // 默认主题色
            };
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
                UpdateStatus("妹抖酱思考ing...", GetThemeStatusColor("processing"));

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
                UpdateStatus("正在发送请求...", GetThemeStatusColor("processing"));
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
                    UpdateStatus("请求失败", GetThemeStatusColor("error"));
                }
                else
                {
                    // 添加正常的AI回复到界面（支持多句分割和延时显示）
                    await AddAiMessageWithDelay(aiResponse);

                    // 添加AI回复到历史记录
                    historyManager.AddMessage("assistant", aiResponse);

                    // 更新历史记录面板
                    UpdateHistoryPanel();

                    UpdateStatus("就绪", GetThemeStatusColor("ready"));
                }
                
                UpdateStatus("就绪", GetThemeStatusColor("ready"));
            }
            catch (Exception ex)
            {
                // 处理异常
                AddAiMessage($"抱歉，发生了错误: {ex.Message}");
                UpdateStatus("发送失败", GetThemeStatusColor("error"));
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
            // 检查是否包含工具执行相关的特殊标记
            if (IsToolExecutionMessage(message))
            {
                AddMcpToolCallBar(message, true); // 主聊天界面支持详细视图
            }
            else
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
            var toolCallData = ParseToolCallMessage(message);
            if (toolCallData == null) return;

            // 创建居中的信息条容器
            var containerBorder = new Border
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 创建信息条
            var messageBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 300
            };

            // 创建文本内容
            var textBlock = new TextBlock
            {
                Text = $"妹抖酱调用了 {toolCallData.ToolName} 工具",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // 如果是详细视图（主聊天界面），添加点击事件
            if (isDetailedView)
            {
                messageBorder.Cursor = Cursors.Hand;
                messageBorder.ToolTip = "点击查看详情";
                
                // 添加悬停效果
                messageBorder.MouseEnter += (s, e) =>
                {
                    messageBorder.Background = new SolidColorBrush(Color.FromRgb(235, 235, 235));
                };
                messageBorder.MouseLeave += (s, e) =>
                {
                    messageBorder.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                };
                
                // 添加点击事件用于展开详情
                messageBorder.MouseLeftButtonDown += (s, e) =>
                {
                    ShowToolCallDetails(toolCallData);
                };
            }

            messageBorder.Child = textBlock;
            containerBorder.Child = messageBorder;
            MessagesPanel.Children.Add(containerBorder);
        }

        /// 解析工具调用消息
        /// <param name="message">工具调用消息</param>
        /// <returns>工具调用数据</returns>
        private ToolCallData ParseToolCallMessage(string message)
        {
            var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var toolCallData = new ToolCallData();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("TOOL_CALL_START:"))
                {
                    toolCallData.ToolName = trimmedLine.Substring("TOOL_CALL_START:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("TOOL_PARAMS:"))
                {
                    toolCallData.Parameters = trimmedLine.Substring("TOOL_PARAMS:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("TOOL_RESULT_SUCCESS:"))
                {
                    toolCallData.Result = trimmedLine.Substring("TOOL_RESULT_SUCCESS:".Length).Trim();
                    toolCallData.IsSuccess = true;
                }
                else if (trimmedLine.StartsWith("TOOL_RESULT_FAILED:"))
                {
                    toolCallData.Result = trimmedLine.Substring("TOOL_RESULT_FAILED:".Length).Trim();
                    toolCallData.IsSuccess = false;
                }
            }
            
            return string.IsNullOrEmpty(toolCallData.ToolName) ? null : toolCallData;
        }

        /// 显示工具调用详情对话框
        /// <param name="toolCallData">工具调用数据</param>
        private void ShowToolCallDetails(ToolCallData toolCallData)
        {
            // 创建现代化的小信息弹窗
            var detailsWindow = new Window
            {
                Title = "工具调用详情",
                Width = 480,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                UseLayoutRounding = true,  // 启用布局舍入以提高渲染清晰度
                SnapsToDevicePixels = true // 启用像素对齐以提高渲染清晰度
            };
            // 主容器，带圆角和阴影效果
            var mainBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(GetThemeUIColor("success")),
                BorderThickness = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 8,
                    BlurRadius = 15,
                    Opacity = 0.3
                },
                Margin = new Thickness(10, 10, 10, 10), // 为阴影留出空间
                UseLayoutRounding = true,  // 启用布局舍入以提高渲染清晰度
                SnapsToDevicePixels = true // 启用像素对齐以提高渲染清晰度
            };
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) }); // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容区域
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 按钮区域

            // 标题栏
            var titleBar = new Border
            {
                Background = new SolidColorBrush(GetThemeUIColor("success")),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };
            Grid.SetRow(titleBar, 0);

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(15, 0, 0, 0),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            titleContent.Children.Add(new TextBlock
            {
                Text = "🔧",
                FontSize = 16,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 8, 0),
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            });
            titleContent.Children.Add(new TextBlock
            {
                Text = "工具调用详情",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            });
            Grid.SetColumn(titleContent, 0);

            // 关闭按钮（X）
            var closeBtn = new Button
            {
                Content = "✕",
                Width = 35,
                Height = 30,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"]
            };
            closeBtn.Click += (s, e) => detailsWindow.Close();
            
            // 悬停效果
            closeBtn.MouseEnter += (s, e) => closeBtn.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
            closeBtn.MouseLeave += (s, e) => closeBtn.Background = Brushes.Transparent;
            
            Grid.SetColumn(closeBtn, 1);

            titleGrid.Children.Add(titleContent);
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;

            // 内容区域
            var contentScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20, 15, 20, 0)
            };
            Grid.SetRow(contentScrollViewer, 1);

            var contentPanel = new StackPanel();

            // 工具名称
            var toolNamePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 18),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            toolNamePanel.Children.Add(new TextBlock
            {
                Text = "⚙️",
                FontSize = 14,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            });
            toolNamePanel.Children.Add(new TextBlock
            {
                Text = "工具名称:",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            });
            toolNamePanel.Children.Add(new TextBlock
            {
                Text = toolCallData.ToolName,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(GetThemeUIColor("success")),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            });
            contentPanel.Children.Add(toolNamePanel);

            // 参数信息
            if (!string.IsNullOrEmpty(toolCallData.Parameters))
            {
                var paramLabel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 8),
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                paramLabel.Children.Add(new TextBlock
                {
                    Text = "📋",
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
                paramLabel.Children.Add(new TextBlock
                {
                    Text = "调用参数:",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(85, 85, 85)),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
                contentPanel.Children.Add(paramLabel);
                
                contentPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 18),
                    Child = new TextBlock
                    {
                        Text = toolCallData.Parameters,
                        FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        LineHeight = 16,
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    },
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
            }

            // 执行结果
            if (!string.IsNullOrEmpty(toolCallData.Result))
            {
                var resultIcon = toolCallData.IsSuccess ? "✅" : "❌";
                var resultTitle = toolCallData.IsSuccess ? "执行结果" : "执行错误";
                var resultColor = toolCallData.IsSuccess ? GetThemeUIColor("success") : GetThemeUIColor("error");
                var resultBgColor = toolCallData.IsSuccess ? GetThemeUIColor("background_success") : GetThemeUIColor("background_error");
                var resultBorderColor = toolCallData.IsSuccess ? GetThemeUIColor("border_success") : GetThemeUIColor("border_error");
                
                var resultLabel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 0, 8),
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                };
                resultLabel.Children.Add(new TextBlock
                {
                    Text = resultIcon,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
                resultLabel.Children.Add(new TextBlock
                {
                    Text = $"{resultTitle}:",
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(resultColor),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
                contentPanel.Children.Add(resultLabel);
                
                contentPanel.Children.Add(new Border
                {
                    Background = new SolidColorBrush(resultBgColor),
                    BorderBrush = new SolidColorBrush(resultBorderColor),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = new ScrollViewer
                    {
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        MaxHeight = 150,
                        Content = new TextBlock
                        {
                            Text = toolCallData.Result,
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                            LineHeight = 16,
                            FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                            UseLayoutRounding = true,
                            SnapsToDevicePixels = true
                        },
                        UseLayoutRounding = true,
                        SnapsToDevicePixels = true
                    },
                    UseLayoutRounding = true,
                    SnapsToDevicePixels = true
                });
            }

            contentScrollViewer.Content = contentPanel;

            // 底部按钮区域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 15, 20, 20),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "确定",
                Width = 90,
                Height = 32,
                Background = new SolidColorBrush(GetThemeUIColor("success")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                FontFamily = (FontFamily)Application.Current.Resources["GlobalFontFamily"],
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };            
            // 简单的圆角样式
            var buttonStyle = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            
            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);
            
            template.VisualTree = border;
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));
            okButton.Style = buttonStyle;
            
            // 悬停效果
            okButton.MouseEnter += (s, e) => okButton.Background = new SolidColorBrush(GetThemeUIColor("processing"));
            okButton.MouseLeave += (s, e) => okButton.Background = new SolidColorBrush(GetThemeUIColor("success"));
            
            okButton.Click += (s, e) => detailsWindow.Close();
            buttonPanel.Children.Add(okButton);

            mainGrid.Children.Add(titleBar);
            mainGrid.Children.Add(contentScrollViewer);
            mainGrid.Children.Add(buttonPanel);
            mainBorder.Child = mainGrid;
            detailsWindow.Content = mainBorder;

            // 窗口拖拽
            titleBar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    detailsWindow.DragMove();
                }
            };

            // 淡入动画
            detailsWindow.Opacity = 0;
            detailsWindow.Show();
            
            var fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new PowerEase { Power = 2, EasingMode = EasingMode.EaseOut }
            };
            detailsWindow.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
        }

        /// 工具调用数据类
        private class ToolCallData
        {
            public string ToolName { get; set; } = "";
            public string Parameters { get; set; } = "";
            public string Result { get; set; } = "";
            public bool IsSuccess { get; set; } = false;
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
        private void ToggleHistoryPanel()
        {
            isHistoryExpanded = !isHistoryExpanded;

            HistoryToggleIcon.Text = isHistoryExpanded ? "📂" : "📁";

            // 使用Storyboard实现动画
            double currentHeight = HistoryPanel.Height;
            if (double.IsNaN(currentHeight)) currentHeight = 0;
            double targetHeight = isHistoryExpanded ? 200 : 0;

            // 创建动画
            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new PowerEase { Power = 3, EasingMode = EasingMode.EaseInOut } // 非线性动画
            };

            // 使用CompositionTarget.Rendering渲染
            EventHandler renderingHandler = null;
            renderingHandler = (sender, e) =>
            {
                if (HistoryPanel.Height == targetHeight)
                {
                    CompositionTarget.Rendering -= renderingHandler;
                    
                    // 如果展开，更新历史记录列表
                    if (isHistoryExpanded)
                    {
                        UpdateHistoryPanel();
                    }
                }
            };
            CompositionTarget.Rendering += renderingHandler;

            // 启动动画
            HistoryPanel.BeginAnimation(FrameworkElement.HeightProperty, animation);
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
            if (messages == null || !messages.Any()) return;

            // 当有实际历史消息时才清空默认欢迎信息
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
            try
            {
                if (apiService != null)
                {
                    var activityLogger = apiService.GetActivityLogger();
                    activityLogger?.ClearActivities();
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
                // 隐藏MCP状态面板
                AnimatePanel(McpStatusPanel, 0);
                isMcpStatusPanelVisible = false;
            }
            else
            {
                // 先隐藏历史面板
                if (isHistoryExpanded)
                {
                    AnimatePanel(HistoryPanel, 0);
                    isHistoryExpanded = false;
                }

                // 显示MCP状态面板
                AnimatePanel(McpStatusPanel, 200);
                isMcpStatusPanelVisible = true;
                UpdateMcpStatusDisplay();
            }
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
                if (apiService == null)
                {
                    DisplayMcpNotAvailable();
                    return;
                }

                // 清空现有内容
                McpServersPanel.Children.Clear();
                McpToolsPanel.Children.Clear();

                // 获取MCP服务实例
                var mcpServiceField = typeof(ApiService).GetField("mcpService",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (mcpServiceField?.GetValue(apiService) is McpService mcpService)
                {
                    // 显示服务器状态
                    var serverStatuses = mcpService.GetServerStatus();
                    await DisplayMcpServersStatus(serverStatuses);

                    // 显示可用工具
                    var availableTools = await mcpService.GetAvailableToolsAsync();
                    DisplayMcpTools(availableTools);

                    // 显示活动日志
                    UpdateMcpActivityDisplay();
                }
                else
                {
                    DisplayMcpNotAvailable();
                }
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
                var noServersText = new TextBlock
                {
                    Text = "未配置MCP服务器",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                    Margin = new Thickness(0, 5, 0, 5)
                };
                McpServersPanel.Children.Add(noServersText);
                return Task.CompletedTask;
            }

            foreach (var server in serverStatuses)
            {
                var serverPanel = new Border
                {
                    Background = new SolidColorBrush(server.IsConnected ?
                        GetThemeUIColor("background_success") : GetThemeUIColor("background_error")),
                    BorderBrush = new SolidColorBrush(server.IsConnected ?
                        GetThemeUIColor("border_success") : GetThemeUIColor("border_error")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(10, 8, 10, 8)
                };

                var contentPanel = new StackPanel();

                // 服务器名称和状态
                var headerPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal
                };

                var statusIcon = new TextBlock
                {
                    Text = server.IsConnected ? "🟢" : "🔴",
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };

                var serverName = new TextBlock
                {
                    Text = server.Name,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var toolCount = new TextBlock
                {
                    Text = $"({server.ToolCount}工具)",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };

                headerPanel.Children.Add(statusIcon);
                headerPanel.Children.Add(serverName);
                headerPanel.Children.Add(toolCount);
                contentPanel.Children.Add(headerPanel);

                // 连接状态详情
                var statusText = new TextBlock
                {
                    Text = server.IsConnected ? "已连接" : "连接失败",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(server.IsConnected ? GetThemeUIColor("success") : GetThemeUIColor("error")),
                    Margin = new Thickness(20, 2, 0, 0)
                };
                contentPanel.Children.Add(statusText);

                serverPanel.Child = contentPanel;
                McpServersPanel.Children.Add(serverPanel);
            }

            return Task.CompletedTask;
        }

        /// 显示MCP工具列表
        private void DisplayMcpTools(IList<McpClientTool> tools)
        {
            if (tools == null || tools.Count == 0)
            {
                var noToolsText = new TextBlock
                {
                    Text = "无可用工具",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 5, 0, 5)
                };
                McpToolsPanel.Children.Add(noToolsText);
                return;
            }

            foreach (var tool in tools)
            {
                var toolPanel = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(248, 248, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 220)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 2, 0, 2),
                    Padding = new Thickness(8, 6, 8, 6)
                };

                var contentPanel = new StackPanel();

                // 工具名称
                var toolName = new TextBlock
                {
                    Text = $"🔧 {tool.Name}",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(25, 25, 112))
                };
                contentPanel.Children.Add(toolName);

                // 工具描述
                if (!string.IsNullOrEmpty(tool.Description))
                {
                    var description = new TextBlock
                    {
                        Text = tool.Description,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Colors.Gray),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    contentPanel.Children.Add(description);
                }

                toolPanel.Child = contentPanel;
                McpToolsPanel.Children.Add(toolPanel);
            }
        }

        /// 显示MCP不可用状态
        private void DisplayMcpNotAvailable()
        {
            var notAvailableText = new TextBlock
            {
                Text = "MCP功能未启用或未配置",
                FontSize = 12,
                Foreground = new SolidColorBrush(GetThemeUIColor("warning")),
                Margin = new Thickness(0, 10, 0, 10),
                TextAlignment = TextAlignment.Center
            };
            McpServersPanel.Children.Add(notAvailableText);
        }

        /// 显示MCP错误状态
        private void DisplayMcpError(string errorMessage)
        {
            var errorText = new TextBlock
            {
                Text = $"MCP状态获取失败: {errorMessage}",
                FontSize = 11,
                Foreground = new SolidColorBrush(GetThemeUIColor("error")),
                Margin = new Thickness(0, 10, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            McpServersPanel.Children.Add(errorText);
        }

        /// 更新MCP活动日志显示
        private void UpdateMcpActivityDisplay()
        {
            try
            {
                McpActivityPanel.Children.Clear();

                if (apiService == null)
                {
                    var noActivityText = new TextBlock
                    {
                        Text = "无活动记录",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    McpActivityPanel.Children.Add(noActivityText);
                    return;
                }

                var activityLogger = apiService.GetActivityLogger();
                if (activityLogger == null)
                {
                    var noLoggerText = new TextBlock
                    {
                        Text = "活动日志记录器未初始化",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(GetThemeUIColor("warning")),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    McpActivityPanel.Children.Add(noLoggerText);
                    return;
                }

                // 获取最近的活动记录
                var recentActivities = activityLogger.GetRecentActivities(20);
                if (recentActivities.Count == 0)
                {
                    var emptyText = new TextBlock
                    {
                        Text = "暂无活动记录",
                        FontSize = 11,
                        Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    McpActivityPanel.Children.Add(emptyText);
                    return;
                }

                // 显示统计信息
                var stats = activityLogger.GetStatistics();
                if (stats.TotalToolCalls > 0)
                {
                    var statsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var statsText = new TextBlock
                    {
                        Text = $"总调用: {stats.TotalToolCalls} | 成功: {stats.SuccessfulCalls} | 失败: {stats.FailedCalls}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(GetThemeUIColor("success")),
                        FontWeight = FontWeights.SemiBold
                    };

                    if (stats.TotalToolCalls > 0)
                    {
                        var avgTimeText = new TextBlock
                        {
                            Text = $" | 平均耗时: {stats.AverageExecutionTime:F0}ms",
                            FontSize = 10,
                            Foreground = new SolidColorBrush(GetThemeUIColor("success")),
                            Margin = new Thickness(5, 0, 0, 0)
                        };
                        statsPanel.Children.Add(avgTimeText);
                    }

                    statsPanel.Children.Add(statsText);
                    McpActivityPanel.Children.Add(statsPanel);

                    // 添加分隔线
                    var separator = new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Colors.LightGray),
                        Margin = new Thickness(0, 5, 0, 8)
                    };
                    McpActivityPanel.Children.Add(separator);
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
            var activityPanel = new Border
            {
                Background = new SolidColorBrush(GetActivityBackgroundColor(activity)),
                BorderBrush = new SolidColorBrush(GetActivityBorderColor(activity)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(6, 4, 6, 4)
            };

            var contentPanel = new StackPanel();

            // 活动标题行
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            var timeText = new TextBlock
            {
                Text = activity.Timestamp.ToString("HH:mm:ss"),
                FontSize = 9,
                Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var activityIcon = new TextBlock
            {
                Text = GetActivityIcon(activity),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var activityText = new TextBlock
            {
                Text = GetActivityDisplayText(activity),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(GetActivityTextColor(activity)),
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(timeText);
            headerPanel.Children.Add(activityIcon);
            headerPanel.Children.Add(activityText);
            contentPanel.Children.Add(headerPanel);

            // 详细信息（如果有）
            if (activity.ActivityType == "ToolCallComplete" && activity.ExecutionTimeMs > 0)
            {
                var detailText = new TextBlock
                {
                    Text = $"耗时: {activity.ExecutionTimeMs:F0}ms",
                    FontSize = 9,
                    Foreground = new SolidColorBrush(GetThemeUIColor("muted")),
                    Margin = new Thickness(40, 1, 0, 0)
                };
                contentPanel.Children.Add(detailText);
            }

            if (!string.IsNullOrEmpty(activity.ErrorMessage))
            {
                var errorText = new TextBlock
                {
                    Text = activity.ErrorMessage,
                    FontSize = 9,
                    Foreground = new SolidColorBrush(GetThemeUIColor("error")),
                    Margin = new Thickness(40, 1, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                contentPanel.Children.Add(errorText);
            }

            activityPanel.Child = contentPanel;
            McpActivityPanel.Children.Add(activityPanel);
        }

        /// 获取活动背景颜色
        private Color GetActivityBackgroundColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? GetThemeUIColor("background_success") : GetThemeUIColor("background_error"),
                "ToolCallComplete" => activity.IsSuccess ? GetThemeUIColor("background_success") : GetThemeUIColor("background_error"),
                _ => Color.FromRgb(248, 248, 248)
            };
        }

        /// 获取活动边框颜色
        private Color GetActivityBorderColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? GetThemeUIColor("border_success") : GetThemeUIColor("border_error"),
                "ToolCallComplete" => activity.IsSuccess ? GetThemeUIColor("border_success") : GetThemeUIColor("border_error"),
                _ => GetThemeUIColor("muted")
            };
        }

        /// 获取活动图标
        private string GetActivityIcon(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? "🔗" : "❌",
                "ToolCallComplete" => activity.IsSuccess ? "🔧" : "⚠️",
                _ => "📝"
            };
        }

        /// 获取活动显示文本
        private string GetActivityDisplayText(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => $"{activity.ServerName} {(activity.IsSuccess ? "连接成功" : "连接失败")}",
                "ToolCallComplete" => $"{activity.ToolName} {(activity.IsSuccess ? "执行完成" : "执行失败")}",
                _ => $"{activity.ActivityType}: {activity.ServerName}"
            };
        }

        /// 获取活动文本颜色
        private Color GetActivityTextColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? GetThemeUIColor("success") : GetThemeUIColor("error"),
                "ToolCallComplete" => activity.IsSuccess ? GetThemeUIColor("success") : GetThemeUIColor("error"),
                _ => GetThemeUIColor("muted")
            };
        }

        #endregion
    }
}
