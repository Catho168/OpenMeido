using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenMeido
{
    /// <summary>
    /// 独立的径向菜单控件，将按钮布局逻辑从 MainWindow 中拆分出来。
    /// </summary>
    public partial class RadialMenuControl : UserControl
    {
        public List<RadialMenuItem> MenuItems { get; set; } = new List<RadialMenuItem>();

        /// <summary>
        /// 当迷你聊天开启时，仅在半圆显示按钮。
        /// </summary>
        public bool IsMiniChatOpen { get; set; } = false;

        /// <summary>
        /// 当用户点击按钮时回调外部方法，以便执行命令并隐藏窗口等。
        /// </summary>
        public Action<ICommand> OnMenuCommand { get; set; }

        public RadialMenuControl()
        {
            InitializeComponent();

            // 随着大小变化重新布局
            SizeChanged += (_, __) => Regenerate();
        }

        /// <summary>
        /// 重新生成所有按钮。
        /// </summary>
        public void Regenerate()
        {
            if (RootCanvas == null) return;
            RootCanvas.Children.Clear();

            if (MenuItems == null || MenuItems.Count == 0) return;

            double radius = Math.Min(ActualWidth, ActualHeight) * 0.3;
            double startAngle = 0;
            double angleRange = 2 * Math.PI;
            if (IsMiniChatOpen)
            {
                startAngle = Math.PI / 2;  // 90°
                angleRange = Math.PI;      // 半圆
            }

            for (int i = 0; i < MenuItems.Count; i++)
            {
                var pos = CalculateButtonPosition(i, MenuItems.Count, radius, startAngle, angleRange);
                var button = CreateRadialButton(MenuItems[i]);
                Canvas.SetLeft(button, pos.X - button.Width / 2);
                Canvas.SetTop(button, pos.Y - button.Height / 2);
                RootCanvas.Children.Add(button);
            }
        }

        /// <summary>
        /// 根据对外传入的鼠标位置更新按钮缩放。
        /// </summary>
        public void UpdateButtonScales(Point mousePos)
        {
            foreach (Button btn in RootCanvas.Children.OfType<Button>())
            {
                UpdateButtonScale(btn, mousePos);
            }
        }

        #region 内部辅助

        private Point CalculateButtonPosition(int index, int total, double radius, double startAngle = 0, double angleRange = 2 * Math.PI)
        {
            double angle;
            if (total == 1)
            {
                angle = startAngle + angleRange / 2;
            }
            else
            {
                if (Math.Abs(angleRange - 2 * Math.PI) < 0.0001)
                {
                    angle = startAngle + angleRange * index / total;
                }
                else
                {
                    angle = startAngle + angleRange * index / (total - 1);
                }
            }

            double centerX = ActualWidth / 2;
            double centerY = ActualHeight / 2;

            return new Point(
                centerX + radius * Math.Cos(angle),
                centerY + radius * Math.Sin(angle));
        }

        private Button CreateRadialButton(RadialMenuItem item)
        {
            var button = new Button
            {
                Content = item.Icon,
                ToolTip = item.ToolTip,
                Width = 50,
                Height = 50,
                FontSize = 24,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };

            // 复用应用程序资源中的样式
            if (Application.Current != null)
            {
                var style = Application.Current.FindResource("RadialButtonStyle") as Style;
                if (style != null)
                {
                    button.Style = style;
                }
            }

            button.Click += (_, __) => OnMenuCommand?.Invoke(item.Command);
            return button;
        }

        private void UpdateButtonScale(Button button, Point mousePos)
        {
            double btnCenterX = Canvas.GetLeft(button) + button.ActualWidth / 2;
            double btnCenterY = Canvas.GetTop(button) + button.ActualHeight / 2;

            double deltaX = mousePos.X - btnCenterX;
            double deltaY = mousePos.Y - btnCenterY;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            double maxDist = 150;
            double scaleFactor = 1 + 1 * Math.Exp(-distance * 3 / maxDist);

            button.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
        }
        #endregion

        /// <summary>
        /// 提供按钮枚举供外部使用（例如旧的缩放逻辑）。
        /// </summary>
        public IEnumerable<Button> RadialButtons => RootCanvas.Children.OfType<Button>();
    }
} 