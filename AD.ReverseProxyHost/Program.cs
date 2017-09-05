using System;
using System.IO.Compression;
using System.Net.Http;
using AD.ReverseProxy;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AD.ReverseProxyHost
{
    [PublicAPI]
    public static class Program
    {
        public static void Main([NotNull][ItemNotNull] string[] args)
        {
            BuildWebHost(args).Run();
        }

        [Pure]
        [NotNull]
        public static IWebHost BuildWebHost([NotNull][ItemNotNull] string[] args)
        {
            IConfigurationRoot configuration =
                new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .SetBasePath(Environment.CurrentDirectory)
                    .AddJsonFile("Properties\\reverse-proxy-options.json", optional: false, reloadOnChange: true)
                    .Build();

            return
                WebHost.CreateDefaultBuilder(args)
                       .UseConfiguration(configuration)
                       .ConfigureServices(
                           services =>
                           {
                               services.AddRouting(x => x.LowercaseUrls = true)
                                       .AddOptions()
                                       .Configure<ReverseProxyOptions>(configuration)
                                       .AddSingleton<HttpMessageInvoker, ReverseProxyHttpMessageInvoker>()
                                       .AddResponseCompression(x => x.Providers.Add<GzipCompressionProvider>())
                                       .Configure<GzipCompressionProviderOptions>(x => x.Level = CompressionLevel.Fastest);
                           })
                       .Configure(
                           app =>
                           {
                               app.UseResponseCompression()
                                  .UseMiddleware<ReverseProxyMiddleware>();
                           })
                       .UseKestrel(
                           x =>
                           {
                               x.AddServerHeader = false;
                           })
                       .Build();
        }
    }
}