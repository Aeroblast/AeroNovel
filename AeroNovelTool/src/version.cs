class Version
{
    public static string date = "20221216";
    public static string codename = "Monika";
    public static string Sign()
    {
        return $"AeroNovelTool \"{codename}\" v{date}";
    }
}