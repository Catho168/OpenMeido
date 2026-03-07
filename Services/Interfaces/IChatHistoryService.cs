using System.Collections.Generic;
using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public interface IChatHistoryService
    {
        IReadOnlyList<ChatSession> SavedSessions { get; }

        ChatSession CurrentSession { get; }

        IReadOnlyList<ChatMessage> CurrentMessages { get; }

        void StartNewSession();

        void AddMessage(string role, string content);

        void DeleteSession(string sessionId);

        void SetCurrentSession(ChatSession session);
    }
}