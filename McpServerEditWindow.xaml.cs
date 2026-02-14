using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using OpenMeido.Models;

namespace OpenMeido
{
    /// <summary>
    /// MCP服务器编辑窗口的交互逻辑
    /// 用于配置MCP服务器的详细参数
    /// </summary>
    public partial class McpServerEditWindow : Window
    {
        // 当前正在编辑的MCP服务器配置
        private McpServerConfig serverConfig;

        // 是否是新建模式（true为新建，false为编辑）
        private bool isNewServer;

        // 已存在的服务器列表，用于验证名称唯一性
        private System.Collections.Generic.List<McpServerConfig> existingServers;

        /// <summary>
        /// 编辑结果属性，外部可通过此属性获取编辑后的配置
        /// </summary>
        public McpServerConfig EditResult { get; private set; }

        /// <summary>
        /// 是否保存了更改
        /// </summary>
        public bool IsSaved { get; private set; } = false;

        /// <summary>
        /// 构造函数 - 新建服务器模式
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="existingServers">已存在的服务器列表</param>
        public McpServerEditWindow(Window owner, System.Collections.Generic.List<McpServerConfig> existingServers)
        {
            InitializeComponent();
            this.Owner = owner;
            this.existingServers = existingServers ?? new System.Collections.Generic.List<McpServerConfig>();
            
            // 新建模式
            isNewServer = true;
            serverConfig = new McpServerConfig
            {
                Id = Guid.NewGuid().ToString(),
                Name = "",
                Command = "",
                Arguments = "",
                Description = "",
                IsEnabled = true
            };

            // 设置窗口标题
            WindowTitleText.Text = "添加MCP服务器";
            FormTitleText.Text = "新服务器配置";
            SaveButton.Content = "💾 创建服务器";

            // 加载数据到界面
            LoadServerDataToUI();

            // 设置焦点到服务器名称文本框
            ServerNameTextBox.Focus();
        }

        /// <summary>
        /// 构造函数 - 编辑服务器模式
        /// </summary>
        /// <param name="owner">父窗口</param>
        /// <param name="serverToEdit">要编辑的服务器配置</param>
        /// <param name="existingServers">已存在的服务器列表</param>
        public McpServerEditWindow(Window owner, McpServerConfig serverToEdit, System.Collections.Generic.List<McpServerConfig> existingServers)
        {
            InitializeComponent();
            this.Owner = owner;
            this.existingServers = existingServers ?? new System.Collections.Generic.List<McpServerConfig>();
            
            // 编辑模式
            isNewServer = false;
            
            // 克隆服务器配置以避免直接修改原对象
            serverConfig = new McpServerConfig
            {
                Id = serverToEdit.Id,
                Name = serverToEdit.Name,
                Command = serverToEdit.Command,
                Arguments = serverToEdit.Arguments,
                Description = serverToEdit.Description,
                IsEnabled = serverToEdit.IsEnabled
            };

            // 设置窗口标题
            WindowTitleText.Text = $"编辑MCP服务器 - {serverConfig.Name}";
            FormTitleText.Text = $"编辑 {serverConfig.Name} 配置";
            SaveButton.Content = "💾 保存更改";

            // 加载数据到界面
            LoadServerDataToUI();

            // 设置焦点到服务器名称文本框
            ServerNameTextBox.Focus();
            ServerNameTextBox.SelectAll();
        }

        /// <summary>
        /// 将服务器配置数据加载到界面控件
        /// </summary>
        private void LoadServerDataToUI()
        {
            ServerNameTextBox.Text = serverConfig.Name;
            ServerCommandTextBox.Text = serverConfig.Command;
            ServerArgumentsTextBox.Text = serverConfig.Arguments;
            ServerDescriptionTextBox.Text = serverConfig.Description;
        }

        /// <summary>
        /// 从界面控件获取服务器配置数据
        /// </summary>
        private void GetServerDataFromUI()
        {
            serverConfig.Name = ServerNameTextBox.Text?.Trim() ?? "";
            serverConfig.Command = ServerCommandTextBox.Text?.Trim() ?? "";
            serverConfig.Arguments = ServerArgumentsTextBox.Text?.Trim() ?? "";
            serverConfig.Description = ServerDescriptionTextBox.Text?.Trim() ?? "";
        }

        /// <summary>
        /// 验证服务器配置是否有效
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateServerConfig()
        {
            // 验证服务器名称
            if (string.IsNullOrWhiteSpace(serverConfig.Name))
            {
                MessageBox.Show("请输入服务器名称", "配置验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ServerNameTextBox.Focus();
                return false;
            }

            // 验证名称唯一性（排除当前编辑的服务器）
            var existingServer = existingServers.FirstOrDefault(s => 
                s.Id != serverConfig.Id && 
                string.Equals(s.Name, serverConfig.Name, StringComparison.OrdinalIgnoreCase));
            
            if (existingServer != null)
            {
                MessageBox.Show($"服务器名称 '{serverConfig.Name}' 已存在，请使用不同的名称", "配置验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ServerNameTextBox.Focus();
                ServerNameTextBox.SelectAll();
                return false;
            }

            // 验证启动命令
            if (string.IsNullOrWhiteSpace(serverConfig.Command))
            {
                MessageBox.Show("请输入启动命令", "配置验证",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ServerCommandTextBox.Focus();
                return false;
            }

            // 验证命令格式（可选）
            if (!IsValidCommand(serverConfig.Command))
            {
                var result = MessageBox.Show(
                    $"命令 '{serverConfig.Command}' 可能无效。\n\n" +
                    "常用命令包括：npx, python, node, 或可执行文件的完整路径。\n\n" +
                    "是否继续保存？",
                    "命令格式提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    ServerCommandTextBox.Focus();
                    ServerCommandTextBox.SelectAll();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 验证命令是否有效（基本格式检查）
        /// </summary>
        /// <param name="command">命令字符串</param>
        /// <returns>是否有效</returns>
        private bool IsValidCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            // 常见的有效命令
            string[] commonCommands = { "node", "python", "python3", "npx", "npm", "yarn", "dotnet", "java" };
            
            var commandLower = command.ToLower();
            
            // 检查是否是常见命令
            if (commonCommands.Any(cmd => commandLower.StartsWith(cmd)))
                return true;
            
            // 检查是否是可执行文件路径
            if (commandLower.EndsWith(".exe") || commandLower.EndsWith(".bat") || commandLower.EndsWith(".cmd"))
                return true;
            
            // 检查是否包含路径分隔符（可能是完整路径）
            if (command.Contains("\\") || command.Contains("/"))
                return true;
            
            return false;
        }

        /// <summary>
        /// 保存按钮点击事件
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从界面获取数据
                GetServerDataFromUI();

                // 验证配置
                if (!ValidateServerConfig())
                {
                    return;
                }

                // 设置编辑结果
                EditResult = serverConfig;
                IsSaved = true;

                // 显示成功消息
                string message = isNewServer ? "MCP服务器配置已创建！" : "MCP服务器配置已更新！";
                MessageBox.Show(message, "保存成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 关闭窗口
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置时出错: {ex.Message}", "保存失败",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消按钮点击事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否有未保存的更改
            if (HasUnsavedChanges())
            {
                var result = CustomNotificationWindow.Show(
                    "您有未保存的更改，确定要取消吗？",
                    "确认取消", MessageBoxButton.YesNo, MessageBoxImage.Question, this);
                
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            // 关闭窗口
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton_Click(sender, e);
        }

        /// <summary>
        /// 标题栏鼠标按下事件处理器 - 实现窗口拖拽
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 检查是否有未保存的更改
        /// </summary>
        /// <returns>是否有未保存的更改</returns>
        private bool HasUnsavedChanges()
        {
            var currentName = ServerNameTextBox.Text?.Trim() ?? "";
            var currentCommand = ServerCommandTextBox.Text?.Trim() ?? "";
            var currentArguments = ServerArgumentsTextBox.Text?.Trim() ?? "";
            var currentDescription = ServerDescriptionTextBox.Text?.Trim() ?? "";

            return serverConfig.Name != currentName ||
                   serverConfig.Command != currentCommand ||
                   serverConfig.Arguments != currentArguments ||
                   serverConfig.Description != currentDescription;
        }

        /// <summary>
        /// 窗口加载完成事件
        /// </summary>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // 可以在这里添加一些初始化逻辑
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        /// <summary>
        /// 处理键盘事件，支持 ESC 取消和 Ctrl+S 保存
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                CancelButton_Click(this, new RoutedEventArgs());
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}