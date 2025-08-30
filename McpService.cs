using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Transport;

namespace OpenMeido
{
    /// MCP服务管理类，负责管理MCP客户端连接和工具调用
    public class McpService : IDisposable
    {
        // MCP客户端字典，键为服务器ID
        private readonly Dictionary<string, IMcpClient> mcpClients;
        
        // 应用程序设置，包含MCP配置信息
        private readonly AppSettings settings;
        
        // 标记是否已释放资源
        private bool disposed = false;

        // MCP活动日志记录器
        private McpActivityLogger activityLogger;

        /// 构造函数，初始化MCP服务
        /// <param name="appSettings">应用程序设置</param>
        /// <param name="logger">活动日志记录器（可选）</param>
        public McpService(AppSettings appSettings, McpActivityLogger logger = null)
        {
            settings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            mcpClients = new Dictionary<string, IMcpClient>();
            activityLogger = logger ?? new McpActivityLogger();
        }

        /// 初始化所有启用的MCP服务器连接
        /// <returns>初始化任务</returns>
        public async Task InitializeAsync()
        {
            if (!settings.EnableMcp || settings.McpServers == null)
            {
                return;
            }

            // 清理现有连接
            await DisposeClientsAsync();

            // 初始化启用的服务器
            foreach (var serverConfig in settings.McpServers.Where(s => s.IsEnabled && s.IsValid()))
            {
                try
                {
                    var client = await CreateMcpClientAsync(serverConfig);
                    if (client != null)
                    {
                        mcpClients[serverConfig.Id] = client;

                        // 获取工具数量并记录连接成功
                        var tools = await client.ListToolsAsync();
                        var toolCount = tools?.Count ?? 0;
                        activityLogger.LogServerConnection(serverConfig.Name, true, toolCount);

                        System.Diagnostics.Debug.WriteLine($"MCP服务器 '{serverConfig.Name}' 初始化成功");
                    }
                    else
                    {
                        activityLogger.LogServerConnection(serverConfig.Name, false, 0, "客户端创建失败");
                    }
                }
                catch (Exception ex)
                {
                    activityLogger.LogServerConnection(serverConfig.Name, false, 0, ex.Message);
                    System.Diagnostics.Debug.WriteLine($"初始化MCP服务器 '{serverConfig.Name}' 失败: {ex.Message}");
                }
            }
        }

        /// 创建MCP客户端连接
        /// <param name="serverConfig">服务器配置</param>
        /// <returns>MCP客户端实例</returns>
        private async Task<IMcpClient> CreateMcpClientAsync(McpServerConfig serverConfig)
        {
            try
            {
                // 准备传输选项
                var transportOptions = new Dictionary<string, string>
                {
                    ["command"] = serverConfig.Command
                };

                // 如果有参数，添加到传输选项
                if (!string.IsNullOrWhiteSpace(serverConfig.Arguments))
                {
                    transportOptions["arguments"] = serverConfig.Arguments;
                }

                // 使用工厂方法创建并连接MCP客户端，按照README示例的格式
                var client = await McpClientFactory.CreateAsync(new ModelContextProtocol.McpServerConfig
                {
                    Id = serverConfig.Id,
                    Name = serverConfig.Name,
                    TransportType = TransportTypes.StdIo,
                    TransportOptions = transportOptions
                });

                System.Diagnostics.Debug.WriteLine($"MCP服务器 '{serverConfig.Name}' 连接成功");
                return client;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建MCP客户端失败: {ex.Message}");
                return null;
            }
        }



        /// 获取所有可用的MCP工具
        /// <returns>MCP工具列表</returns>
        public async Task<IList<McpClientTool>> GetAvailableToolsAsync()
        {
            var allTools = new List<McpClientTool>();

            foreach (var kvp in mcpClients)
            {
                try
                {
                    var tools = await kvp.Value.ListToolsAsync();
                    allTools.AddRange(tools);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取MCP服务器 '{kvp.Key}' 工具列表失败: {ex.Message}");
                }
            }

            return allTools;
        }

        /// 测试MCP服务器连接
        /// <param name="serverConfig">服务器配置</param>
        /// <returns>测试结果</returns>
        public async Task<(bool Success, string Message)> TestConnectionAsync(McpServerConfig serverConfig)
        {
            if (!serverConfig.IsValid())
            {
                return (false, "服务器配置无效：请填写完整的服务器名称和命令");
            }

            try
            {
                // 创建临时客户端进行测试
                var testClient = await CreateMcpClientAsync(serverConfig);
                if (testClient != null)
                {
                    // 尝试获取工具列表来验证连接
                    var tools = await testClient.ListToolsAsync();
                    var toolCount = tools?.Count ?? 0;

                    // 清理测试客户端
                    if (testClient is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }

                    return (true, $"连接成功！\n服务器：{serverConfig.Name}\n可用工具：{toolCount} 个");
                }
                else
                {
                    return (false, $"无法连接到服务器 '{serverConfig.Name}'。\n请检查命令和参数是否正确。");
                }
            }
            catch (Exception ex)
            {
                return (false, $"连接测试失败：{ex.Message}");
            }
        }

        /// 获取指定服务器的工具列表
        /// <param name="serverId">服务器ID</param>
        /// <returns>工具列表</returns>
        public async Task<IList<McpClientTool>> GetServerToolsAsync(string serverId)
        {
            if (mcpClients.TryGetValue(serverId, out var client))
            {
                try
                {
                    return await client.ListToolsAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取服务器 '{serverId}' 工具列表失败: {ex.Message}");
                }
            }

            return new List<McpClientTool>();
        }

        /// 检查MCP功能是否可用
        /// <returns>是否可用</returns>
        public bool IsAvailable()
        {
            // 检查是否启用MCP且有可用的客户端连接
            return settings.EnableMcp && mcpClients.Count > 0;
        }

        /// 获取已连接的服务器数量
        /// <returns>服务器数量</returns>
        public int GetConnectedServerCount()
        {
            return mcpClients.Count;
        }

        /// 获取MCP服务器状态信息
        /// <returns>服务器状态信息列表</returns>
        public List<(string Id, string Name, bool IsConnected, int ToolCount)> GetServerStatus()
        {
            var statusList = new List<(string Id, string Name, bool IsConnected, int ToolCount)>();

            if (settings.McpServers != null)
            {
                foreach (var serverConfig in settings.McpServers.Where(s => s.IsEnabled))
                {
                    bool isConnected = mcpClients.ContainsKey(serverConfig.Id);
                    int toolCount = 0;

                    if (isConnected)
                    {
                        try
                        {
                            var tools = mcpClients[serverConfig.Id].ListToolsAsync().Result;
                            toolCount = tools?.Count ?? 0;
                        }
                        catch
                        {
                            // 如果获取工具列表失败，认为连接有问题
                            isConnected = false;
                        }
                    }

                    statusList.Add((serverConfig.Id, serverConfig.Name, isConnected, toolCount));
                }
            }

            return statusList;
        }

        /// 重新连接指定的MCP服务器
        /// <param name="serverId">服务器ID</param>
        /// <returns>重连是否成功</returns>
        public async Task<bool> ReconnectServerAsync(string serverId)
        {
            var serverConfig = settings.McpServers?.FirstOrDefault(s => s.Id == serverId);
            if (serverConfig == null || !serverConfig.IsEnabled)
            {
                return false;
            }

            try
            {
                // 先断开现有连接
                if (mcpClients.TryGetValue(serverId, out var existingClient))
                {
                    if (existingClient is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    mcpClients.Remove(serverId);
                }

                // 重新创建连接
                var newClient = await CreateMcpClientAsync(serverConfig);
                if (newClient != null)
                {
                    mcpClients[serverId] = newClient;
                    System.Diagnostics.Debug.WriteLine($"MCP服务器 '{serverConfig.Name}' 重连成功");
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"重连MCP服务器 '{serverConfig.Name}' 失败: {ex.Message}");
            }

            return false;
        }

        /// 清理所有MCP客户端连接
        private async Task DisposeClientsAsync()
        {
            foreach (var kvp in mcpClients)
            {
                try
                {
                    if (kvp.Value is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"释放MCP客户端 '{kvp.Key}' 失败: {ex.Message}");
                }
            }
            mcpClients.Clear();
            await Task.CompletedTask; // 添加await以消除警告
        }

        /// 释放资源
        public void Dispose()
        {
            if (!disposed)
            {
                // 同步释放资源
                Task.Run(async () => await DisposeClientsAsync()).Wait();
                disposed = true;
            }
        }
    }
}
