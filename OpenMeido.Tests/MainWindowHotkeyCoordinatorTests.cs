using System;
using OpenMeido.Infrastructure;

namespace OpenMeido.Tests;

public sealed class MainWindowHotkeyCoordinatorTests
{
    [Fact]
    public void Attach_AddsHook_AndRegistersConfiguredHotkey()
    {
        var platform = new FakeMainWindowHotkeyPlatform();
        using var coordinator = new MainWindowHotkeyCoordinator(platform);

        coordinator.Attach(new IntPtr(123), () => { });

        Assert.Equal(1, platform.AddHookCallCount);
        Assert.Equal(new IntPtr(123), platform.AddedHookHwnd);
        Assert.NotNull(platform.AddedHook);
        Assert.Equal(1, platform.RegisterHotKeyCallCount);
        Assert.Equal(new IntPtr(123), platform.RegisteredHotkeyHwnd);
        Assert.Equal(9000, platform.RegisterHotKeyId);
        Assert.Equal(0x0001u, platform.RegisterModifiers);
        Assert.Equal(0x52u, platform.RegisterVirtualKey);
    }

    [Fact]
    public void Attach_WhenHotkeyMessageReceived_InvokesCallback_AndMarksHandled()
    {
        var platform = new FakeMainWindowHotkeyPlatform();
        using var coordinator = new MainWindowHotkeyCoordinator(platform);
        var callbackCount = 0;

        coordinator.Attach(new IntPtr(123), () => callbackCount++);

        var handled = false;
        platform.RaiseWindowMessage(0x0312, new IntPtr(9000), IntPtr.Zero, ref handled);

        Assert.Equal(1, callbackCount);
        Assert.True(handled);
    }

    [Fact]
    public void Attach_WhenOtherWindowMessageReceived_DoesNotInvokeCallback()
    {
        var platform = new FakeMainWindowHotkeyPlatform();
        using var coordinator = new MainWindowHotkeyCoordinator(platform);
        var callbackCount = 0;

        coordinator.Attach(new IntPtr(123), () => callbackCount++);

        var handled = false;
        platform.RaiseWindowMessage(0x0400, new IntPtr(9000), IntPtr.Zero, ref handled);

        Assert.Equal(0, callbackCount);
        Assert.False(handled);
    }

    [Fact]
    public void Dispose_RemovesHook_AndUnregistersHotkey_OnlyOnce()
    {
        var platform = new FakeMainWindowHotkeyPlatform();
        var coordinator = new MainWindowHotkeyCoordinator(platform);

        coordinator.Attach(new IntPtr(123), () => { });
        coordinator.Dispose();
        coordinator.Dispose();

        Assert.Equal(1, platform.RemoveHookCallCount);
        Assert.Equal(new IntPtr(123), platform.RemovedHookHwnd);
        Assert.Same(platform.AddedHook, platform.RemovedHook);
        Assert.Equal(1, platform.UnregisterHotKeyCallCount);
        Assert.Equal(new IntPtr(123), platform.UnregisteredHotkeyHwnd);
        Assert.Equal(9000, platform.UnregisterHotKeyId);
    }
}