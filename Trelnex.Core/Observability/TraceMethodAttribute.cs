using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace Trelnex.Core.Observability;

/// <summary>
/// Attribute to mark a method for tracing with OpenTelemetry.
/// </summary>
/// <param name="sourceName">The name of the activity source.</param>
/// <remarks>
/// Creates spans for performance monitoring.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
[PSerializable]
public class TraceMethodAttribute(
    string? sourceName = null) : OnMethodBoundaryAspect
{
    #region Private Static Fields

    /// <summary>
    /// A thread-safe collection of <see cref="string"/> to <see cref="ActivitySource"/>.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Lazy<ActivitySource>> _activitySourcesByName = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes when entering the method.
    /// </summary>
    /// <param name="args">Arguments providing information about the method being executed.</param>
    public override void OnEntry(
        MethodExecutionArgs args)
    {
        // Get the source name.
        // If the source name is null, return.
        sourceName = GetSourceName(args.Method, sourceName);
        if (sourceName is null) return;

        // Get the activity name.
        // If the activity name is null, return.
        var activityName = GetActivityName(args.Method);
        if (activityName is null) return;

        // Get the activity source.
        var activitySource = GetActivitySource(sourceName);

        // Start the activity.
        var activity = activitySource.StartActivity(
            name: activityName,
            kind: ActivityKind.Internal);

        // If the activity is not null, add the method parameters as tags.
        if (activity is not null)
        {
            // Get the method parameters.
            var parameters = args.Method.GetParameters();

            // Iterate over the method parameters.
            for (var index = 0; index < args.Arguments.Count; index++)
            {
                // Get the parameter.
                var parameter = parameters[index];

                // Skip the parameter if it is not valid.
                if (parameter.Name is null) continue;

                // Skip the parameter if it is not marked with the TraceParameterAttribute.
                if (parameter.GetCustomAttribute<TraceParameterAttribute>() is null) continue;

                // Set the tag on the activity.
                activity.SetTag(parameter.Name, args.Arguments[index]);
            }
        }

        // Set the method execution tag.
        args.MethodExecutionTag = activity;
    }

    /// <summary>
    /// Executes when an exception is thrown during method execution.
    /// </summary>
    /// <param name="args">Arguments providing information about the method being executed and the exception.</param>
    public override void OnException(
        MethodExecutionArgs args)
    {
        // If the method execution tag is an activity, set the status to error and dispose the activity.
        if (args.MethodExecutionTag is Activity activity)
        {
            activity.SetStatus(ActivityStatusCode.Error, args.Exception.Message);
            activity.Dispose();
        }
    }

    /// <summary>
    /// Executes when the method completes successfully.
    /// </summary>
    /// <param name="args">Arguments providing information about the method being executed.</param>
    public override void OnSuccess(
        MethodExecutionArgs args)
    {
        // If the method execution tag is an activity, dispose the activity.
        if (args.MethodExecutionTag is Activity activity)
        {
            activity.Dispose();
        }
    }

    #endregion

    #region Private Static Methods

    /// <summary>
    /// Generates an activity name from the method information.
    /// </summary>
    /// <param name="method">The method being traced.</param>
    /// <returns>A formatted activity name in the form "ClassName.MethodName", or null if no declaring type.</returns>
    private static string? GetActivityName(
        MethodBase method)
    {
        // If the method's declaring type is null, return null.
        if (method.DeclaringType is null) return null;

        // Return the activity name.
        return $"{method.DeclaringType.Name}.{method.Name}";
    }

    /// <summary>
    /// Gets or creates an <see cref="ActivitySource"/> for the specified name.
    /// </summary>
    /// <param name="sourceName">The name for the activity source.</param>
    /// <returns>The <see cref="ActivitySource"/> for the specified name.</returns>
    /// <remarks>
    /// Uses thread-safe lazy initialization to ensure only one ActivitySource is created per name.
    /// </remarks>
    private static ActivitySource GetActivitySource(
        string sourceName)
    {
        // Thread-safe implementation to get or add source
        var lazyClient =
            _activitySourcesByName.GetOrAdd(
                key: sourceName,
                value: new Lazy<ActivitySource>(() =>
                {
                    // Create a new activity source.
                    return new ActivitySource(sourceName);
                }));

        // Return the activity source.
        return lazyClient.Value;
    }

    /// <summary>
    /// Determines the source name for the activity.
    /// </summary>
    /// <param name="method">The method being traced.</param>
    /// <param name="sourceName">The optional explicit source name.</param>
    /// <returns>The provided source name if specified, otherwise the assembly name, or null if neither is available.</returns>
    private static string? GetSourceName(
        MethodBase method,
        string? sourceName)
    {
        // If the source name is not null or whitespace, return the source name.
        if (string.IsNullOrWhiteSpace(sourceName) is false) return sourceName;

        // If the method's declaring type is null, return null.
        if (method.DeclaringType is null) return null;

        // Return the assembly name.
        return method.DeclaringType.Assembly.GetName().Name;
    }

    #endregion
}
