using System;
using Microsoft.Extensions.DependencyInjection;
using OpenMeido.Services;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels;

namespace OpenMeido.Infrastructure.DI
{
    public static class ServiceRegistration
    {
        public static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IMcpServiceFactory, McpServiceFactory>();
            services.AddSingleton<IApiServiceFactory, ApiServiceFactory>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<IChatService, ChatService>();
            services.AddTransient<IChatHistoryService, ChatHistoryService>();
            services.AddTransient<ChatViewModel>();
            services.AddTransient<ChatWindow>();
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingsWindow>();

            return services.BuildServiceProvider();
        }
    }
}