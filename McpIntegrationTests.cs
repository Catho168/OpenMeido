using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace OpenMeido
{
    /// <summary>
    /// MCP集成测试类，用于验证增强的MCP功能
    /// </summary>
    public class McpIntegrationTests
    {
        private readonly McpToolContextAnalyzer contextAnalyzer;
        private readonly McpToolAnalyticsService analyticsService;
        private readonly List<TestScenario> testScenarios;

        public McpIntegrationTests()
        {
            contextAnalyzer = new McpToolContextAnalyzer();
            analyticsService = new McpToolAnalyticsService();
            testScenarios = InitializeTestScenarios();
        }

        /// <summary>
        /// 运行所有测试场景
        /// </summary>
        /// <returns>测试结果</returns>
        public async Task<TestResults> RunAllTestsAsync()
        {
            var results = new TestResults();
            
            Console.WriteLine("🧪 开始MCP集成测试...");
            Console.WriteLine(new string('=', 50));

            foreach (var scenario in testScenarios)
            {
                Console.WriteLine($"\n📋 测试场景: {scenario.Name}");
                Console.WriteLine($"   输入: {scenario.UserInput}");
                Console.WriteLine($"   期望: {scenario.ExpectedBehavior}");

                var testResult = await RunTestScenarioAsync(scenario);
                results.ScenarioResults.Add(testResult);

                var status = testResult.Passed ? "✅ 通过" : "❌ 失败";
                Console.WriteLine($"   结果: {status}");
                
                if (!testResult.Passed)
                {
                    Console.WriteLine($"   原因: {testResult.FailureReason}");
                }
            }

            // 计算总体结果
            results.TotalTests = testScenarios.Count;
            results.PassedTests = results.ScenarioResults.Count(r => r.Passed);
            results.FailedTests = results.TotalTests - results.PassedTests;
            results.SuccessRate = (double)results.PassedTests / results.TotalTests;

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine($"📊 测试总结:");
            Console.WriteLine($"   总测试数: {results.TotalTests}");
            Console.WriteLine($"   通过: {results.PassedTests}");
            Console.WriteLine($"   失败: {results.FailedTests}");
            Console.WriteLine($"   成功率: {results.SuccessRate:P0}");

            return results;
        }

        /// <summary>
        /// 运行单个测试场景
        /// </summary>
        private async Task<TestScenarioResult> RunTestScenarioAsync(TestScenario scenario)
        {
            var result = new TestScenarioResult
            {
                ScenarioName = scenario.Name,
                UserInput = scenario.UserInput
            };

            try
            {
                // 创建模拟工具列表
                var mockTools = CreateMockTools();
                
                // 测试上下文分析
                var suggestion = await contextAnalyzer.AnalyzeInputAsync(scenario.UserInput, mockTools);
                result.GeneratedSuggestion = suggestion;

                // 验证建议质量
                var qualityCheck = ValidateSuggestionQuality(suggestion, scenario);
                result.SuggestionQuality = qualityCheck;

                // 测试分析服务
                if (suggestion.HasHighConfidenceRecommendation)
                {
                    var bestTool = suggestion.GetBestRecommendation();
                    await analyticsService.RecordSuggestionEventAsync(
                        scenario.UserInput,
                        suggestion.RecommendedTools.Select(t => GetToolName(t.Tool)).ToList(),
                        GetToolName(bestTool.Tool),
                        suggestion.Confidence
                    );
                }

                // 检查是否满足期望
                result.Passed = EvaluateTestResult(suggestion, scenario, qualityCheck);
                
                if (!result.Passed)
                {
                    result.FailureReason = GenerateFailureReason(suggestion, scenario, qualityCheck);
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.FailureReason = $"测试执行异常: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 创建模拟工具列表
        /// </summary>
        private List<AITool> CreateMockTools()
        {
            var tools = new List<AITool>();

            // 模拟微信工具
            tools.Add(CreateMockTool("wechat_send_message", "发送微信消息给指定联系人", 
                new[] { "recipient", "message" }));

            // 模拟文件工具
            tools.Add(CreateMockTool("file_read", "读取文件内容", 
                new[] { "file_path" }));
            tools.Add(CreateMockTool("file_write", "写入文件内容", 
                new[] { "file_path", "content" }));
            tools.Add(CreateMockTool("directory_list", "列出目录内容", 
                new[] { "directory_path" }));

            // 模拟网络工具
            tools.Add(CreateMockTool("web_search", "搜索网络信息", 
                new[] { "query" }));
            tools.Add(CreateMockTool("api_request", "发送API请求", 
                new[] { "url", "method", "data" }));

            // 模拟系统工具
            tools.Add(CreateMockTool("system_execute", "执行系统命令", 
                new[] { "command", "args" }));

            // 模拟数据处理工具
            tools.Add(CreateMockTool("data_analyze", "分析数据", 
                new[] { "data", "analysis_type" }));

            return tools;
        }

        /// <summary>
        /// 创建模拟工具
        /// </summary>
        private AITool CreateMockTool(string name, string description, string[] parameters)
        {
            // 创建一个模拟的AITool实现
            return new MockAITool(name, description);
        }

        /// <summary>
        /// 模拟的AITool实现
        /// </summary>
        private class MockAITool : AITool
        {
            public new string Name { get; }
            public new string Description { get; }

            public MockAITool(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        /// <summary>
        /// 初始化测试场景
        /// </summary>
        private List<TestScenario> InitializeTestScenarios()
        {
            return new List<TestScenario>
            {
                new TestScenario
                {
                    Name = "微信消息发送",
                    UserInput = "发送消息给张三说明天开会",
                    ExpectedBehavior = "应该推荐wechat_send_message工具",
                    ExpectedToolNames = new[] { "wechat_send_message" },
                    MinimumConfidence = 0.7
                },
                new TestScenario
                {
                    Name = "文件读取请求",
                    UserInput = "查看config.txt文件的内容",
                    ExpectedBehavior = "应该推荐file_read工具",
                    ExpectedToolNames = new[] { "file_read" },
                    MinimumConfidence = 0.6
                },
                new TestScenario
                {
                    Name = "目录浏览",
                    UserInput = "看看Documents文件夹里有什么文件",
                    ExpectedBehavior = "应该推荐directory_list工具",
                    ExpectedToolNames = new[] { "directory_list" },
                    MinimumConfidence = 0.6
                },
                new TestScenario
                {
                    Name = "网络搜索",
                    UserInput = "搜索今天的天气情况",
                    ExpectedBehavior = "应该推荐web_search工具",
                    ExpectedToolNames = new[] { "web_search" },
                    MinimumConfidence = 0.5
                },
                new TestScenario
                {
                    Name = "系统命令执行",
                    UserInput = "运行dir命令查看当前目录",
                    ExpectedBehavior = "应该推荐system_execute工具",
                    ExpectedToolNames = new[] { "system_execute" },
                    MinimumConfidence = 0.6
                },
                new TestScenario
                {
                    Name = "数据分析",
                    UserInput = "分析这些销售数据的趋势",
                    ExpectedBehavior = "应该推荐data_analyze工具",
                    ExpectedToolNames = new[] { "data_analyze" },
                    MinimumConfidence = 0.5
                },
                new TestScenario
                {
                    Name = "模糊请求",
                    UserInput = "你好",
                    ExpectedBehavior = "不应该推荐任何工具",
                    ExpectedToolNames = new string[0],
                    MinimumConfidence = 0.0
                },
                new TestScenario
                {
                    Name = "复合请求",
                    UserInput = "读取数据文件然后发送分析结果给老板",
                    ExpectedBehavior = "应该推荐file_read和wechat_send_message工具",
                    ExpectedToolNames = new[] { "file_read", "wechat_send_message" },
                    MinimumConfidence = 0.4
                }
            };
        }

        /// <summary>
        /// 验证建议质量
        /// </summary>
        private SuggestionQuality ValidateSuggestionQuality(ToolUsageSuggestion suggestion, TestScenario scenario)
        {
            var quality = new SuggestionQuality();

            // 检查是否有建议
            quality.HasSuggestions = suggestion.RecommendedTools.Any();

            // 检查置信度
            quality.MeetsConfidenceThreshold = suggestion.Confidence >= scenario.MinimumConfidence;

            // 检查工具匹配
            var suggestedToolNames = suggestion.RecommendedTools.Select(t => GetToolName(t.Tool)).ToList();
            quality.CorrectToolsRecommended = scenario.ExpectedToolNames.All(expected => 
                suggestedToolNames.Any(suggested => suggested.Contains(expected) || expected.Contains(suggested)));

            // 检查是否有误报
            quality.NoFalsePositives = !suggestion.HasHighConfidenceRecommendation || 
                scenario.ExpectedToolNames.Any();

            return quality;
        }

        /// <summary>
        /// 评估测试结果
        /// </summary>
        private bool EvaluateTestResult(ToolUsageSuggestion suggestion, TestScenario scenario, SuggestionQuality quality)
        {
            // 如果期望没有工具推荐
            if (!scenario.ExpectedToolNames.Any())
            {
                return !suggestion.HasHighConfidenceRecommendation;
            }

            // 如果期望有工具推荐
            return quality.HasSuggestions && 
                   quality.MeetsConfidenceThreshold && 
                   quality.CorrectToolsRecommended && 
                   quality.NoFalsePositives;
        }

        /// <summary>
        /// 生成失败原因
        /// </summary>
        private string GenerateFailureReason(ToolUsageSuggestion suggestion, TestScenario scenario, SuggestionQuality quality)
        {
            var reasons = new List<string>();

            if (!quality.HasSuggestions && scenario.ExpectedToolNames.Any())
            {
                reasons.Add("未生成任何工具建议");
            }

            if (!quality.MeetsConfidenceThreshold)
            {
                reasons.Add($"置信度过低 ({suggestion.Confidence:P0} < {scenario.MinimumConfidence:P0})");
            }

            if (!quality.CorrectToolsRecommended)
            {
                var suggested = string.Join(", ", suggestion.RecommendedTools.Select(t => GetToolName(t.Tool)));
                var expected = string.Join(", ", scenario.ExpectedToolNames);
                reasons.Add($"推荐工具不匹配 (推荐: {suggested}, 期望: {expected})");
            }

            if (!quality.NoFalsePositives)
            {
                reasons.Add("产生了误报建议");
            }

            return string.Join("; ", reasons);
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
}
