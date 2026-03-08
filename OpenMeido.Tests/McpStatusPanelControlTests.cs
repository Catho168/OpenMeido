using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OpenMeido.Controls;

namespace OpenMeido.Tests;

public sealed class McpStatusPanelControlTests
{
    [Fact]
    public void Constructor_ExposesPanelElements()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.PanelBorder);
            Assert.NotNull(control.ServersPanel);
            Assert.NotNull(control.ToolsPanel);
            Assert.NotNull(control.ActivityPanel);
        });
    }

    [Fact]
    public void Buttons_Click_RaiseForwardedEvents()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var refreshRaised = 0;
            var clearRaised = 0;
            control.RefreshRequested += (_, _) => refreshRaised++;
            control.ClearLogRequested += (_, _) => clearRaised++;

            var refreshButton = Assert.IsType<Button>(control.FindName("RefreshMcpButton"));
            var clearButton = Assert.IsType<Button>(control.FindName("ClearMcpLogButton"));
            refreshButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            clearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(1, refreshRaised);
            Assert.Equal(1, clearRaised);
        });
    }

    private static McpStatusPanelControl CreateControl()
    {
        EnsureApplication();
        return new McpStatusPanelControl();
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