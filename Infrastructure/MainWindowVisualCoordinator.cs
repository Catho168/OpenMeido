using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowVisualCoordinator
    {
        private const string MeidoStandbyImagePath = "Assets/Meido/Meido_standby.png";
        private const string MeidoChattingImagePath = "Assets/Meido/Meido_chatting.png";

        private readonly TranslateTransform _contentShift;
        private readonly Canvas _mainCanvas;
        private readonly Image _meidoImage;
        private readonly RadialMenuControl _radialMenu;

        private Func<bool> _isMiniChatOpen = static () => false;
        private Action _positionMiniChat = static () => { };
        private Action _hideMiniChatPopupContent = static () => { };
        private Action _hideMiniChat = static () => { };

        private double _hostWidth;
        private double _hostHeight;
        private bool _isClosingAnimationRunning;

        public MainWindowVisualCoordinator(
            TranslateTransform contentShift,
            Canvas mainCanvas,
            Image meidoImage,
            RadialMenuControl radialMenu)
        {
            ArgumentNullException.ThrowIfNull(contentShift);
            ArgumentNullException.ThrowIfNull(mainCanvas);
            ArgumentNullException.ThrowIfNull(meidoImage);
            ArgumentNullException.ThrowIfNull(radialMenu);

            _contentShift = contentShift;
            _mainCanvas = mainCanvas;
            _meidoImage = meidoImage;
            _radialMenu = radialMenu;
        }

        public void ConnectMiniChat(
            Func<bool> isMiniChatOpen,
            Action positionMiniChat,
            Action hideMiniChatPopupContent,
            Action hideMiniChat)
        {
            ArgumentNullException.ThrowIfNull(isMiniChatOpen);
            ArgumentNullException.ThrowIfNull(positionMiniChat);
            ArgumentNullException.ThrowIfNull(hideMiniChatPopupContent);
            ArgumentNullException.ThrowIfNull(hideMiniChat);

            _isMiniChatOpen = isMiniChatOpen;
            _positionMiniChat = positionMiniChat;
            _hideMiniChatPopupContent = hideMiniChatPopupContent;
            _hideMiniChat = hideMiniChat;
        }

        public void RefreshLayout(double hostWidth, double hostHeight)
        {
            if (hostWidth <= 0 || hostHeight <= 0)
            {
                return;
            }

            _hostWidth = hostWidth;
            _hostHeight = hostHeight;

            EnsureRadialMenuHost();
            PositionMeidoInCenter();
            _radialMenu.RefreshLayout(hostWidth, hostHeight, _isMiniChatOpen());

            if (_isMiniChatOpen())
            {
                _positionMiniChat();
            }
        }

        public void HideContent()
        {
            _meidoImage.Visibility = Visibility.Hidden;
            _radialMenu.SetButtonsVisibility(Visibility.Hidden);
            _radialMenu.Visibility = Visibility.Hidden;
            _hideMiniChatPopupContent();
        }

        public void RestoreContent()
        {
            _meidoImage.Visibility = Visibility.Visible;
            _radialMenu.SetButtonsVisibility(Visibility.Visible);
            _radialMenu.Visibility = Visibility.Visible;

            if (_isMiniChatOpen())
            {
                _hideMiniChat();
            }

            ShowStandbyImage();
        }

        public void PositionMeidoInCenter()
        {
            double hostWidth = _hostWidth > 0 ? _hostWidth : GetActualOrConfiguredSize(_mainCanvas.ActualWidth, _mainCanvas.Width);
            double hostHeight = _hostHeight > 0 ? _hostHeight : GetActualOrConfiguredSize(_mainCanvas.ActualHeight, _mainCanvas.Height);

            Canvas.SetLeft(_meidoImage, hostWidth / 2 - _meidoImage.Width / 2);
            Canvas.SetTop(_meidoImage, hostHeight / 2 - _meidoImage.Height / 2);

            AnimateMeidoEntrance();
        }

        public void ShowStandbyImage() => SetMeidoImage(MeidoStandbyImagePath);

        public void ShowChattingImage() => SetMeidoImage(MeidoChattingImagePath);

        public async Task PlayCloseAnimationAsync(Action hideWindow)
        {
            ArgumentNullException.ThrowIfNull(hideWindow);

            if (_isClosingAnimationRunning)
            {
                return;
            }

            _isClosingAnimationRunning = true;

            try
            {
                var dispatcher = _meidoImage.Dispatcher;
                var duration = TimeSpan.FromMilliseconds(120);
                var radialMenuAnimationTask = _radialMenu.PlayCloseAnimationAsync(duration);

                var meidoScale = _meidoImage.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                _meidoImage.RenderTransform = meidoScale;
                var meidoStoryboard = new Storyboard { Duration = duration };

                var scaleAnimation = new DoubleAnimation(0.0, duration);
                Storyboard.SetTarget(scaleAnimation, meidoScale);
                Storyboard.SetTargetProperty(scaleAnimation, new PropertyPath(ScaleTransform.ScaleXProperty));
                meidoStoryboard.Children.Add(scaleAnimation);

                var scaleAnimationY = scaleAnimation.Clone();
                Storyboard.SetTargetProperty(scaleAnimationY, new PropertyPath(ScaleTransform.ScaleYProperty));
                meidoStoryboard.Children.Add(scaleAnimationY);

                var opacityAnimation = new DoubleAnimation(0.0, duration);
                Storyboard.SetTarget(opacityAnimation, _meidoImage);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                meidoStoryboard.Children.Add(opacityAnimation);

                meidoStoryboard.Begin();

                var shiftAnimationX = new DoubleAnimation(0, duration)
                {
                    FillBehavior = FillBehavior.Stop
                };

                var shiftAnimationY = new DoubleAnimation(0, duration)
                {
                    FillBehavior = FillBehavior.Stop
                };

                _contentShift.BeginAnimation(TranslateTransform.XProperty, shiftAnimationX);
                _contentShift.BeginAnimation(TranslateTransform.YProperty, shiftAnimationY);

                await Task.WhenAll(radialMenuAnimationTask, Task.Delay(duration + TimeSpan.FromMilliseconds(20)));

                if (dispatcher.CheckAccess())
                {
                    CompleteCloseAnimation(hideWindow);
                }
                else
                {
                    await dispatcher.InvokeAsync(() => CompleteCloseAnimation(hideWindow), DispatcherPriority.Send);
                }
            }
            finally
            {
                _isClosingAnimationRunning = false;
            }
        }

        private void CompleteCloseAnimation(Action hideWindow)
        {
            _contentShift.BeginAnimation(TranslateTransform.XProperty, null);
            _contentShift.BeginAnimation(TranslateTransform.YProperty, null);
            _contentShift.X = 0;
            _contentShift.Y = 0;

            hideWindow();
        }

        private void EnsureRadialMenuHost()
        {
            if (_mainCanvas.Children.Contains(_radialMenu))
            {
                return;
            }

            _mainCanvas.Children.Add(_radialMenu);
            Canvas.SetLeft(_radialMenu, 0);
            Canvas.SetTop(_radialMenu, 0);
            Canvas.SetZIndex(_radialMenu, 1);
        }

        private void AnimateMeidoEntrance()
        {
            var scaleTransform = new ScaleTransform(0.1, 0.1);
            _meidoImage.RenderTransform = scaleTransform;

            var scaleAnimation = new DoubleAnimation
            {
                From = 0.1,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };

            var opacityAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 0.9,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            _meidoImage.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private void SetMeidoImage(string relativePath)
        {
            try
            {
                var packUri = new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = packUri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                _meidoImage.Source = bitmap;
            }
            catch
            {
                try
                {
                    _meidoImage.Source = new BitmapImage(new Uri(relativePath, UriKind.RelativeOrAbsolute));
                }
                catch
                {
                }
            }
        }

        private static double GetActualOrConfiguredSize(double actualSize, double configuredSize)
        {
            if (actualSize > 0)
            {
                return actualSize;
            }

            if (!double.IsNaN(configuredSize) && configuredSize > 0)
            {
                return configuredSize;
            }

            return 0;
        }
    }
}