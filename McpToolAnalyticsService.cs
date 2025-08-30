using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenMeido
{
    /// <summary>
    /// MCP工具使用分析服务，用于跟踪工具使用模式并优化工具推荐
    /// </summary>
    public class McpToolAnalyticsService
    {
        private readonly string analyticsDataPath;
        private readonly object lockObject = new object();
        private ToolAnalyticsData analyticsData;

        /// <summary>
        /// 构造函数
        /// </summary>
        public McpToolAnalyticsService()
        {
            // 设置分析数据存储路径
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OpenMeido"
            );
            Directory.CreateDirectory(appDataPath);
            analyticsDataPath = Path.Combine(appDataPath, "mcp_analytics.json");
            
            // 加载现有数据
            LoadAnalyticsData();
        }

        /// <summary>
        /// 记录工具使用事件
        /// </summary>
        /// <param name="toolName">工具名称</param>
        /// <param name="userInput">用户输入</param>
        /// <param name="wasSuccessful">是否成功</param>
        /// <param name="executionTimeMs">执行时间（毫秒）</param>
        /// <param name="userRating">用户评分（1-5，可选）</param>
        public async Task RecordToolUsageAsync(string toolName, string userInput, bool wasSuccessful, 
            double executionTimeMs = 0, int? userRating = null)
        {
            try
            {
                lock (lockObject)
                {
                    var usageEvent = new ToolUsageEvent
                    {
                        ToolName = toolName,
                        UserInput = userInput,
                        Timestamp = DateTime.Now,
                        WasSuccessful = wasSuccessful,
                        ExecutionTimeMs = executionTimeMs,
                        UserRating = userRating,
                        InputLength = userInput?.Length ?? 0,
                        InputKeywords = ExtractKeywords(userInput)
                    };

                    analyticsData.UsageEvents.Add(usageEvent);
                    
                    // 更新工具统计
                    UpdateToolStatistics(toolName, usageEvent);
                    
                    // 更新用户模式
                    UpdateUserPatterns(userInput, toolName, wasSuccessful);
                    
                    // 限制历史记录数量
                    TrimHistoryData();
                }

                // 异步保存数据
                await SaveAnalyticsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录工具使用分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录工具建议事件
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <param name="suggestedTools">建议的工具</param>
        /// <param name="selectedTool">用户选择的工具</param>
        /// <param name="suggestionAccuracy">建议准确度</param>
        public async Task RecordSuggestionEventAsync(string userInput, List<string> suggestedTools, 
            string selectedTool = null, double suggestionAccuracy = 0)
        {
            try
            {
                lock (lockObject)
                {
                    var suggestionEvent = new ToolSuggestionEvent
                    {
                        UserInput = userInput,
                        SuggestedTools = suggestedTools,
                        SelectedTool = selectedTool,
                        Timestamp = DateTime.Now,
                        SuggestionAccuracy = suggestionAccuracy,
                        WasAccepted = !string.IsNullOrEmpty(selectedTool) && suggestedTools.Contains(selectedTool)
                    };

                    analyticsData.SuggestionEvents.Add(suggestionEvent);
                    
                    // 更新建议统计
                    UpdateSuggestionStatistics(suggestionEvent);
                }

                await SaveAnalyticsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"记录工具建议分析失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取工具使用统计
        /// </summary>
        /// <returns>工具统计信息</returns>
        public ToolUsageStatistics GetToolUsageStatistics()
        {
            lock (lockObject)
            {
                return new ToolUsageStatistics
                {
                    TotalUsageEvents = analyticsData.UsageEvents.Count,
                    TotalSuggestionEvents = analyticsData.SuggestionEvents.Count,
                    MostUsedTools = GetMostUsedTools(10),
                    ToolSuccessRates = GetToolSuccessRates(),
                    AverageExecutionTimes = GetAverageExecutionTimes(),
                    SuggestionAccuracy = GetOverallSuggestionAccuracy(),
                    UserPatterns = analyticsData.UserPatterns.Values.ToList(),
                    RecentActivity = GetRecentActivity(50)
                };
            }
        }

        /// <summary>
        /// 获取工具推荐优化建议
        /// </summary>
        /// <param name="userInput">用户输入</param>
        /// <returns>优化建议</returns>
        public ToolRecommendationOptimization GetRecommendationOptimization(string userInput)
        {
            lock (lockObject)
            {
                var optimization = new ToolRecommendationOptimization
                {
                    UserInput = userInput,
                    GeneratedAt = DateTime.Now
                };

                // 基于历史模式分析
                var relevantPatterns = FindRelevantPatterns(userInput);
                optimization.RelevantPatterns = relevantPatterns;

                // 基于成功率调整工具权重
                var toolWeights = CalculateToolWeights();
                optimization.ToolWeights = toolWeights;

                // 基于用户偏好调整
                var userPreferences = AnalyzeUserPreferences();
                optimization.UserPreferences = userPreferences;

                // 生成优化建议
                optimization.OptimizationSuggestions = GenerateOptimizationSuggestions(
                    relevantPatterns, toolWeights, userPreferences);

                return optimization;
            }
        }

        /// <summary>
        /// 清理旧数据
        /// </summary>
        /// <param name="daysToKeep">保留天数</param>
        public async Task CleanupOldDataAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                
                lock (lockObject)
                {
                    analyticsData.UsageEvents.RemoveAll(e => e.Timestamp < cutoffDate);
                    analyticsData.SuggestionEvents.RemoveAll(e => e.Timestamp < cutoffDate);
                    
                    // 重新计算统计数据
                    RecalculateStatistics();
                }

                await SaveAnalyticsDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理分析数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载分析数据
        /// </summary>
        private void LoadAnalyticsData()
        {
            try
            {
                if (File.Exists(analyticsDataPath))
                {
                    var json = File.ReadAllText(analyticsDataPath);
                    analyticsData = JsonSerializer.Deserialize<ToolAnalyticsData>(json) ?? new ToolAnalyticsData();
                }
                else
                {
                    analyticsData = new ToolAnalyticsData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载分析数据失败: {ex.Message}");
                analyticsData = new ToolAnalyticsData();
            }
        }

        /// <summary>
        /// 保存分析数据
        /// </summary>
        private async Task SaveAnalyticsDataAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(analyticsData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(analyticsDataPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存分析数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 提取关键词
        /// </summary>
        private List<string> ExtractKeywords(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            var keywords = new List<string>();
            var words = input.ToLower().Split(new[] { ' ', '\t', '\n', '\r', '，', '。', '！', '？' }, 
                StringSplitOptions.RemoveEmptyEntries);

            // 过滤常见停用词并提取有意义的关键词
            var stopWords = new HashSet<string> { "的", "了", "在", "是", "我", "你", "他", "她", "它", "我们", "你们", "他们" };
            
            foreach (var word in words)
            {
                if (word.Length > 1 && !stopWords.Contains(word))
                {
                    keywords.Add(word);
                }
            }

            return keywords.Take(10).ToList(); // 限制关键词数量
        }

        /// <summary>
        /// 更新工具统计
        /// </summary>
        private void UpdateToolStatistics(string toolName, ToolUsageEvent usageEvent)
        {
            if (!analyticsData.ToolStatistics.ContainsKey(toolName))
            {
                analyticsData.ToolStatistics[toolName] = new ToolStatistic
                {
                    ToolName = toolName
                };
            }

            var stat = analyticsData.ToolStatistics[toolName];
            stat.TotalUsages++;
            stat.LastUsed = usageEvent.Timestamp;
            
            if (usageEvent.WasSuccessful)
            {
                stat.SuccessfulUsages++;
            }

            if (usageEvent.ExecutionTimeMs > 0)
            {
                stat.TotalExecutionTime += usageEvent.ExecutionTimeMs;
                stat.AverageExecutionTime = stat.TotalExecutionTime / stat.TotalUsages;
            }

            if (usageEvent.UserRating.HasValue)
            {
                stat.TotalRating += usageEvent.UserRating.Value;
                stat.RatingCount++;
                stat.AverageRating = (double)stat.TotalRating / stat.RatingCount;
            }
        }

        /// <summary>
        /// 更新用户模式
        /// </summary>
        private void UpdateUserPatterns(string userInput, string toolName, bool wasSuccessful)
        {
            var keywords = ExtractKeywords(userInput);
            
            foreach (var keyword in keywords)
            {
                if (!analyticsData.UserPatterns.ContainsKey(keyword))
                {
                    analyticsData.UserPatterns[keyword] = new UserPattern
                    {
                        Keyword = keyword
                    };
                }

                var pattern = analyticsData.UserPatterns[keyword];
                pattern.Frequency++;
                pattern.LastSeen = DateTime.Now;

                if (!pattern.AssociatedTools.ContainsKey(toolName))
                {
                    pattern.AssociatedTools[toolName] = new ToolAssociation
                    {
                        ToolName = toolName
                    };
                }

                var association = pattern.AssociatedTools[toolName];
                association.UsageCount++;
                if (wasSuccessful)
                {
                    association.SuccessCount++;
                }
                association.SuccessRate = (double)association.SuccessCount / association.UsageCount;
            }
        }

        /// <summary>
        /// 限制历史数据数量
        /// </summary>
        private void TrimHistoryData()
        {
            const int maxEvents = 10000;

            if (analyticsData.UsageEvents.Count > maxEvents)
            {
                analyticsData.UsageEvents = analyticsData.UsageEvents
                    .OrderByDescending(e => e.Timestamp)
                    .Take(maxEvents)
                    .ToList();
            }

            if (analyticsData.SuggestionEvents.Count > maxEvents)
            {
                analyticsData.SuggestionEvents = analyticsData.SuggestionEvents
                    .OrderByDescending(e => e.Timestamp)
                    .Take(maxEvents)
                    .ToList();
            }
        }

        /// <summary>
        /// 获取最常用工具
        /// </summary>
        private List<ToolStatistic> GetMostUsedTools(int count)
        {
            return analyticsData.ToolStatistics.Values
                .OrderByDescending(t => t.TotalUsages)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 获取工具成功率
        /// </summary>
        private Dictionary<string, double> GetToolSuccessRates()
        {
            return analyticsData.ToolStatistics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.SuccessRate
            );
        }

        /// <summary>
        /// 获取平均执行时间
        /// </summary>
        private Dictionary<string, double> GetAverageExecutionTimes()
        {
            return analyticsData.ToolStatistics
                .Where(kvp => kvp.Value.AverageExecutionTime > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.AverageExecutionTime
                );
        }

        /// <summary>
        /// 获取整体建议准确度
        /// </summary>
        private double GetOverallSuggestionAccuracy()
        {
            if (!analyticsData.SuggestionEvents.Any())
                return 0;

            return analyticsData.SuggestionEvents.Average(e => e.SuggestionAccuracy);
        }

        /// <summary>
        /// 获取最近活动
        /// </summary>
        private List<ToolUsageEvent> GetRecentActivity(int count)
        {
            return analyticsData.UsageEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 查找相关模式
        /// </summary>
        private List<UserPattern> FindRelevantPatterns(string userInput)
        {
            var keywords = ExtractKeywords(userInput);
            var relevantPatterns = new List<UserPattern>();

            foreach (var keyword in keywords)
            {
                if (analyticsData.UserPatterns.ContainsKey(keyword))
                {
                    relevantPatterns.Add(analyticsData.UserPatterns[keyword]);
                }
            }

            return relevantPatterns.OrderByDescending(p => p.Frequency).ToList();
        }

        /// <summary>
        /// 计算工具权重
        /// </summary>
        private Dictionary<string, double> CalculateToolWeights()
        {
            var weights = new Dictionary<string, double>();

            foreach (var toolStat in analyticsData.ToolStatistics.Values)
            {
                // 基于成功率、使用频率和用户评分计算权重
                var successWeight = toolStat.SuccessRate * 0.4;
                var usageWeight = Math.Min(toolStat.TotalUsages / 100.0, 1.0) * 0.3;
                var ratingWeight = toolStat.AverageRating / 5.0 * 0.3;

                weights[toolStat.ToolName] = successWeight + usageWeight + ratingWeight;
            }

            return weights;
        }

        /// <summary>
        /// 分析用户偏好
        /// </summary>
        private UserPreferenceAnalysis AnalyzeUserPreferences()
        {
            var preferences = new UserPreferenceAnalysis();

            // 分析偏好工具
            var toolUsageCounts = analyticsData.ToolStatistics
                .OrderByDescending(kvp => kvp.Value.TotalUsages)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();
            preferences.PreferredTools = toolUsageCounts;

            // 分析避免的工具（低成功率或低评分）
            var avoidedTools = analyticsData.ToolStatistics
                .Where(kvp => kvp.Value.SuccessRate < 0.5 || kvp.Value.AverageRating < 2.0)
                .Select(kvp => kvp.Key)
                .ToList();
            preferences.AvoidedTools = avoidedTools;

            // 分析常用关键词
            var commonKeywords = analyticsData.UserPatterns
                .OrderByDescending(kvp => kvp.Value.Frequency)
                .Take(10)
                .Select(kvp => kvp.Key)
                .ToList();
            preferences.CommonKeywords = commonKeywords;

            return preferences;
        }

        /// <summary>
        /// 生成优化建议
        /// </summary>
        private List<string> GenerateOptimizationSuggestions(List<UserPattern> patterns,
            Dictionary<string, double> weights, UserPreferenceAnalysis preferences)
        {
            var suggestions = new List<string>();

            // 基于模式的建议
            if (patterns.Any())
            {
                var topPattern = patterns.First();
                var bestTool = topPattern.AssociatedTools.Values
                    .OrderByDescending(a => a.SuccessRate)
                    .FirstOrDefault();

                if (bestTool != null)
                {
                    suggestions.Add($"基于历史模式，建议优先推荐 {bestTool.ToolName} 工具");
                }
            }

            // 基于权重的建议
            var highWeightTools = weights.Where(kvp => kvp.Value > 0.7).ToList();
            if (highWeightTools.Any())
            {
                suggestions.Add($"高权重工具: {string.Join("、", highWeightTools.Select(t => t.Key))}");
            }

            // 基于用户偏好的建议
            if (preferences.PreferredTools.Any())
            {
                suggestions.Add($"用户偏好工具: {string.Join("、", preferences.PreferredTools.Take(3))}");
            }

            return suggestions;
        }

        /// <summary>
        /// 更新建议统计
        /// </summary>
        private void UpdateSuggestionStatistics(ToolSuggestionEvent suggestionEvent)
        {
            // 这里可以添加更复杂的建议统计逻辑
            // 目前只是简单记录事件
        }

        /// <summary>
        /// 重新计算统计数据
        /// </summary>
        private void RecalculateStatistics()
        {
            // 清空现有统计
            analyticsData.ToolStatistics.Clear();
            analyticsData.UserPatterns.Clear();

            // 重新计算
            foreach (var usageEvent in analyticsData.UsageEvents)
            {
                UpdateToolStatistics(usageEvent.ToolName, usageEvent);
                UpdateUserPatterns(usageEvent.UserInput, usageEvent.ToolName, usageEvent.WasSuccessful);
            }
        }
    }
}
