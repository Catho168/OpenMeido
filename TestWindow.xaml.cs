using System;
using System.Windows;

namespace OpenMeido
{
    /// <summary>
    /// 测试窗口，用于验证各个组件是否正常工作
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        private void TestSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "正在测试设置功能...";
                
                // 测试AppSettings类
                var settings = new AppSettings();
                settings.ApiBaseUrl = "https://api.openai.com/v1";
                settings.ApiKey = "test-key";
                settings.ModelName = "gpt-3.5-turbo";
                
                bool isValid = settings.IsValid();
                StatusText.Text = $"设置验证: {(isValid ? "成功" : "失败")}";
                
                // 尝试打开设置窗口
                var settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
                
                StatusText.Text = "设置功能测试完成";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"设置测试失败: {ex.Message}";
                MessageBox.Show($"设置测试错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestChatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "正在测试聊天功能...";
                
                // 尝试打开聊天窗口
                var chatWindow = new ChatWindow();
                chatWindow.Show();
                
                StatusText.Text = "聊天功能测试完成";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"聊天测试失败: {ex.Message}";
                MessageBox.Show($"聊天测试错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TestApiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "正在测试API配置...";
                
                // 测试配置保存和加载
                var settings = new AppSettings
                {
                    ApiBaseUrl = "https://api.openai.com/v1",
                    ApiKey = "test-key-123",
                    ModelName = "gpt-3.5-turbo",
                    MaxTokens = 1000,
                    Temperature = 0.7
                };
                
                // 保存配置
                settings.Save();
                StatusText.Text = "配置保存成功";
                
                // 加载配置
                var loadedSettings = AppSettings.Load();
                bool configMatch = loadedSettings.ApiBaseUrl == settings.ApiBaseUrl;
                
                StatusText.Text = $"API配置测试: {(configMatch ? "成功" : "失败")}";
                
                MessageBox.Show($"API配置测试完成\n保存: 成功\n加载: {(configMatch ? "成功" : "失败")}", 
                    "测试结果", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"API测试失败: {ex.Message}";
                MessageBox.Show($"API测试错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDebugButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "正在打开API调试工具...";

                // 打开API调试窗口
                var debugWindow = new ApiDebugWindow();
                debugWindow.Show();

                StatusText.Text = "API调试工具已打开";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"打开调试工具失败: {ex.Message}";
                MessageBox.Show($"打开调试工具错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
