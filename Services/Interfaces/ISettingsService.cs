using System.Threading.Tasks;
using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Load();

        Task SaveAsync(AppSettings settings);

        Task<bool> TestConnectionAsync(AppSettings settings);
    }
}