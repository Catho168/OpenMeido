using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace OpenMeido
{
    /// <summary>
    /// AI API服务类，负责与OpenAI格式的API进行通信
    /// 支持发送聊天消息并接收AI回复
    /// </summary>
    public class ApiService : IDisposable
    {
        // HTTP客户端实例，用于发送API请求
        private readonly HttpClient httpClient;
        
        // 应用程序设置，包含API配置信息
        private readonly AppSettings settings;

        /// <summary>
        /// 构造函数，初始化API服务
        /// </summary>
        /// <param name="settings">应用程序设置对象</param>
        public ApiService(AppSettings settings)
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
        }


        /// <summary>
        /// 发送聊天消息到AI API并获取回复（带上下文）
        /// </summary>
        /// <param name="messagesHistory">完整历史消息列表</param>
        /// <returns>AI的回复消息</returns>
        public async Task<string> SendMessageAsync(List<ChatMessage> messagesHistory)
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

                // 如果有系统提示词，先添加系统消息
                if (!string.IsNullOrWhiteSpace(settings.SystemPrompt))
                {
                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = "system",
                        ["content"] = settings.SystemPrompt
                    });
                }

                // 添加历史消息
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
                var serializer = new JavaScriptSerializer();
                string jsonContent = serializer.Serialize(requestBody);

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
                    // 如果请求失败，返回详细错误信息
                    string errorDetail = $"HTTP {(int)response.StatusCode} {response.StatusCode}";

                    // 根据状态码提供更具体的错误信息
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
                        case (System.Net.HttpStatusCode)429: // TooManyRequests 在 .NET Framework 4.7.2 中不存在，使用数字代码
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
                var responseSerializer = new JavaScriptSerializer();
                Dictionary<string, object> responseObject = null;

                try
                {
                    responseObject = responseSerializer.DeserializeObject(responseContent) as Dictionary<string, object>;
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
                    if (!responseObject.ContainsKey("choices"))
                    {
                        return $"响应中缺少choices字段\n响应内容: {responseContent}";
                    }

                    var choices = responseObject["choices"] as object[];
                    if (choices == null || choices.Length == 0)
                    {
                        return $"choices字段为空或不是数组\n响应内容: {responseContent}";
                    }

                    var firstChoice = choices[0] as Dictionary<string, object>;
                    if (firstChoice == null)
                    {
                        return $"第一个choice不是字典对象\n响应内容: {responseContent}";
                    }

                    if (!firstChoice.ContainsKey("message"))
                    {
                        return $"choice中缺少message字段\n响应内容: {responseContent}";
                    }

                    var messageObj = firstChoice["message"] as Dictionary<string, object>;
                    if (messageObj == null)
                    {
                        return $"message字段不是字典对象\n响应内容: {responseContent}";
                    }

                    if (!messageObj.ContainsKey("content"))
                    {
                        return $"message中缺少content字段\n响应内容: {responseContent}";
                    }

                    aiReply = messageObj["content"]?.ToString();
                }
                catch (Exception ex)
                {
                    return $"解析响应时出错: {ex.Message}\n响应内容: {responseContent}";
                }

                // 如果没有获取到有效回复，返回错误消息
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

        /// <summary>
        /// 测试API连接是否正常
        /// </summary>
        /// <returns>如果连接成功返回true，否则返回false</returns>
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

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放托管和非托管资源
        /// </summary>
        /// <param name="disposing">是否释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient?.Dispose();
            }
        }
    }
}
