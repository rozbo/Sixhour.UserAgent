using Volo.Abp.DependencyInjection;

namespace SixHour.UserAgent;

using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

internal static class VersionString
{
    public static string Format(params string[] parts)
    {
        return string.Join(".", parts.Where(v => !String.IsNullOrEmpty(v)).ToArray());
    }
}

/// <summary>
/// Represents a parser of a user agent string
/// </summary>
public class UserAgentParser : IUserAgentParser, ITransientDependency
{
    /// <summary>
    /// The constant string value used to signal an unknown match for a given property or value. This
    /// is by default the string "Other".
    /// </summary>
    public const string Other = "Other";

    private readonly Func<string, OS> _osParser;
    private readonly Func<string, Device> _deviceParser;
    private readonly Func<string, UserAgent> _userAgentParser;

    public UserAgentParser(IYamlParser yamlParser)
    {
        var config = new Config(new ParserOptions());

        _userAgentParser = CreateParser(Read(yamlParser.ReadMapping("user_agent_parsers"), config.UserAgentSelector),
            new UserAgent(Other, null, null, null));
        _osParser = CreateParser(Read(yamlParser.ReadMapping("os_parsers"), config.OSSelector),
            new OS(Other, null, null, null, null));
        _deviceParser = CreateParser(Read(yamlParser.ReadMapping("device_parsers"), config.DeviceSelector),
            new Device(Other, string.Empty, string.Empty));
    }

    private static IEnumerable<T> Read<T>(IEnumerable<Dictionary<string, string>> entries,
        Func<Func<string, string>, T> selector)
    {
        return from cm in entries select selector(cm.Find);
    }


    /// <summary>
    /// Parse a user agent string and obtain all client information
    /// </summary>
    public ClientInfo Parse(string uaString)
    {
        var os = ParseOS(uaString);
        var device = ParseDevice(uaString);
        var ua = ParseUserAgent(uaString);
        return new ClientInfo(uaString, os, device, ua);
    }

    /// <summary>
    /// Parse a user agent string and obtain the OS information
    /// </summary>
    public OS ParseOS(string uaString)
    {
        return _osParser(uaString);
    }

    /// <summary>
    /// Parse a user agent string and obtain the device information
    /// </summary>
    public Device ParseDevice(string uaString)
    {
        return _deviceParser(uaString);
    }

    /// <summary>
    /// Parse a user agent string and obtain the UserAgent information
    /// </summary>
    public UserAgent ParseUserAgent(string uaString)
    {
        return _userAgentParser(uaString);
    }

    private static Func<string, T> CreateParser<T>(IEnumerable<Func<string, T>> parsers, T defaultValue) where T : class
    {
        return CreateParser(parsers, defaultValue, t => t);
    }

    private static Func<string, TResult> CreateParser<T, TResult>(IEnumerable<Func<string, T>> parsers, T defaultValue,
        Func<T, TResult> selector) where T : class
    {
        parsers = parsers?.ToArray() ?? Enumerable.Empty<Func<string, T>>();
        return ua => selector(parsers.Select(p => p(ua)).FirstOrDefault(m => m != null) ?? defaultValue);
    }

    private class Config
    {
        private readonly ParserOptions _options;

        internal Config(ParserOptions options)
        {
            _options = options;
        }

        // ReSharper disable once InconsistentNaming
        public Func<string, OS> OSSelector(Func<string, string> indexer)
        {
            var regex = Regex(indexer, "OS");
            var os = indexer("os_replacement");
            var v1 = indexer("os_v1_replacement");
            var v2 = indexer("os_v2_replacement");
            var v3 = indexer("os_v3_replacement");
            var v4 = indexer("os_v4_replacement");
            return Parsers.OS(regex, os, v1, v2, v3, v4);
        }

        public Func<string, UserAgent> UserAgentSelector(Func<string, string> indexer)
        {
            var regex = Regex(indexer, "User agent");
            var family = indexer("family_replacement");
            var v1 = indexer("v1_replacement");
            var v2 = indexer("v2_replacement");
            var v3 = indexer("v3_replacement");
            return Parsers.UserAgent(regex, family, v1, v2, v3);
        }

        public Func<string, Device> DeviceSelector(Func<string, string> indexer)
        {
            var regex = Regex(indexer, "Device", indexer("regex_flag"));
            var device = indexer("device_replacement");
            var brand = indexer("brand_replacement");
            var model = indexer("model_replacement");
            return Parsers.Device(regex, device, brand, model);
        }

        private Regex Regex(Func<string, string> indexer, string key, string regexFlag = null)
        {
            var pattern = indexer("regex");
            if (pattern == null)
                throw new Exception($"{key} is missing regular expression specification.");

            // Some expressions in the regex.yaml file causes parsing errors
            // in .NET such as the \_ token so need to alter them before
            // proceeding.

            if (pattern.IndexOf(@"\_", StringComparison.Ordinal) >= 0)
                pattern = pattern.Replace(@"\_", "_");

            //Singleline: User agent strings do not contain newline characters. RegexOptions.Singleline improves performance.
            //CultureInvariant: The interpretation of a user agent never depends on the current locale.
            RegexOptions options = RegexOptions.Singleline | RegexOptions.CultureInvariant;

            if ("i".Equals(regexFlag))
            {
                options |= RegexOptions.IgnoreCase;
            }

#if REGEX_COMPILATION
                if (_options.UseCompiledRegex)
                {
                    options |= RegexOptions.Compiled;
                }
#endif

#if REGEX_MATCHTIMEOUT
                return new Regex(pattern, options, _options.MatchTimeOut);
#else
            return new Regex(pattern, options);
#endif
        }
    }

    private static class Parsers
    {
        // ReSharper disable once InconsistentNaming
        public static Func<string, OS> OS(Regex regex, string osReplacement, string v1Replacement, string v2Replacement,
            string v3Replacement, string v4Replacement)
        {
            // For variable replacements to be consistent the order of the linq statements are important ($1
            // is only available to the first 'from X in Replace(..)' and so forth) so a a bit of conditional
            // is required to get the creations to work. This is backed by unit tests
            if (v1Replacement == "$1")
            {
                if (v2Replacement == "$2")
                {
                    return Create(regex, from v1 in Replace(v1Replacement, "$1")
                        from v2 in Replace(v2Replacement, "$2")
                        from v3 in Replace(v3Replacement, "$3")
                        from v4 in Replace(v4Replacement, "$4")
                        from family in Replace(osReplacement, "$5")
                        select new OS(family, v1, v2, v3, v4));
                }

                return Create(regex, from v1 in Replace(v1Replacement, "$1")
                    from family in Replace(osReplacement, "$2")
                    from v2 in Replace(v2Replacement, "$3")
                    from v3 in Replace(v3Replacement, "$4")
                    from v4 in Replace(v4Replacement, "$5")
                    select new OS(family, v1, v2, v3, v4));
            }

            return Create(regex, from family in Replace(osReplacement, "$1")
                from v1 in Replace(v1Replacement, "$2")
                from v2 in Replace(v2Replacement, "$3")
                from v3 in Replace(v3Replacement, "$4")
                from v4 in Replace(v4Replacement, "$5")
                select new OS(family, v1, v2, v3, v4));
        }

        public static Func<string, Device> Device(Regex regex, string familyReplacement, string brandReplacement,
            string modelReplacement)
        {
            return Create(regex, from family in ReplaceAll(familyReplacement)
                from brand in ReplaceAll(brandReplacement)
                from model in ReplaceAll(modelReplacement)
                select new Device(family, brand, model));
        }

        public static Func<string, UserAgent> UserAgent(Regex regex, string familyReplacement, string majorReplacement,
            string minorReplacement, string patchReplacement)
        {
            return Create(regex, from family in Replace(familyReplacement, "$1")
                from v1 in Replace(majorReplacement, "$2")
                from v2 in Replace(minorReplacement, "$3")
                from v3 in Replace(patchReplacement, "$4")
                select new UserAgent(family, v1, v2, v3));
        }

        private static Func<Match, IEnumerator<int>, string> Replace(string replacement)
        {
            return replacement != null ? Select(_ => replacement) : Select();
        }

        private static Func<Match, IEnumerator<int>, string> Replace(
            string replacement, string token)
        {
            return replacement != null && replacement.Contains(token)
                ? Select(s => s != null ? replacement.ReplaceFirstOccurence(token, s) : replacement)
                : Replace(replacement);
        }

        private static readonly string[] _allReplacementTokens = new string[]
        {
            "$1", "$2", "$3", "$4", "$5", "$6", "$7", "$8", "$9",
        };

        private static Func<Match, IEnumerator<int>, string> ReplaceAll(string replacement)
        {
            if (replacement == null)
                return Select();

            string ReplaceFunction(string replacementString, string matchedGroup, string token)
            {
                return matchedGroup != null
                    ? replacementString.ReplaceFirstOccurence(token, matchedGroup)
                    : replacementString;
            }

            return (m, num) =>
            {
                var finalString = replacement;
                if (finalString.Contains("$"))
                {
                    var groups = m.Groups;
                    for (int i = 0; i < _allReplacementTokens.Length; i++)
                    {
                        int tokenNumber = i + 1;
                        string token = _allReplacementTokens[i];
                        if (finalString.Contains(token))
                        {
                            var replacementText = string.Empty;
                            Group group;
                            if (tokenNumber <= groups.Count && (group = groups[tokenNumber]).Success)
                                replacementText = group.Value;

                            finalString = ReplaceFunction(finalString, replacementText, token);
                        }

                        if (!finalString.Contains("$"))
                            break;
                    }
                }

                return finalString;
            };
        }

        private static Func<Match, IEnumerator<int>, string> Select()
        {
            return Select(v => v);
        }

        private static Func<Match, IEnumerator<int>, T> Select<T>(Func<string, T> selector)
        {
            return (m, num) =>
            {
                if (!num.MoveNext()) throw new InvalidOperationException();
                var groups = m.Groups;
                Group group;
                return selector(num.Current <= groups.Count && (group = groups[num.Current]).Success
                    ? group.Value
                    : null);
            };
        }

        private static Func<string, T> Create<T>(Regex regex, Func<Match, IEnumerator<int>, T> binder)
        {
            return input =>
            {
#if REGEX_MATCHTIMEOUT
                    try
                    {
                        var m = regex.Match(input);
                        var num = Generate(1, n => n + 1);
                        return m.Success ? binder(m, num) : default(T);
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        // we'll simply swallow this exception and return the default (non-matched)
                        return default(T);
                    }
#else
                var m = regex.Match(input);
                var num = Generate(1, n => n + 1);
                return m.Success ? binder(m, num) : default(T);
#endif
            };
        }

        private static IEnumerator<T> Generate<T>(T initial, Func<T, T> next)
        {
            for (var state = initial;; state = next(state))
                yield return state;
            // ReSharper disable once FunctionNeverReturns
        }
    }
}

internal static class RegexBinderBuilder
{
    public static Func<Match, IEnumerator<int>, TResult> SelectMany<T1, T2, TResult>(
        this Func<Match, IEnumerator<int>, T1> binder,
        Func<T1, Func<Match, IEnumerator<int>, T2>> continuation,
        Func<T1, T2, TResult> projection)
    {
        return (m, num) =>
        {
            T1 bound = binder(m, num);
            T2 continued = continuation(bound)(m, num);
            TResult projected = projection(bound, continued);
            return projected;
        };
    }
}

internal static class StringExtensions
{
    public static string ReplaceFirstOccurence(this string input, string search, string replacement)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        var index = input.IndexOf(search, StringComparison.Ordinal);
        return index >= 0
            ? input.Substring(0, index) + replacement + input.Substring(index + search.Length)
            : input;
    }
}

internal static class DictionaryExtensions
{
    public static TValue Find<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
    {
        if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));
        return dictionary.TryGetValue(key, out var result) ? result : default(TValue);
    }
}