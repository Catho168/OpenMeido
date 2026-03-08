using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using OpenMeido.Controls;

namespace OpenMeido.Tests;

public sealed class ChatInputPanelControlTests
{
    [Fact]
    public void Constructor_ExposesInputElements()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.InputTextBox);
            Assert.NotNull(control.PlaceholderTextBlock);
        });
    }

    [Fact]
    public void Constructor_LoadsWithoutApplicationLevelInputStyles_AndAppliesLocalStyles()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var sendButton = Assert.IsType<Button>(control.FindName("SendButton"));

            Assert.NotNull(control.InputTextBox.Style);
            Assert.NotNull(sendButton.Style);
            Assert.Same(control.InputTextBox.Style, control.Resources["InputTextBoxStyle"]);
            Assert.Same(sendButton.Style, control.Resources["SendButtonStyle"]);
        });
    }

    [Fact]
    public void Interactions_RaiseForwardedEvents()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var sendRaised = 0;
            var textChangedRaised = 0;
            control.SendRequested += (_, _) => sendRaised++;
            control.InputTextChanged += (_, _) => textChangedRaised++;

            var sendButton = Assert.IsType<Button>(control.FindName("SendButton"));
            control.InputTextBox.RaiseEvent(new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None));
            sendButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.Equal(1, sendRaised);
            Assert.Equal(1, textChangedRaised);
        });
    }

    private static ChatInputPanelControl CreateControl()
    {
        EnsureApplication();
        return new ChatInputPanelControl();
    }

    private static void EnsureApplication()
    {
        var application = WpfTestApplicationResources.EnsureLoaded();
        application.Resources.Remove("SendButtonStyle");
        application.Resources.Remove("InputTextBoxStyle");
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