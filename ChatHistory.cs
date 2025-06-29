using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace OpenMeido
{
    /// 单条聊天消息
    public class ChatMessage
    {
        /// 消息发送时间
        public DateTime Timestamp { get; set; }

        /// 消息角色：user 或 assistant
        public string Role { get; set; }

        /// 消息内容
        public string Content { get; set; }

        public ChatMessage()
        {
            Timestamp = DateTime.Now;
        }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
            Timestamp = DateTime.Now;
        }
    }

    /// 单次对话会话
    public class ChatSession
    {
        /// 会话唯一标识
        public string SessionId { get; set; }

        /// 会话标题（基于第一条用户消息的时间）
        public string Title { get; set; }

        /// 会话创建时间
        public DateTime CreatedTime { get; set; }

        /// 最后更新时间
        public DateTime LastUpdated { get; set; }

        /// 消息列表
        public List<ChatMessage> Messages { get; set; }

        /// 是否已保存（只有用户发送过消息才保存）
        public bool IsSaved { get; set; }

        public ChatSession()
        {
            SessionId = Guid.NewGuid().ToString();
            Messages = new List<ChatMessage>();
            CreatedTime = DateTime.Now;
            LastUpdated = DateTime.Now;
            IsSaved = false;
        }

        /// 添加消息到会话
        /// <param name="role">消息角色</param>
        /// <param name="content">消息内容</param>
        public void AddMessage(string role, string content)
        {
            Messages.Add(new ChatMessage(role, content));
            LastUpdated = DateTime.Now;

            // 如果是用户消息且是第一条消息，生成标题并标记为已保存
            if (role == "user" && !IsSaved)
            {
                GenerateTitle();
                IsSaved = true;
            }
        }

        /// 根据第一条用户消息的时间生成标题
        private void GenerateTitle()
        {
            var firstUserMessage = Messages.Find(m => m.Role == "user");
            if (firstUserMessage != null)
            {
                Title = firstUserMessage.Timestamp.ToString("yyyy年MM月dd日 HH:mm");
            }
            else
            {
                Title = CreatedTime.ToString("yyyy年MM月dd日 HH:mm");
            }
        }
    }

    /// 聊天历史管理器
    public class ChatHistoryManager
    {
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMeido",
            "chat_history.xml"
        );

        /// 所有聊天会话
        public List<ChatSession> Sessions { get; set; }

        /// 当前活动会话
        public ChatSession CurrentSession { get; set; }

        public ChatHistoryManager()
        {
            Sessions = new List<ChatSession>();
            LoadHistory();
            StartNewSession();
        }

        /// 开始新的聊天会话
        public void StartNewSession()
        {
            CurrentSession = new ChatSession();
        }

        /// 添加消息到当前会话
        /// <param name="role">消息角色</param>
        /// <param name="content">消息内容</param>
        public void AddMessage(string role, string content)
        {
            CurrentSession.AddMessage(role, content);

            // 如果会话已保存，将其添加到历史记录中（如果还没有的话）
            if (CurrentSession.IsSaved && !Sessions.Contains(CurrentSession))
            {
                Sessions.Insert(0, CurrentSession); // 插入到列表开头，最新的在前面
                SaveHistory();
            }
            else if (CurrentSession.IsSaved)
            {
                // 更新已存在的会话
                SaveHistory();
            }
        }

        /// 加载聊天历史
        public void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryPath))
                {
                    var serializer = new XmlSerializer(typeof(List<ChatSession>));
                    using (var reader = new FileStream(HistoryPath, FileMode.Open))
                    {
                        var loadedSessions = (List<ChatSession>)serializer.Deserialize(reader);
                        Sessions = loadedSessions ?? new List<ChatSession>();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载聊天历史失败: {ex.Message}");
                Sessions = new List<ChatSession>();
            }
        }

        /// 保存聊天历史
        public void SaveHistory()
        {
            try
            {
                string directory = Path.GetDirectoryName(HistoryPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 只保存已保存的会话
                var sessionsToSave = Sessions.Where(s => s.IsSaved).ToList();

                var serializer = new XmlSerializer(typeof(List<ChatSession>));
                using (var writer = new FileStream(HistoryPath, FileMode.Create))
                {
                    serializer.Serialize(writer, sessionsToSave);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存聊天历史失败: {ex.Message}");
            }
        }

        /// 删除指定会话
        /// <param name="sessionId">会话ID</param>
        public void DeleteSession(string sessionId)
        {
            Sessions.RemoveAll(s => s.SessionId == sessionId);
            SaveHistory();
        }

        /// 清空所有历史记录
        public void ClearAllHistory()
        {
            Sessions.Clear();
            SaveHistory();
        }

        /// 加载指定会话
        /// <param name="sessionId">会话ID</param>
        /// <returns>找到的会话，如果没找到返回null</returns>
        public ChatSession LoadSession(string sessionId)
        {
            return Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        }
    }
}
