using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenMeido.Services;

namespace OpenMeido.Helpers
{
    internal static class McpPanelElementFactory
    {
        public static TextBlock CreateNoServersText()
        {
            return new TextBlock
            {
                Text = "未配置MCP服务器",
                FontSize = 12,
                Foreground = Brush("text_secondary"),
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        public static Border CreateServerStatusItem(string serverName, bool isConnected, int toolCount)
        {
            var panel = new Border
            {
                Background = Brush(isConnected ? "background_success" : "background_error"),
                BorderBrush = Brush(isConnected ? "border_success" : "border_error"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var contentPanel = new StackPanel();
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

            headerPanel.Children.Add(new TextBlock
            {
                Text = isConnected ? "🟢" : "🔴",
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = serverName,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = Brush("text_primary"),
                VerticalAlignment = VerticalAlignment.Center
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"({toolCount}工具)",
                FontSize = 11,
                Foreground = Brush("text_secondary"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            });

            contentPanel.Children.Add(headerPanel);
            contentPanel.Children.Add(new TextBlock
            {
                Text = isConnected ? "已连接" : "连接失败",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brush(isConnected ? "success" : "error"),
                Margin = new Thickness(20, 4, 0, 0)
            });

            panel.Child = contentPanel;
            return panel;
        }

        public static TextBlock CreateNoToolsText()
        {
            return new TextBlock
            {
                Text = "无可用工具",
                FontSize = 12,
                Foreground = Brush("text_secondary"),
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        public static Border CreateToolItem(string toolName, string description)
        {
            var panel = new Border
            {
                Background = Brush("surface_muted"),
                BorderBrush = Brush("border_subtle"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var contentPanel = new StackPanel();
            contentPanel.Children.Add(new TextBlock
            {
                Text = $"🔧 {toolName}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = Brush("text_primary")
            });

            if (!string.IsNullOrEmpty(description))
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = 10,
                    Foreground = Brush("text_secondary"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            panel.Child = contentPanel;
            return panel;
        }

        public static TextBlock CreateNotAvailableText()
        {
            return new TextBlock
            {
                Text = "MCP功能未启用或未配置",
                FontSize = 12,
                Foreground = Brush("warning"),
                Margin = new Thickness(0, 10, 0, 10),
                TextAlignment = TextAlignment.Center
            };
        }

        public static TextBlock CreateErrorText(string errorMessage)
        {
            return new TextBlock
            {
                Text = $"MCP状态获取失败: {errorMessage}",
                FontSize = 11,
                Foreground = Brush("error"),
                Margin = new Thickness(0, 10, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
        }

        public static TextBlock CreateNoActivityText()
        {
            return new TextBlock
            {
                Text = "无活动记录",
                FontSize = 11,
                Foreground = Brush("text_secondary"),
                Margin = new Thickness(0, 5, 0, 5)
            };
        }

        public static TextBlock CreateEmptyActivityText()
        {
            return new TextBlock
            {
                Text = "暂无活动记录",
                FontSize = 11,
                Foreground = Brush("text_secondary"),
                Margin = new Thickness(0, 5, 0, 5)
            };
        }

        public static StackPanel CreateActivitySummary(McpActivityStatistics stats)
        {
            var statsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8)
            };

            statsPanel.Children.Add(new TextBlock
            {
                Text = $"总调用: {stats.TotalToolCalls} | 成功: {stats.SuccessfulCalls} | 失败: {stats.FailedCalls}",
                FontSize = 10,
                Foreground = Brush("success"),
                FontWeight = FontWeights.SemiBold
            });

            if (stats.TotalToolCalls > 0)
            {
                statsPanel.Children.Add(new TextBlock
                {
                    Text = $" | 平均耗时: {stats.AverageExecutionTime:F0}ms",
                    FontSize = 10,
                    Foreground = Brush("text_secondary"),
                    Margin = new Thickness(5, 0, 0, 0)
                });
            }

            return statsPanel;
        }

        public static Border CreateActivitySummarySeparator()
        {
            return new Border
            {
                Height = 1,
                Background = Brush("border_subtle"),
                Margin = new Thickness(0, 5, 0, 8)
            };
        }

        public static Border CreateActivityRecordItem(McpActivityRecord activity)
        {
            var activityPanel = new Border
            {
                Background = new SolidColorBrush(GetActivityBackgroundColor(activity)),
                BorderBrush = new SolidColorBrush(GetActivityBorderColor(activity)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var contentPanel = new StackPanel();
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

            headerPanel.Children.Add(new TextBlock
            {
                Text = activity.Timestamp.ToString("HH:mm:ss"),
                FontSize = 9,
                Foreground = Brush("text_secondary"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = GetActivityIcon(activity),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = GetActivityDisplayText(activity),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(GetActivityTextColor(activity)),
                VerticalAlignment = VerticalAlignment.Center
            });
            contentPanel.Children.Add(headerPanel);

            if (activity.ActivityType == "ToolCallComplete" && activity.ExecutionTimeMs > 0)
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = $"耗时: {activity.ExecutionTimeMs:F0}ms",
                    FontSize = 9,
                    Foreground = Brush("text_secondary"),
                    Margin = new Thickness(40, 1, 0, 0)
                });
            }

            if (!string.IsNullOrEmpty(activity.ErrorMessage))
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = activity.ErrorMessage,
                    FontSize = 9,
                    Foreground = Brush("error"),
                    Margin = new Thickness(40, 1, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            activityPanel.Child = contentPanel;
            return activityPanel;
        }

        private static Color GetActivityBackgroundColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? ThemeColors.GetUiColor("background_success") : ThemeColors.GetUiColor("background_error"),
                "ToolCallComplete" => activity.IsSuccess ? ThemeColors.GetUiColor("background_success") : ThemeColors.GetUiColor("background_error"),
                _ => ThemeColors.GetUiColor("surface_alt")
            };
        }

        private static Color GetActivityBorderColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? ThemeColors.GetUiColor("border_success") : ThemeColors.GetUiColor("border_error"),
                "ToolCallComplete" => activity.IsSuccess ? ThemeColors.GetUiColor("border_success") : ThemeColors.GetUiColor("border_error"),
                _ => ThemeColors.GetUiColor("border_subtle")
            };
        }

        private static string GetActivityIcon(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? "🔗" : "❌",
                "ToolCallComplete" => activity.IsSuccess ? "🔧" : "⚠️",
                _ => "📝"
            };
        }

        private static string GetActivityDisplayText(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => $"{activity.ServerName} {(activity.IsSuccess ? "连接成功" : "连接失败")}",
                "ToolCallComplete" => $"{activity.ToolName} {(activity.IsSuccess ? "执行完成" : "执行失败")}",
                _ => $"{activity.ActivityType}: {activity.ServerName}"
            };
        }

        private static Color GetActivityTextColor(McpActivityRecord activity)
        {
            return activity.ActivityType switch
            {
                "ServerConnection" => activity.IsSuccess ? ThemeColors.GetUiColor("success") : ThemeColors.GetUiColor("error"),
                "ToolCallComplete" => activity.IsSuccess ? ThemeColors.GetUiColor("success") : ThemeColors.GetUiColor("error"),
                _ => ThemeColors.GetUiColor("text_secondary")
            };
        }

        private static SolidColorBrush Brush(string colorType)
        {
            return new SolidColorBrush(ThemeColors.GetUiColor(colorType));
        }
    }
}