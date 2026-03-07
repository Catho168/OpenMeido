using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido.Helpers
{
    internal static class ToolCallDetailsWindowFactory
    {
        public static Window Create(Window owner, string toolName, string parameters, string result, bool isSuccess)
        {
            var successColor = ThemeColors.GetUiColor("success");
            var errorColor = ThemeColors.GetUiColor("error");
            var titleColor = isSuccess ? successColor : errorColor;
            var detailsWindow = new Window
            {
                Title = "工具调用详情",
                Width = 480,
                Height = 420,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            var mainBorder = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(12),
                BorderBrush = new SolidColorBrush(titleColor),
                BorderThickness = new Thickness(2),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(45) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleBar = CreateTitleBar(detailsWindow, titleColor);
            Grid.SetRow(titleBar, 0);

            var contentScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20, 15, 20, 0),
                Content = CreateContentPanel(toolName, parameters, result, isSuccess)
            };
            Grid.SetRow(contentScrollViewer, 1);

            var buttonPanel = CreateButtonPanel(detailsWindow, titleColor);
            Grid.SetRow(buttonPanel, 2);

            mainGrid.Children.Add(titleBar);
            mainGrid.Children.Add(contentScrollViewer);
            mainGrid.Children.Add(buttonPanel);
            mainBorder.Child = mainGrid;
            detailsWindow.Content = mainBorder;

            titleBar.MouseLeftButtonDown += (_, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    detailsWindow.DragMove();
                }
            };

            return detailsWindow;
        }

        private static Border CreateTitleBar(Window detailsWindow, Color titleColor)
        {
            var titleBar = new Border
            {
                Background = new SolidColorBrush(titleColor),
                CornerRadius = new CornerRadius(10, 10, 0, 0)
            };

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleContent = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
            titleContent.Children.Add(CreateTextBlock("🔧", 16, Brushes.White, new Thickness(0, 0, 8, 0)));
            titleContent.Children.Add(CreateTextBlock("工具调用详情", 14, Brushes.White, fontWeight: FontWeights.SemiBold));
            Grid.SetColumn(titleContent, 0);

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
                FontFamily = GetGlobalFontFamily()
            };
            closeBtn.Click += (_, __) => detailsWindow.Close();
            closeBtn.MouseEnter += (_, __) => closeBtn.Background = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255));
            closeBtn.MouseLeave += (_, __) => closeBtn.Background = Brushes.Transparent;
            Grid.SetColumn(closeBtn, 1);

            titleGrid.Children.Add(titleContent);
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;
            return titleBar;
        }

        private static StackPanel CreateContentPanel(string toolName, string parameters, string result, bool isSuccess)
        {
            var contentPanel = new StackPanel();

            var toolNamePanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 18) };
            toolNamePanel.Children.Add(CreateTextBlock("⚙️", 14, Brushes.Black, new Thickness(0, 0, 8, 0)));
            toolNamePanel.Children.Add(CreateTextBlock("工具名称:", 13, new SolidColorBrush(Color.FromRgb(85, 85, 85)), new Thickness(0, 0, 8, 0), FontWeights.SemiBold));
            toolNamePanel.Children.Add(CreateTextBlock(toolName, 13, new SolidColorBrush(ThemeColors.GetUiColor("success")), fontWeight: FontWeights.Bold));
            contentPanel.Children.Add(toolNamePanel);

            if (!string.IsNullOrEmpty(parameters))
            {
                contentPanel.Children.Add(CreateSectionLabel("📋", "调用参数:", new SolidColorBrush(Color.FromRgb(85, 85, 85))));
                contentPanel.Children.Add(CreateCodeBlock(parameters));
            }

            if (!string.IsNullOrEmpty(result))
            {
                var resultColor = isSuccess ? ThemeColors.GetUiColor("success") : ThemeColors.GetUiColor("error");
                var resultBackground = isSuccess ? ThemeColors.GetUiColor("background_success") : ThemeColors.GetUiColor("background_error");
                var resultBorder = isSuccess ? ThemeColors.GetUiColor("border_success") : ThemeColors.GetUiColor("border_error");
                var resultIcon = isSuccess ? "✅" : "❌";
                var resultTitle = isSuccess ? "执行结果:" : "执行错误:";

                contentPanel.Children.Add(CreateSectionLabel(resultIcon, resultTitle, new SolidColorBrush(resultColor)));
                contentPanel.Children.Add(CreateResultBlock(result, resultBackground, resultBorder));
            }

            return contentPanel;
        }

        private static StackPanel CreateButtonPanel(Window detailsWindow, Color primaryColor)
        {
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(20, 15, 20, 20) };
            var okButton = new Button
            {
                Content = "确定",
                Width = 90,
                Height = 32,
                Background = new SolidColorBrush(primaryColor),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                FontFamily = GetGlobalFontFamily(),
                Style = CreateRoundedButtonStyle()
            };

            okButton.MouseEnter += (_, __) => okButton.Background = new SolidColorBrush(ThemeColors.GetUiColor("processing"));
            okButton.MouseLeave += (_, __) => okButton.Background = new SolidColorBrush(primaryColor);
            okButton.Click += (_, __) => detailsWindow.Close();
            buttonPanel.Children.Add(okButton);
            return buttonPanel;
        }

        private static StackPanel CreateSectionLabel(string icon, string text, Brush foreground)
        {
            var label = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            label.Children.Add(CreateTextBlock(icon, 12, Brushes.Black, new Thickness(0, 0, 6, 0)));
            label.Children.Add(CreateTextBlock(text, 12, foreground, fontWeight: FontWeights.SemiBold));
            return label;
        }

        private static Border CreateCodeBlock(string text)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 18),
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                    LineHeight = 16
                }
            };
        }

        private static Border CreateResultBlock(string text, Color backgroundColor, Color borderColor)
        {
            return new Border
            {
                Background = new SolidColorBrush(backgroundColor),
                BorderBrush = new SolidColorBrush(borderColor),
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
                        Text = text,
                        FontSize = 11,
                        TextWrapping = TextWrapping.Wrap,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        LineHeight = 16,
                        FontFamily = GetGlobalFontFamily()
                    }
                }
            };
        }

        private static TextBlock CreateTextBlock(string text, double fontSize, Brush foreground, Thickness? margin = null, FontWeight? fontWeight = null)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = foreground,
                Margin = margin ?? default,
                FontWeight = fontWeight ?? FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = GetGlobalFontFamily(),
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
        }

        private static Style CreateRoundedButtonStyle()
        {
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
            return buttonStyle;
        }

        private static FontFamily GetGlobalFontFamily()
        {
            return Application.Current?.Resources["GlobalFontFamily"] as FontFamily ?? new FontFamily("Microsoft YaHei UI");
        }
    }
}