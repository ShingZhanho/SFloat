namespace SFloat;

internal static class ListExtension {
    public static T Pop<T>(this List<T> list, int? index = null) {
        index ??= list.Count - 1;
        var item = list[index.Value];
        list.RemoveAt(index.Value);
        return item;
    }
}