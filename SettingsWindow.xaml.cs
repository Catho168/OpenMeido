using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido
{
    /// 设置窗口的交互逻辑
    /// 用于配置妹抖酱的API参数和其他设置
    public partial class SettingsWindow : Window
    {
        // 存储当前的应用程序设置
        private AppSettings currentSettings;

        // 标记设置是否已保存
        private bool settingsSaved = false;

        // 标记窗口是否正在关闭，防止递归调用
        private bool isClosing = false;

        // MCP服务器配置的可观察集合
        private ObservableCollection<McpServerConfig> mcpServers;

        // MCP服务实例，用于测试连接
        private McpService mcpService;

        // 当前选中的设置分类
        private SettingsCategory currentCategory = SettingsCategory.General;

        /// 构造函数，初始化设置窗口
        public SettingsWindow()
        {
            InitializeComponent();

            // 加载当前设置
            LoadCurrentSettings();

            // 绑定滑块值变化事件
            MaxTokensSlider.ValueChanged += MaxTokensSlider_ValueChanged;
            TemperatureSlider.ValueChanged += TemperatureSlider_ValueChanged;

            // 设置窗口关闭事件
            this.Closing += SettingsWindow_Closing;

            // 初始化分类导航
            InitializeCategoryNavigation();
        }

        /// 加载当前应用程序设置到界面控件
        private void LoadCurrentSettings()
        {
            try
            {
                // 从配置文件加载设置
                currentSettings = AppSettings.Load();
                
                // 将设置值填充到界面控件
                ApiBaseUrlTextBox.Text = currentSettings.ApiBaseUrl;
                ApiKeyPasswordBox.Password = currentSettings.ApiKey;
                ModelNameComboBox.Text = currentSettings.ModelName;
                MaxTokensSlider.Value = currentSettings.MaxTokens;
                TemperatureSlider.Value = currentSettings.Temperature;
                SystemPromptTextBox.Text = currentSettings.SystemPrompt;

                // 加载MCP设置
                LoadMcpSettings();

                // 更新标签显示
                UpdateSliderLabels();

                // 设置当前分类
                currentCategory = currentSettings.SelectedCategory;
            }
            catch (Exception ex)
            {
                // 如果加载设置失败，显示错误消息
                MessageBox.Show($"加载妹抖酱的设置时出错了: {ex.Message}", "出错了",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                // 使用默认设置
                currentSettings = new AppSettings();
            }
        }

        /// 初始化分类导航
        private void InitializeCategoryNavigation()
        {
            // 根据当前分类设置界面
            SwitchToCategory(currentCategory);
        }

        /// 分类按钮点击事件处理器
        private void CategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string categoryTag)
            {
                if (Enum.TryParse<SettingsCategory>(categoryTag, out var category))
                {
                    SwitchToCategory(category);
                }
            }
        }

        /// 切换到指定分类
        private void SwitchToCategory(SettingsCategory category)
        {
            currentCategory = category;

            // 更新按钮样式
            UpdateCategoryButtonStyles();

            // 显示对应的设置面板
            switch (category)
            {
                case SettingsCategory.General:
                    GeneralSettingsPanel.Visibility = Visibility.Visible;
                    McpSettingsPanel.Visibility = Visibility.Collapsed;
                    break;
                case SettingsCategory.MCP:
                    GeneralSettingsPanel.Visibility = Visibility.Collapsed;
                    McpSettingsPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// 更新分类按钮样式
        private void UpdateCategoryButtonStyles()
        {
            // 重置所有按钮样式
            GeneralCategoryButton.Style = (Style)FindResource("CategoryButtonStyle");
            McpCategoryButton.Style = (Style)FindResource("CategoryButtonStyle");

            // 设置选中按钮样式
            switch (currentCategory)
            {
                case SettingsCategory.General:
                    GeneralCategoryButton.Style = (Style)FindResource("SelectedCategoryButtonStyle");
                    break;
                case SettingsCategory.MCP:
                    McpCategoryButton.Style = (Style)FindResource("SelectedCategoryButtonStyle");
                    break;
            }
        }

        /// 更新滑块标签显示
        private void UpdateSliderLabels()
        {
            MaxTokensLabel.Text = ((int)MaxTokensSlider.Value).ToString();
            TemperatureLabel.Text = TemperatureSlider.Value.ToString("F1");
        }

        /// 最大令牌数滑块值变化事件处理器
        private void MaxTokensSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxTokensLabel != null)
            {
                MaxTokensLabel.Text = ((int)e.NewValue).ToString();
            }
        }

        /// 温度滑块值变化事件处理器
        private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TemperatureLabel != null)
            {
                TemperatureLabel.Text = e.NewValue.ToString("F1");
            }
        }

        /// 测试连接按钮点击事件处理器
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 禁用测试按钮，防止重复点击
                TestConnectionButton.IsEnabled = false;
                TestConnectionButton.Content = "测试中~";
                
                // 从界面获取当前设置
                var testSettings = GetSettingsFromUI();
                
                // 验证设置是否有效
                if (!testSettings.IsValid())
                {
                    MessageBox.Show("请把API配置信息填写完整哦~", "配置不完整",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 创建API服务实例并测试连接
                using (var apiService = new ApiService(testSettings))
                {
                    bool connectionSuccess = await apiService.TestConnectionAsync();
                    
                    if (connectionSuccess)
                    {
                        MessageBox.Show("妹抖酱连接成功！可以开始聊天了♪", "连接成功",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("妹抖酱连接失败了，请检查配置信息~", "连接失败",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试连接时出错了: {ex.Message}", "出错了",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 恢复测试按钮状态
                TestConnectionButton.IsEnabled = true;
                TestConnectionButton.Content = "测试连接";
            }
        }

        /// 保存设置按钮点击事件处理器
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从界面获取设置
                var newSettings = GetSettingsFromUI();
                
                // 验证设置是否有效
                if (!newSettings.IsValid())
                {
                    MessageBox.Show("请填写完整且正确的配置信息", "配置无效",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 异步保存设置到文件
                await newSettings.SaveAsync();
                
                // 更新当前设置
                currentSettings = newSettings;
                settingsSaved = true;
                
                // 只有在窗口未关闭时才显示消息框
                if (!isClosing)
                {
                    MessageBox.Show("设置已保存成功！", "保存成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                // 不再在这里调用 this.Close()，因为窗口可能已经处于关闭过程中
            }
            catch (Exception ex)
            {
                // 只有在窗口未关闭时才显示错误消息框
                if (!isClosing)
                {
                    MessageBox.Show($"保存设置时出错: {ex.Message}", "保存失败",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// 取消按钮点击事件处理器
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 直接关闭窗口，不保存更改
            this.Close();
        }

        /// 从界面控件获取设置对象
        /// <returns>包含界面设置值的AppSettings对象</returns>
        private AppSettings GetSettingsFromUI()
        {
            var settings = new AppSettings
            {
                ApiBaseUrl = ApiBaseUrlTextBox.Text?.Trim() ?? "",
                ApiKey = ApiKeyPasswordBox.Password?.Trim() ?? "",
                ModelName = ModelNameComboBox.Text?.Trim() ?? "",
                MaxTokens = (int)MaxTokensSlider.Value,
                Temperature = TemperatureSlider.Value,
                SystemPrompt = SystemPromptTextBox.Text?.Trim() ?? "",
                EnableMcp = EnableMcpCheckBox.IsChecked == true,
                McpServers = mcpServers?.ToList() ?? new System.Collections.Generic.List<McpServerConfig>(),
                SelectedCategory = currentCategory
            };

            return settings;
        }

        /// 异步保存设置
        private async Task SaveSettingsAsync()
        {
            try
            {
                // 从界面获取设置
                var newSettings = GetSettingsFromUI();
                
                // 验证设置是否有效
                if (!newSettings.IsValid())
                {
                    MessageBox.Show("请填写完整且正确的配置信息", "配置无效",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // 异步保存设置到文件
                await newSettings.SaveAsync();
                
                // 更新当前设置
                currentSettings = newSettings;
                settingsSaved = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}", "保存失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 窗口关闭事件处理器
        private async void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果窗口已经在关闭过程中，不进行任何操作
            if (isClosing)
            {
                return;
            }

            // 标记窗口正在关闭
            isClosing = true;

            // 如果设置已更改但未保存，询问用户是否要保存
            if (!settingsSaved && HasSettingsChanged())
            {
                var result = CustomNotificationWindow.Show("设置已更改但未保存，是否要保存更改？",
                    "未保存的更改", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, this);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 用户选择保存，触发保存操作
                    await SaveSettingsAsync();
                    
                    // 如果保存失败，取消关闭操作
                    if (!settingsSaved)
                    {
                        e.Cancel = true;
                        isClosing = false;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // 用户选择取消，不关闭窗口
                    e.Cancel = true;
                    isClosing = false;
                }
                // 如果用户选择No，直接关闭窗口，不保存更改
            }
        }

        /// 检查设置是否已更改
        /// <returns>如果设置已更改返回true，否则返回false</returns>
        private bool HasSettingsChanged()
        {
            var uiSettings = GetSettingsFromUI();
            
            // 检查基本设置是否改变
            if (currentSettings.ApiBaseUrl != uiSettings.ApiBaseUrl ||
                currentSettings.ApiKey != uiSettings.ApiKey ||
                currentSettings.ModelName != uiSettings.ModelName ||
                currentSettings.MaxTokens != uiSettings.MaxTokens ||
                currentSettings.EnableMcp != uiSettings.EnableMcp ||
                currentSettings.SelectedCategory != uiSettings.SelectedCategory ||
                currentSettings.SystemPrompt != uiSettings.SystemPrompt)
            {
                return true;
            }
            
            // 使用更安全的方式比较浮点数
            if (Math.Abs(currentSettings.Temperature - uiSettings.Temperature) > 0.001)
            {
                return true;
            }
            
            // 检查MCP服务器配置是否改变
            return HasMcpServersChanged(uiSettings);
        }

        /// 检查MCP服务器配置是否已更改
        /// <param name="uiSettings">界面设置</param>
        /// <returns>如果MCP服务器配置已更改返回true</returns>
        private bool HasMcpServersChanged(AppSettings uiSettings)
        {
            // 处理null情况
            var currentServers = currentSettings.McpServers ?? new List<McpServerConfig>();
            var uiServers = uiSettings.McpServers ?? new List<McpServerConfig>();

            if (currentServers.Count != uiServers.Count)
                return true;

            // 比较每个服务器配置
            for (int i = 0; i < currentServers.Count; i++)
            {
                var current = currentServers[i];
                var ui = uiServers[i];

                if (current.Id != ui.Id ||
                    current.Name != ui.Name ||
                    current.Command != ui.Command ||
                    current.Arguments != ui.Arguments ||
                    current.IsEnabled != ui.IsEnabled ||
                    current.Description != ui.Description)
                {
                    return true;
                }
            }

            return false;
        }

        /// 加载MCP设置
        private void LoadMcpSettings()
        {
            try
            {
                // 设置MCP启用状态
                EnableMcpCheckBox.IsChecked = currentSettings.EnableMcp;

                // 初始化MCP服务器集合
                mcpServers = new ObservableCollection<McpServerConfig>();
                if (currentSettings.McpServers != null)
                {
                    foreach (var server in currentSettings.McpServers)
                    {
                        mcpServers.Add(server);
                    }
                }

                // 绑定到列表框
                McpServersListBox.ItemsSource = mcpServers;

                // 初始化MCP服务
                mcpService = new McpService(currentSettings);

                // 更新MCP面板可见性
                UpdateMcpPanelVisibility();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载MCP设置时出错: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 更新MCP配置面板的可见性
        private void UpdateMcpPanelVisibility()
        {
            McpConfigPanel.Visibility = EnableMcpCheckBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }



        /// MCP启用复选框选中事件
        private void EnableMcpCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMcpPanelVisibility();
        }

        /// MCP启用复选框取消选中事件
        private void EnableMcpCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateMcpPanelVisibility();
        }

        /// MCP服务器列表选择变化事件
        private void McpServersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 不再自动进入编辑模式，用户需要明确点击编辑按钮
            // 这里可以添加一些选中状态的处理逻辑，如果需要的话
        }

        /// 添加MCP服务器按钮点击事件
        private void AddMcpServerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建编辑窗口（新建模式）
                var editWindow = new McpServerEditWindow(this, mcpServers?.ToList());
                
                // 显示模态对话框
                var result = editWindow.ShowDialog();
                
                if (result == true && editWindow.IsSaved)
                {
                    // 添加新的服务器配置
                    mcpServers.Add(editWindow.EditResult);
                    
                    MessageBox.Show("新MCP服务器配置已添加", "添加成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加MCP服务器时出错: {ex.Message}", "添加失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// 添加示例服务器配置按钮点击事件
        private void AddExampleServerButton_Click(object sender, RoutedEventArgs e)
        {
            var exampleMenu = new ContextMenu();

            // 文件系统服务器示例
            var fileSystemItem = new MenuItem
            {
                Header = "📁 文件系统访问",
                ToolTip = "允许读写本地文件系统"
            };
            fileSystemItem.Click += (s, args) => AddExampleServer("文件系统", "npx", "-y @modelcontextprotocol/server-filesystem C:\\", "允许读写本地文件系统");

            // Web搜索服务器示例
            var webSearchItem = new MenuItem
            {
                Header = "🔍 Web搜索",
                ToolTip = "提供网络搜索功能"
            };
            webSearchItem.Click += (s, args) => AddExampleServer("Web搜索", "npx", "-y @modelcontextprotocol/server-brave-search", "提供网络搜索功能");

            // Git服务器示例
            var gitItem = new MenuItem
            {
                Header = "📦 Git操作",
                ToolTip = "Git仓库操作功能"
            };
            gitItem.Click += (s, args) => AddExampleServer("Git助手", "npx", "-y @modelcontextprotocol/server-git C:\\YourRepo", "Git仓库操作功能");

            // SQLite数据库示例
            var sqliteItem = new MenuItem
            {
                Header = "🗄️ SQLite数据库",
                ToolTip = "SQLite数据库操作"
            };
            sqliteItem.Click += (s, args) => AddExampleServer("SQLite数据库", "npx", "-y @modelcontextprotocol/server-sqlite C:\\path\\to\\database.db", "SQLite数据库操作");

            exampleMenu.Items.Add(fileSystemItem);
            exampleMenu.Items.Add(webSearchItem);
            exampleMenu.Items.Add(gitItem);
            exampleMenu.Items.Add(sqliteItem);

            exampleMenu.PlacementTarget = AddExampleServerButton;
            exampleMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            exampleMenu.IsOpen = true;
        }

        /// 添加示例服务器配置
        private void AddExampleServer(string name, string command, string arguments, string description)
        {
            try
            {
                var exampleServer = new McpServerConfig
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    Command = command,
                    Arguments = arguments,
                    Description = description,
                    IsEnabled = true
                };

                // 创建编辑窗口（新建模式，但预填充数据）
                var editWindow = new McpServerEditWindow(this, mcpServers?.ToList());
                
                // 可以通过反射或者其他方式设置预填充数据，这里简化处理
                // 直接传入预设服务器进行编辑
                var prefilledEditWindow = new McpServerEditWindow(this, exampleServer, mcpServers?.ToList());
                
                // 显示模态对话框
                var result = prefilledEditWindow.ShowDialog();
                
                if (result == true && prefilledEditWindow.IsSaved)
                {
                    // 添加新的服务器配置
                    mcpServers.Add(prefilledEditWindow.EditResult);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加示例服务器时出错: {ex.Message}", "添加失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        /// 编辑MCP服务器按钮点击事件
        private void EditMcpServer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button?.Tag as McpServerConfig;

            if (server != null)
            {
                try
                {
                    // 创建编辑窗口（编辑模式）
                    var editWindow = new McpServerEditWindow(this, server, mcpServers?.ToList());
                    
                    // 显示模态对话框
                    var result = editWindow.ShowDialog();
                    
                    if (result == true && editWindow.IsSaved)
                    {
                        // 更新现有的服务器配置
                        var index = mcpServers.IndexOf(server);
                        if (index >= 0)
                        {
                            // 移除旧的配置，添加新的配置以触发UI更新
                            mcpServers.RemoveAt(index);
                            mcpServers.Insert(index, editWindow.EditResult);
                            
                            MessageBox.Show("服务器配置已更新", "编辑成功",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"编辑MCP服务器时出错: {ex.Message}", "编辑失败",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// 删除MCP服务器按钮点击事件
        private void DeleteMcpServer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button?.Tag as McpServerConfig;

            if (server != null)
            {
                var result = MessageBox.Show($"确定要删除MCP服务器 '{server.Name}' 吗？", "确认删除",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    mcpServers.Remove(server);
                }
            }
        }

        /// 测试MCP服务器连接按钮点击事件
        private async void TestMcpServer_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var server = button?.Tag as McpServerConfig;

            if (server != null)
            {
                button.IsEnabled = false;
                button.Content = "测试中...";

                try
                {
                    var (success, message) = await mcpService.TestConnectionAsync(server);

                    MessageBox.Show(message, success ? "连接成功" : "连接失败",
                        MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"测试连接时出错: {ex.Message}", "测试失败",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    button.Content = "测试";
                    button.IsEnabled = true;
                }
            }
        }

        /// MCP服务器启用状态变化事件
        private void McpServerEnabled_Changed(object sender, RoutedEventArgs e)
        {
            // 这里可以添加启用状态变化的处理逻辑
            // 当前实现会自动通过数据绑定更新
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

        /// 窗口关闭时清理资源
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }
    }
}
