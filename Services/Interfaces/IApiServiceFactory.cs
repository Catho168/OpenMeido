using OpenMeido.Models;

namespace OpenMeido.Services.Interfaces
{
    public interface IApiServiceFactory
    {
        IApiService Create(AppSettings settings);
    }
}
