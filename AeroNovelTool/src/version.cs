class Version
{
    public static string date = "20240225";
    public static string codename = "Monika";
    public static string Sign()
    {
        return $"AeroNovelTool \"{codename}\" v{date}";
    }
}