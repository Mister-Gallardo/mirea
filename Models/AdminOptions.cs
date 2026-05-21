namespace CampusHub.ConfigCenter.Models;

/// <summary>
/// Настройки администратора портала — вложенная секция Portal:Admin.
/// </summary>
public class AdminOptions
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
