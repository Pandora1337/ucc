namespace ucc.Models;

public class StorageEstimate
{
    public long Usage { get; set; }
    public long Quota { get; set; }
    // public Dictionary<string, long>? UsageDetails { get; set; }

    public float GetPercent(long portion)
    {
        return (float)portion / Quota * 100;
    }

    public static string Get2SigFig(float num)
    {
        return $"{num:0.##}";
    }

    public static string GetHumanReadable(long bytes)
    {
        if (bytes < 1024)
            return bytes + " B";

        double size = bytes;
        int order = 0;
        string[] units = ["B", "KB", "MB", "GB", "TB"]; // who has TBs for browsers???

        while (size >= 1024 && order < units.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {units[order]}";
    }
}
