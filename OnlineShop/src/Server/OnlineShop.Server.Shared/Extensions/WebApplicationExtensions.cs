﻿using OnlineShop.Server.Shared;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{

    public static WebApplication UseAppForwardedHeaders(this WebApplication app)
    {
        var configuration = app.Configuration;

        ServerSharedSettings settings = new();
        configuration.Bind(settings);

        var forwardedHeadersOptions = settings.ForwardedHeaders;
        if (forwardedHeadersOptions != null)
        {
            forwardedHeadersOptions.AllowedHosts = [.. (forwardedHeadersOptions.AllowedHosts ?? []).Union(settings.TrustedOrigins.Select(origin => origin.Host))];

            if (app.Environment.IsDevelopment() || forwardedHeadersOptions.AllowedHosts.Any())
            {
                // If the list is empty then all hosts are allowed. Failing to restrict this these values may allow an attacker to spoof links generated for reset password etc.
                app.UseForwardedHeaders(forwardedHeadersOptions);
            }
        }

        return app;
    }

    public static WebApplication UseLocalization(this WebApplication app)
    {
        if (CultureInfoManager.InvariantGlobalization is false)
        {
            var supportedCultures = CultureInfoManager.SupportedCultures.Select(sc => sc.Culture).ToArray();
            var options = new RequestLocalizationOptions
            {
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            };
            options.SetDefaultCulture(CultureInfoManager.DefaultCulture.Name);
            options.RequestCultureProviders.Insert(1, new RouteDataRequestCultureProvider() { Options = options });
            app.UseRequestLocalization(options);
        }

        return app;
    }
}
