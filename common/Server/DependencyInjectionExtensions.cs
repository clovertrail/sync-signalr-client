using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SyncClient;

namespace common.sync
{
    public static class SyncSDKDependencyInjectionExtensions
    {
        public static ISignalRServerBuilder AddAzureSignalRSyncDemo(this ISignalRServerBuilder builder)
        {
            //builder.Services.
            builder.Services.AddSingleton(typeof(ClientStatTracker<>)) // counter for demo purpose
                            .AddSingleton(typeof(SyncServer)); // generate the access token and redirect URL
            return builder;
        }
    }
}
