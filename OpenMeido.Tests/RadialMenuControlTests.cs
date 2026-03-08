using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Tests;

public sealed class RadialMenuControlTests
{
    [Fact]
    public void RefreshLayout_CreatesAndPositionsButtons()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            control.RefreshLayout(300, 300, isMiniChatOpen: false);

            var buttons = control.RadialButtons.ToList();
            Assert.Equal(2, buttons.Count);
            Assert.All(buttons, button =>
            {
                Assert.False(double.IsNaN(System.Windows.Controls.Canvas.GetLeft(button)));
                Assert.False(double.IsNaN(System.Windows.Controls.Canvas.GetTop(button)));
            });
        });
    }

    [Fact]
    public void SetButtonsVisibility_UpdatesAllButtons()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            control.RefreshLayout(300, 300, isMiniChatOpen: false);

            control.SetButtonsVisibility(Visibility.Hidden);
            Assert.All(control.RadialButtons, button => Assert.Equal(Visibility.Hidden, button.Visibility));

            control.SetButtonsVisibility(Visibility.Visible);
            Assert.All(control.RadialButtons, button => Assert.Equal(Visibility.Visible, button.Visibility));
        });
    }

    [Fact]
    public void PlayCloseAnimationAsync_AddsScaleAndRotateTransforms()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            control.RefreshLayout(300, 300, isMiniChatOpen: false);

#pragma warning disable xUnit1031 // WPF control access must stay on this STA thread during the test.
            control.PlayCloseAnimationAsync(TimeSpan.FromMilliseconds(1)).GetAwaiter().GetResult();
#pragma warning restore xUnit1031

            Assert.All(control.RadialButtons, button =>
            {
                var group = Assert.IsType<TransformGroup>(button.RenderTransform);
                Assert.Single(group.Children.OfType<ScaleTransform>());
                Assert.Single(group.Children.OfType<RotateTransform>());
            });
        });
    }

    [Fact]
    public void RefreshLayout_AfterCloseAnimation_RestoresSpreadPositions_AndClearsAnimationResidue()
    {
        RunInStaAsync(async () =>
        {
            var control = CreateControl();
            control.RefreshLayout(300, 300, isMiniChatOpen: false);
            var initialPositions = control.RadialButtons
                .Select(button => (Left: Canvas.GetLeft(button), Top: Canvas.GetTop(button)))
                .ToList();

            await control.PlayCloseAnimationAsync(TimeSpan.FromMilliseconds(1));
            control.RefreshLayout(300, 300, isMiniChatOpen: false);

            var buttons = control.RadialButtons.ToList();
            Assert.Equal(initialPositions.Count, buttons.Count);

            for (int i = 0; i < buttons.Count; i++)
            {
                Assert.Equal(initialPositions[i].Left, Canvas.GetLeft(buttons[i]), 3);
                Assert.Equal(initialPositions[i].Top, Canvas.GetTop(buttons[i]), 3);
                var scale = Assert.IsType<ScaleTransform>(buttons[i].RenderTransform);
                Assert.Equal(1, scale.ScaleX, 3);
                Assert.Equal(1, scale.ScaleY, 3);
            }
        });
    }

    private static RadialMenuControl CreateControl() =>
        CreateInitializedControl();

    private static RadialMenuControl CreateInitializedControl()
    {
        EnsureApplication();
        return new RadialMenuControl
        {
            MenuItems =
            [
                new RadialMenuItem { Icon = "📝", Command = MenuCommands.OpenNotepad, ToolTip = "打开记事本" },
                new RadialMenuItem { Icon = "⚙️", Command = MenuCommands.OpenSettings, ToolTip = "设置妹抖酱" }
            ]
        };
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
}