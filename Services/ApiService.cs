using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using ModelContextProtocol.Client;
using ModelContextProtocol;
using ModelContextProtocol.Protocol.Transport;
using OpenMeido.Helpers;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;
using ChatMessage = OpenMeido.Models.ChatMessage;

namespace OpenMeido.Services
{
    /// AI API服务类，负责与OpenAI格式的API进行通信，支持MCP工具集成
    public class ApiService : IApiService
    {
        // HTTP客户端实例，用于发送API请求
        private readonly HttpClient httpClient;

        // 应用程序设置，包含API配置信息
        private readonly AppSettings settings;

        // Microsoft.Extensions.AI 聊天客户端，用于MCP集成
        private IChatClient chatClient;

        // MCP服务实例
        private readonly IMcpService mcpService;

        // MCP活动日志记录器
        private readonly McpActivityLogger mcpActivityLogger;



        /// 构造函数，初始化API服务
        /// <param name="settings">应用程序设置对象</param>
        public ApiService(AppSettings settings)
            : this(settings, new McpServiceFactory())
        {
        }

        public ApiService(AppSettings settings, IMcpServiceFactory mcpServiceFactory)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            // 创建HTTP客户端实例
            httpClient = new HttpClient();

            // 设置请求超时时间为30秒
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // 设置Authorization头，使用Bearer令牌认证
            httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey);

            // 设置Content-Type头为JSON格式
            httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            // 初始化MCP活动日志记录器
            mcpActivityLogger = new McpActivityLogger();

            // 初始化MCP服务
            mcpService = (mcpServiceFactory ?? new McpServiceFactory()).Create(settings, mcpActivityLogger);



            // 初始化Microsoft.Extensions.AI聊天客户端
            InitializeChatClient();
        }

        /// 初始化Microsoft.Extensions.AI聊天客户端
        private void InitializeChatClient()
        {
            try
            {
                if (!settings.IsValid())
                {
                    return;
                }

                var apiKeyCredential = new ApiKeyCredential(settings.ApiKey);
                var openAIClientOptions = new OpenAIClientOptions();
                openAIClientOptions.Endpoint = new Uri(settings.ApiBaseUrl);

                var openaiClient = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                    .AsChatClient(settings.ModelName);

                // 创建支持函数调用的聊天客户端
                chatClient = new ChatClientBuilder(openaiClient)
                    .UseFunctionInvocation()
                    .Build();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化聊天客户端失败: {ex.Message}");
                chatClient = null;
            }
        }


        /// 初始化MCP服务
        /// <returns>初始化任务</returns>
        public async Task InitializeMcpAsync()
        {
            try
            {
                // 初始化MCP服务（统一管理所有MCP功能）
                await mcpService.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("MCP服务初始化完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化MCP服务失败: {ex.Message}");
            }
        }



        /// 发送聊天消息到AI API并获取回复（支持MCP工具）
        /// <param name="messagesHistory">完整历史消息列表</param>
        /// <returns>AI的回复消息</returns>
        public async Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
        {
            // 如果启用了MCP且聊天客户端可用，使用Microsoft.Extensions.AI方式
            if (settings.EnableMcp && chatClient != null && mcpService.IsAvailable())
            {
                return await SendMessageWithMcpAsync(messagesHistory);
            }

            // 否则使用传统的HTTP方式
            return await SendMessageHttpAsync(messagesHistory);
        }

        /// 使用Microsoft.Extensions.AI和MCP工具发送消息
        /// <param name="messagesHistory">完整历史消息列表</param>
        /// <returns>AI的回复消息</returns>
        private async Task<string> SendMessageWithMcpAsync(List<ChatMessage> messagesHistory)
        {
            try
            {
                if (messagesHistory == null || messagesHistory.Count == 0)
                {
                    throw new ArgumentException("消息历史不能为空", nameof(messagesHistory));
                }

                // 转换消息格式
                var messages = new List<Microsoft.Extensions.AI.ChatMessage>();

                // 添加系统提示词
                if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
                {
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, settings.SystemPrompt));
                }

                // 添加历史消息
                foreach (var msg in messagesHistory)
                {
                    var role = msg.Role == "user" ? ChatRole.User : ChatRole.Assistant;
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(role, msg.Content));
                }

                // 获取MCP工具（使用MCP服务）
                var allMcpTools = new List<AITool>();
                if (mcpService != null && mcpService.IsAvailable())
                {
                    try
                    {
                        var tools = await mcpService.GetAvailableToolsAsync();
                        allMcpTools.AddRange(tools);
                        System.Diagnostics.Debug.WriteLine($"从MCP服务获取到 {allMcpTools.Count} 个工具");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"从MCP服务获取工具失败: {ex.Message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MCP服务不可用或未初始化");
                }

                var options = new ChatOptions
                {
                    Temperature = (float)settings.Temperature,
                    MaxOutputTokens = settings.MaxTokens
                };

                if (allMcpTools.Count > 0)
                {
                    options.Tools = allMcpTools;
                    System.Diagnostics.Debug.WriteLine($"已添加 {options.Tools.Count} 个工具到聊天选项");
                }

                // 使用GetResponseAsync，UseFunctionInvocation中间件会自动处理工具调用
                // 关键是要让中间件处理完整的对话流程，包括工具执行和后续响应
                var response = await chatClient.GetResponseAsync(messages, options);

                // 记录MCP工具使用情况（如果有的话）
                await LogMcpToolUsageAsync(response);

                // 构建完整的回复内容，包括工具调用信息
                var fullResponse = BuildCompleteResponseWithToolInfo(response);
                
                return fullResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MCP消息发送失败: {ex.Message}");
                // 如果MCP发送失败，回退到传统HTTP方式
                System.Diagnostics.Debug.WriteLine("回退到传统HTTP API方式");
                return await SendMessageHttpAsync(messagesHistory);
            }
        }

        /// 构建完整的回复内容，包括工具调用信息
        /// <param name="response">聊天响应结果</param>
        /// <returns>完整的回复内容</returns>
        private string BuildCompleteResponseWithToolInfo(ChatResponse response)
        {
            var responseBuilder = new StringBuilder();
            bool hasToolCalls = false;
            
            // 遵循消息顺序，按照AI的思维流程构建回复
            foreach (var message in response.Messages)
            {
                // 检查是否有工具调用
                var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
                var toolResults = message.Contents.OfType<FunctionResultContent>().ToList();
                var textContent = message.Contents.OfType<TextContent>().FirstOrDefault();
                
                // 如果有工具调用，添加简略的工具调用信息
                if (toolCalls.Any())
                {
                    hasToolCalls = true;
                    
                    foreach (var toolCall in toolCalls)
                    {
                        // 查找对应的工具结果来确定执行状态
                        var correspondingResult = response.Messages
                            .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
                            .FirstOrDefault(r => r.CallId == toolCall.CallId);
                            
                        // 构建简略的工具调用信息，包含详细数据用于展开显示
                        var toolCallInfo = new StringBuilder();
                        toolCallInfo.AppendLine($"{ToolCallMessageMarkers.ToolCallStart}{toolCall.Name}");
                        
                        // 添加参数信息（用于详情展开）
                        if (toolCall.Arguments != null && toolCall.Arguments.Any())
                        {
                            var args = string.Join(", ", toolCall.Arguments.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                            toolCallInfo.AppendLine($"{ToolCallMessageMarkers.ToolParams}{args}");
                        }
                        
                        // 添加执行结果信息
                        if (correspondingResult != null)
                        {
                            var resultContent = correspondingResult.Result?.ToString() ?? "";
                            toolCallInfo.AppendLine($"{ToolCallMessageMarkers.ToolResultSuccess}{resultContent}");
                        }
                        else
                        {
                            toolCallInfo.AppendLine($"{ToolCallMessageMarkers.ToolResultFailed}未找到执行结果");
                        }
                        
                        toolCallInfo.AppendLine("TOOL_CALL_END");
                        responseBuilder.Append(toolCallInfo.ToString());
                    }
                }
            }
            
            // 添加AI的最终回复
            var finalResponse = response.Text ?? "";
            if (!string.IsNullOrWhiteSpace(finalResponse))
            {
                // 如果有工具调用，使用分句符分隔
                if (hasToolCalls && responseBuilder.Length > 0)
                {
                    responseBuilder.Append(@"\\\"); // 使用项目的分句符
                }
                
                responseBuilder.Append(finalResponse);
            }
            
            var result = responseBuilder.ToString();
            
            // 如果没有内容，返回默认回复
            return string.IsNullOrWhiteSpace(result) ? "抱歉，我没有生成有效的回复。" : result;
        }

        /// 记录MCP工具使用情况
        /// <param name="response">聊天响应结果</param>
        /// <returns>记录任务</returns>
        private async Task LogMcpToolUsageAsync(ChatResponse response)
        {
            try
            {
                // 检查响应中是否包含工具调用的痕迹
                // 注意：由于UseFunctionInvocation中间件已经自动处理了工具调用，
                // 这里我们主要是记录工具使用情况用于活动日志

                // 遍历所有消息，查找工具调用和结果
                System.Diagnostics.Debug.WriteLine($"[MCP] 检查响应中的 {response.Messages.Count} 条消息");

                // 首先收集所有工具调用和结果
                var allToolCalls = new List<FunctionCallContent>();
                var allToolResults = new List<FunctionResultContent>();

                foreach (var message in response.Messages)
                {
                    var toolCalls = message.Contents.OfType<FunctionCallContent>().ToList();
                    var toolResults = message.Contents.OfType<FunctionResultContent>().ToList();

                    System.Diagnostics.Debug.WriteLine($"[MCP] 消息包含 {toolCalls.Count} 个工具调用，{toolResults.Count} 个工具结果");

                    allToolCalls.AddRange(toolCalls);
                    allToolResults.AddRange(toolResults);
                }

                System.Diagnostics.Debug.WriteLine($"[MCP] 总计: {allToolCalls.Count} 个工具调用，{allToolResults.Count} 个工具结果");

                // 记录工具调用
                foreach (var toolCall in allToolCalls)
                {
                    var startTime = DateTime.Now;

                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[MCP] 处理工具调用: {toolCall.Name}, CallId: {toolCall.CallId}");

                        // 获取工具所属的MCP服务器信息
                        var serverInfo = await GetToolServerInfoAsync(toolCall.Name);
                        var serverName = ExtractServerName(serverInfo);

                        // 记录工具调用开始
                        var parameters = toolCall.Arguments?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object>();
                        var recordId = mcpActivityLogger.LogToolCallStart(serverName, toolCall.Name, parameters);

                        // 查找对应的工具结果（在所有消息中查找）
                        System.Diagnostics.Debug.WriteLine($"[MCP] 查找 CallId {toolCall.CallId} 对应的结果");
                        foreach (var result in allToolResults)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MCP] 结果 CallId: {result.CallId}");
                        }

                        var correspondingResult = allToolResults.FirstOrDefault(r => r.CallId == toolCall.CallId);
                        if (correspondingResult != null)
                        {
                            // 记录工具调用完成（成功）
                            var resultContent = correspondingResult.Result?.ToString() ?? "";
                            mcpActivityLogger.LogToolCallEnd(recordId, resultContent, true, 0); // 执行时间未知，设为0

                            System.Diagnostics.Debug.WriteLine($"[MCP] 工具调用完成: {toolCall.Name} | 服务器: {serverInfo} | 结果长度: {resultContent.Length}");
                        }
                        else
                        {
                            // 没有找到对应结果，可能是调用失败
                            mcpActivityLogger.LogToolCallEnd(recordId, "", false, 0, "未找到工具执行结果");
                            System.Diagnostics.Debug.WriteLine($"[MCP] 工具调用未找到结果: {toolCall.Name}, CallId: {toolCall.CallId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MCP] 记录工具调用失败: {toolCall.Name} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录MCP工具使用情况失败: {ex.Message}");
            }
        }

        /// 获取工具所属的MCP服务器信息
        /// <param name="toolName">工具名称</param>
        /// <returns>服务器信息字符串</returns>
        private async Task<string> GetToolServerInfoAsync(string toolName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[MCP] 查找工具 '{toolName}' 所属服务器");

                if (mcpService == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MCP] mcpService 为 null");
                    return "";
                }

                var serverStatuses = await mcpService.GetServerStatusAsync();
                System.Diagnostics.Debug.WriteLine($"[MCP] 找到 {serverStatuses.Count} 个服务器状态");

                foreach (var serverStatus in serverStatuses)
                {
                    System.Diagnostics.Debug.WriteLine($"[MCP] 检查服务器: {serverStatus.Name} (连接状态: {serverStatus.IsConnected})");

                    if (serverStatus.IsConnected)
                    {
                        var tools = await mcpService.GetServerToolsAsync(serverStatus.Id);
                        System.Diagnostics.Debug.WriteLine($"[MCP] 服务器 '{serverStatus.Name}' 有 {tools.Count} 个工具");

                        foreach (var tool in tools)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MCP] 工具: {tool.Name}");
                        }

                        if (tools.Any(t => t.Name == toolName))
                        {
                            var result = $"{serverStatus.Name} ({serverStatus.ToolCount}工具)";
                            System.Diagnostics.Debug.WriteLine($"[MCP] 找到工具 '{toolName}' 属于服务器: {result}");
                            return result;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[MCP] 未找到工具 '{toolName}' 所属的服务器");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取工具服务器信息失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常详情: {ex}");
            }

            return "";
        }

        /// 从服务器信息字符串中提取服务器名称
        /// <param name="serverInfo">服务器信息字符串</param>
        /// <returns>服务器名称</returns>
        private string ExtractServerName(string serverInfo)
        {
            System.Diagnostics.Debug.WriteLine($"[MCP] 提取服务器名称，输入: '{serverInfo}'");

            if (string.IsNullOrEmpty(serverInfo))
            {
                System.Diagnostics.Debug.WriteLine($"[MCP] 服务器信息为空，返回空字符串");
                return "";
            }

            var spaceIndex = serverInfo.IndexOf(' ');
            var result = spaceIndex > 0 ? serverInfo.Substring(0, spaceIndex) : serverInfo;

            System.Diagnostics.Debug.WriteLine($"[MCP] 提取的服务器名称: '{result}'");
            return result;
        }

        /// 获取MCP活动日志记录器
        /// <returns>活动日志记录器实例</returns>
        public McpActivityLogger GetActivityLogger()
        {
            return mcpActivityLogger;
        }

        /// 获取MCP服务器状态信息
        /// <returns>服务器状态信息列表</returns>
        public async Task<List<(string Id, string Name, bool IsConnected, int ToolCount)>> GetMcpServerStatusesAsync()
        {
            if (mcpService == null)
            {
                return new List<(string Id, string Name, bool IsConnected, int ToolCount)>();
            }

            return await mcpService.GetServerStatusAsync();
        }

        /// 获取所有可用的MCP工具
        /// <returns>工具列表</returns>
        public async Task<IList<McpClientTool>> GetAvailableMcpToolsAsync()
        {
            if (mcpService == null)
            {
                return new List<McpClientTool>();
            }

            return await mcpService.GetAvailableToolsAsync();
        }

        /// 获取最近的MCP活动记录
        /// <param name="count">记录数量</param>
        /// <returns>活动记录列表</returns>
        public List<McpActivityRecord> GetRecentMcpActivities(int count = 20)
        {
            return mcpActivityLogger?.GetRecentActivities(count) ?? new List<McpActivityRecord>();
        }

        /// 获取MCP活动统计信息
        /// <returns>统计信息</returns>
        public McpActivityStatistics GetMcpActivityStatistics()
        {
            return mcpActivityLogger?.GetStatistics() ?? new McpActivityStatistics();
        }

        /// 清空MCP活动记录
        public void ClearMcpActivities()
        {
            mcpActivityLogger?.ClearActivities();
        }

        /// 使用传统HTTP方式发送聊天消息到AI API并获取回复
        /// <param name="messagesHistory">完整历史消息列表</param>
        /// <returns>AI的回复消息</returns>
        private async Task<string> SendMessageHttpAsync(List<ChatMessage> messagesHistory)
        {
            try
            {
                if (messagesHistory == null || messagesHistory.Count == 0)
                {
                    throw new ArgumentException("消息历史不能为空", nameof(messagesHistory));
                }

                if (!settings.IsValid())
                {
                    return "API配置无效，请检查API密钥和基础URL设置。";
                }

                string apiUrl = $"{settings.ApiBaseUrl.TrimEnd('/')}/chat/completions";

                var messages = new List<Dictionary<string, object>>();

                // 系统提示词
                if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
                {
                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = "system",
                        ["content"] = settings.SystemPrompt
                    });
                }

                // 历史消息
                foreach (var msg in messagesHistory)
                {
                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = msg.Role,
                        ["content"] = msg.Content
                    });
                }

                var requestBody = new Dictionary<string, object>
                {
                    ["model"] = settings.ModelName,
                    ["messages"] = messages.ToArray(),
                    ["max_tokens"] = settings.MaxTokens,
                    ["temperature"] = settings.Temperature
                };

                // 将请求体序列化为JSON字符串
                string jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // 调试信息：记录请求内容
                System.Diagnostics.Debug.WriteLine($"API请求URL: {apiUrl}");
                System.Diagnostics.Debug.WriteLine($"API密钥前缀: {settings.ApiKey?.Substring(0, Math.Min(10, settings.ApiKey?.Length ?? 0))}...");
                System.Diagnostics.Debug.WriteLine($"请求内容: {jsonContent}");

                // 创建HTTP请求内容
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 发送POST请求到API
                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

                // 读取响应内容
                string responseContent = await response.Content.ReadAsStringAsync();

                // 调试信息：记录响应
                System.Diagnostics.Debug.WriteLine($"响应状态码: {response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"响应内容: {responseContent}");

                // 检查响应状态码
                if (!response.IsSuccessStatusCode)
                {
                    // 如果请求失败，返回错误信息
                    string errorDetail = $"HTTP {(int)response.StatusCode} {response.StatusCode}";

                    // 根据状态码提供具体错误
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.Unauthorized:
                            errorDetail += "\n❌ 认证失败：API密钥无效或已过期";
                            break;
                        case System.Net.HttpStatusCode.Forbidden:
                            errorDetail += "\n❌ 访问被拒绝：API密钥权限不足";
                            break;
                        case System.Net.HttpStatusCode.NotFound:
                            errorDetail += "\n❌ 接口不存在：请检查API基础URL是否正确";
                            break;
                        case (System.Net.HttpStatusCode)429: // TooManyRequests 在 .NET Framework 4.7.2 中不存在
                            errorDetail += "\n❌ 请求过于频繁：请稍后重试";
                            break;
                        case System.Net.HttpStatusCode.InternalServerError:
                            errorDetail += "\n❌ 服务器内部错误：API服务暂时不可用";
                            break;
                        default:
                            errorDetail += $"\n❌ 请求失败：{response.ReasonPhrase}";
                            break;
                    }

                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        errorDetail += $"\n\n服务器响应详情:\n{responseContent}";
                    }

                    return $"API请求失败: {errorDetail}";
                }

                // 解析响应JSON
                JsonDocument responseDocument = null;
                Dictionary<string, object> responseObject = null;

                try
                {
                    responseDocument = JsonDocument.Parse(responseContent);
                    responseObject = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                }
                catch (Exception parseEx)
                {
                    return $"JSON解析失败: {parseEx.Message}\n原始响应: {responseContent}";
                }

                if (responseObject == null)
                {
                    return $"响应解析为空\n原始响应: {responseContent}";
                }

                // 提取AI回复的消息内容
                string aiReply = "";
                try
                {
                    using (responseDocument)
                    {
                        var root = responseDocument.RootElement;

                        if (!root.TryGetProperty("choices", out var choicesElement))
                        {
                            return $"响应中缺少choices字段\n响应内容: {responseContent}";
                        }

                        if (choicesElement.ValueKind != JsonValueKind.Array || choicesElement.GetArrayLength() == 0)
                        {
                            return $"choices字段为空或不是数组\n响应内容: {responseContent}";
                        }

                        var firstChoice = choicesElement[0];
                        if (!firstChoice.TryGetProperty("message", out var messageElement))
                        {
                            return $"choice中缺少message字段\n响应内容: {responseContent}";
                        }

                        if (!messageElement.TryGetProperty("content", out var contentElement))
                        {
                            return $"message中缺少content字段\n响应内容: {responseContent}";
                        }

                        aiReply = contentElement.GetString();
                    }
                }
                catch (Exception ex)
                {
                    return $"解析响应时出错: {ex.Message}\n响应内容: {responseContent}";
                }

                if (string.IsNullOrWhiteSpace(aiReply))
                {
                    return "没有收到有效的回复。";
                }

                return aiReply.Trim();
            }
            catch (HttpRequestException ex)
            {
                // 处理HTTP请求异常
                string errorMsg = "网络请求错误";
                if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
                {
                    errorMsg += "：SSL证书验证失败，请检查网络连接或API服务器证书";
                }
                else if (ex.Message.Contains("timeout") || ex.Message.Contains("超时"))
                {
                    errorMsg += "：连接超时，请检查网络连接和API服务器状态";
                }
                else if (ex.Message.Contains("refused") || ex.Message.Contains("拒绝"))
                {
                    errorMsg += "：连接被拒绝，请检查API基础URL是否正确";
                }
                else
                {
                    errorMsg += $"：{ex.Message}";
                }

                System.Diagnostics.Debug.WriteLine($"HTTP请求异常: {ex}");
                return errorMsg;
            }
            catch (TaskCanceledException ex)
            {
                // 处理请求超时异常
                string errorMsg = "请求超时";
                if (ex.InnerException is TimeoutException)
                {
                    errorMsg += "：API服务器响应超时，请稍后重试";
                }
                else
                {
                    errorMsg += "：请求被取消，请检查网络连接或稍后重试";
                }

                System.Diagnostics.Debug.WriteLine($"请求超时异常: {ex}");
                return errorMsg;
            }
            catch (ArgumentException ex)
            {
                // 处理参数异常
                System.Diagnostics.Debug.WriteLine($"参数异常: {ex}");
                return $"参数错误: {ex.Message}";
            }
            catch (Exception ex)
            {
                // 处理其他未预期的异常
                System.Diagnostics.Debug.WriteLine($"未知异常: {ex}");
                return $"发生未知错误: {ex.Message}\n\n如果问题持续存在，请检查：\n1. 网络连接是否正常\n2. API密钥是否有效\n3. API基础URL是否正确\n4. 防火墙是否阻止了连接";
            }
        }

        /// 测试API连接是否正常
        /// 如果连接成功返回true，否则返回false
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // 发送一个简单的测试消息
            // 构造一条简单的历史消息用于测试
            var testHistory = new List<ChatMessage> { new ChatMessage("user", "Hello") };
            string testResponse = await SendMessageAsync(testHistory);
                
                // 如果收到回复且不是错误消息，则认为连接成功
                return !string.IsNullOrWhiteSpace(testResponse) && 
                       !testResponse.StartsWith("网络请求错误") &&
                       !testResponse.StartsWith("请求超时") &&
                       !testResponse.StartsWith("响应解析错误") &&
                       !testResponse.StartsWith("发生未知错误");
            }
            catch
            {
                // 如果测试过程中出现任何异常，返回false
                return false;
            }
        }

        /// 释放资源
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// 释放托管和非托管资源
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient?.Dispose();
                mcpService?.Dispose();
            }
        }
    }
}
