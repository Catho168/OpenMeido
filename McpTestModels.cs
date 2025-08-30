using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenMeido
{
    /// <summary>
    /// 测试场景定义
    /// </summary>
    public class TestScenario
    {
        /// <summary>
        /// 测试场景名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 用户输入
        /// </summary>
        public string UserInput { get; set; } = "";

        /// <summary>
        /// 期望行为描述
        /// </summary>
        public string ExpectedBehavior { get; set; } = "";

        /// <summary>
        /// 期望推荐的工具名称
        /// </summary>
        public string[] ExpectedToolNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 最小置信度要求
        /// </summary>
        public double MinimumConfidence { get; set; } = 0.5;

        /// <summary>
        /// 测试类别
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        /// 测试优先级
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// 是否为关键测试
        /// </summary>
        public bool IsCritical { get; set; } = false;
    }

    /// <summary>
    /// 建议质量评估
    /// </summary>
    public class SuggestionQuality
    {
        /// <summary>
        /// 是否有建议
        /// </summary>
        public bool HasSuggestions { get; set; }

        /// <summary>
        /// 是否满足置信度阈值
        /// </summary>
        public bool MeetsConfidenceThreshold { get; set; }

        /// <summary>
        /// 是否推荐了正确的工具
        /// </summary>
        public bool CorrectToolsRecommended { get; set; }

        /// <summary>
        /// 是否没有误报
        /// </summary>
        public bool NoFalsePositives { get; set; }

        /// <summary>
        /// 整体质量评分 (0-1)
        /// </summary>
        public double OverallScore => 
            (HasSuggestions ? 0.25 : 0) +
            (MeetsConfidenceThreshold ? 0.25 : 0) +
            (CorrectToolsRecommended ? 0.25 : 0) +
            (NoFalsePositives ? 0.25 : 0);

        /// <summary>
        /// 质量等级
        /// </summary>
        public string QualityGrade => OverallScore switch
        {
            >= 0.9 => "优秀",
            >= 0.7 => "良好",
            >= 0.5 => "一般",
            >= 0.3 => "较差",
            _ => "很差"
        };
    }

    /// <summary>
    /// 测试场景结果
    /// </summary>
    public class TestScenarioResult
    {
        /// <summary>
        /// 场景名称
        /// </summary>
        public string ScenarioName { get; set; } = "";

        /// <summary>
        /// 用户输入
        /// </summary>
        public string UserInput { get; set; } = "";

        /// <summary>
        /// 是否通过测试
        /// </summary>
        public bool Passed { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        public string FailureReason { get; set; } = "";

        /// <summary>
        /// 生成的建议
        /// </summary>
        public ToolUsageSuggestion GeneratedSuggestion { get; set; }

        /// <summary>
        /// 建议质量评估
        /// </summary>
        public SuggestionQuality SuggestionQuality { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public double ExecutionTimeMs { get; set; }

        /// <summary>
        /// 测试时间
        /// </summary>
        public DateTime TestTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 测试结果汇总
    /// </summary>
    public class TestResults
    {
        /// <summary>
        /// 场景测试结果列表
        /// </summary>
        public List<TestScenarioResult> ScenarioResults { get; set; } = new List<TestScenarioResult>();

        /// <summary>
        /// 总测试数
        /// </summary>
        public int TotalTests { get; set; }

        /// <summary>
        /// 通过测试数
        /// </summary>
        public int PassedTests { get; set; }

        /// <summary>
        /// 失败测试数
        /// </summary>
        public int FailedTests { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate { get; set; }

        /// <summary>
        /// 测试开始时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 测试结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 总执行时间
        /// </summary>
        public TimeSpan TotalExecutionTime => EndTime - StartTime;

        /// <summary>
        /// 平均建议质量评分
        /// </summary>
        public double AverageQualityScore => 
            ScenarioResults.Any() ? ScenarioResults.Average(r => r.SuggestionQuality?.OverallScore ?? 0) : 0;

        /// <summary>
        /// 关键测试通过率
        /// </summary>
        public double CriticalTestPassRate { get; set; }

        /// <summary>
        /// 测试报告摘要
        /// </summary>
        public string GetSummary()
        {
            var summary = $"""
                MCP集成测试报告
                ================
                
                测试概况:
                - 总测试数: {TotalTests}
                - 通过: {PassedTests}
                - 失败: {FailedTests}
                - 成功率: {SuccessRate:P1}
                - 平均质量评分: {AverageQualityScore:F2}
                
                执行信息:
                - 开始时间: {StartTime:yyyy-MM-dd HH:mm:ss}
                - 结束时间: {EndTime:yyyy-MM-dd HH:mm:ss}
                - 总耗时: {TotalExecutionTime.TotalSeconds:F1}秒
                
                """;

            if (FailedTests > 0)
            {
                summary += "\n失败的测试:\n";
                foreach (var failed in ScenarioResults.Where(r => !r.Passed))
                {
                    summary += $"- {failed.ScenarioName}: {failed.FailureReason}\n";
                }
            }

            return summary;
        }
    }

    /// <summary>
    /// 性能测试结果
    /// </summary>
    public class PerformanceTestResult
    {
        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// 最大响应时间（毫秒）
        /// </summary>
        public double MaxResponseTime { get; set; }

        /// <summary>
        /// 最小响应时间（毫秒）
        /// </summary>
        public double MinResponseTime { get; set; }

        /// <summary>
        /// 95百分位响应时间（毫秒）
        /// </summary>
        public double P95ResponseTime { get; set; }

        /// <summary>
        /// 内存使用情况
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// CPU使用率
        /// </summary>
        public double CpuUsagePercent { get; set; }
    }

    /// <summary>
    /// 集成测试配置
    /// </summary>
    public class TestConfiguration
    {
        /// <summary>
        /// 是否运行性能测试
        /// </summary>
        public bool RunPerformanceTests { get; set; } = false;

        /// <summary>
        /// 是否运行压力测试
        /// </summary>
        public bool RunStressTests { get; set; } = false;

        /// <summary>
        /// 测试超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 并发测试数量
        /// </summary>
        public int ConcurrentTests { get; set; } = 1;

        /// <summary>
        /// 是否详细输出
        /// </summary>
        public bool VerboseOutput { get; set; } = true;

        /// <summary>
        /// 测试数据目录
        /// </summary>
        public string TestDataDirectory { get; set; } = "";

        /// <summary>
        /// 结果输出目录
        /// </summary>
        public string OutputDirectory { get; set; } = "";
    }

    /// <summary>
    /// 测试数据生成器
    /// </summary>
    public static class TestDataGenerator
    {
        /// <summary>
        /// 生成随机测试场景
        /// </summary>
        /// <param name="count">生成数量</param>
        /// <returns>测试场景列表</returns>
        public static List<TestScenario> GenerateRandomScenarios(int count)
        {
            var scenarios = new List<TestScenario>();
            var random = new Random();

            var templates = new[]
            {
                ("发送消息给{0}", new[] { "wechat_send_message" }),
                ("读取{0}文件", new[] { "file_read" }),
                ("搜索{0}", new[] { "web_search" }),
                ("执行{0}命令", new[] { "system_execute" }),
                ("分析{0}数据", new[] { "data_analyze" })
            };

            var placeholders = new[]
            {
                "张三", "李四", "config.txt", "data.csv", "今天天气", 
                "dir", "ls", "销售", "用户行为", "财务"
            };

            for (int i = 0; i < count; i++)
            {
                var template = templates[random.Next(templates.Length)];
                var placeholder = placeholders[random.Next(placeholders.Length)];
                
                scenarios.Add(new TestScenario
                {
                    Name = $"随机测试{i + 1}",
                    UserInput = string.Format(template.Item1, placeholder),
                    ExpectedToolNames = template.Item2,
                    MinimumConfidence = 0.3 + random.NextDouble() * 0.4,
                    Category = "随机生成"
                });
            }

            return scenarios;
        }
    }
}
