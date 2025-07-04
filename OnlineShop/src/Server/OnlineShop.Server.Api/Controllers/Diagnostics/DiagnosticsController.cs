﻿using System.Text;
using OnlineShop.Server.Api.Services;
using OnlineShop.Server.Api.Models.Identity;
using OnlineShop.Shared.Controllers.Diagnostics;

namespace OnlineShop.Server.Api.Controllers.Diagnostics;

[ApiController, AllowAnonymous]
[Route("api/[controller]/[action]")]
public partial class DiagnosticsController : AppControllerBase, IDiagnosticsController
{

    [HttpPost]
    public async Task<string> PerformDiagnostics([FromQuery] string? signalRConnectionId, [FromQuery] string? pushNotificationSubscriptionDeviceId, CancellationToken cancellationToken)
    {
        StringBuilder result = new();

        result.AppendLine($"Client IP: {HttpContext.Connection.RemoteIpAddress}");

        result.AppendLine($"Trace => {Request.HttpContext.TraceIdentifier}");

        var isAuthenticated = User.IsAuthenticated();
        Guid? userSessionId = null;
        UserSession? userSession = null;

        if (isAuthenticated)
        {
            userSessionId = User.GetSessionId();
            userSession = await DbContext
                .UserSessions.SingleAsync(us => us.Id == userSessionId, cancellationToken);
        }

        result.AppendLine($"IsAuthenticated: {isAuthenticated.ToString().ToLowerInvariant()}");



        result.AppendLine($"Culture => C: {CultureInfo.CurrentCulture.Name}, UC: {CultureInfo.CurrentUICulture.Name}");

        result.AppendLine();

        foreach (var header in Request.Headers.OrderBy(h => h.Key))
        {
            result.AppendLine($"{header.Key}: {header.Value}");
        }

        result.AppendLine();
        result.AppendLine("Base url: " + Request.GetBaseUrl());
        result.AppendLine("Web app url: " + Request.GetWebAppUrl());

        return result.ToString();
    }
}
