using System;
using System.Threading.Tasks;
using System.Windows;

namespace OpenMeido
{
    /// <summary>
    /// API调试工具窗口
    /// </summary>
    public partial class ApiDebugWindow : Window
    {
        private ApiService apiService;
        private AppSettings settings;

        public ApiDebugWindow()
        {
            InitializeComponent();
            InitializeApiService();
        }

        private void InitializeApiService()
        {
            try
            {
                settings = AppSettings.Load();
                if (settings.IsValid())
                {
                    apiService = new ApiService(settings);
                    LogMessage("✓ API服务初始化成功");
                    LogMessage($"API URL: {settings.ApiBaseUrl}");
                    LogMessage($"模型: {settings.ModelName}");
                    LogMessage($"最大令牌: {settings.MaxTokens}");
                    LogMessage($"温度: {settings.Temperature}");
                }
                else
                {
                    LogMessage("✗ API配置无效，请先在设置中配置");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"✗ 初始化失败: {ex.Message}");
            }
        }

        private void LogMessage(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            LogTextBox.AppendText($"[{timestamp}] {message}\n");
            LogTextBox.ScrollToEnd();
        }

        private async void SendTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (apiService == null)
            {
                LogMessage("✗ API服务未初始化");
                return;
            }

            string testMessage = TestMessageTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(testMessage))
            {
                LogMessage("✗ 请输入测试消息");
                return;
            }

            try
            {
                StatusTextBlock.Text = "发送中...";
                SendTestButton.IsEnabled = false;

                LogMessage($"📤 发送消息: {testMessage}");
                
                // 构造历史消息，仅本条
                var testHistory = new System.Collections.Generic.List<ChatMessage> { new ChatMessage("user", testMessage) };
                string response = await apiService.SendMessageAsync(testHistory);
                
                LogMessage($"📥 收到响应: {response}");

                if (response.StartsWith("网络请求错误") || 
                    response.StartsWith("API请求失败") ||
                    response.Contains("解析失败"))
                {
                    StatusTextBlock.Text = "请求失败";
                    LogMessage("❌ 请求失败，请检查上面的错误信息");
                }
                else
                {
                    StatusTextBlock.Text = "请求成功";
                    LogMessage("✅ 请求成功");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 异常: {ex.Message}");
                StatusTextBlock.Text = "发生异常";
            }
            finally
            {
                SendTestButton.IsEnabled = true;
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (apiService == null)
            {
                LogMessage("✗ API服务未初始化");
                return;
            }

            try
            {
                StatusTextBlock.Text = "测试连接中...";
                TestConnectionButton.IsEnabled = false;

                LogMessage("🔗 开始连接测试...");
                
                bool isConnected = await apiService.TestConnectionAsync();
                
                if (isConnected)
                {
                    LogMessage("✅ 连接测试成功");
                    StatusTextBlock.Text = "连接正常";
                }
                else
                {
                    LogMessage("❌ 连接测试失败");
                    StatusTextBlock.Text = "连接失败";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ 连接测试异常: {ex.Message}");
                StatusTextBlock.Text = "测试异常";
            }
            finally
            {
                TestConnectionButton.IsEnabled = true;
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            StatusTextBlock.Text = "日志已清空";
        }

        protected override void OnClosed(EventArgs e)
        {
            apiService?.Dispose();
            base.OnClosed(e);
        }
    }
}
