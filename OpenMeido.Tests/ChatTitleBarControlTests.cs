using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using OpenMeido.Controls;

namespace OpenMeido.Tests;

public sealed class ChatTitleBarControlTests
{
    [Fact]
    public void Constructor_ExposesStatusTextBlock()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.StatusTextBlock);
        });
    }

    [Fact]
    public void Constructor_LoadsWithSharedTitleBarStyles_AndAppliesApplicationStyles()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var minimizeButton = Assert.IsType<Button>(control.FindName("MinimizeButton"));
            var closeButton = Assert.IsType<Button>(control.FindName("CloseButton"));
            var application = Assert.IsType<Application>(Application.Current);

            Assert.NotNull(minimizeButton.Style);
            Assert.NotNull(closeButton.Style);
            Assert.Same(minimizeButton.Style, application.FindResource("ChatTitleBarButtonStyle"));
            Assert.Same(closeButton.Style, application.FindResource("ChatCloseButtonStyle"));
        });
    }

    [Fact]
    public void Interactions_RaiseForwardedEvents()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var dragRaised = 0;
            var minimizeRaised = 0;
            var closeRaised = 0;
            control.DragRequested += (_, _) => dragRaised++;
            control.MinimizeRequested += (_, _) => minimizeRaised++;
            control.CloseRequested += (_, _) => closeRaised++;

            var titleBarBorder = Assert.IsType<Border>(control.FindName("TitleBarBorder"));
            var minimizeButton = Assert.IsType<Button>(control.FindName("MinimizeButton"));
            var closeButton = Assert.IsType<Button>(control.FindName("CloseButton"));

            titleBarBorder.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent
            });
            minimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(1, dragRaised);
            Assert.Equal(1, minimizeRaised);
            Assert.Equal(1, closeRaised);
        });
    }

    private static ChatTitleBarControl CreateControl()
    {
        EnsureApplication();
        return new ChatTitleBarControl();
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
}