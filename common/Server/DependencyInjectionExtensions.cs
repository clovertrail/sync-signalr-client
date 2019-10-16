using common.SyncProtocol;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SyncClient;

namespace common.SyncAPI
{
    public static class SyncSDKDependencyInjectionExtensions
    {
        public static ISignalRServerBuilder AddAzureSignalRSyncDemo(this ISignalRServerBuilder builder)
        {
            //builder.Services.
            builder.Services.AddSingleton(typeof(Counter<>)) // counter for demo purpose
                            .AddSingleton(typeof(SyncServer)); // generate the access token and redirect URL
            return builder;
        }
    }
}
