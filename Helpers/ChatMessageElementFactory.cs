using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido.Helpers
{
    internal static class ChatMessageElementFactory
    {
        public static Border CreateUserMessage(string message, Style borderStyle, Style textStyle)
        {
            var border = new Border { Style = borderStyle };
            border.Child = new TextBlock { Text = message, Style = textStyle };
            return border;
        }

        public static Border CreateAiMessage(string message, Style borderStyle, Style textStyle)
        {
            var border = new Border { Style = borderStyle };
            border.Child = new TextBlock { Text = message, Style = textStyle };
            return border;
        }

        public static Border CreateWelcomeMessage(string message, Style borderStyle, Style textStyle)
        {
            var border = new Border
            {
                Style = borderStyle,
                Margin = new Thickness(10, 20, 50, 10)
            };

            border.Child = new TextBlock
            {
                Style = textStyle,
                Text = message
            };

            return border;
        }

        public static Border CreateToolCallBar(string toolName, bool isDetailedView, Action onClick = null)
        {
            var containerBorder = new Border
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var normalBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            var hoverBackground = new SolidColorBrush(Color.FromRgb(235, 235, 235));

            var messageBorder = new Border
            {
                Background = normalBackground,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 300
            };

            messageBorder.Child = new TextBlock
            {
                Text = $"妹抖酱调用了 {toolName} 工具",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            if (isDetailedView && onClick != null)
            {
                messageBorder.Cursor = Cursors.Hand;
                messageBorder.ToolTip = "点击查看详情";
                messageBorder.MouseEnter += (_, __) => messageBorder.Background = hoverBackground;
                messageBorder.MouseLeave += (_, __) => messageBorder.Background = normalBackground;
                messageBorder.MouseLeftButtonDown += (_, e) =>
                {
                    onClick();
                    e.Handled = true;
                };
            }

            containerBorder.Child = messageBorder;
            return containerBorder;
        }
    }
}