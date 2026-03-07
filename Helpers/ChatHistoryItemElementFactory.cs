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
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(10, 8, 10, 8),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 11,
                Foreground = Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);

            var deleteButton = new Button
            {
                Content = "❌",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 10,
                Padding = new Thickness(2),
                ToolTip = "删除此对话"
            };
            deleteButton.Click += (_, __) => onDelete?.Invoke();
            Grid.SetColumn(deleteButton, 1);

            border.MouseLeftButtonDown += (_, __) => onOpen?.Invoke();

            grid.Children.Add(titleBlock);
            grid.Children.Add(deleteButton);
            border.Child = grid;
            return border;
        }
    }
}