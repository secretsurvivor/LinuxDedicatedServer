using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;
using LinuxDedicatedServer.Api;
using LinuxDedicatedServer.Legacy.Models;
using LinuxDedicatedServer.Legacy.Handlers;
using LinuxDedicatedServer.Legacy.Utility;
using LinuxDedicatedServer.Legacy.Services;
using LinuxDedicatedServer.Legacy.Interfaces;

namespace LinuxDedicatedServer
{
    internal class Program
    {
        static AppConstants AppConstants { get; }

        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(async services =>
                {
                    services.AddSingleton<IConfig>(await HostConfig.GetConfiguration(AppConstants));
                    services.AddSingleton(_ => Channel.CreateUnbounded<LogRecord>(new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false }));
                    services.AddSingleton<ILoggerHandler, LoggerHandler>();
                    services.AddSingleton<IPackageHandler, PackageHandler>();
                    services.AddSingleton<MessageFactory>();

                    services.AddHostedService<LoggingService>();
                    services.AddHostedService<CommandListenService>();
                })
                .UseDefaultServiceProvider(options =>
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                })
                .Build();

            await host.RunAsync();
        }

        /// Server Requirements:
        /// - Parse incoming commands on a configured port
        /// - Be able to update remotely
        /// - Support dependancies
        /// - Support server packages
        ///     - Packages have their own files behind it
        ///     - Packages can be updated
        ///     - Packages can be executed
        ///     - Support a console tunnel
        ///     - Support a ftp server on packages
        /// - Support execution of multiple server packages at once
        /// 

        /// Command Register
        /// PACKAGE install
        /// PACKAGE upload
        /// INSTANCE start -name STRING -
    }
}
