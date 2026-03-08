using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using OpenMeido.Controls;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class ChatMessageListControlTests
{
    [Fact]
    public void Constructor_ExposesMessageHostElements()
    {
        RunInSta(() =>
        {
            var control = CreateControl();

            Assert.NotNull(control.ScrollViewer);
            Assert.NotNull(control.ItemsHost);
        });
    }

    [Fact]
    public void BindingToDisplayMessages_ReflectsItemsInHost()
    {
        RunInSta(() =>
        {
            var control = CreateControl();
            var viewModel = new ChatViewModel(new FakeChatService(), new FakeChatHistoryService());
            viewModel.DisplayMessages.Add(ChatMessageDisplayItemViewModel.CreateUser("你好"));
            viewModel.DisplayMessages.Add(ChatMessageDisplayItemViewModel.CreateAssistant("欢迎回来"));
            var hostWindow = new Window
            {
                Content = control,
                DataContext = viewModel,
                Width = 480,
                Height = 320
            };

            hostWindow.Show();
            control.ApplyTemplate();
            control.UpdateLayout();

            Assert.Equal(2, control.ItemsHost.Items.Count);
            hostWindow.Close();
        });
    }

    private static ChatMessageListControl CreateControl()
    {
        EnsureApplication();
        return new ChatMessageListControl();
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