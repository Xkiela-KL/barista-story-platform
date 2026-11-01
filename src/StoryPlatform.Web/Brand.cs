namespace StoryPlatform.Web;

/// <summary>Splits the channel name into an accented prefix and the rest: "XKStories" → "XK" + "Stories".</summary>
public static class Brand
{
    public const string Fallback = "XKStories";

    public static string Prefix(string name)
    {
        var run = 0;
        while (run < name.Length && char.IsUpper(name[run])) run++;
        return run >= 2 && run < name.Length ? name[..(run - 1)] : "";
    }

    public static string Rest(string name) => name[Prefix(name).Length..];
}
