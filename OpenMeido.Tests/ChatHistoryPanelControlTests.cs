using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OpenMeido.Controls;

namespace OpenMeido.Tests;

public sealed class ChatHistoryPanelControlTests
{
    [Fact]
    public void Constructor_ExposesPanelElements()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.PanelBorder);
            Assert.NotNull(control.ItemsPanel);
        });
    }

    [Fact]
    public void NewChatButton_Click_RaisesNewChatRequested()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var raised = 0;
            control.NewChatRequested += (_, _) => raised++;

            var button = Assert.IsType<Button>(control.FindName("NewChatButton"));
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(1, raised);
        });
    }

    private static ChatHistoryPanelControl CreateControl()
    {
        EnsureApplication();
        return new ChatHistoryPanelControl();
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