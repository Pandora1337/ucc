namespace ucc.Models;

public class Icon
{
    public string Id { get; set; } = "";
    public string FileType { get; set; } = "";
    public required byte[] FileBytes { get; set; }
}