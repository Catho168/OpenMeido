using System;
using OpenMeido.Models;
using OpenMeido.Services.Interfaces;

namespace OpenMeido.Services
{
    public class ApiServiceFactory : IApiServiceFactory
    {
        private readonly IMcpServiceFactory _mcpServiceFactory;

        public ApiServiceFactory()
            : this(new McpServiceFactory())
        {
        }

        public ApiServiceFactory(IMcpServiceFactory mcpServiceFactory)
        {
            _mcpServiceFactory = mcpServiceFactory ?? new McpServiceFactory();
        }

        public IApiService Create(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            return new ApiService(settings, _mcpServiceFactory);
        }
    }
}
