using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.AI;

namespace OpenMeido
{
    /// <summary>
    /// 用户意图类型枚举
    /// </summary>
    public enum IntentType
    {
        /// <summary>
        /// 通信交流
        /// </summary>
        Communication,

        /// <summary>
        /// 文件操作
        /// </summary>
        FileOperation,

        /// <summary>
        /// 信息查询
        /// </summary>
        InformationQuery,

        /// <summary>
        /// 系统操作
        /// </summary>
        SystemOperation,

        /// <summary>
        /// 数据处理
        /// </summary>
        DataProcessing,

        /// <summary>
        /// 其他
        /// </summary>
        Other
    }

    /// <summary>
    /// 用户意图模型
    /// </summary>
    public class UserIntent
    {
        /// <summary>
        /// 意图类型
        /// </summary>
        public IntentType Type { get; set; }

        /// <summary>
        /// 置信度 (0.0 - 1.0)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 相关关键词
        /// </summary>
        public List<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// 意图描述
        /// </summary>
        public string Description { get; set; } = "";
    }

    /// <summary>
    /// 推荐工具模型
    /// </summary>
    public class RecommendedTool
    {
        /// <summary>
        /// 工具实例
        /// </summary>
        public AITool Tool { get; set; }

        /// <summary>
        /// 相关度评分 (0.0 - 1.0)
        /// </summary>
        public double RelevanceScore { get; set; }

        /// <summary>
        /// 推荐理由
        /// </summary>
        public string ReasonForRecommendation { get; set; } = "";

        /// <summary>
        /// 使用优先级
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 预期参数提示
        /// </summary>
        public List<string> ParameterHints { get; set; } = new List<string>();
    }

    /// <summary>
    /// 工具使用建议模型
    /// </summary>
    public class ToolUsageSuggestion
    {
        /// <summary>
        /// 用户输入
        /// </summary>
        public string UserInput { get; set; } = "";

        /// <summary>
        /// 分析时间
        /// </summary>
        public DateTime AnalyzedAt { get; set; }

        /// <summary>
        /// 检测到的用户意图
        /// </summary>
        public List<UserIntent> DetectedIntents { get; set; } = new List<UserIntent>();

        /// <summary>
        /// 推荐的工具列表
        /// </summary>
        public List<RecommendedTool> RecommendedTools { get; set; } = new List<RecommendedTool>();

        /// <summary>
        /// 上下文提示
        /// </summary>
        public List<string> ContextHints { get; set; } = new List<string>();

        /// <summary>
        /// 总体置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 是否有高置信度的工具推荐
        /// </summary>
        public bool HasHighConfidenceRecommendation => 
            RecommendedTools.Any(t => t.RelevanceScore > 0.7) && Confidence > 0.6;

        /// <summary>
        /// 获取最佳推荐工具
        /// </summary>
        public RecommendedTool GetBestRecommendation()
        {
            return RecommendedTools.OrderByDescending(t => t.RelevanceScore).FirstOrDefault();
        }

        /// <summary>
        /// 获取格式化的建议文本
        /// </summary>
        public string GetFormattedSuggestion()
        {
            if (!RecommendedTools.Any())
            {
                return "未找到相关工具建议";
            }

            var suggestion = new System.Text.StringBuilder();

            if (HasHighConfidenceRecommendation)
            {
                var bestTool = GetBestRecommendation();
                var toolName = GetToolName(bestTool.Tool);
                suggestion.AppendLine($"🎯 强烈建议使用 **{toolName}** 工具");
                suggestion.AppendLine($"   理由: {bestTool.ReasonForRecommendation}");

                if (bestTool.ParameterHints.Any())
                {
                    suggestion.AppendLine($"   参数提示: {string.Join("、", bestTool.ParameterHints)}");
                }
            }
            else
            {
                suggestion.AppendLine("💡 可能相关的工具:");
                foreach (var tool in RecommendedTools.Take(3))
                {
                    var toolName = GetToolName(tool.Tool);
                    suggestion.AppendLine($"   • {toolName} (相关度: {tool.RelevanceScore:P0})");
                }
            }

            if (ContextHints.Any())
            {
                suggestion.AppendLine();
                suggestion.AppendLine("📋 上下文提示:");
                foreach (var hint in ContextHints.Take(3))
                {
                    suggestion.AppendLine($"   • {hint}");
                }
            }

            return suggestion.ToString();
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
    }

    /// <summary>
    /// 工具使用模式定义
    /// </summary>
    public class ToolUsagePattern
    {
        /// <summary>
        /// 关键词列表
        /// </summary>
        public string[] Keywords { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 相关意图类型
        /// </summary>
        public IntentType[] IntentTypes { get; set; } = Array.Empty<IntentType>();

        /// <summary>
        /// 工具名称模式
        /// </summary>
        public string[] ToolNamePatterns { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 模式权重
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// 模式描述
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// 示例用户输入
        /// </summary>
        public string[] ExampleInputs { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// 工具使用上下文
    /// </summary>
    public class ToolUsageContext
    {
        /// <summary>
        /// 当前会话ID
        /// </summary>
        public string SessionId { get; set; } = "";

        /// <summary>
        /// 用户历史输入
        /// </summary>
        public List<string> UserInputHistory { get; set; } = new List<string>();

        /// <summary>
        /// 工具使用历史
        /// </summary>
        public List<ToolUsageRecord> ToolUsageHistory { get; set; } = new List<ToolUsageRecord>();

        /// <summary>
        /// 当前可用工具
        /// </summary>
        public List<AITool> AvailableTools { get; set; } = new List<AITool>();

        /// <summary>
        /// 用户偏好设置
        /// </summary>
        public UserPreferences UserPreferences { get; set; } = new UserPreferences();
    }

    /// <summary>
    /// 工具使用记录
    /// </summary>
    public class ToolUsageRecord
    {
        /// <summary>
        /// 使用时间
        /// </summary>
        public DateTime UsedAt { get; set; }

        /// <summary>
        /// 工具名称
        /// </summary>
        public string ToolName { get; set; } = "";

        /// <summary>
        /// 用户输入
        /// </summary>
        public string UserInput { get; set; } = "";

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// 用户反馈评分 (1-5)
        /// </summary>
        public int? UserRating { get; set; }
    }

    /// <summary>
    /// 用户偏好设置
    /// </summary>
    public class UserPreferences
    {
        /// <summary>
        /// 偏好的工具类型
        /// </summary>
        public List<IntentType> PreferredIntentTypes { get; set; } = new List<IntentType>();

        /// <summary>
        /// 常用工具列表
        /// </summary>
        public List<string> FrequentlyUsedTools { get; set; } = new List<string>();

        /// <summary>
        /// 是否启用主动建议
        /// </summary>
        public bool EnableProactiveSuggestions { get; set; } = true;

        /// <summary>
        /// 建议置信度阈值
        /// </summary>
        public double SuggestionConfidenceThreshold { get; set; } = 0.6;

        /// <summary>
        /// 最大建议工具数量
        /// </summary>
        public int MaxSuggestedTools { get; set; } = 3;
    }
}
