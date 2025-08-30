using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OpenMeido
{
    /// 设置页面分类枚举
    public enum SettingsCategory
    {
        General = 0,    // 通用设置
        MCP = 1         // MCP设置
    }

    /// MCP服务器配置类
    [Serializable]
    public class McpServerConfig
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Command { get; set; } = "";
        public string Arguments { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
        public string Description { get; set; } = "";

        /// 验证MCP服务器配置是否有效
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Command);
        }
    }

    /// 应用程序设置管理类，用于保存和加载AI API配置
    public class AppSettings
    {
        // 配置文件的默认保存路径，存储在用户的应用数据目录中
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenMeido",
            "config.xml"
        );

        public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1";

        public string ApiKey { get; set; } = "";

        public string ModelName { get; set; } = "gpt-3.5-turbo";

        public int MaxTokens { get; set; } = 1000;

        /// 0.0到2.0
        public double Temperature { get; set; } = 0.7;

        public string SystemPrompt { get; set; } = @"示例系统提示词";

        /// MCP服务器配置列表
        public List<McpServerConfig> McpServers { get; set; } = new List<McpServerConfig>();

        /// 是否启用MCP功能
        public bool EnableMcp { get; set; } = false;

        /// 当前选中的设置分类
        public SettingsCategory SelectedCategory { get; set; } = SettingsCategory.General;

        /// 从配置文件加载设置
        /// 如果配置文件不存在，则返回默认设置
        public static AppSettings Load()
        {
            try
            {
                // 检查配置文件是否存在
                if (File.Exists(ConfigPath))
                {
                    // 创建XML序列化器
                    var serializer = new XmlSerializer(typeof(AppSettings));

                    // 从文件读取并反序列化设置对象
                    using (var reader = new FileStream(ConfigPath, FileMode.Open))
                    {
                        var settings = (AppSettings)serializer.Deserialize(reader);
                        return settings ?? new AppSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                // 待添加日志记录功能
                System.Diagnostics.Debug.WriteLine($"加载配置文件时出错: {ex.Message}");
            }

            // 如果文件不存在或加载失败，返回默认设置
            return new AppSettings();
        }

        /// 将当前设置保存到配置文件
        /// 自动创建必要的目录结构
        public void Save()
        {
            try
            {
                // 获取配置文件的目录路径
                string directory = Path.GetDirectoryName(ConfigPath);

                // 如果目录不存在，则创建目录
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建XML序列化器
                var serializer = new XmlSerializer(typeof(AppSettings));

                // 将当前设置对象序列化为XML并写入文件
                using (var writer = new FileStream(ConfigPath, FileMode.Create))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                // 如果保存过程中出现异常，记录错误信息
                System.Diagnostics.Debug.WriteLine($"保存配置文件时出错: {ex.Message}");

                // 可以考虑向用户显示错误消息
                throw new InvalidOperationException($"无法保存配置文件: {ex.Message}", ex);
            }
        }

        /// 将当前设置异步保存到配置文件
        /// 自动创建必要的目录结构
        public async Task SaveAsync()
        {
            try
            {
                // 获取配置文件的目录路径
                string directory = Path.GetDirectoryName(ConfigPath);

                // 如果目录不存在，则创建目录
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 创建XML序列化器
                var serializer = new XmlSerializer(typeof(AppSettings));

                // 将当前设置对象序列化为XML并异步写入文件
                using (var writer = new FileStream(ConfigPath, FileMode.Create))
                {
                    await Task.Run(() => serializer.Serialize(writer, this));
                }
            }
            catch (Exception ex)
            {
                // 如果保存过程中出现异常，记录错误信息
                System.Diagnostics.Debug.WriteLine($"异步保存配置文件时出错: {ex.Message}");

                // 可以考虑向用户显示错误消息
                throw new InvalidOperationException($"无法保存配置文件: {ex.Message}", ex);
            }
        }

        /// 验证当前设置是否有效
        /// 如果设置有效返回true，否则返回false
        public bool IsValid()
        {
            // 检查是否为空
            if (string.IsNullOrWhiteSpace(ApiKey))
                return false;

            if (string.IsNullOrWhiteSpace(ApiBaseUrl))
                return false;

            if (string.IsNullOrWhiteSpace(ModelName))
                return false;

            // 检查最大令牌数是否在合理范围内
            if (MaxTokens <= 0 || MaxTokens > 4000)
                return false;

            // 检查温度参数是否在有效范围内
            if (Temperature < 0.0 || Temperature > 2.0)
                return false;

            // 尝试验证URL格式是否正确
            try
            {
                var uri = new Uri(ApiBaseUrl);
                // 确保是HTTP或HTTPS协议
                if (uri.Scheme != "http" && uri.Scheme != "https")
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
