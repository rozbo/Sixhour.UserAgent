using Volo.Abp;
using Volo.Abp.Modularity;

namespace SixHour.UserAgent.Tests;

[DependsOn(
    typeof(AbpTestBaseModule),
    typeof(SixHourUserAgentModule)
)]
public class SixHourUserAgentTestModule : AbpModule
{
}