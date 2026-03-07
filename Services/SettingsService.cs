using System;
using System.Threading.Tasks;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Services
{
    public class SettingsService : ISettingsService
    {
        public AppSettings Load()
        {
            return AppSettings.Load();
        }

        public Task SaveAsync(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return settings.SaveAsync();
        }

        public async Task<bool> TestConnectionAsync(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            using var apiService = new ApiService(settings);
            return await apiService.TestConnectionAsync();
        }
    }
}