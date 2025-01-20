using System.Net;
using System.Text.RegularExpressions;

namespace Trelnex.Core;

public static partial class HttpStatusCodeExtensions
{
    public static string ToReason(
        this HttpStatusCode httpStatusCode)
    {
        return HttpStatusCodeRegex().Replace(httpStatusCode.ToString(), " $1");
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])")]
    private static partial Regex HttpStatusCodeRegex();
}
