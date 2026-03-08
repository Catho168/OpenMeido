using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OpenMeido.Infrastructure;
using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class MainWindowVisualCoordinatorTests
{
    [Fact]
    public void RefreshLayout_AddsRadialMenu_CentersMeido_AndRepositionsMiniChatWhenOpen()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: true);

            context.Coordinator.RefreshLayout(300, 300);

            Assert.Contains(context.RadialMenu, context.Canvas.Children.Cast<UIElement>());
            Assert.Equal(108.5, Canvas.GetLeft(context.MeidoImage), 3);
            Assert.Equal(108.5, Canvas.GetTop(context.MeidoImage), 3);
            Assert.Equal(1, context.PositionMiniChatCallCount);
        });
    }

    [Fact]
    public void HideContent_HidesMeidoAndRadialMenu_AndPopupContent()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: false);
            context.Coordinator.RefreshLayout(300, 300);

            context.Coordinator.HideContent();

            Assert.Equal(Visibility.Hidden, context.MeidoImage.Visibility);
            Assert.Equal(Visibility.Hidden, context.RadialMenu.Visibility);
            Assert.All(context.RadialMenu.RadialButtons, button => Assert.Equal(Visibility.Hidden, button.Visibility));
            Assert.Equal(1, context.HidePopupContentCallCount);
        });
    }

    [Fact]
    public void RestoreContent_WhenMiniChatOpen_HidesMiniChat_AndRestoresVisibleState()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: true);
            context.Coordinator.RefreshLayout(300, 300);
            context.Coordinator.HideContent();

            context.Coordinator.RestoreContent();

            Assert.Equal(Visibility.Visible, context.MeidoImage.Visibility);
            Assert.Equal(Visibility.Visible, context.RadialMenu.Visibility);
            Assert.All(context.RadialMenu.RadialButtons, button => Assert.Equal(Visibility.Visible, button.Visibility));
            Assert.Equal(1, context.HideMiniChatCallCount);
        });
    }

    [Fact]
    public void PlayCloseAnimationAsync_ResetsContentShift_AndInvokesHideWindow()
    {
        RunInStaAsync(async () =>
        {
            var context = CreateContext(isMiniChatOpen: false);
            context.Coordinator.RefreshLayout(300, 300);
            context.ContentShift.X = 5;
            context.ContentShift.Y = -4;
            var hideWindowCallCount = 0;

            await context.Coordinator.PlayCloseAnimationAsync(() => hideWindowCallCount++);

            Assert.Equal(0, context.ContentShift.X, 3);
            Assert.Equal(0, context.ContentShift.Y, 3);
            Assert.Equal(1, hideWindowCallCount);
        });
    }

    private static TestContext CreateContext(bool isMiniChatOpen)
    {
        EnsureApplication();

        var canvas = new Canvas { Width = 300, Height = 300 };
        var meidoImage = new System.Windows.Controls.Image { Width = 83, Height = 83 };
        canvas.Children.Add(meidoImage);

        var radialMenu = new RadialMenuControl
        {
            MenuItems =
            [
                new RadialMenuItem { Icon = "📝", Command = MenuCommands.OpenNotepad, ToolTip = "打开记事本" },
                new RadialMenuItem { Icon = "⚙️", Command = MenuCommands.OpenSettings, ToolTip = "设置妹抖酱" }
            ]
        };

        var contentShift = new TranslateTransform();
        var miniChatOpen = isMiniChatOpen;
        var positionMiniChatCallCount = 0;
        var hidePopupContentCallCount = 0;
        var hideMiniChatCallCount = 0;

        var coordinator = new MainWindowVisualCoordinator(contentShift, canvas, meidoImage, radialMenu);
        coordinator.ConnectMiniChat(
            () => miniChatOpen,
            () => positionMiniChatCallCount++,
            () => hidePopupContentCallCount++,
            () =>
            {
                hideMiniChatCallCount++;
                miniChatOpen = false;
            });

        return new TestContext(
            contentShift,
            canvas,
            meidoImage,
            radialMenu,
            coordinator,
            () => positionMiniChatCallCount,
            () => hidePopupContentCallCount,
            () => hideMiniChatCallCount);
    }

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

    private static void RunInStaAsync(Func<Task> action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

            try
            {
                var task = action();
                task.ContinueWith(
                    _ => dispatcher.BeginInvokeShutdown(DispatcherPriority.Background),
                    TaskScheduler.Default);
                Dispatcher.Run();
                task.GetAwaiter().GetResult();
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

    private sealed class TestContext(
        TranslateTransform contentShift,
        Canvas canvas,
        System.Windows.Controls.Image meidoImage,
        RadialMenuControl radialMenu,
        MainWindowVisualCoordinator coordinator,
        Func<int> getPositionMiniChatCallCount,
        Func<int> getHidePopupContentCallCount,
        Func<int> getHideMiniChatCallCount)
    {
        public TranslateTransform ContentShift { get; } = contentShift;
        public Canvas Canvas { get; } = canvas;
        public System.Windows.Controls.Image MeidoImage { get; } = meidoImage;
        public RadialMenuControl RadialMenu { get; } = radialMenu;
        public MainWindowVisualCoordinator Coordinator { get; } = coordinator;
        public int PositionMiniChatCallCount => getPositionMiniChatCallCount();
        public int HidePopupContentCallCount => getHidePopupContentCallCount();
        public int HideMiniChatCallCount => getHideMiniChatCallCount();
    }
}