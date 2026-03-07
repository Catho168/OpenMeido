using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;
using OpenMeido.ViewModels.Base;

namespace OpenMeido.ViewModels
{
    public enum SettingsOperationStatus
    {
        Success,
        ValidationError,
        Failure
    }

    public sealed class SettingsOperationResult
    {
        private SettingsOperationResult(SettingsOperationStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public SettingsOperationStatus Status { get; }

        public string Message { get; }

        public bool IsSuccess => Status == SettingsOperationStatus.Success;

        public static SettingsOperationResult Succeeded(string message = "") => new SettingsOperationResult(SettingsOperationStatus.Success, message);

        public static SettingsOperationResult ValidationFailed(string message) => new SettingsOperationResult(SettingsOperationStatus.ValidationError, message);

        public static SettingsOperationResult Failed(string message) => new SettingsOperationResult(SettingsOperationStatus.Failure, message);
    }

    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private string _apiBaseUrl = string.Empty;
        private string _apiKey = string.Empty;
        private string _modelName = string.Empty;
        private int _maxTokens = 1000;
        private double _temperature = 0.7;
        private string _systemPrompt = string.Empty;
        private bool _enableMcp;
        private SettingsCategory _selectedCategory = SettingsCategory.General;
        private ObservableCollection<McpServerConfig> _mcpServers = new ObservableCollection<McpServerConfig>();

        public SettingsViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public string ApiBaseUrl
        {
            get => _apiBaseUrl;
            set => SetProperty(ref _apiBaseUrl, value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        public int MaxTokens
        {
            get => _maxTokens;
            set => SetProperty(ref _maxTokens, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public string SystemPrompt
        {
            get => _systemPrompt;
            set => SetProperty(ref _systemPrompt, value);
        }

        public bool EnableMcp
        {
            get => _enableMcp;
            set => SetProperty(ref _enableMcp, value);
        }

        public SettingsCategory SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ObservableCollection<McpServerConfig> McpServers
        {
            get => _mcpServers;
            private set => SetProperty(ref _mcpServers, value);
        }

        public SettingsOperationResult Initialize()
        {
            try
            {
                ApplySettings(_settingsService.Load());
                return SettingsOperationResult.Succeeded();
            }
            catch (Exception ex)
            {
                ApplySettings(new AppSettings());
                return SettingsOperationResult.Failed(ex.Message);
            }
        }

        public AppSettings CreateSettingsSnapshot()
        {
            return new AppSettings
            {
                ApiBaseUrl = ApiBaseUrl?.Trim() ?? string.Empty,
                ApiKey = ApiKey?.Trim() ?? string.Empty,
                ModelName = ModelName?.Trim() ?? string.Empty,
                MaxTokens = MaxTokens,
                Temperature = Temperature,
                SystemPrompt = SystemPrompt?.Trim() ?? string.Empty,
                EnableMcp = EnableMcp,
                SelectedCategory = SelectedCategory,
                McpServers = CloneServers(McpServers)
            };
        }

        public async Task<SettingsOperationResult> TestConnectionAsync()
        {
            var settings = CreateSettingsSnapshot();
            if (!settings.IsValid())
            {
                return SettingsOperationResult.ValidationFailed("请把API配置信息填写完整哦~");
            }

            try
            {
                var connectionSuccess = await _settingsService.TestConnectionAsync(settings);
                return connectionSuccess
                    ? SettingsOperationResult.Succeeded("妹抖酱连接成功！可以开始聊天了♪")
                    : SettingsOperationResult.Failed("妹抖酱连接失败了，请检查配置信息~");
            }
            catch (Exception ex)
            {
                return SettingsOperationResult.Failed($"测试连接时出错了: {ex.Message}");
            }
        }

        public async Task<SettingsOperationResult> SaveAsync()
        {
            var settings = CreateSettingsSnapshot();
            if (!settings.IsValid())
            {
                return SettingsOperationResult.ValidationFailed("请填写完整且正确的配置信息");
            }

            try
            {
                await _settingsService.SaveAsync(settings);
                return SettingsOperationResult.Succeeded("设置已保存成功！");
            }
            catch (Exception ex)
            {
                return SettingsOperationResult.Failed($"保存设置时出错: {ex.Message}");
            }
        }

        private void ApplySettings(AppSettings settings)
        {
            var snapshot = settings ?? new AppSettings();
            ApiBaseUrl = snapshot.ApiBaseUrl;
            ApiKey = snapshot.ApiKey;
            ModelName = snapshot.ModelName;
            MaxTokens = snapshot.MaxTokens;
            Temperature = snapshot.Temperature;
            SystemPrompt = snapshot.SystemPrompt;
            EnableMcp = snapshot.EnableMcp;
            SelectedCategory = snapshot.SelectedCategory;
            McpServers = new ObservableCollection<McpServerConfig>(CloneServers(snapshot.McpServers));
        }

        private static List<McpServerConfig> CloneServers(IEnumerable<McpServerConfig> servers)
        {
            return servers?.Select(CloneServer).ToList() ?? new List<McpServerConfig>();
        }

        private static McpServerConfig CloneServer(McpServerConfig server)
        {
            if (server == null)
            {
                return new McpServerConfig();
            }

            return new McpServerConfig
            {
                Id = server.Id,
                Name = server.Name,
                Command = server.Command,
                Arguments = server.Arguments,
                IsEnabled = server.IsEnabled,
                Description = server.Description
            };
        }
    }
}