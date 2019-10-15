using common.SyncProtocol;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SyncClient;

namespace common.SyncAPI
{
    public static class SyncSDKDependencyInjectionExtensions
    {
        public static ISignalRServerBuilder AddAzureSignalRSyncSDK(this ISignalRServerBuilder builder)
        {
            //builder.Services.
            builder.Services.AddSingleton(typeof(Pairing<>)).AddSingleton(typeof(SyncProtocols));
            return builder;
        }
    }
}
