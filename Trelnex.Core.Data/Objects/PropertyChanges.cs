using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;

namespace Trelnex.Core.Data;

/// <summary>
/// Compares JsonNode instances and generates PropertyChange arrays by analyzing differences.
/// </summary>
internal static class PropertyChanges
{
    #region Private Static Fields

    // Configuration for JSON diff operations
    private static readonly JsonDiffOptions _jsonDiffOptions = new()
    {
        JsonElementComparison = JsonElementComparison.Semantic,
        ArrayObjectItemMatchByPosition = true,
        SuppressDetectArrayMove = true
    };

    private static readonly JsonPatchDeltaFormatter _jsonPatchDeltaFormatter = new();

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Compares two JsonNode instances and returns an array of PropertyChange objects for detected differences.
    /// </summary>
    /// <param name="initialJsonNode">The initial state JsonNode.</param>
    /// <param name="currentJsonNode">The current state JsonNode.</param>
    /// <returns>Array of PropertyChange objects representing differences, or null if no changes.</returns>
    public static PropertyChange[]? Compare(
        JsonNode initialJsonNode,
        JsonNode currentJsonNode)
    {
        // Create indexed access to the initial state
        var initialJsonNodePath = JsonNodePath.Parse(initialJsonNode);

        // Generate diff using JSON Patch format
        var diff = initialJsonNode.Diff(
            right: currentJsonNode,
            formatter: _jsonPatchDeltaFormatter,
            options: _jsonDiffOptions);

        if (diff == null) return null;
        if (diff is not JsonArray diffArray || diffArray.Count == 0) return null;

        var changes = new List<PropertyChange>();

        // Process each diff operation
        foreach (var diffNode in diffArray)
        {
            if (diffNode is not JsonObject diffObject) continue;

            var op = diffObject["op"]?.GetValue<string>();
            if (op is null) continue;

            var path = diffObject["path"]?.GetValue<string>();
            if (path is null) continue;

            switch (op)
            {
                case "add":
                    // Process addition operations
                    if (diffObject["value"] is JsonArray addArray)
                    {
                        AddArrayChanges(changes, path, null, addArray);
                    }
                    else if (diffObject["value"] is JsonObject addObject)
                    {
                        AddObjectChanges(changes, path, null, addObject);
                    }
                    else
                    {
                        var addChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = null,
                            NewValue = GetValue(diffObject["value"])
                        };

                        changes.Add(addChange);
                    }

                    break;

                case "copy":
                    // Process copy operations from one path to another
                    var copyFromPath = diffObject["from"]?.GetValue<string>();
                    if (copyFromPath is null) break;

                    var copyFromNode = initialJsonNodePath.Get(copyFromPath);
                    if (copyFromNode is JsonArray copyFromArray)
                    {
                        AddArrayChanges(changes, path, null, copyFromArray);
                    }
                    else if (copyFromNode is JsonObject copyFromObject)
                    {
                        AddObjectChanges(changes, path, null, copyFromObject);
                    }
                    else
                    {
                        var copyChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = null,
                            NewValue = GetValue(copyFromNode)
                        };

                        changes.Add(copyChange);
                    }

                    break;

                case "move":
                    // Process move operations (remove from source, add to destination)
                    var moveFromPath = diffObject["from"]?.GetValue<string>();
                    if (moveFromPath is null) break;

                    var moveFromNode = initialJsonNodePath.Get(moveFromPath);
                    if (moveFromNode is JsonArray moveFromArray)
                    {
                        AddArrayChanges(changes, moveFromPath, moveFromArray, null);
                    }
                    else if (moveFromNode is JsonObject moveFromObject)
                    {
                        AddObjectChanges(changes, moveFromPath, moveFromObject, null);
                    }
                    else
                    {
                        var moveFromChange = new PropertyChange
                        {
                            PropertyName = moveFromPath,
                            OldValue = GetValue(moveFromNode),
                            NewValue = null
                        };

                        changes.Add(moveFromChange);
                    }

                    // Process addition at destination
                    if (diffObject["value"] is JsonArray moveToArray)
                    {
                        AddArrayChanges(changes, path, null, moveToArray);
                    }
                    else if (diffObject["value"] is JsonObject moveToObject)
                    {
                        AddObjectChanges(changes, path, null, moveToObject);
                    }
                    else
                    {
                        var moveToChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = null,
                            NewValue = GetValue(diffObject["value"])
                        };

                        changes.Add(moveToChange);
                    }

                    break;

                case "remove":
                    // Process removal operations
                    if (initialJsonNodePath.Get(path) is JsonArray removeArray)
                    {
                        AddArrayChanges(changes, path, removeArray, null);
                    }
                    else if (initialJsonNodePath.Get(path) is JsonObject removeObject)
                    {
                        AddObjectChanges(changes, path, removeObject, null);
                    }
                    else
                    {
                        var removeChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = GetValue(initialJsonNodePath.Get(path)),
                            NewValue = null
                        };

                        changes.Add(removeChange);
                    }

                    break;

                case "replace":
                    // Process replacement operations
                    if (initialJsonNodePath.Get(path) is JsonArray || diffObject["value"] is JsonArray)
                    {
                        AddArrayChanges(
                            changes,
                            path,
                            initialJsonNodePath.Get(path) as JsonArray,
                            diffObject["value"] as JsonArray);
                    }
                    else if (initialJsonNodePath.Get(path) is JsonObject || diffObject["value"] is JsonObject)
                    {
                        AddObjectChanges(
                            changes,
                            path,
                            initialJsonNodePath.Get(path) as JsonObject,
                            diffObject["value"] as JsonObject);
                    }
                    else
                    {
                        var replaceChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = GetValue(initialJsonNodePath.Get(path)),
                            NewValue = GetValue(diffObject["value"])
                        };

                        changes.Add(replaceChange);
                    }

                    break;
            }
        }

        // Consolidate and sort the changes
        return MergeAndSortPropertyChanges(changes);
    }

    /// <summary>
    /// Processes array changes by comparing elements at each index and creating PropertyChange entries.
    /// </summary>
    /// <param name="changes">List to add PropertyChange entries to.</param>
    /// <param name="basePath">Base JSON Pointer path for the array.</param>
    /// <param name="oldJsonArray">Old array state, or null for additions.</param>
    /// <param name="newJsonArray">New array state, or null for removals.</param>
    private static void AddArrayChanges(
        List<PropertyChange> changes,
        string basePath,
        JsonArray? oldJsonArray,
        JsonArray? newJsonArray)
    {
        // Compare elements up to the maximum length of either array
        var oldCount = oldJsonArray?.Count ?? 0;
        var newCount = newJsonArray?.Count ?? 0;
        var maxCount = Math.Max(oldCount, newCount);

        for (var index = 0; index < maxCount; index++)
        {
            // Build path for this array element
            var path = $"{basePath}/{index}";

            // Get elements at current index
            var oldJsonNode = index < oldCount ? oldJsonArray![index] : null;
            var newJsonNode = index < newCount ? newJsonArray![index] : null;

            // Process based on element types
            if (oldJsonNode is JsonArray || newJsonNode is JsonArray)
            {
                AddArrayChanges(changes, path, oldJsonNode as JsonArray, newJsonNode as JsonArray);
            }
            else if (oldJsonNode is JsonObject || newJsonNode is JsonObject)
            {
                AddObjectChanges(changes, path, oldJsonNode as JsonObject, newJsonNode as JsonObject);
            }
            else
            {
                // Create change for primitive values
                changes.Add(new PropertyChange
                {
                    PropertyName = path,
                    OldValue = GetValue(oldJsonNode),
                    NewValue = GetValue(newJsonNode)
                });
            }
        }
    }

    /// <summary>
    /// Processes object changes by comparing properties and creating PropertyChange entries for leaf values.
    /// </summary>
    /// <param name="changes">List to add PropertyChange entries to.</param>
    /// <param name="basePath">Base JSON Pointer path for the object.</param>
    /// <param name="oldJsonObject">Old object state, or null for additions.</param>
    /// <param name="newJsonObject">New object state, or null for removals.</param>
    private static void AddObjectChanges(
        List<PropertyChange> changes,
        string basePath,
        JsonObject? oldJsonObject,
        JsonObject? newJsonObject)
    {
        // Get all property keys from both objects
        var keys = new HashSet<string>();
        if (oldJsonObject != null) keys.UnionWith(oldJsonObject.Select(kvp => kvp.Key));
        if (newJsonObject != null) keys.UnionWith(newJsonObject.Select(kvp => kvp.Key));

        foreach (var key in keys)
        {
            var path = $"{basePath}/{key}";
            var oldJsonNode = oldJsonObject?.TryGetPropertyValue(key, out var oldNode) == true ? oldNode : null;
            var newJsonNode = newJsonObject?.TryGetPropertyValue(key, out var newNode) == true ? newNode : null;

            // Process based on property types
            if (oldJsonNode is JsonArray || newJsonNode is JsonArray)
            {
                AddArrayChanges(changes, path, oldJsonNode as JsonArray, newJsonNode as JsonArray);
            }
            else if (oldJsonNode is JsonObject || newJsonNode is JsonObject)
            {
                AddObjectChanges(changes, path, oldJsonNode as JsonObject, newJsonNode as JsonObject);
            }
            else
            {
                // Create change for primitive values
                changes.Add(new PropertyChange
                {
                    PropertyName = path,
                    OldValue = GetValue(oldJsonNode),
                    NewValue = GetValue(newJsonNode)
                });
            }
        }
    }

    /// <summary>
    /// Attempts to extract the most appropriate numeric type from a JsonNode.
    /// </summary>
    /// <param name="node">JsonNode containing a numeric value.</param>
    /// <returns>The extracted numeric value or null if extraction fails.</returns>
    private static dynamic? GetNumber(JsonNode node)
    {
        if (node is not JsonValue jsonValue) return null;

        // Try different numeric types in order of preference
        if (jsonValue.TryGetValue<int>(out var intValue)) return intValue;
        if (jsonValue.TryGetValue<long>(out var longValue)) return longValue;
        if (jsonValue.TryGetValue<ulong>(out var ulongValue)) return ulongValue;
        if (jsonValue.TryGetValue<float>(out var floatValue)) return floatValue;
        if (jsonValue.TryGetValue<double>(out var doubleValue)) return doubleValue;
        if (jsonValue.TryGetValue<decimal>(out var decimalValue)) return decimalValue;

        return null;
    }

    /// <summary>
    /// Extracts a primitive .NET value from a JsonNode for use in PropertyChange objects.
    /// </summary>
    /// <param name="node">JsonNode to extract value from.</param>
    /// <returns>Extracted primitive value or null.</returns>
    private static dynamic? GetValue(JsonNode? node)
    {
        if (node == null) return null;

        return node.GetValueKind() switch
        {
            JsonValueKind.String => node.GetValue<string>(),
            JsonValueKind.Number => GetNumber(node),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    /// <summary>
    /// Merges PropertyChange entries that target the same path and sorts by property name.
    /// </summary>
    /// <param name="changes">List of PropertyChange entries to merge and sort.</param>
    /// <returns>Sorted array of merged PropertyChange entries.</returns>
    private static PropertyChange[] MergeAndSortPropertyChanges(
        List<PropertyChange> changes)
    {
        // Group changes by property path
        var changesByPath = changes.GroupBy(change => change.PropertyName);
        var mergeChanges = new List<PropertyChange>();

        foreach (var group in changesByPath)
        {
            var pathChanges = group.ToList();

            if (pathChanges.Count == 1)
            {
                mergeChanges.Add(pathChanges[0]);
                continue;
            }

            // Merge remove and add operations for the same path
            var removeChange = pathChanges.FirstOrDefault(c => c.NewValue == null);
            var addChange = pathChanges.FirstOrDefault(c => c.OldValue == null);

            var mergeChange = new PropertyChange
            {
                PropertyName = group.Key,
                OldValue = removeChange?.OldValue,
                NewValue = addChange?.NewValue
            };

            // Only include changes where values actually differ
            if (Equals(mergeChange.OldValue, mergeChange.NewValue) is false)
            {
                mergeChanges.Add(mergeChange);
            }
        }

        return mergeChanges.OrderBy(c => c.PropertyName).ToArray();
    }

    #endregion
}
