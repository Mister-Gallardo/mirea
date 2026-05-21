namespace CampusHub.ConfigCenter.Models;

/// <summary>
/// Настройки портала — корневая секция Portal из конфигурации.
/// Содержит вложенный объект Admin и коллекцию Modules.
/// </summary>
public class PortalOptions
{
    public const string Section = "Portal";

    public string Title { get; set; } = string.Empty;
    public string Semester { get; set; } = string.Empty;
    public string SupportEmail { get; set; } = string.Empty;
    public AdminOptions Admin { get; set; } = new();
    public List<string> Modules { get; set; } = new();
}
