#nullable enable

using System;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Services
{
    public class McpServiceFactory : IMcpServiceFactory
    {
        public IMcpService Create(AppSettings settings, McpActivityLogger? logger = null)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return new McpService(settings, logger);
        }
    }
}