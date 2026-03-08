using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido.Helpers
{
    internal static class ChatHistoryItemElementFactory
    {
        public static Border Create(string title, Action onOpen, Action onDelete)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(ThemeColors.GetUiColor("surface")),
                BorderBrush = new SolidColorBrush(ThemeColors.GetUiColor("border_subtle")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(ThemeColors.GetUiColor("text_primary")),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titleBlock, 0);

            var deleteButton = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(ThemeColors.GetUiColor("text_secondary")),
                Width = 24,
                Height = 24,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(0),
                ToolTip = "删除此对话"
            };
            deleteButton.Click += (_, __) => onDelete?.Invoke();
            Grid.SetColumn(deleteButton, 1);

            border.MouseEnter += (_, __) =>
            {
                border.Background = new SolidColorBrush(ThemeColors.GetUiColor("surface_muted"));
                border.BorderBrush = new SolidColorBrush(ThemeColors.PrimaryLight);
            };
            border.MouseLeave += (_, __) =>
            {
                border.Background = new SolidColorBrush(ThemeColors.GetUiColor("surface"));
                border.BorderBrush = new SolidColorBrush(ThemeColors.GetUiColor("border_subtle"));
            };
            border.MouseLeftButtonDown += (_, __) => onOpen?.Invoke();

            grid.Children.Add(titleBlock);
            grid.Children.Add(deleteButton);
            border.Child = grid;
            return border;
        }
    }
}