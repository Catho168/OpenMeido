using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using OpenMeido.Controls;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class MainWindowMiniChatPopupCoordinatorTests
{
    [Fact]
    public void Show_OpensPopup_AndSyncsMiniChatState()
    {
        RunInSta(() =>
        {
            var context = CreateContext();

            context.Coordinator.Show();

            Assert.True(context.Popup.IsOpen);
            Assert.True(context.Coordinator.IsOpen);
            Assert.True(context.ViewModel.IsMiniChatOpen);
            Assert.Equal(1, context.ChattingImageCallCount);
            Assert.Equal(1, context.PositionMeidoInCenterCallCount);
            Assert.Equal(0, context.ContentShift.X);
            Assert.Equal(0, context.ContentShift.Y);
        });
    }

    [Fact]
    public void Hide_ClosesPopup_ResetsState_AndClearsMiniChatInput()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            context.Coordinator.Show();
            context.ViewModel.MiniChat.InputText = "hello";

            context.Coordinator.Hide();

            Assert.False(context.Popup.IsOpen);
            Assert.False(context.Coordinator.IsOpen);
            Assert.False(context.ViewModel.IsMiniChatOpen);
            Assert.Equal(string.Empty, context.ViewModel.MiniChat.InputText);
            Assert.Equal(1, context.StandbyImageCallCount);
        });
    }

    [Fact]
    public void HidePopupContent_HidesPopupWithoutResettingOpenState()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            context.Coordinator.Show();

            context.Coordinator.HidePopupContent();

            Assert.False(context.Popup.IsOpen);
            Assert.True(context.Coordinator.IsOpen);
            Assert.True(context.ViewModel.IsMiniChatOpen);
        });
    }

    [Fact]
    public void Position_PlacesPopupBesideMeidoImage()
    {
        RunInSta(() =>
        {
            var context = CreateContext();
            var meidoPosition = context.MeidoImage.TranslatePoint(new Point(0, 0), context.Canvas);
            var meidoWidth = GetActualOrConfiguredSize(context.MeidoImage.ActualWidth, context.MeidoImage.Width);
            var meidoHeight = GetActualOrConfiguredSize(context.MeidoImage.ActualHeight, context.MeidoImage.Height);
            var miniChatHeight = GetActualOrConfiguredSize(context.MiniChatControl.ActualHeight, context.MiniChatControl.Height);
            var expectedLeft = meidoPosition.X + meidoWidth + 12;
            var expectedTop = meidoPosition.Y + (meidoHeight - miniChatHeight) / 2;

            context.Coordinator.Position();

            Assert.Equal(expectedLeft, context.Popup.HorizontalOffset, 3);
            Assert.Equal(expectedTop, context.Popup.VerticalOffset, 3);
        });
    }

    private static TestContext CreateContext()
    {
        EnsureApplication();

        var settingsService = new FakeSettingsService { LoadResult = CreateValidSettings() };
        var apiServiceFactory = new FakeApiServiceFactory();
        apiServiceFactory.Enqueue(new FakeApiService());
        var viewModel = new MainViewModel(settingsService, apiServiceFactory);
        var contentShift = new TranslateTransform(3, 4);
        var canvas = new Canvas { Width = 400, Height = 300 };
        var meidoImage = new System.Windows.Controls.Image { Width = 83, Height = 83 };
        Canvas.SetLeft(meidoImage, 100);
        Canvas.SetTop(meidoImage, 80);
        canvas.Children.Add(meidoImage);

        var radialMenu = new RadialMenuControl();
        canvas.Children.Add(radialMenu);

        var miniChatControl = new MiniChatControl
        {
            DataContext = viewModel.MiniChat,
            Width = 220,
            Height = 100
        };

        miniChatControl.Measure(new Size(220, 100));
        miniChatControl.Arrange(new Rect(0, 0, 220, 100));
        meidoImage.Measure(new Size(83, 83));
        meidoImage.Arrange(new Rect(100, 80, 83, 83));
        canvas.Measure(new Size(400, 300));
        canvas.Arrange(new Rect(0, 0, 400, 300));
        canvas.UpdateLayout();

        var popup = new Popup
        {
            Placement = PlacementMode.Relative,
            PlacementTarget = canvas,
            Child = miniChatControl
        };

        var standbyImageCallCount = 0;
        var chattingImageCallCount = 0;
        var positionMeidoInCenterCallCount = 0;
        var coordinator = new MainWindowMiniChatPopupCoordinator(
            viewModel,
            contentShift,
            popup,
            miniChatControl,
            meidoImage,
            canvas,
            radialMenu,
            () => standbyImageCallCount++,
            () => chattingImageCallCount++,
            () => positionMeidoInCenterCallCount++);

        return new TestContext(
            viewModel,
            contentShift,
            canvas,
            meidoImage,
            miniChatControl,
            popup,
            coordinator,
            () => standbyImageCallCount,
            () => chattingImageCallCount,
            () => positionMeidoInCenterCallCount);
    }

    private static AppSettings CreateValidSettings() => new()
    {
        ApiBaseUrl = "https://example.com/v1",
        ApiKey = "key",
        ModelName = "model",
        MaxTokens = 1000,
        Temperature = 0.7
    };

    private static void EnsureApplication()
    {
        WpfTestApplicationResources.EnsureLoaded();
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private static double GetActualOrConfiguredSize(double actualSize, double configuredSize)
    {
        if (actualSize > 0)
        {
            return actualSize;
        }

        return !double.IsNaN(configuredSize) && configuredSize > 0
            ? configuredSize
            : 0;
    }

    private sealed class TestContext(
        MainViewModel viewModel,
        TranslateTransform contentShift,
        Canvas canvas,
        System.Windows.Controls.Image meidoImage,
        MiniChatControl miniChatControl,
        Popup popup,
        MainWindowMiniChatPopupCoordinator coordinator,
        Func<int> getStandbyImageCallCount,
        Func<int> getChattingImageCallCount,
        Func<int> getPositionMeidoInCenterCallCount)
    {
        public MainViewModel ViewModel { get; } = viewModel;
        public TranslateTransform ContentShift { get; } = contentShift;
        public Canvas Canvas { get; } = canvas;
        public System.Windows.Controls.Image MeidoImage { get; } = meidoImage;
        public MiniChatControl MiniChatControl { get; } = miniChatControl;
        public Popup Popup { get; } = popup;
        public MainWindowMiniChatPopupCoordinator Coordinator { get; } = coordinator;
        public int StandbyImageCallCount => getStandbyImageCallCount();
        public int ChattingImageCallCount => getChattingImageCallCount();
        public int PositionMeidoInCenterCallCount => getPositionMeidoInCenterCallCount();
    }
}