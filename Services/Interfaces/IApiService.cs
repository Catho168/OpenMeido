using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public interface IApiService : IDisposable
    {
        Task InitializeMcpAsync();

        Task<string> SendMessageAsync(List<ChatMessage> messagesHistory);

        Task<List<(string Id, string Name, bool IsConnected, int ToolCount)>> GetMcpServerStatusesAsync();

        Task<IList<McpClientTool>> GetAvailableMcpToolsAsync();

        List<McpActivityRecord> GetRecentMcpActivities(int count = 20);

        McpActivityStatistics GetMcpActivityStatistics();

        void ClearMcpActivities();

        Task<bool> TestConnectionAsync();
    }
}
