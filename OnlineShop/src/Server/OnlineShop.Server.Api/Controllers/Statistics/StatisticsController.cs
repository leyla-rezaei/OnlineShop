using OnlineShop.Server.Api.Services;
using OnlineShop.Shared.Dtos.Statistics;
using OnlineShop.Shared.Controllers.Statistics;

namespace OnlineShop.Server.Api.Controllers.Statistics;

[ApiController, Route("api/[controller]/[action]")]
public partial class StatisticsController : AppControllerBase, IStatisticsController
{
    [AutoInject] private NugetStatisticsService nugetHttpClient = default!;

    [AllowAnonymous]
    [HttpGet("{packageId}")]
    [AppResponseCache(MaxAge = 3600 * 24, UserAgnostic = true)]
    public async Task<NugetStatsDto> GetNugetStats(string packageId, CancellationToken cancellationToken)
    {
        return await nugetHttpClient.GetPackageStats(packageId, cancellationToken);
    }
}
