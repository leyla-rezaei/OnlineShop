﻿using OnlineShop.Client.Core;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using OnlineShop.Client.Core.Services.HttpMessageHandlers;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IClientCoreServiceCollectionExtensions
{
    public static IServiceCollection AddClientCoreProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected in client side (WebAssembly, Android, iOS, Windows and macOS) + server side (during pre-rendering and Blazor Server)
        services.AddSharedProjectServices(configuration);

        services.AddTransient<IPrerenderStateService, NoOpPrerenderStateService>();

        services.AddScoped<ThemeService>();
        services.AddScoped<CultureService>();
        services.AddScoped<LazyAssemblyLoader>();
        services.AddScoped<SignInModalService>();
        services.AddScoped<IAuthTokenProvider, ClientSideAuthTokenProvider>();
        services.AddScoped<IExternalNavigationService, DefaultExternalNavigationService>();

        if (Uri.TryCreate(configuration.GetServerAddress(), UriKind.Absolute, out var serverAddress))
        {
            services.AddScoped<AbsoluteServerAddressProvider>(sp => new() { GetAddress = () => serverAddress });
        }
        else
        {
            services.AddScoped<AbsoluteServerAddressProvider>(sp => new() { GetAddress = () => sp.GetRequiredService<HttpClient>().BaseAddress! /* Read AbsoluteServerAddressProvider's comments for more info. */ });
        }

        // The following services must be unique to each app session.
        // Defining them as singletons would result in them being shared across all users in Blazor Server and during pre-rendering.
        // To address this, we use the AddSessioned extension method.
        // AddSessioned applies AddSingleton in BlazorHybrid and AddScoped in Blazor WebAssembly and Blazor Server, ensuring correct service lifetimes for each environment.
        services.AddSessioned<PubSubService>();
        services.AddSessioned<PromptService>();
        services.AddSessioned<SnackBarService>();
        services.AddSessioned<ILocalHttpServer, NoOpLocalHttpServer>();
        services.AddSessioned<ITelemetryContext, AppTelemetryContext>();
        services.AddSessioned<AuthenticationStateProvider>(sp =>
        {
            var authenticationStateProvider = ActivatorUtilities.CreateInstance<AuthManager>(sp);
            authenticationStateProvider.OnInit();
            return authenticationStateProvider;
        });
        services.AddSessioned(sp => (AuthManager)sp.GetRequiredService<AuthenticationStateProvider>());

        services.AddSingleton(sp =>
        {
            ClientCoreSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });

        services.AddOptions<ClientCoreSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddBitButilServices();
        services.AddBitBlazorUIServices();
        services.AddBitBlazorUIExtrasServices(trySingleton: AppPlatform.IsBlazorHybrid);

        // This code constructs a chain of HTTP message handlers. By default, it uses `HttpClientHandler` 
        // to send requests to the server. However, you can replace `HttpClientHandler` with other HTTP message 
        // handlers, such as `SocketsHttpHandler` or ASP.NET Core's `HttpMessageHandler` from the Test Host, which is useful for integration tests.
        services.AddScoped<HttpMessageHandlersChainFactory>(serviceProvider => transportHandler =>
        {
            var constructedHttpMessageHandler = ActivatorUtilities.CreateInstance<LoggingDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<CacheDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RequestHeadersDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<AuthDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<RetryDelegatingHandler>(serviceProvider,
                        [ActivatorUtilities.CreateInstance<ExceptionDelegatingHandler>(serviceProvider, [transportHandler])])])])])]);
            return constructedHttpMessageHandler;
        });
        services.AddScoped<AuthDelegatingHandler>();
        services.AddScoped<CacheDelegatingHandler>();
        services.AddScoped<RetryDelegatingHandler>();
        services.AddScoped<ExceptionDelegatingHandler>();
        services.AddScoped<RequestHeadersDelegatingHandler>();
        services.AddScoped(serviceProvider =>
        {
            var transportHandler = serviceProvider.GetRequiredKeyedService<HttpMessageHandler>("PrimaryHttpMessageHandler");
            var constructedHttpMessageHandler = serviceProvider.GetRequiredService<HttpMessageHandlersChainFactory>().Invoke(transportHandler);
            return constructedHttpMessageHandler;
        });



        services.AddTypedHttpClients(); // See OnlineShop.Shared/Controllers/Readme.md


        return services;
    }

    internal static IServiceCollection AddSessioned<TService, TImplementation>(this IServiceCollection services)
        where TImplementation : class, TService
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            return services.AddSingleton<TService, TImplementation>();
        }
        else
        {
            return services.AddScoped<TService, TImplementation>();
        }
    }

    internal static IServiceCollection AddSessioned<TService>(this IServiceCollection services, Func<IServiceProvider, TService> implementationFactory)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            services.Add(ServiceDescriptor.Singleton(implementationFactory));
        }
        else
        {
            services.Add(ServiceDescriptor.Scoped(implementationFactory));
        }

        return services;
    }

    internal static void AddSessioned<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services)
        where TService : class
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            services.AddSingleton<TService, TService>();
        }
        else
        {
            services.AddScoped<TService, TService>();
        }
    }
}
