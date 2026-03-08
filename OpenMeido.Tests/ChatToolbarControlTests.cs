using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OpenMeido.Controls;

namespace OpenMeido.Tests;

public sealed class ChatToolbarControlTests
{
    [Fact]
    public void Constructor_ExposesToolbarElements()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.HistoryToggleIcon);
            Assert.NotNull(control.CurrentSessionTitle);
        });
    }

    [Fact]
    public void ToolbarButtons_RaiseForwardedEvents()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var historyToggleRaised = 0;
            var mcpStatusRaised = 0;
            var clearRaised = 0;
            var settingsRaised = 0;

            control.HistoryToggleRequested += (_, _) => historyToggleRaised++;
            control.McpStatusRequested += (_, _) => mcpStatusRaised++;
            control.ClearRequested += (_, _) => clearRaised++;
            control.SettingsRequested += (_, _) => settingsRaised++;

            Assert.IsType<Button>(control.FindName("HistoryToggleButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.IsType<Button>(control.FindName("McpStatusButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.IsType<Button>(control.FindName("ClearButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.IsType<Button>(control.FindName("SettingsButton"))
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(1, historyToggleRaised);
            Assert.Equal(1, mcpStatusRaised);
            Assert.Equal(1, clearRaised);
            Assert.Equal(1, settingsRaised);
        });
    }

    private static ChatToolbarControl CreateControl()
    {
        EnsureApplication();
        return new ChatToolbarControl();
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