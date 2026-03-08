using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public RadialMenuControl()
        {
            InitializeComponent();

            // 初始化属性
            MenuItems = new List<RadialMenuItem>();
            IsMiniChatOpen = false;

            // 随着大小变化重新布局
            SizeChanged += new SizeChangedEventHandler((sender, e) => Regenerate());
        }

        /// 根据宿主窗口尺寸刷新菜单布局。
        public void RefreshLayout(double hostWidth, double hostHeight, bool isMiniChatOpen)
        {
            if (hostWidth <= 0 || hostHeight <= 0)
            {
                return;
            }

            Width = hostWidth;
            Height = hostHeight;
            RootCanvas.Width = hostWidth;
            RootCanvas.Height = hostHeight;
            IsMiniChatOpen = isMiniChatOpen;
            Regenerate();
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
            if (MenuItems == null || MenuItems.Count == 0 || GetRenderableWidth() <= 0 || GetRenderableHeight() <= 0) return;

            var (radius, startAngle, angleRange) = GetLayoutParameters(IsMiniChatOpen);

            for (int i = 0; i < MenuItems.Count; i++)
            {
                var pos = CalculateButtonPosition(i, MenuItems.Count, radius, startAngle, angleRange);
                var button = RootCanvas.Children[i] as Button;
                ResetButtonLayoutState(button);
                Canvas.SetLeft(button, pos.X - button.Width / 2);
                Canvas.SetTop(button, pos.Y - button.Height / 2);
            }
        }

        /// 带动画效果的按钮重新布局
        public void RegenerateWithAnimation(bool isMiniChatOpen)
        {
            EnsureButtons();
            if (MenuItems == null || MenuItems.Count == 0 || GetRenderableWidth() <= 0 || GetRenderableHeight() <= 0) return;

            var (radius, startAngle, angleRange) = GetLayoutParameters(isMiniChatOpen);

            var storyboard = new Storyboard();
            for (int i = 0; i < MenuItems.Count; i++)
            {
                var button = RootCanvas.Children[i] as Button;
                ResetButtonLayoutState(button);
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

        /// 批量更新按钮可见性，避免宿主窗口直接管理内部按钮集合。
        public void SetButtonsVisibility(Visibility visibility)
        {
            EnsureButtons();
            foreach (Button btn in RootCanvas.Children.OfType<Button>())
            {
                btn.Visibility = visibility;
            }
        }

        /// 播放按钮向中心收拢的关闭动画。
        public Task PlayCloseAnimationAsync(TimeSpan duration)
        {
            EnsureButtons();
            if (MenuItems == null || MenuItems.Count == 0 || GetRenderableWidth() <= 0 || GetRenderableHeight() <= 0)
            {
                return Task.CompletedTask;
            }

            double centerX = GetRenderableWidth() / 2;
            double centerY = GetRenderableHeight() / 2;

            foreach (Button btn in RootCanvas.Children.OfType<Button>())
            {
                var (scale, rotate) = EnsureCloseAnimationTransforms(btn);
                Storyboard sb = new Storyboard { Duration = duration };

                var rotateAnim = new DoubleAnimation(360, duration);
                Storyboard.SetTarget(rotateAnim, rotate);
                Storyboard.SetTargetProperty(rotateAnim, new PropertyPath(RotateTransform.AngleProperty));
                sb.Children.Add(rotateAnim);

                var scaleAnim = new DoubleAnimation(0.0, duration);
                Storyboard.SetTarget(scaleAnim, scale);
                Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));
                sb.Children.Add(scaleAnim);

                var scaleAnimY = scaleAnim.Clone();
                Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath(ScaleTransform.ScaleYProperty));
                sb.Children.Add(scaleAnimY);

                double targetLeft = centerX - btn.Width / 2;
                double targetTop = centerY - btn.Height / 2;

                var moveX = new DoubleAnimation(targetLeft, duration);
                Storyboard.SetTarget(moveX, btn);
                Storyboard.SetTargetProperty(moveX, new PropertyPath("(Canvas.Left)"));
                sb.Children.Add(moveX);

                var moveY = new DoubleAnimation(targetTop, duration);
                Storyboard.SetTarget(moveY, btn);
                Storyboard.SetTargetProperty(moveY, new PropertyPath("(Canvas.Top)"));
                sb.Children.Add(moveY);

                sb.Begin();
            }

            return Task.Delay(duration + TimeSpan.FromMilliseconds(20));
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

        private (double Radius, double StartAngle, double AngleRange) GetLayoutParameters(bool isMiniChatOpen)
        {
            double radius = Math.Min(GetRenderableWidth(), GetRenderableHeight()) * 0.3;
            double startAngle = isMiniChatOpen ? Math.PI / 2 : 0;
            double angleRange = isMiniChatOpen ? Math.PI : 2 * Math.PI;
            return (radius, startAngle, angleRange);
        }

        private double GetRenderableWidth() => ActualWidth > 0 ? ActualWidth : Width;

        private double GetRenderableHeight() => ActualHeight > 0 ? ActualHeight : Height;

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

            double centerX = GetRenderableWidth() / 2;
            double centerY = GetRenderableHeight() / 2;

            return new Point(
                centerX + radius * Math.Cos(angle),
                centerY + radius * Math.Sin(angle));
        }

        private (ScaleTransform Scale, RotateTransform Rotate) EnsureCloseAnimationTransforms(Button button)
        {
            TransformGroup group = button.RenderTransform as TransformGroup;
            ScaleTransform scale;

            if (group == null)
            {
                group = new TransformGroup();
                if (button.RenderTransform is ScaleTransform existingScale)
                {
                    scale = existingScale;
                }
                else
                {
                    scale = new ScaleTransform(1, 1);
                }

                group.Children.Add(scale);
                group.Children.Add(new RotateTransform(0));
                button.RenderTransform = group;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                scale = group.Children.OfType<ScaleTransform>().FirstOrDefault() ?? new ScaleTransform(1, 1);
                if (!group.Children.Contains(scale))
                {
                    group.Children.Insert(0, scale);
                }

                if (!group.Children.OfType<RotateTransform>().Any())
                {
                    group.Children.Add(new RotateTransform(0));
                }
            }

            return (scale, group.Children.OfType<RotateTransform>().First());
        }

        private static void ResetButtonLayoutState(Button button)
        {
            if (button == null)
            {
                return;
            }

            button.BeginAnimation(Canvas.LeftProperty, null);
            button.BeginAnimation(Canvas.TopProperty, null);
            button.RenderTransform = new ScaleTransform(1, 1);
            button.RenderTransformOrigin = new Point(0.5, 0.5);
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

        /// 提供按钮枚举供外部观察/测试使用。
        public IEnumerable<Button> RadialButtons 
        { 
            get { return RootCanvas.Children.OfType<Button>(); } 
        }
    }
} 