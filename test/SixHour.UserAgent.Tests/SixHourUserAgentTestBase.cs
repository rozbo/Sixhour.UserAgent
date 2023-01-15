using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace SixHour.UserAgent.Tests;

public abstract class SixHourUserAgentTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    
}