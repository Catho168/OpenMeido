using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenMeido.Services
{
    /// MCP活动记录项
    public class McpActivityRecord
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = "";
        public string ServerName { get; set; } = "";
        public string ToolName { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string Result { get; set; } = "";
        public bool IsSuccess { get; set; }
        public double ExecutionTimeMs { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    /// MCP活动日志管理器，负责记录和管理MCP工具调用活动
    public class McpActivityLogger
    {
        private readonly List<McpActivityRecord> activityRecords;
        private readonly string logFilePath;
        private readonly int maxRecords;

        /// 构造函数
        /// <param name="maxRecords">最大记录数量，默认1000条</param>
        public McpActivityLogger(int maxRecords = 1000)
        {
            this.maxRecords = maxRecords;
            activityRecords = new List<McpActivityRecord>();
            
            // 设置日志文件路径
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMeido",
                "Logs"
            );
            
            Directory.CreateDirectory(appDataPath);
            logFilePath = Path.Combine(appDataPath, "mcp_activity.json");
            
            // 加载现有日志
            LoadActivityRecords();
        }

        /// 记录MCP工具调用开始
        /// <param name="serverName">服务器名称</param>
        /// <param name="toolName">工具名称</param>
        /// <param name="parameters">调用参数</param>
        /// <returns>活动记录ID</returns>
        public string LogToolCallStart(string serverName, string toolName, Dictionary<string, object> parameters)
        {
            var record = new McpActivityRecord
            {
                Timestamp = DateTime.Now,
                ActivityType = "ToolCallStart",
                ServerName = serverName,
                ToolName = toolName,
                Parameters = parameters ?? new Dictionary<string, object>(),
                IsSuccess = false // 初始状态
            };

            lock (activityRecords)
            {
                activityRecords.Add(record);
                TrimRecords();
            }

            System.Diagnostics.Debug.WriteLine($"[MCP活动] 工具调用开始: {serverName}.{toolName} | 参数: {JsonSerializer.Serialize(parameters)} | 时间: {record.Timestamp:HH:mm:ss.fff}");
            
            return record.Timestamp.Ticks.ToString();
        }

        /// 记录MCP工具调用完成
        /// <param name="recordId">记录ID</param>
        /// <param name="result">执行结果</param>
        /// <param name="isSuccess">是否成功</param>
        /// <param name="executionTimeMs">执行时间（毫秒）</param>
        /// <param name="errorMessage">错误信息（如果有）</param>
        public void LogToolCallEnd(string recordId, string result, bool isSuccess, double executionTimeMs, string errorMessage = "")
        {
            if (!long.TryParse(recordId, out long ticks))
                return;

            var timestamp = new DateTime(ticks);

            lock (activityRecords)
            {
                var record = activityRecords.FindLast(r => 
                    r.Timestamp == timestamp && r.ActivityType == "ToolCallStart");

                if (record != null)
                {
                    record.Result = result ?? "";
                    record.IsSuccess = isSuccess;
                    record.ExecutionTimeMs = executionTimeMs;
                    record.ErrorMessage = errorMessage ?? "";
                    record.ActivityType = "ToolCallComplete";
                }
            }

            var status = isSuccess ? "成功" : "失败";
            System.Diagnostics.Debug.WriteLine($"[MCP活动] 工具调用{status}: 耗时{executionTimeMs:F0}ms | 结果: {(result?.Length > 100 ? result.Substring(0, 100) + "..." : result)}");
        }

        /// 记录MCP服务器连接事件
        /// <param name="serverName">服务器名称</param>
        /// <param name="isConnected">是否连接成功</param>
        /// <param name="toolCount">工具数量</param>
        /// <param name="errorMessage">错误信息（如果有）</param>
        public void LogServerConnection(string serverName, bool isConnected, int toolCount, string errorMessage = "")
        {
            var record = new McpActivityRecord
            {
                Timestamp = DateTime.Now,
                ActivityType = "ServerConnection",
                ServerName = serverName,
                IsSuccess = isConnected,
                Result = isConnected ? $"连接成功，{toolCount}个工具可用" : "连接失败",
                ErrorMessage = errorMessage ?? ""
            };

            lock (activityRecords)
            {
                activityRecords.Add(record);
                TrimRecords();
            }

            var status = isConnected ? "成功" : "失败";
            System.Diagnostics.Debug.WriteLine($"[MCP活动] 服务器连接{status}: {serverName} | 工具数: {toolCount} | 时间: {record.Timestamp:HH:mm:ss.fff}");
        }

        /// 获取最近的活动记录
        /// <param name="count">记录数量</param>
        /// <returns>活动记录列表</returns>
        public List<McpActivityRecord> GetRecentActivities(int count = 50)
        {
            lock (activityRecords)
            {
                var recentRecords = new List<McpActivityRecord>();
                var startIndex = Math.Max(0, activityRecords.Count - count);
                
                for (int i = startIndex; i < activityRecords.Count; i++)
                {
                    recentRecords.Add(activityRecords[i]);
                }
                
                return recentRecords;
            }
        }

        /// 获取指定时间范围内的活动记录
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns>活动记录列表</returns>
        public List<McpActivityRecord> GetActivitiesByTimeRange(DateTime startTime, DateTime endTime)
        {
            lock (activityRecords)
            {
                return activityRecords.FindAll(r => r.Timestamp >= startTime && r.Timestamp <= endTime);
            }
        }

        /// 获取工具调用统计信息
        /// <returns>统计信息</returns>
        public McpActivityStatistics GetStatistics()
        {
            lock (activityRecords)
            {
                var stats = new McpActivityStatistics();
                var completedCalls = activityRecords.FindAll(r => r.ActivityType == "ToolCallComplete");
                
                stats.TotalToolCalls = completedCalls.Count;
                stats.SuccessfulCalls = completedCalls.FindAll(r => r.IsSuccess).Count;
                stats.FailedCalls = stats.TotalToolCalls - stats.SuccessfulCalls;
                
                if (completedCalls.Count > 0)
                {
                    stats.AverageExecutionTime = completedCalls.Average(r => r.ExecutionTimeMs);
                    stats.MaxExecutionTime = completedCalls.Max(r => r.ExecutionTimeMs);
                    stats.MinExecutionTime = completedCalls.Min(r => r.ExecutionTimeMs);
                }

                // 统计各服务器的调用次数
                stats.ServerCallCounts = new Dictionary<string, int>();
                foreach (var call in completedCalls)
                {
                    if (!string.IsNullOrEmpty(call.ServerName))
                    {
                        stats.ServerCallCounts[call.ServerName] = 
                            stats.ServerCallCounts.GetValueOrDefault(call.ServerName, 0) + 1;
                    }
                }

                // 统计各工具的调用次数
                stats.ToolCallCounts = new Dictionary<string, int>();
                foreach (var call in completedCalls)
                {
                    if (!string.IsNullOrEmpty(call.ToolName))
                    {
                        stats.ToolCallCounts[call.ToolName] = 
                            stats.ToolCallCounts.GetValueOrDefault(call.ToolName, 0) + 1;
                    }
                }

                return stats;
            }
        }

        /// 清空活动记录
        public void ClearActivities()
        {
            lock (activityRecords)
            {
                activityRecords.Clear();
            }
            
            SaveActivityRecords();
            System.Diagnostics.Debug.WriteLine("[MCP活动] 活动记录已清空");
        }

        /// 保存活动记录到文件
        public async Task SaveActivityRecordsAsync()
        {
            try
            {
                List<McpActivityRecord> recordsToSave;
                lock (activityRecords)
                {
                    recordsToSave = new List<McpActivityRecord>(activityRecords);
                }

                var json = JsonSerializer.Serialize(recordsToSave, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(logFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存MCP活动记录失败: {ex.Message}");
            }
        }

        /// 同步保存活动记录
        private void SaveActivityRecords()
        {
            Task.Run(async () => await SaveActivityRecordsAsync());
        }

        /// 加载活动记录
        private void LoadActivityRecords()
        {
            try
            {
                if (File.Exists(logFilePath))
                {
                    var json = File.ReadAllText(logFilePath);
                    var records = JsonSerializer.Deserialize<List<McpActivityRecord>>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (records != null)
                    {
                        lock (activityRecords)
                        {
                            activityRecords.AddRange(records);
                            TrimRecords();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载MCP活动记录失败: {ex.Message}");
            }
        }

        /// 修剪记录数量
        private void TrimRecords()
        {
            if (activityRecords.Count > maxRecords)
            {
                var removeCount = activityRecords.Count - maxRecords;
                activityRecords.RemoveRange(0, removeCount);
            }
        }
    }

    /// MCP活动统计信息
    public class McpActivityStatistics
    {
        public int TotalToolCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public double AverageExecutionTime { get; set; }
        public double MaxExecutionTime { get; set; }
        public double MinExecutionTime { get; set; }
        public Dictionary<string, int> ServerCallCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ToolCallCounts { get; set; } = new Dictionary<string, int>();
    }
}
