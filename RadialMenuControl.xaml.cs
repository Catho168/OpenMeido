using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OpenMeido.Models;

namespace OpenMeido
{
    /// 独立的径向菜单控件，将按钮布局逻辑从 MainWindow 中拆分出来。
    public partial class RadialMenuControl : UserControl
    {
        public List<RadialMenuItem> MenuItems { get; set; }

        /// 迷你聊天开启时，仅在半圆显示按钮。
        public bool IsMiniChatOpen { get; set; }

        /// 用户点击按钮时回调外部方法，以便执行命令并隐藏窗口等。
        public Action<ICommand> OnMenuCommand { get; set; }

        // 动画相关字段
        private Dictionary<Button, Point> _buttonPositions = new Dictionary<Button, Point>();

        public RadialMenuControl()
        {
            InitializeComponent();

            // 初始化属性
            MenuItems = new List<RadialMenuItem>();
            IsMiniChatOpen = false;

            // 随着大小变化重新布局
            SizeChanged += new SizeChangedEventHandler((sender, e) => Regenerate());
        }

        // 初始化时只创建一次按钮
        private void EnsureButtons()
        {
            if (RootCanvas.Children.Count != MenuItems.Count)
            {
                RootCanvas.Children.Clear();
                for (int i = 0; i < MenuItems.Count; i++)
                {
                    var button = CreateRadialButton(MenuItems[i]);
                    RootCanvas.Children.Add(button);
                }
            }
        }

        /// 重新生成所有按钮
        public void Regenerate()
        {
            EnsureButtons();
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
                var button = RootCanvas.Children[i] as Button;
                Canvas.SetLeft(button, pos.X - button.Width / 2);
                Canvas.SetTop(button, pos.Y - button.Height / 2);
            }
        }

        /// 带动画效果的按钮重新布局
        public void RegenerateWithAnimation(bool isMiniChatOpen)
        {
            EnsureButtons();
            if (MenuItems == null || MenuItems.Count == 0) return;

            double radius = Math.Min(ActualWidth, ActualHeight) * 0.3;
            double startAngle = 0;
            double angleRange = 2 * Math.PI;
            if (isMiniChatOpen)
            {
                startAngle = Math.PI / 2;  // 90°
                angleRange = Math.PI;      // 半圆
            }

            var storyboard = new Storyboard();
            for (int i = 0; i < MenuItems.Count; i++)
            {
                var button = RootCanvas.Children[i] as Button;
                var targetPos = CalculateButtonPosition(i, MenuItems.Count, radius, startAngle, angleRange);
                double fromLeft = Canvas.GetLeft(button);
                double fromTop = Canvas.GetTop(button);
                double toLeft = targetPos.X - button.Width / 2;
                double toTop = targetPos.Y - button.Height / 2;

                var xAnimation = new DoubleAnimation
                {
                    From = fromLeft,
                    To = toLeft,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var yAnimation = new DoubleAnimation
                {
                    From = fromTop,
                    To = toTop,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(xAnimation, button);
                Storyboard.SetTargetProperty(xAnimation, new PropertyPath(Canvas.LeftProperty));
                Storyboard.SetTarget(yAnimation, button);
                Storyboard.SetTargetProperty(yAnimation, new PropertyPath(Canvas.TopProperty));
                storyboard.Children.Add(xAnimation);
                storyboard.Children.Add(yAnimation);
            }
            storyboard.Completed += new EventHandler((s, e) =>
            {
                IsMiniChatOpen = isMiniChatOpen;
            });
            storyboard.Begin();
        }

        /// 根据对外传入的鼠标位置更新按钮缩放。
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

            button.Click += new RoutedEventHandler((sender, e) => 
            {
                if (OnMenuCommand != null)
                {
                    OnMenuCommand.Invoke(item.Command);
                }
            });
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

        /// 提供按钮枚举供外部使用（例如旧的缩放逻辑）。
        public IEnumerable<Button> RadialButtons 
        { 
            get { return RootCanvas.Children.OfType<Button>(); } 
        }
    }
} 