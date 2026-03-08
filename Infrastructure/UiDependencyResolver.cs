using System;
using System.Windows;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure
{
    internal static class UiDependencyResolver
    {
        private static IServiceProvider AppServices => (Application.Current as App)?.Services;

        public static MainViewModel ResolveMainViewModel()
            => AppServices?.GetService(typeof(MainViewModel)) as MainViewModel ?? new MainViewModel();

        public static ISettingsService ResolveSettingsService()
            => AppServices?.GetService(typeof(ISettingsService)) as ISettingsService
               ?? new SettingsService(ResolveApiServiceFactory());

        public static IApiServiceFactory ResolveApiServiceFactory()
            => AppServices?.GetService(typeof(IApiServiceFactory)) as IApiServiceFactory ?? new ApiServiceFactory();

        public static IMcpServiceFactory ResolveMcpServiceFactory()
            => AppServices?.GetService(typeof(IMcpServiceFactory)) as IMcpServiceFactory ?? new McpServiceFactory();

        public static IChatService ResolveChatService()
            => AppServices?.GetService(typeof(IChatService)) as IChatService
               ?? new ChatService(ResolveSettingsService(), ResolveApiServiceFactory());

        public static IChatHistoryService ResolveChatHistoryService()
            => AppServices?.GetService(typeof(IChatHistoryService)) as IChatHistoryService ?? new ChatHistoryService();

        public static ChatViewModel ResolveChatViewModel()
            => AppServices?.GetService(typeof(ChatViewModel)) as ChatViewModel
               ?? new ChatViewModel(ResolveChatService(), ResolveChatHistoryService());

        public static SettingsViewModel ResolveSettingsViewModel()
            => AppServices?.GetService(typeof(SettingsViewModel)) as SettingsViewModel
               ?? new SettingsViewModel(ResolveSettingsService());
    }
}