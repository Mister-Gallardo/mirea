using Microsoft.Extensions.Configuration;

namespace CampusHub.ConfigCenter.Configuration;

/// <summary>
/// Источник конфигурации для TextConfigurationProvider.
/// Хранит путь к файлу и создаёт экземпляр провайдера.
/// </summary>
public class TextConfigurationSource : IConfigurationSource
{
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Строит провайдер конфигурации для данного источника.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // Если путь относительный — разрешаем относительно базовой директории билдера
        var fullPath = Path.IsPathRooted(FilePath)
            ? FilePath
            : Path.Combine(builder.GetFileProvider().GetFileInfo(".").PhysicalPath ?? "", FilePath);

        return new TextConfigurationProvider(fullPath);
    }
}
