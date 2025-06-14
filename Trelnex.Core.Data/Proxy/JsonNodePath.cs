using System.Text.Json.Nodes;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides efficient lookup of JsonNode values by JSON Pointer paths.
/// Builds an index of all paths in a JSON structure for O(1) access.
/// </summary>
/// <remarks>
/// This class parses a JsonNode tree and creates a dictionary mapping
/// JSON Pointer paths (like "/User/Name") to their corresponding JsonNode values.
/// Supports both objects and arrays in the JSON structure.
///
/// Uses JSON Pointer format as defined in RFC 6901: https://datatracker.ietf.org/doc/html/rfc6901
/// Array indices are represented as strings with forward slash separators (e.g., "/items/0").
///
/// Implementation inspired by: https://github.com/dotnet/runtime/issues/31068#issuecomment-2028930071
/// </remarks>
internal class JsonNodePath
{
    #region Private Fields

    /// <summary>
    /// Dictionary mapping JSON Pointer paths to their JsonNode values.
    /// </summary>
    /// <remarks>
    /// Key: JSON Pointer path (e.g., "/User/Name", "/items/0")
    /// Value: The JsonNode at that path, or null if the value is null
    /// </remarks>
    private readonly Dictionary<string, JsonNode?> _jsonNodesByPath = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor - use Parse() to create instances.
    /// </summary>
    /// <param name="jsonNodesByPath">Pre-built dictionary of paths to nodes</param>
    private JsonNodePath(
        Dictionary<string, JsonNode?> jsonNodesByPath)
    {
        _jsonNodesByPath = jsonNodesByPath;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Parses a JsonNode tree and creates an indexed path lookup structure.
    /// </summary>
    /// <param name="jsonNode">The root JsonNode to parse</param>
    /// <returns>JsonNodePath instance for efficient path-based lookups</returns>
    /// <remarks>
    /// Uses breadth-first traversal to visit all nodes in the JSON structure.
    /// Creates JSON Pointer paths following RFC 6901 format.
    /// </remarks>
    public static JsonNodePath Parse(
        JsonNode jsonNode)
    {
        var jsonNodesByPath = new Dictionary<string, JsonNode?>();

        // Use queue for breadth-first traversal of the JSON tree
        var search = new Queue<(string Pointer, JsonNode? Value)>();

        // Start with empty string - will be converted to "/" for root
        search.Enqueue((string.Empty, jsonNode));

        // Process all nodes in the JSON structure
        while (search.TryDequeue(out var current))
        {
            // Store root path as "/" for proper JSON Pointer format
            // All other paths are already in correct format from concatenation
            var currentPointer = current.Pointer == string.Empty ? "/" : current.Pointer;
            jsonNodesByPath[currentPointer] = current.Value;

            // Recursively process child nodes based on JSON type
            switch (current.Value)
            {
                case JsonObject jsonObject:
                    // Add all object properties to the queue for processing
                    foreach (var kvp in jsonObject)
                    {
                        // Build path: empty string + "/PropertyName" = "/PropertyName"
                        // or existing path + "/PropertyName" = "/Parent/PropertyName"
                        var pointer = $"{current.Pointer}/{kvp.Key}";
                        search.Enqueue((pointer, kvp.Value));
                    }
                    break;

                case JsonArray jsonArray:
                    // Add all array elements to the queue for processing
                    for (int index = 0; index < jsonArray.Count; index++)
                    {
                        // Build path: existing path + "/index" = "/arrayName/0"
                        var pointer = $"{current.Pointer}/{index}";
                        search.Enqueue((pointer, jsonArray[index]));
                    }
                    break;

                // JsonValue types (string, number, boolean, null) are leaf nodes
                // No further processing needed - they're stored in the dictionary above
            }
        }

        return new JsonNodePath(jsonNodesByPath);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Retrieves the JsonNode at the specified JSON Pointer path.
    /// </summary>
    /// <param name="path">JSON Pointer path (e.g., "/User/Name", "/items/0", "/")</param>
    /// <returns>The JsonNode at the specified path, or null if path not found</returns>
    /// <remarks>
    /// Provides O(1) lookup performance after initial parsing.
    /// Supports all valid JSON Pointer paths including root ("/").
    /// </remarks>
    public JsonNode? Get(
        string path)
    {
        // Direct dictionary lookup - no path traversal needed
        return _jsonNodesByPath.TryGetValue(path, out var value) ? value : null;
    }

    #endregion
}
