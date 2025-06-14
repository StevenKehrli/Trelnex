using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;

namespace Trelnex.Core.Data;

/// <summary>
/// Compares JsonNode instances and generates PropertyChange arrays using RFC 6902 JSON Patch operations.
/// </summary>
/// <remarks>
/// Creates granular PropertyChange entries for leaf properties using JSON Pointer paths.
/// Consolidates array reordering operations by merging remove/add pairs for the same path.
///
/// Supported operations: add, copy, move, remove, replace (test operations ignored).
/// </remarks>
internal static class PropertyChanges
{
    #region Private Static Fields

    /// <summary>
    /// Options for JSON diffing operations.
    /// </summary>
    /// <remarks>
    /// Uses semantic comparison and intelligent array move detection for accurate diff generation.
    /// </remarks>
    private static readonly JsonDiffOptions _jsonDiffOptions = new()
    {
        JsonElementComparison = JsonElementComparison.Semantic,

        // Use position-based matching - simpler and more predictable
        ArrayObjectItemMatchByPosition = true,

        // Disable move detection - PropertyChange represents value transitions, not moves
        SuppressDetectArrayMove = true
    };

    private static readonly JsonPatchDeltaFormatter _jsonPatchDeltaFormatter = new();

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Compares two JsonNode instances and returns PropertyChange array for detected differences.
    /// </summary>
    /// <param name="initialJsonNode">Initial state</param>
    /// <param name="currentJsonNode">Current state</param>
    /// <returns>PropertyChange array for leaf-level differences, or null if no changes</returns>
    /// <remarks>
    /// Uses RFC 6902 JSON Patch diff to detect changes, then creates separate PropertyChange
    /// entries for each modified leaf property. Automatically consolidates array reordering.
    /// </remarks>
    public static PropertyChange[]? Compare(
        JsonNode initialJsonNode,
        JsonNode currentJsonNode)
    {
        // Create indexed lookup for efficient initial state access
        var initialJsonNodePath = JsonNodePath.Parse(initialJsonNode);

        // Generate RFC 6902 JSON Patch diff
        var diff = initialJsonNode.Diff(
            right: currentJsonNode,
            formatter: _jsonPatchDeltaFormatter,
            options: _jsonDiffOptions);

        if (diff == null) return null;
        if (diff is not JsonArray diffArray || diffArray.Count == 0) return null;

        var changes = new List<PropertyChange>();

        // Process each JSON Patch operation
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
                    // Handle arrays, objects, and primitive values
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
                    // RFC 6902 Copy Operation: {"op": "copy", "from": "/User/Name", "path": "/User/DisplayName"}
                    // Copies value from source path to destination path
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
                        // For simple values, create a single PropertyChange
                        var copyChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = null, // Property did not exist before
                            NewValue = GetValue(copyFromNode)
                        };

                        changes.Add(copyChange);
                    }

                    break;

                case "move":
                    // RFC 6902 Move Operation: {"op": "move", "from": "/User/TempName", "path": "/User/Name"}
                    // Removes value from source path and adds it to destination path
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
                            NewValue = null // Property no longer exists at source
                        };

                        changes.Add(moveFromChange);
                    }

                    // Create change for addition to destination location
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
                            OldValue = null, // Property did not exist before
                            NewValue = GetValue(diffObject["value"])
                        };

                        changes.Add(moveToChange);
                    }

                    break;

                case "remove":
                    // RFC 6902 Remove Operation: {"op": "remove", "path": "/trackedSettingsWithAttribute"}
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
                        // For simple values, create a single PropertyChange
                        var removeChange = new PropertyChange
                        {
                            PropertyName = path,
                            OldValue = GetValue(initialJsonNodePath.Get(path)),
                            NewValue = null // Property no longer exists
                        };

                        changes.Add(removeChange);
                    }

                    break;

                case "replace":
                    // RFC 6902 Replace Operation: {"op": "replace", "path": "/settings/name", "value": "NewValue"}
                    // Handle arrays, objects, and primitive replacement
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

        // Consolidate remove/add pairs and sort for consistent output
        return MergeAndSortPropertyChanges(changes);
    }

    /// <summary>
    /// Recursively processes array elements to create PropertyChange entries for each element and nested properties.
    /// </summary>
    /// <param name="changes">Collection to add PropertyChange entries to</param>
    /// <param name="basePath">Base JSON Pointer path (e.g., "/trackedSettingsArray")</param>
    /// <param name="oldJsonArray">Old array state (null for additions)</param>
    /// <param name="newJsonArray">New array state (null for removals)</param>
    /// <remarks>
    /// Creates PropertyChange entries for array elements using indexed JSON Pointer paths.
    /// Handles array length changes, reordering, and element modifications by comparing elements at each index.
    /// </remarks>
    private static void AddArrayChanges(
        List<PropertyChange> changes,
        string basePath,
        JsonArray? oldJsonArray,
        JsonArray? newJsonArray)
    {
        // Determine the maximum index to process (covers both arrays)
        var oldCount = oldJsonArray?.Count ?? 0;
        var newCount = newJsonArray?.Count ?? 0;
        var maxCount = Math.Max(oldCount, newCount);

        for (var index = 0; index < maxCount; index++)
        {
            // Build JSON Pointer path for this array index (e.g., "/trackedSettingsArray/0")
            var path = $"{basePath}/{index}";

            // Get elements at current index from both arrays
            var oldJsonNode = index < oldCount ? oldJsonArray![index] : null;
            var newJsonNode = index < newCount ? newJsonArray![index] : null;

            // Handle different element type combinations
            if (oldJsonNode is JsonArray || newJsonNode is JsonArray)
            {
                // Element is/was an array - recurse into nested array
                AddArrayChanges(changes, path, oldJsonNode as JsonArray, newJsonNode as JsonArray);
            }
            else if (oldJsonNode is JsonObject || newJsonNode is JsonObject)
            {
                // Element is/was an object - recurse into object properties
                AddObjectChanges(changes, path, oldJsonNode as JsonObject, newJsonNode as JsonObject);
            }
            else
            {
                // Create PropertyChange for primitive values
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
    /// Recursively processes object properties to create PropertyChange entries for leaf values only.
    /// </summary>
    /// <param name="changes">Collection to add PropertyChange entries to</param>
    /// <param name="basePath">Base JSON Pointer path</param>
    /// <param name="oldJsonObject">Old object state (null for additions)</param>
    /// <param name="newJsonObject">New object state (null for removals)</param>
    /// <remarks>
    /// Creates PropertyChange entries only for primitive values, recursing into nested objects.
    /// Builds JSON Pointer paths incrementally during recursion.
    /// </remarks>
    private static void AddObjectChanges(
        List<PropertyChange> changes,
        string basePath,
        JsonObject? oldJsonObject,
        JsonObject? newJsonObject)
    {
        // Get all unique property keys from both states
        var keys = new HashSet<string>();
        if (oldJsonObject != null) keys.UnionWith(oldJsonObject.Select(kvp => kvp.Key));
        if (newJsonObject != null) keys.UnionWith(newJsonObject.Select(kvp => kvp.Key));

        foreach (var key in keys)
        {
            var path = $"{basePath}/{key}";
            var oldJsonNode = oldJsonObject?.TryGetPropertyValue(key, out var oldNode) == true ? oldNode : null;
            var newJsonNode = newJsonObject?.TryGetPropertyValue(key, out var newNode) == true ? newNode : null;

            // Handle different element type combinations
            if (oldJsonNode is JsonArray || newJsonNode is JsonArray)
            {
                // Element is/was an array - recurse into nested array
                AddArrayChanges(changes, path, oldJsonNode as JsonArray, newJsonNode as JsonArray);
            }
            else if (oldJsonNode is JsonObject || newJsonNode is JsonObject)
            {
                // Element is/was an object - recurse into object properties
                AddObjectChanges(changes, path, oldJsonNode as JsonObject, newJsonNode as JsonObject);
            }
            else
            {
                // Create PropertyChange for primitive values
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
    /// Extracts the most appropriate .NET numeric type from a JsonNode.
    /// </summary>
    /// <param name="node">JsonNode containing numeric value</param>
    /// <returns>Most appropriate numeric type or null</returns>
    /// <remarks>
    /// Tries types in order: int → long → ulong → float → double → decimal
    /// </remarks>
    private static dynamic? GetNumber(JsonNode node)
    {
        if (node is not JsonValue jsonValue) return null;

        // Try common integer types
        if (jsonValue.TryGetValue<int>(out var intValue)) return intValue;
        if (jsonValue.TryGetValue<long>(out var longValue)) return longValue;
        if (jsonValue.TryGetValue<ulong>(out var ulongValue)) return ulongValue;

        // Try floating point and decimal types
        if (jsonValue.TryGetValue<float>(out var floatValue)) return floatValue;
        if (jsonValue.TryGetValue<double>(out var doubleValue)) return doubleValue;
        if (jsonValue.TryGetValue<decimal>(out var decimalValue)) return decimalValue;

        return null;
    }

    /// <summary>
    /// Extracts .NET primitive value from JsonNode for PropertyChange entries.
    /// </summary>
    /// <param name="node">JsonNode to extract value from</param>
    /// <returns>Corresponding .NET primitive type or null</returns>
    /// <remarks>
    /// Converts to: string, numeric types, bool, or null. Returns null for Object/Array types.
    /// </remarks>
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
    /// Merges PropertyChange entries targeting the same path and sorts results by PropertyName.
    /// </summary>
    /// <param name="changes">PropertyChange entries to merge</param>
    /// <returns>Merged and sorted PropertyChange array</returns>
    /// <remarks>
    /// Consolidates remove/add pairs for the same path into single entries showing value transitions.
    /// Filters out no-op changes where old and new values are identical.
    /// </remarks>
    private static PropertyChange[] MergeAndSortPropertyChanges(
        List<PropertyChange> changes)
    {
        // Group by path to identify duplicates
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

            // Merge remove + add pairs (common in array reordering)
            var removeChange = pathChanges.FirstOrDefault(c => c.NewValue == null);
            var addChange = pathChanges.FirstOrDefault(c => c.OldValue == null);

            var mergeChange = new PropertyChange
            {
                PropertyName = group.Key,
                OldValue = removeChange?.OldValue,
                NewValue = addChange?.NewValue
            };

            // Only add if values actually differ
            if (Equals(mergeChange.OldValue, mergeChange.NewValue) is false)
            {
                mergeChanges.Add(mergeChange);
            }
        }

        return mergeChanges.OrderBy(c => c.PropertyName).ToArray();
    }

    #endregion
}
