using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido
{
    /// 自定义通知窗口，提供与主界面设计系统一致的弹窗体验
    public partial class CustomNotificationWindow : Window
    {
        private MessageBoxResult _result = MessageBoxResult.None;
        private readonly List<Button> _buttons = new List<Button>();

        /// 构造函数
        public CustomNotificationWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += (s, e) => this.DragMove();
        }

        /// 显示通知对话框
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <param name="button">按钮类型</param>
        /// <param name="icon">图标类型</param>
        /// <param name="owner">父窗口</param>
        /// <returns>用户选择的结果</returns>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.Information, Window owner = null)
        {
            var window = new CustomNotificationWindow
            {
                Title = title,
                Owner = owner
            };
            
            return window.ShowInternal(message, title, button, icon);
        }

        /// 内部显示方法
        private MessageBoxResult ShowInternal(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            // 设置消息和标题
            MessageTextBlock.Text = message;
            TitleTextBlock.Text = title;

            // 设置图标
            SetIcon(icon);

            // 设置按钮
            SetupButtons(button);

            // 调整窗口大小以适应内容
            AdjustWindowSize();

            // 显示窗口
            ShowDialog();

            return _result;
        }

        /// 设置图标
        private void SetIcon(MessageBoxImage icon)
        {
            string iconText;
            switch (icon)
            {
                case MessageBoxImage.Information:
                    iconText = "ℹ️";
                    break;
                case MessageBoxImage.Warning:
                    iconText = "⚠️";
                    break;
                case MessageBoxImage.Error:
                    iconText = "❌";
                    break;
                case MessageBoxImage.Question:
                    iconText = "❓";
                    break;
                default:
                    iconText = "💬";
                    break;
            }
            IconTextBlock.Text = iconText;
        }

        /// 设置按钮
        private void SetupButtons(MessageBoxButton button)
        {
            ButtonPanel.Children.Clear();
            _buttons.Clear();

            switch (button)
            {
                case MessageBoxButton.OK:
                    CreateButton("确定", MessageBoxResult.OK, "PrimaryButtonStyle", true);
                    break;
                case MessageBoxButton.OKCancel:
                    CreateButton("确定", MessageBoxResult.OK, "PrimaryButtonStyle", false);
                    CreateButton("取消", MessageBoxResult.Cancel, "SecondaryButtonStyle", true);
                    break;
                case MessageBoxButton.YesNo:
                    CreateButton("是", MessageBoxResult.Yes, "PrimaryButtonStyle", false);
                    CreateButton("否", MessageBoxResult.No, "SecondaryButtonStyle", true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    CreateButton("是", MessageBoxResult.Yes, "PrimaryButtonStyle", false);
                    CreateButton("否", MessageBoxResult.No, "SecondaryButtonStyle", false);
                    CreateButton("取消", MessageBoxResult.Cancel, "SecondaryButtonStyle", true);
                    break;
            }
        }

        /// 创建按钮
        private void CreateButton(string text, MessageBoxResult result, string styleName, bool isDefault)
        {
            var button = new Button
            {
                Content = text,
                Style = (Style)FindResource(styleName),
                IsDefault = isDefault,
                IsCancel = result == MessageBoxResult.Cancel || result == MessageBoxResult.No
            };

            button.Click += (s, e) =>
            {
                _result = result;
                Close();
            };

            if (isDefault)
            {
                button.Focus();
            }

            ButtonPanel.Children.Add(button);
            _buttons.Add(button);
        }

        /// 调整窗口大小以适应内容
        private void AdjustWindowSize()
        {
            // 测量文本大小
            var formattedText = new FormattedText(
                MessageTextBlock.Text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(MessageTextBlock.FontFamily, MessageTextBlock.FontStyle, MessageTextBlock.FontWeight, MessageTextBlock.FontStretch),
                MessageTextBlock.FontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // 计算所需宽度
            double requiredWidth = formattedText.Width + 80; // 添加内边距
            double minWidth = 350;
            double maxWidth = 500;
            
            this.Width = Math.Max(minWidth, Math.Min(maxWidth, requiredWidth));
            
            // 如果文本很长
            if (formattedText.Width > maxWidth - 80)
            {
                this.Height = 220 + (int)(formattedText.Height * 2);
            }
        }

        /// 处理键盘事件
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape)
            {
                // ESC键相当于点击取消按钮
                var cancelButton = _buttons.FirstOrDefault(b => b.IsCancel);
                if (cancelButton != null)
                {
                    cancelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else
                {
                    _result = MessageBoxResult.Cancel;
                    Close();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // Enter键相当于点击默认按钮
                var defaultButton = _buttons.FirstOrDefault(b => b.IsDefault);
                if (defaultButton != null)
                {
                    defaultButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                }
            }
        }
    }
}