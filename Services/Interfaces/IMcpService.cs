using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModelContextProtocol.Client;
using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public interface IMcpService : IDisposable
    {
        Task InitializeAsync();
        Task<IList<McpClientTool>> GetAvailableToolsAsync();
        Task<(bool Success, string Message)> TestConnectionAsync(McpServerConfig serverConfig);
        Task<IList<McpClientTool>> GetServerToolsAsync(string serverId);
        bool IsAvailable();
        int GetConnectedServerCount();
        Task<List<(string Id, string Name, bool IsConnected, int ToolCount)>> GetServerStatusAsync();
        Task<bool> ReconnectServerAsync(string serverId);
    }
}