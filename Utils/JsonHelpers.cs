using System.Text.Json.Nodes;

namespace PregoStripeMapper.Utils;

public static class JsonHelpers
{
    // Gets a property value as a string (falls back to ToString for non-string JSON values)
    public static string? GetString(this JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null) return null;
        if (node is JsonValue v)
        {
            if (v.TryGetValue<string?>(out var s)) return s;
            return v.ToString();
        }
        return node.ToString();
    }

    // Traverses a dotted JSON path and returns the JsonNode at that location, or null if missing.
    public static JsonNode? GetPath(this JsonObject obj, string dotPath)
    {
        var parts = dotPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
        JsonNode? cur = obj;

        foreach (var p in parts)
        {
            if (cur is not JsonObject curObj) return null;
            if (!curObj.TryGetPropertyValue(p, out cur)) return null;
        }
        return cur;
    }

    // Gets a dotted-path JSON value as a string (falls back to ToString for non-string JSON values)
    public static string? GetPathString(this JsonObject obj, string dotPath)
    {
        var n = obj.GetPath(dotPath);
        if (n is null) return null;

        if (n is JsonValue v)
        {
            if (v.TryGetValue<string?>(out var s)) return s;
            return v.ToString();
        }
        return n.ToString();
    }

    // Gets a boolean property value, returning false if missing or not a bool.
    public static bool GetBool(this JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null) return false;
        try { return node.GetValue<bool>(); } catch { return false; }
    }

    // Gets a long property value, returning null if missing or not a number.
    public static long? GetLong(this JsonObject obj, string key)
    {
        if (!obj.TryGetPropertyValue(key, out var node) || node is null) return null;
        try { return node.GetValue<long>(); } catch { return null; }
    }

    // Sorts a JSON array in-place by a string key to make output deterministic.
    public static void SortArrayByKey(this JsonObject output, string arrayKey, string sortKey)
    {
        if (output[arrayKey] is not JsonArray arr) return;

        var items = arr
            .OfType<JsonObject>()
            .OrderBy(o => o.GetString(sortKey))
            .Cast<JsonNode?>()
            .ToList();

        arr.Clear();
        foreach (var it in items) arr.Add(it);
    }
}