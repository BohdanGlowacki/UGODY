namespace UGODY.Core.Entities;

public class Configuration
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; }
}
