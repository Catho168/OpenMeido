using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Services
{
    public class ChatService : IChatService
    {
        private readonly ISettingsService _settingsService;
        private readonly IApiServiceFactory _apiServiceFactory;

        public ChatService(ISettingsService settingsService, IApiServiceFactory apiServiceFactory)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _apiServiceFactory = apiServiceFactory ?? throw new ArgumentNullException(nameof(apiServiceFactory));
        }

        public IApiService CurrentApiService { get; private set; }

        public async Task<ChatServiceInitializationResult> InitializeAsync()
        {
            DisposeCurrentApiService();

            try
            {
                var settings = _settingsService.Load();
                if (settings?.IsValid() != true)
                {
                    return ChatServiceInitializationResult.Warning("需要配置API");
                }

                CurrentApiService = _apiServiceFactory.Create(settings);
                if (settings.EnableMcp)
                {
                    await CurrentApiService.InitializeMcpAsync();
                }

                return ChatServiceInitializationResult.Ready();
            }
            catch (Exception ex)
            {
                DisposeCurrentApiService();
                Debug.WriteLine($"初始化聊天服务失败: {ex.Message}");
                return ChatServiceInitializationResult.Error("初始化失败");
            }
        }

        public Task<ChatServiceInitializationResult> ReinitializeAsync()
        {
            return InitializeAsync();
        }

        public Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
        {
            if (CurrentApiService == null)
            {
                throw new InvalidOperationException("聊天服务尚未初始化");
            }

            return CurrentApiService.SendMessageAsync(messagesHistory);
        }

        public void Dispose()
        {
            DisposeCurrentApiService();
        }

        private void DisposeCurrentApiService()
        {
            CurrentApiService?.Dispose();
            CurrentApiService = null;
        }
    }
}