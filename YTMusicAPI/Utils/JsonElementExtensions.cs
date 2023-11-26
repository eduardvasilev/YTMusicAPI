using System.Text.Json;

namespace YTMusicAPI.Utils;

internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty(propertyName, out var result) &&
            result.ValueKind != JsonValueKind.Null &&
            result.ValueKind != JsonValueKind.Undefined)
        {
            return result;
        }

        return null;
    }
    public static JsonElement.ArrayEnumerator? EnumerateArrayOrNull(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
            : null;

    public static JsonElement.ArrayEnumerator EnumerateArrayOrEmpty(this JsonElement element) =>
        element.EnumerateArrayOrNull() ?? default;

    public static JsonElement.ObjectEnumerator? EnumerateObjectOrNull(this JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element.EnumerateObject()
            : null;

    public static JsonElement.ObjectEnumerator EnumerateObjectOrEmpty(this JsonElement element) =>
        element.EnumerateObjectOrNull() ?? default;

    public static IEnumerable<JsonElement> EnumerateDescendantProperties(
        this JsonElement element,
        string propertyName)
    {
        var property = GetPropertyOrNull(element, propertyName);
        if (property is not null)
            yield return property.Value;

        var deepArrayDescendants = element
            .EnumerateArrayOrEmpty()
            .SelectMany(j => j.EnumerateDescendantProperties(propertyName));

        foreach (var deepDescendant in deepArrayDescendants)
        {
            yield return deepDescendant;
        }

        var deepObjectDescendants = element
            .EnumerateObjectOrEmpty()
            .SelectMany(j => j.Value.EnumerateDescendantProperties(propertyName));

        foreach (var deepDescendant in deepObjectDescendants)
        {
            yield return deepDescendant;
        }
    }
}