using System;
using System.Collections.Generic;
using System.Linq;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Services
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly ChatHistoryManager _historyManager = new ChatHistoryManager();

        public IReadOnlyList<ChatSession> SavedSessions => _historyManager.Sessions.Where(s => s.IsSaved).ToList();

        public ChatSession CurrentSession => _historyManager.CurrentSession;

        public IReadOnlyList<ChatMessage> CurrentMessages => _historyManager.CurrentSession?.Messages ?? new List<ChatMessage>();

        public void StartNewSession()
        {
            _historyManager.StartNewSession();
        }

        public void AddMessage(string role, string content)
        {
            _historyManager.AddMessage(role, content);
        }

        public void DeleteSession(string sessionId)
        {
            _historyManager.DeleteSession(sessionId);
        }

        public void SetCurrentSession(ChatSession session)
        {
            _historyManager.CurrentSession = session ?? new ChatSession();
        }
    }
}