using Microsoft.Extensions.Localization;

namespace DotLocz.Demo.Locz;

public static class LoczExtensions
{
    public static string Get(this IStringLocalizer localizer, Enum key) =>
        localizer[key.ToString()];
}