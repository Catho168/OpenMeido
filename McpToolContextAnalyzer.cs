using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace OpenMeido
{
    /// <summary>
    /// MCP工具上下文分析器，用于分析用户输入并提供工具使用建议
    /// </summary>
    public class McpToolContextAnalyzer
    {
        /// <summary>
        /// 工具使用模式定义
        /// </summary>
        private readonly Dictionary<string, ToolUsagePattern> toolPatterns;

        /// <summary>
        /// 构造函数
        /// </summary>
        public McpToolContextAnalyzer()
        {
            toolPatterns = InitializeToolPatterns();
        }

        /// <summary>
        /// 分析用户输入并生成工具使用建议
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="availableTools">可用工具列表</param>
        /// <returns>工具使用建议</returns>
        public Task<ToolUsageSuggestion> AnalyzeInputAsync(string userInput, IList<AITool> availableTools)
        {
            if (string.IsNullOrWhiteSpace(userInput) || availableTools == null || !availableTools.Any())
            {
                return Task.FromResult(new ToolUsageSuggestion());
            }

            var suggestion = new ToolUsageSuggestion
            {
                UserInput = userInput,
                AnalyzedAt = DateTime.Now
            };

            // 分析用户意图
            var intents = AnalyzeUserIntents(userInput);
            suggestion.DetectedIntents = intents;

            // 匹配相关工具
            var relevantTools = FindRelevantTools(userInput, availableTools, intents);
            suggestion.RecommendedTools = relevantTools;

            // 生成上下文提示
            var contextHints = GenerateContextHints(userInput, relevantTools, intents);
            suggestion.ContextHints = contextHints;

            // 计算置信度
            suggestion.Confidence = CalculateConfidence(intents, relevantTools);

            return Task.FromResult(suggestion);
        }

        /// <summary>
        /// 分析用户意图
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <returns>检测到的意图列表</returns>
        private List<UserIntent> AnalyzeUserIntents(string userInput)
        {
            var intents = new List<UserIntent>();
            var input = userInput.ToLower();

            // 通信意图
            if (ContainsAny(input, new[] { "发送", "send", "消息", "message", "微信", "wechat", "联系", "告诉" }))
            {
                intents.Add(new UserIntent
                {
                    Type = IntentType.Communication,
                    Confidence = CalculatePatternConfidence(input, new[] { "发送", "消息", "微信", "联系" }),
                    Keywords = ExtractKeywords(input, new[] { "发送", "消息", "微信", "联系", "告诉" })
                });
            }

            // 文件操作意图
            if (ContainsAny(input, new[] { "文件", "file", "读取", "read", "写入", "write", "目录", "folder", "保存", "save" }))
            {
                intents.Add(new UserIntent
                {
                    Type = IntentType.FileOperation,
                    Confidence = CalculatePatternConfidence(input, new[] { "文件", "读取", "写入", "目录" }),
                    Keywords = ExtractKeywords(input, new[] { "文件", "读取", "写入", "目录", "保存" })
                });
            }

            // 信息查询意图
            if (ContainsAny(input, new[] { "查看", "查询", "搜索", "search", "find", "获取", "get", "显示", "show" }))
            {
                intents.Add(new UserIntent
                {
                    Type = IntentType.InformationQuery,
                    Confidence = CalculatePatternConfidence(input, new[] { "查看", "查询", "搜索", "获取" }),
                    Keywords = ExtractKeywords(input, new[] { "查看", "查询", "搜索", "获取", "显示" })
                });
            }

            // 系统操作意图
            if (ContainsAny(input, new[] { "执行", "运行", "run", "execute", "命令", "command", "启动", "start" }))
            {
                intents.Add(new UserIntent
                {
                    Type = IntentType.SystemOperation,
                    Confidence = CalculatePatternConfidence(input, new[] { "执行", "运行", "命令", "启动" }),
                    Keywords = ExtractKeywords(input, new[] { "执行", "运行", "命令", "启动" })
                });
            }

            // 数据处理意图
            if (ContainsAny(input, new[] { "处理", "分析", "计算", "转换", "convert", "analyze", "process" }))
            {
                intents.Add(new UserIntent
                {
                    Type = IntentType.DataProcessing,
                    Confidence = CalculatePatternConfidence(input, new[] { "处理", "分析", "计算", "转换" }),
                    Keywords = ExtractKeywords(input, new[] { "处理", "分析", "计算", "转换" })
                });
            }

            return intents;
        }

        /// <summary>
        /// 查找相关工具
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="availableTools">可用工具</param>
        /// <param name="intents">用户意图</param>
        /// <returns>相关工具列表</returns>
        private List<RecommendedTool> FindRelevantTools(string userInput, IList<AITool> availableTools, List<UserIntent> intents)
        {
            var recommendedTools = new List<RecommendedTool>();

            foreach (var tool in availableTools)
            {
                var relevanceScore = CalculateToolRelevance(userInput, tool, intents);
                
                if (relevanceScore > 0.3) // 只推荐相关度较高的工具
                {
                    recommendedTools.Add(new RecommendedTool
                    {
                        Tool = tool,
                        RelevanceScore = relevanceScore,
                        ReasonForRecommendation = GenerateRecommendationReason(tool, intents, relevanceScore)
                    });
                }
            }

            // 按相关度排序
            return recommendedTools.OrderByDescending(t => t.RelevanceScore).ToList();
        }

        /// <summary>
        /// 生成上下文提示
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="recommendedTools">推荐工具</param>
        /// <param name="intents">用户意图</param>
        /// <returns>上下文提示</returns>
        private List<string> GenerateContextHints(string userInput, List<RecommendedTool> recommendedTools, List<UserIntent> intents)
        {
            var hints = new List<string>();

            if (recommendedTools.Any())
            {
                var topTool = recommendedTools.First();
                hints.Add($"建议使用 {GetToolName(topTool.Tool)} 工具来完成此任务");

                if (recommendedTools.Count > 1)
                {
                    hints.Add($"还可以考虑使用 {string.Join("、", recommendedTools.Skip(1).Take(2).Select(t => GetToolName(t.Tool)))} 等工具");
                }
            }

            // 根据意图添加特定提示
            foreach (var intent in intents.Where(i => i.Confidence > 0.7))
            {
                switch (intent.Type)
                {
                    case IntentType.Communication:
                        hints.Add("检测到通信需求，可以使用消息发送工具");
                        break;
                    case IntentType.FileOperation:
                        hints.Add("检测到文件操作需求，可以使用文件系统工具");
                        break;
                    case IntentType.InformationQuery:
                        hints.Add("检测到信息查询需求，可以使用搜索或读取工具");
                        break;
                    case IntentType.SystemOperation:
                        hints.Add("检测到系统操作需求，可以使用命令执行工具");
                        break;
                    case IntentType.DataProcessing:
                        hints.Add("检测到数据处理需求，可以使用分析或转换工具");
                        break;
                }
            }

            return hints;
        }

        /// <summary>
        /// 初始化工具使用模式
        /// </summary>
        /// <returns>工具模式字典</returns>
        private Dictionary<string, ToolUsagePattern> InitializeToolPatterns()
        {
            return new Dictionary<string, ToolUsagePattern>
            {
                ["communication"] = new ToolUsagePattern
                {
                    Keywords = new[] { "发送", "消息", "微信", "联系", "告诉", "通知" },
                    IntentTypes = new[] { IntentType.Communication },
                    ToolNamePatterns = new[] { "wechat", "message", "send", "notify" }
                },
                ["file_operation"] = new ToolUsagePattern
                {
                    Keywords = new[] { "文件", "读取", "写入", "保存", "目录", "文档" },
                    IntentTypes = new[] { IntentType.FileOperation },
                    ToolNamePatterns = new[] { "file", "read", "write", "save", "directory", "folder" }
                },
                ["information_query"] = new ToolUsagePattern
                {
                    Keywords = new[] { "查看", "查询", "搜索", "获取", "显示", "列表" },
                    IntentTypes = new[] { IntentType.InformationQuery },
                    ToolNamePatterns = new[] { "search", "find", "get", "list", "show", "view" }
                }
            };
        }

        /// <summary>
        /// 检查输入是否包含任何关键词
        /// </summary>
        private bool ContainsAny(string input, string[] keywords)
        {
            return keywords.Any(keyword => input.Contains(keyword));
        }

        /// <summary>
        /// 计算模式置信度
        /// </summary>
        private double CalculatePatternConfidence(string input, string[] keywords)
        {
            var matchCount = keywords.Count(keyword => input.Contains(keyword));
            return (double)matchCount / keywords.Length;
        }

        /// <summary>
        /// 提取关键词
        /// </summary>
        private List<string> ExtractKeywords(string input, string[] possibleKeywords)
        {
            return possibleKeywords.Where(keyword => input.Contains(keyword)).ToList();
        }

        /// <summary>
        /// 计算工具相关度
        /// </summary>
        private double CalculateToolRelevance(string userInput, AITool tool, List<UserIntent> intents)
        {
            var score = 0.0;
            var toolName = GetToolName(tool).ToLower();
            var toolDesc = GetToolDescription(tool).ToLower();
            var input = userInput.ToLower();

            // 基于工具名称匹配
            foreach (var intent in intents)
            {
                foreach (var keyword in intent.Keywords)
                {
                    if (toolName.Contains(keyword) || toolDesc.Contains(keyword))
                    {
                        score += intent.Confidence * 0.5;
                    }
                }
            }

            // 基于直接关键词匹配
            if (toolPatterns.Values.Any(pattern => 
                pattern.ToolNamePatterns.Any(p => toolName.Contains(p))))
            {
                score += 0.3;
            }

            return Math.Min(score, 1.0);
        }

        /// <summary>
        /// 生成推荐理由
        /// </summary>
        private string GenerateRecommendationReason(AITool tool, List<UserIntent> intents, double relevanceScore)
        {
            var reasons = new List<string>();
            
            if (relevanceScore > 0.8)
            {
                reasons.Add("高度匹配用户需求");
            }
            else if (relevanceScore > 0.6)
            {
                reasons.Add("较好匹配用户需求");
            }
            else
            {
                reasons.Add("可能相关");
            }

            var primaryIntent = intents.OrderByDescending(i => i.Confidence).FirstOrDefault();
            if (primaryIntent != null)
            {
                reasons.Add($"适用于{GetIntentDescription(primaryIntent.Type)}");
            }

            return string.Join("，", reasons);
        }

        /// <summary>
        /// 获取意图描述
        /// </summary>
        private string GetIntentDescription(IntentType intentType)
        {
            return intentType switch
            {
                IntentType.Communication => "通信交流",
                IntentType.FileOperation => "文件操作",
                IntentType.InformationQuery => "信息查询",
                IntentType.SystemOperation => "系统操作",
                IntentType.DataProcessing => "数据处理",
                _ => "一般任务"
            };
        }

        /// <summary>
        /// 计算总体置信度
        /// </summary>
        private double CalculateConfidence(List<UserIntent> intents, List<RecommendedTool> recommendedTools)
        {
            if (!intents.Any() || !recommendedTools.Any())
            {
                return 0.0;
            }

            var intentConfidence = intents.Average(i => i.Confidence);
            var toolConfidence = recommendedTools.Take(3).Average(t => t.RelevanceScore);

            return (intentConfidence + toolConfidence) / 2.0;
        }

        /// <summary>
        /// 获取工具名称（兼容不同的AITool实现）
        /// </summary>
        private string GetToolName(AITool tool)
        {
            try
            {
                var nameProperty = tool.GetType().GetProperty("Name");
                if (nameProperty != null)
                {
                    return nameProperty.GetValue(tool)?.ToString() ?? "";
                }

                var functionProperty = tool.GetType().GetProperty("Function");
                if (functionProperty != null)
                {
                    var function = functionProperty.GetValue(tool);
                    if (function != null)
                    {
                        var functionNameProperty = function.GetType().GetProperty("Name");
                        if (functionNameProperty != null)
                        {
                            return functionNameProperty.GetValue(function)?.ToString() ?? "";
                        }
                    }
                }

                return tool.ToString() ?? "";
            }
            catch
            {
                return "Unknown Tool";
            }
        }

        /// <summary>
        /// 获取工具描述（兼容不同的AITool实现）
        /// </summary>
        private string GetToolDescription(AITool tool)
        {
            try
            {
                var descProperty = tool.GetType().GetProperty("Description");
                if (descProperty != null)
                {
                    return descProperty.GetValue(tool)?.ToString() ?? "";
                }

                var functionProperty = tool.GetType().GetProperty("Function");
                if (functionProperty != null)
                {
                    var function = functionProperty.GetValue(tool);
                    if (function != null)
                    {
                        var functionDescProperty = function.GetType().GetProperty("Description");
                        if (functionDescProperty != null)
                        {
                            return functionDescProperty.GetValue(function)?.ToString() ?? "";
                        }
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}
