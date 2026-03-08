using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OpenMeido.Controls;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure
{
    public sealed class MainWindowMiniChatPopupCoordinator
    {
        private readonly MainViewModel _viewModel;
        private readonly TranslateTransform _contentShift;
        private readonly Popup _miniChatPopup;
        private readonly MiniChatControl _miniChatControl;
        private readonly FrameworkElement _meidoImage;
        private readonly UIElement _mainCanvas;
        private readonly RadialMenuControl _radialMenu;
        private readonly Action _showStandbyMeidoImage;
        private readonly Action _showChattingMeidoImage;
        private readonly Action _positionMeidoInCenter;
        private bool _isOpen;

        public MainWindowMiniChatPopupCoordinator(
            MainViewModel viewModel,
            TranslateTransform contentShift,
            Popup miniChatPopup,
            MiniChatControl miniChatControl,
            FrameworkElement meidoImage,
            UIElement mainCanvas,
            RadialMenuControl radialMenu,
            Action showStandbyMeidoImage,
            Action showChattingMeidoImage,
            Action positionMeidoInCenter)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(contentShift);
            ArgumentNullException.ThrowIfNull(miniChatPopup);
            ArgumentNullException.ThrowIfNull(miniChatControl);
            ArgumentNullException.ThrowIfNull(meidoImage);
            ArgumentNullException.ThrowIfNull(mainCanvas);
            ArgumentNullException.ThrowIfNull(radialMenu);
            ArgumentNullException.ThrowIfNull(showStandbyMeidoImage);
            ArgumentNullException.ThrowIfNull(showChattingMeidoImage);
            ArgumentNullException.ThrowIfNull(positionMeidoInCenter);

            _viewModel = viewModel;
            _contentShift = contentShift;
            _miniChatPopup = miniChatPopup;
            _miniChatControl = miniChatControl;
            _meidoImage = meidoImage;
            _mainCanvas = mainCanvas;
            _radialMenu = radialMenu;
            _showStandbyMeidoImage = showStandbyMeidoImage;
            _showChattingMeidoImage = showChattingMeidoImage;
            _positionMeidoInCenter = positionMeidoInCenter;
            _isOpen = _viewModel.IsMiniChatOpen;
        }

        public bool IsOpen => _isOpen;

        public void Toggle()
        {
            if (_isOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        public void Show()
        {
            ResetContentShift();
            _viewModel.MiniChat.Open();
            Position();

            _miniChatPopup.IsOpen = true;
            _miniChatControl.FocusInput();
            SetOpenState(true);

            _showChattingMeidoImage();
            _positionMeidoInCenter();
            _radialMenu.RegenerateWithAnimation(true);
        }

        public void HidePopupContent()
        {
            _miniChatPopup.IsOpen = false;
        }

        public void Hide()
        {
            _miniChatPopup.IsOpen = false;
            SetOpenState(false);
            _viewModel.MiniChat.Close();

            _showStandbyMeidoImage();
            _radialMenu.RegenerateWithAnimation(false);
        }

        public void Position()
        {
            Point meidoPos = _meidoImage.TranslatePoint(new Point(0, 0), _mainCanvas);
            double meidoWidth = GetActualOrConfiguredSize(_meidoImage.ActualWidth, _meidoImage.Width);
            double meidoHeight = GetActualOrConfiguredSize(_meidoImage.ActualHeight, _meidoImage.Height);
            double miniChatHeight = GetActualOrConfiguredSize(_miniChatControl.ActualHeight, _miniChatControl.Height);

            _miniChatPopup.HorizontalOffset = meidoPos.X + meidoWidth + 12;
            _miniChatPopup.VerticalOffset = meidoPos.Y + (meidoHeight - miniChatHeight) / 2;
        }

        private void ResetContentShift()
        {
            _contentShift.BeginAnimation(TranslateTransform.XProperty, null);
            _contentShift.BeginAnimation(TranslateTransform.YProperty, null);
            _contentShift.X = 0;
            _contentShift.Y = 0;
        }

        private void SetOpenState(bool isOpen)
        {
            _isOpen = isOpen;
            _viewModel.IsMiniChatOpen = isOpen;
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