using System.IO.Compression;
using OnlineShop.Server.Shared;
using OnlineShop.Server.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

public static class WebApplicationBuilderExtensions
{
    public static TBuilder AddServerSharedServices<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddSharedProjectServices(configuration);


        services.AddSingleton(sp =>
        {
            ServerSharedSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOutputCache(options =>
        {
            options.AddPolicy("AppResponseCachePolicy", policy =>
            {
                var builder = policy.AddPolicy<AppResponseCachePolicy>();
            }, excludeDefaultPolicy: true);
        });
        services.AddDistributedMemoryCache();

        services.AddHttpContextAccessor();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);


        services.AddAntiforgery();

        services.AddAuthorization();

        return builder;
    }

}
