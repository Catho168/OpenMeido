using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OpenMeido.Models;
using OpenMeido.ViewModels;

namespace OpenMeido.Tests;

public sealed class SettingsWindowTests
{
    [Fact]
    public void Constructor_AppliesSharpRenderingSettings_OnTransparentWindow()
    {
        RunInSta(() =>
        {
            var window = CreateWindow();
            var rootGrid = Assert.IsType<Grid>(window.Content);
            var surfaceBorder = Assert.IsType<Border>(Assert.Single(rootGrid.Children));

            Assert.True(window.UseLayoutRounding);
            Assert.True(window.SnapsToDevicePixels);
            Assert.Equal(TextFormattingMode.Display, TextOptions.GetTextFormattingMode(window));
            Assert.True(rootGrid.UseLayoutRounding);
            Assert.True(rootGrid.SnapsToDevicePixels);
            Assert.True(surfaceBorder.UseLayoutRounding);
            Assert.True(surfaceBorder.SnapsToDevicePixels);
            Assert.Equal(ClearTypeHint.Enabled, RenderOptions.GetClearTypeHint(surfaceBorder));
        });
    }

    private static SettingsWindow CreateWindow()
    {
        WpfTestApplicationResources.EnsureLoaded();

        var viewModel = new SettingsViewModel(new FakeSettingsService
        {
            LoadResult = new AppSettings
            {
                ApiBaseUrl = "https://example.com/v1",
                ApiKey = "key",
                ModelName = "model",
                MaxTokens = 1000,
                Temperature = 0.7,
                SelectedCategory = SettingsCategory.General
            }
        });

        var mcpServiceFactory = new FakeMcpServiceFactory();
        mcpServiceFactory.Enqueue(new FakeMcpService());
        return new SettingsWindow(viewModel, mcpServiceFactory);
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