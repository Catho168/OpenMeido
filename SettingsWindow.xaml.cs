using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                
                // 更新标签显示
                UpdateSliderLabels();
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
        private void SaveButton_Click(object sender, RoutedEventArgs e)
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
                
                // 保存设置到文件
                newSettings.Save();
                
                // 更新当前设置
                currentSettings = newSettings;
                settingsSaved = true;
                
                MessageBox.Show("设置已保存成功！", "保存成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 关闭窗口
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置时出错: {ex.Message}", "保存失败", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            return new AppSettings
            {
                ApiBaseUrl = ApiBaseUrlTextBox.Text?.Trim() ?? "",
                ApiKey = ApiKeyPasswordBox.Password?.Trim() ?? "",
                ModelName = ModelNameComboBox.Text?.Trim() ?? "",
                MaxTokens = (int)MaxTokensSlider.Value,
                Temperature = TemperatureSlider.Value,
                SystemPrompt = SystemPromptTextBox.Text?.Trim() ?? ""
            };
        }

        /// 窗口关闭事件处理器
        private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 如果设置已更改但未保存，询问用户是否要保存
            if (!settingsSaved && HasSettingsChanged())
            {
                var result = MessageBox.Show("设置已更改但未保存，是否要保存更改？", 
                    "未保存的更改", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // 用户选择保存，触发保存操作
                    SaveButton_Click(this, new RoutedEventArgs());
                    
                    // 如果保存失败，取消关闭操作
                    if (!settingsSaved)
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    // 用户选择取消，不关闭窗口
                    e.Cancel = true;
                }
                // 如果用户选择No，直接关闭窗口，不保存更改
            }
        }

        /// 检查设置是否已更改
        /// <returns>如果设置已更改返回true，否则返回false</returns>
        private bool HasSettingsChanged()
        {
            var uiSettings = GetSettingsFromUI();
            
            return currentSettings.ApiBaseUrl != uiSettings.ApiBaseUrl ||
                   currentSettings.ApiKey != uiSettings.ApiKey ||
                   currentSettings.ModelName != uiSettings.ModelName ||
                   currentSettings.MaxTokens != uiSettings.MaxTokens ||
                   Math.Abs(currentSettings.Temperature - uiSettings.Temperature) > 0.01 ||
                   currentSettings.SystemPrompt != uiSettings.SystemPrompt;
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
    }
}
