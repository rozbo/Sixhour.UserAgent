
# [uap-csharp](https://github.com/ua-parser/uap-csharp) port for abp.

Not just integration, but also with DI and `VirtualFileSystem` so that you can override the `regexes.yaml` easily.



# Usage:

You can instal this module same as other Abp module.

```c#
[DependsOn(
    typeof(SixHourUserAgentModule)
)]
public class YourSomeModule : AbpModule
```
And then, you can use it by DI

```c#
_userAgentParser = GetService<IUserAgentParser>();
```

or

```c#
    private readonly IUserAgentParser _userAgentParser;

    public ComeClass(IUserAgentParser userAgentParser)
    {
        _userAgentParser = userAgentParser;
    }
```

# Inspire

  * [https://github.com/ua-parser/uap-core](https://github.com/ua-parser/uap-core)
  * [https://github.com/ua-parser/uap-csharp](https://github.com/ua-parser/uap-csharp)

# Thanks
  * Søren Enemærke [@sorenenemaerke](https://twitter.com/sorenenemaerke) / [github](https://github.com/enemaerke)
  * Atif Aziz [@raboof](https://twitter.com/raboof) / [github](https://github.com/atifaziz)