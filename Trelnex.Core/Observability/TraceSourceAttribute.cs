using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace Trelnex.Core.Observability;

/// <summary>
/// Attribute to mark a method to be traced.
/// </summary>
/// <param name="sourceName">The name of the activity source.</param>
[AttributeUsage(AttributeTargets.Method)]
[PSerializable]
public class TraceAttribute(
    string? sourceName = null) : OnMethodBoundaryAspect
{
    /// <summary>
    /// A thread-safe collection of <see cref="string"/> to <see cref="ActivitySource"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<ActivitySource>> _activitySourcesByName = new();

    public override void OnEntry(
        MethodExecutionArgs args)
    {
        // get the source name
        sourceName = GetSourceName(args.Method, sourceName);
        if (sourceName is null) return;

        // get the activity name
        var activityName = GetActivityName(args.Method);
        if (activityName is null) return;

        // get the activity source
        var activitySource = GetActivitySource(sourceName);

        // start the activity
        var activity = activitySource.StartActivity(
            name: activityName,
            kind: ActivityKind.Internal);

        if (activity is not null)
        {
            // add the method parameters as tags
            var parameters = args.Method.GetParameters();

            for (var index = 0; index < args.Arguments.Count; index++)
            {
                var parameter = parameters[index];

                // skip the parameter if it is not valid
                if (parameter.Name is null) continue;

                // skip the parameter if it is not marked with the TraceIncludeAttribute
                if (parameter.GetCustomAttribute<TraceIncludeAttribute>() is null) continue;

                activity.SetTag(parameter.Name, args.Arguments[index]);
            }
        }

        args.MethodExecutionTag = activity;
    }

    public override void OnException(
        MethodExecutionArgs args)
    {
        if (args.MethodExecutionTag is Activity activity)
        {
            activity.SetStatus(ActivityStatusCode.Error, args.Exception.Message);
            activity.Dispose();
        }
    }

    public override void OnSuccess(
        MethodExecutionArgs args)
    {
        if (args.MethodExecutionTag is Activity activity)
        {
            activity.Dispose();
        }
    }

    private static string? GetActivityName(
        MethodBase method)
    {
        if (method.DeclaringType is null) return null;

        return $"{method.DeclaringType.Name}.{method.Name}";
    }

    private static string? GetSourceName(
        MethodBase method,
        string? sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName) is false) return sourceName;

        if (method.DeclaringType is null) return null;

        return method.DeclaringType.Assembly.GetName().Name;
    }

    /// <summary>
    /// Gets the <see cref="GetActivitySource"/> for the specified name.
    /// </summary>
    /// <param name="sourceName">The name for the activity source.</param>
    /// <returns>The <see cref="ActivitySource"/>.</returns>
    private static ActivitySource GetActivitySource(
        string sourceName)
    {
        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var lazyClient =
            _activitySourcesByName.GetOrAdd(
                key: sourceName,
                value: new Lazy<ActivitySource>(() =>
                {
                    return new ActivitySource(sourceName);
                }));

        return lazyClient.Value;
    }
}
