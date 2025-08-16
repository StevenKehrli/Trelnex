using System.Text.Json.Nodes;

namespace Trelnex.Core.Data;

/// <summary>
/// Provides indexed access to JsonNode values using JSON Pointer paths.
/// </summary>
internal class JsonNodePath
{
    #region Private Fields

    // Dictionary that maps JSON Pointer paths to their corresponding JsonNode values
    private readonly Dictionary<string, JsonNode?> _jsonNodesByPath = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance with the specified path-to-node mappings.
    /// </summary>
    /// <param name="jsonNodesByPath">Dictionary mapping JSON Pointer paths to JsonNode values.</param>
    private JsonNodePath(
        Dictionary<string, JsonNode?> jsonNodesByPath)
    {
        _jsonNodesByPath = jsonNodesByPath;
    }

    #endregion

    #region Public Static Methods

    /// <summary>
    /// Parses a JsonNode and creates an indexed lookup structure for all paths within it.
    /// </summary>
    /// <param name="jsonNode">The JsonNode to parse and index.</param>
    /// <returns>A JsonNodePath instance that provides efficient path-based access.</returns>
    public static JsonNodePath Parse(
        JsonNode jsonNode)
    {
        var jsonNodesByPath = new Dictionary<string, JsonNode?>();

        // Queue for breadth-first traversal of the JSON structure
        var search = new Queue<(string Pointer, JsonNode? Value)>();

        // Start with the root node
        search.Enqueue((string.Empty, jsonNode));

        // Process all nodes in the JSON tree
        while (search.TryDequeue(out var current))
        {
            // Convert empty root path to "/" for JSON Pointer compliance
            var currentPointer = current.Pointer == string.Empty ? "/" : current.Pointer;
            jsonNodesByPath[currentPointer] = current.Value;

            // Add child nodes to the processing queue
            switch (current.Value)
            {
                case JsonObject jsonObject:
                    // Add each property as a child path
                    foreach (var kvp in jsonObject)
                    {
                        var pointer = $"{current.Pointer}/{kvp.Key}";
                        search.Enqueue((pointer, kvp.Value));
                    }
                    break;

                case JsonArray jsonArray:
                    // Add each array element with its index as a child path
                    for (int index = 0; index < jsonArray.Count; index++)
                    {
                        var pointer = $"{current.Pointer}/{index}";
                        search.Enqueue((pointer, jsonArray[index]));
                    }
                    break;

                // Leaf nodes (values) don't have children to process
            }
        }

        return new JsonNodePath(jsonNodesByPath);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the JsonNode at the specified JSON Pointer path.
    /// </summary>
    /// <param name="path">JSON Pointer path to retrieve.</param>
    /// <returns>The JsonNode at the path, or null if the path doesn't exist.</returns>
    public JsonNode? Get(
        string path)
    {
        // Perform dictionary lookup for the requested path
        return _jsonNodesByPath.TryGetValue(path, out var value) ? value : null;
    }

    #endregion
}
