using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenMeido
{
    /// <summary>
    /// 工具使用事件
    /// </summary>
    public class ToolUsageEvent
    {
        public string ToolName { get; set; } = "";
        public string UserInput { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool WasSuccessful { get; set; }
        public double ExecutionTimeMs { get; set; }
        public int? UserRating { get; set; }
        public int InputLength { get; set; }
        public List<string> InputKeywords { get; set; } = new List<string>();
    }

    /// <summary>
    /// 工具建议事件
    /// </summary>
    public class ToolSuggestionEvent
    {
        public string UserInput { get; set; } = "";
        public List<string> SuggestedTools { get; set; } = new List<string>();
        public string SelectedTool { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double SuggestionAccuracy { get; set; }
        public bool WasAccepted { get; set; }
    }

    /// <summary>
    /// 工具统计信息
    /// </summary>
    public class ToolStatistic
    {
        public string ToolName { get; set; } = "";
        public int TotalUsages { get; set; }
        public int SuccessfulUsages { get; set; }
        public double SuccessRate => TotalUsages > 0 ? (double)SuccessfulUsages / TotalUsages : 0;
        public DateTime LastUsed { get; set; }
        public double TotalExecutionTime { get; set; }
        public double AverageExecutionTime { get; set; }
        public int TotalRating { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// 用户模式
    /// </summary>
    public class UserPattern
    {
        public string Keyword { get; set; } = "";
        public int Frequency { get; set; }
        public DateTime LastSeen { get; set; }
        public Dictionary<string, ToolAssociation> AssociatedTools { get; set; } = new Dictionary<string, ToolAssociation>();
    }

    /// <summary>
    /// 工具关联信息
    /// </summary>
    public class ToolAssociation
    {
        public string ToolName { get; set; } = "";
        public int UsageCount { get; set; }
        public int SuccessCount { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// 分析数据容器
    /// </summary>
    public class ToolAnalyticsData
    {
        public List<ToolUsageEvent> UsageEvents { get; set; } = new List<ToolUsageEvent>();
        public List<ToolSuggestionEvent> SuggestionEvents { get; set; } = new List<ToolSuggestionEvent>();
        public Dictionary<string, ToolStatistic> ToolStatistics { get; set; } = new Dictionary<string, ToolStatistic>();
        public Dictionary<string, UserPattern> UserPatterns { get; set; } = new Dictionary<string, UserPattern>();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 工具使用统计汇总
    /// </summary>
    public class ToolUsageStatistics
    {
        public int TotalUsageEvents { get; set; }
        public int TotalSuggestionEvents { get; set; }
        public List<ToolStatistic> MostUsedTools { get; set; } = new List<ToolStatistic>();
        public Dictionary<string, double> ToolSuccessRates { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> AverageExecutionTimes { get; set; } = new Dictionary<string, double>();
        public double SuggestionAccuracy { get; set; }
        public List<UserPattern> UserPatterns { get; set; } = new List<UserPattern>();
        public List<ToolUsageEvent> RecentActivity { get; set; } = new List<ToolUsageEvent>();
    }

    /// <summary>
    /// 工具推荐优化信息
    /// </summary>
    public class ToolRecommendationOptimization
    {
        public string UserInput { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public List<UserPattern> RelevantPatterns { get; set; } = new List<UserPattern>();
        public Dictionary<string, double> ToolWeights { get; set; } = new Dictionary<string, double>();
        public UserPreferenceAnalysis UserPreferences { get; set; } = new UserPreferenceAnalysis();
        public List<string> OptimizationSuggestions { get; set; } = new List<string>();
    }

    /// <summary>
    /// 用户偏好分析
    /// </summary>
    public class UserPreferenceAnalysis
    {
        public List<string> PreferredTools { get; set; } = new List<string>();
        public List<string> AvoidedTools { get; set; } = new List<string>();
        public Dictionary<string, double> CategoryPreferences { get; set; } = new Dictionary<string, double>();
        public double AverageSessionLength { get; set; }
        public List<string> CommonKeywords { get; set; } = new List<string>();
        public Dictionary<string, int> UsageTimePatterns { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// 工具性能指标
    /// </summary>
    public class ToolPerformanceMetrics
    {
        public string ToolName { get; set; } = "";
        public double SuccessRate { get; set; }
        public double AverageExecutionTime { get; set; }
        public double UserSatisfactionScore { get; set; }
        public int UsageFrequency { get; set; }
        public DateTime LastUsed { get; set; }
        public List<string> CommonFailureReasons { get; set; } = new List<string>();
        public Dictionary<string, double> ContextualSuccessRates { get; set; } = new Dictionary<string, double>();
    }

    /// <summary>
    /// 建议质量指标
    /// </summary>
    public class SuggestionQualityMetrics
    {
        public double OverallAccuracy { get; set; }
        public double AcceptanceRate { get; set; }
        public Dictionary<string, double> CategoryAccuracy { get; set; } = new Dictionary<string, double>();
        public List<string> MostAccurateSuggestions { get; set; } = new List<string>();
        public List<string> LeastAccurateSuggestions { get; set; } = new List<string>();
        public double AverageResponseTime { get; set; }
        public int TotalSuggestionsMade { get; set; }
        public int TotalSuggestionsAccepted { get; set; }
    }

    /// <summary>
    /// 使用趋势分析
    /// </summary>
    public class UsageTrendAnalysis
    {
        public Dictionary<DateTime, int> DailyUsageCounts { get; set; } = new Dictionary<DateTime, int>();
        public Dictionary<string, List<int>> ToolUsageTrends { get; set; } = new Dictionary<string, List<int>>();
        public List<string> GrowingTools { get; set; } = new List<string>();
        public List<string> DecliningTools { get; set; } = new List<string>();
        public Dictionary<int, int> HourlyUsagePatterns { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> WeeklyUsagePatterns { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// 工具推荐改进建议
    /// </summary>
    public class ToolRecommendationImprovement
    {
        public string RecommendationType { get; set; } = "";
        public string Description { get; set; } = "";
        public double PotentialImpact { get; set; }
        public string ImplementationDifficulty { get; set; } = "";
        public List<string> RequiredActions { get; set; } = new List<string>();
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 分析报告
    /// </summary>
    public class AnalyticsReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        public TimeSpan ReportPeriod { get; set; }
        public ToolUsageStatistics UsageStatistics { get; set; } = new ToolUsageStatistics();
        public SuggestionQualityMetrics SuggestionMetrics { get; set; } = new SuggestionQualityMetrics();
        public UsageTrendAnalysis TrendAnalysis { get; set; } = new UsageTrendAnalysis();
        public List<ToolRecommendationImprovement> ImprovementSuggestions { get; set; } = new List<ToolRecommendationImprovement>();
        public Dictionary<string, object> CustomMetrics { get; set; } = new Dictionary<string, object>();
    }
}
