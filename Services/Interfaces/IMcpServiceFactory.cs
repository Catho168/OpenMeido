#nullable enable

using OpenMeido.Models;
using OpenMeido.Services;

namespace OpenMeido.Services.Interfaces
{
    public interface IMcpServiceFactory
    {
        IMcpService Create(AppSettings settings, McpActivityLogger? logger = null);
    }
}