namespace CampusHub.ConfigCenter.Models;

/// <summary>
/// Настройки уведомлений — секция Notifications из notifications.ini
/// и in-memory коллекции (демонстрация конфликта ключей).
/// </summary>
public class NotificationOptions
{
    public const string Section = "Notifications";

    public string Sender { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
}
