using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowInteractionCoordinator
    {
        private readonly Window _owner;
        private readonly TranslateTransform _contentShift;
        private readonly Action<Point> _updateButtonScales;
        private readonly Action _refreshLayout;
        private readonly Func<bool> _isMiniChatOpen;
        private readonly Func<Task> _playCloseAnimationAsync;
        private readonly IMainWindowInteractionPlatform _platform;
        private readonly double _maxWindowShift;

        public MainWindowInteractionCoordinator(
            Window owner,
            TranslateTransform contentShift,
            Action<Point> updateButtonScales,
            Action refreshLayout,
            Func<bool> isMiniChatOpen,
            Func<Task> playCloseAnimationAsync,
            IMainWindowInteractionPlatform platform = null,
            double maxWindowShift = 7)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(contentShift);
            ArgumentNullException.ThrowIfNull(updateButtonScales);
            ArgumentNullException.ThrowIfNull(refreshLayout);
            ArgumentNullException.ThrowIfNull(isMiniChatOpen);
            ArgumentNullException.ThrowIfNull(playCloseAnimationAsync);

            _owner = owner;
            _contentShift = contentShift;
            _updateButtonScales = updateButtonScales;
            _refreshLayout = refreshLayout;
            _isMiniChatOpen = isMiniChatOpen;
            _playCloseAnimationAsync = playCloseAnimationAsync;
            _platform = platform ?? new MainWindowInteractionPlatform();
            _maxWindowShift = maxWindowShift;
        }

        public void ShowAtMouse()
        {
            Point dpiScale = _platform.GetDpiScale(_owner);
            double dpiX = dpiScale.X > 0 ? dpiScale.X : 1.0;
            double dpiY = dpiScale.Y > 0 ? dpiScale.Y : 1.0;

            Point screenPosition = _platform.GetCursorScreenPosition();
            double logicalX = screenPosition.X / dpiX;
            double logicalY = screenPosition.Y / dpiY;
            double windowWidth = GetActualOrConfiguredSize(_owner.ActualWidth, _owner.Width);
            double windowHeight = GetActualOrConfiguredSize(_owner.ActualHeight, _owner.Height);

            _owner.Left = logicalX - windowWidth / 2;
            _owner.Top = logicalY - windowHeight / 2;

            _platform.Show(_owner);
            _refreshLayout();
            _platform.Activate(_owner);
        }

        public void HandleMouseMove(Point windowMousePosition)
        {
            if (_isMiniChatOpen())
            {
                return;
            }

            double hostWidth = GetActualOrConfiguredSize(_owner.ActualWidth, _owner.Width);
            double hostHeight = GetActualOrConfiguredSize(_owner.ActualHeight, _owner.Height);
            if (hostWidth <= 0 || hostHeight <= 0)
            {
                return;
            }

            double centerX = hostWidth / 2;
            double centerY = hostHeight / 2;
            _contentShift.X = (windowMousePosition.X - centerX) / centerX * _maxWindowShift;
            _contentShift.Y = (windowMousePosition.Y - centerY) / centerY * _maxWindowShift;

            _updateButtonScales(windowMousePosition);
        }

        public Task HandleMouseLeaveAsync()
        {
            if (_isMiniChatOpen())
            {
                return Task.CompletedTask;
            }

            return _playCloseAnimationAsync();
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