using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Volo.Abp.Modularity;
using Volo.Abp.VirtualFileSystem;

namespace SixHour.UserAgent;

[DependsOn(typeof(AbpVirtualFileSystemModule))]
public class SixHourUserAgentModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IYamlParser>((sp) =>
        {
            var vfp = sp.GetService<IVirtualFileProvider>();
            var yaml = vfp.GetFileInfo("/Resources/regexes.yaml").ReadAsString();
            return new MinimalYamlParser(yaml);
        });
        Configure<AbpVirtualFileSystemOptions>(options => { options.FileSets.AddEmbedded<SixHourUserAgentModule>(); });
    }
}