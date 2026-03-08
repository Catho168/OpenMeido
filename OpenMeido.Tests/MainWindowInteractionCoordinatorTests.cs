using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using OpenMeido.Infrastructure;

namespace OpenMeido.Tests;

public sealed class MainWindowInteractionCoordinatorTests
{
    [Fact]
    public void ShowAtMouse_CentersWindowAtCursor_RefreshesLayout_AndActivatesWindow()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: false);
            context.Platform.CursorScreenPosition = new Point(300, 200);
            context.Platform.DpiScale = new Point(2, 2);

            context.Coordinator.ShowAtMouse();

            Assert.Equal(50, context.Window.Left, 3);
            Assert.Equal(50, context.Window.Top, 3);
            Assert.Equal(1, context.Platform.ShowCallCount);
            Assert.Equal(1, context.Platform.ActivateCallCount);
            Assert.Equal(1, context.RefreshLayoutCallCount);
        });
    }

    [Fact]
    public void HandleMouseMove_WhenMiniChatClosed_UpdatesContentShift_AndButtonScaleTrigger()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: false);

            context.Coordinator.HandleMouseMove(new Point(200, 100));

            Assert.Equal(7, context.ContentShift.X, 3);
            Assert.Equal(7, context.ContentShift.Y, 3);
            Assert.Equal(new Point(200, 100), context.LastScalePoint);
            Assert.Equal(1, context.UpdateButtonScalesCallCount);
        });
    }

    [Fact]
    public void HandleMouseMove_WhenMiniChatOpen_DoesNothing()
    {
        RunInSta(() =>
        {
            var context = CreateContext(isMiniChatOpen: true);
            context.ContentShift.X = 3;
            context.ContentShift.Y = -2;

            context.Coordinator.HandleMouseMove(new Point(200, 100));

            Assert.Equal(3, context.ContentShift.X, 3);
            Assert.Equal(-2, context.ContentShift.Y, 3);
            Assert.Equal(0, context.UpdateButtonScalesCallCount);
        });
    }

    [Fact]
    public void HandleMouseLeaveAsync_WhenMiniChatClosed_StartsCloseAnimation()
    {
        RunInStaAsync(async () =>
        {
            var context = CreateContext(isMiniChatOpen: false);

            await context.Coordinator.HandleMouseLeaveAsync();

            Assert.Equal(1, context.CloseAnimationCallCount);
        });
    }

    [Fact]
    public void HandleMouseLeaveAsync_WhenMiniChatOpen_DoesNotStartCloseAnimation()
    {
        RunInStaAsync(async () =>
        {
            var context = CreateContext(isMiniChatOpen: true);

            await context.Coordinator.HandleMouseLeaveAsync();

            Assert.Equal(0, context.CloseAnimationCallCount);
        });
    }

    private static TestContext CreateContext(bool isMiniChatOpen)
    {
        var window = new Window
        {
            Width = 200,
            Height = 100
        };

        var contentShift = new TranslateTransform();
        var platform = new FakeMainWindowInteractionPlatform();
        var refreshLayoutCallCount = 0;
        var updateButtonScalesCallCount = 0;
        Point? lastScalePoint = null;
        var closeAnimationCallCount = 0;
        var miniChatOpen = isMiniChatOpen;

        var coordinator = new MainWindowInteractionCoordinator(
            window,
            contentShift,
            point =>
            {
                updateButtonScalesCallCount++;
                lastScalePoint = point;
            },
            () => refreshLayoutCallCount++,
            () => miniChatOpen,
            () =>
            {
                closeAnimationCallCount++;
                return Task.CompletedTask;
            },
            platform);

        return new TestContext(
            window,
            contentShift,
            coordinator,
            platform,
            () => refreshLayoutCallCount,
            () => updateButtonScalesCallCount,
            () => lastScalePoint,
            () => closeAnimationCallCount);
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
            try
            {
                action().GetAwaiter().GetResult();
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
        Window window,
        TranslateTransform contentShift,
        MainWindowInteractionCoordinator coordinator,
        FakeMainWindowInteractionPlatform platform,
        Func<int> getRefreshLayoutCallCount,
        Func<int> getUpdateButtonScalesCallCount,
        Func<Point?> getLastScalePoint,
        Func<int> getCloseAnimationCallCount)
    {
        public Window Window { get; } = window;
        public TranslateTransform ContentShift { get; } = contentShift;
        public MainWindowInteractionCoordinator Coordinator { get; } = coordinator;
        public FakeMainWindowInteractionPlatform Platform { get; } = platform;
        public int RefreshLayoutCallCount => getRefreshLayoutCallCount();
        public int UpdateButtonScalesCallCount => getUpdateButtonScalesCallCount();
        public Point? LastScalePoint => getLastScalePoint();
        public int CloseAnimationCallCount => getCloseAnimationCallCount();
    }

    private sealed class FakeMainWindowInteractionPlatform : IMainWindowInteractionPlatform
    {
        public Point CursorScreenPosition { get; set; } = new(0, 0);
        public Point DpiScale { get; set; } = new(1, 1);
        public int ShowCallCount { get; private set; }
        public int ActivateCallCount { get; private set; }

        public Point GetCursorScreenPosition() => CursorScreenPosition;

        public Point GetDpiScale(Visual visual) => DpiScale;

        public void Show(Window window)
        {
            ShowCallCount++;
        }

        public void Activate(Window window)
        {
            ActivateCallCount++;
        }
    }
}