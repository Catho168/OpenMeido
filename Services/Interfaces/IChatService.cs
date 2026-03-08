using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMeido.Helpers;
using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public sealed class ChatServiceInitializationResult
    {
        private ChatServiceInitializationResult(string statusText, string statusType)
        {
            StatusText = statusText;
            StatusType = statusType;
        }

        public string StatusText { get; }

        public string StatusType { get; }

        public static ChatServiceInitializationResult Ready(string statusText = "就绪") => new ChatServiceInitializationResult(statusText, ChatStatusTypes.Ready);

        public static ChatServiceInitializationResult Warning(string statusText) => new ChatServiceInitializationResult(statusText, ChatStatusTypes.Warning);

        public static ChatServiceInitializationResult Error(string statusText) => new ChatServiceInitializationResult(statusText, ChatStatusTypes.Error);
    }

    public interface IChatService : IDisposable
    {
        IApiService CurrentApiService { get; }

        Task<ChatServiceInitializationResult> InitializeAsync();

        Task<ChatServiceInitializationResult> ReinitializeAsync();

        Task<string> SendMessageAsync(List<ChatMessage> messagesHistory);
    }
}