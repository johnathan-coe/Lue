namespace LueGtk.Ui;

static class FontFamilies
{
    public static HashSet<string> Load()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var map = PangoCairo.FontMapHelper.GetDefault();
        var count = map.GetNItems();
        for (uint index = 0; index < count; index++)
        {
            if (map.GetObject(index) is Pango.FontFamily family)
                names.Add(family.GetName());
        }
        return names;
    }
}
