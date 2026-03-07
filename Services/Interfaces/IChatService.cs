using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services;

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

        public static ChatServiceInitializationResult Ready(string statusText = "就绪") => new ChatServiceInitializationResult(statusText, "ready");

        public static ChatServiceInitializationResult Warning(string statusText) => new ChatServiceInitializationResult(statusText, "warning");

        public static ChatServiceInitializationResult Error(string statusText) => new ChatServiceInitializationResult(statusText, "error");
    }

    public interface IChatService : IDisposable
    {
        ApiService CurrentApiService { get; }

        Task<ChatServiceInitializationResult> InitializeAsync();

        Task<ChatServiceInitializationResult> ReinitializeAsync();

        Task<string> SendMessageAsync(List<ChatMessage> messagesHistory);
    }
}