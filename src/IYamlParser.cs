using Volo.Abp.DependencyInjection;

namespace SixHour.UserAgent;

public interface IYamlParser
{
    IEnumerable<Dictionary<string, string>> ReadMapping(string mappingName);
}