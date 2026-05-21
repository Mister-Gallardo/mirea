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
    /// Относительный путь разрешается относительно рабочей директории приложения.
    /// </summary>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // Если путь абсолютный — используем как есть.
        // Если относительный — разрешаем относительно AppContext.BaseDirectory (выходной каталог),
        // что соответствует поведению AddJsonFile / AddXmlFile / AddIniFile.
        var basePath = AppContext.BaseDirectory;
        var fullPath = Path.IsPathRooted(FilePath)
            ? FilePath
            : Path.GetFullPath(FilePath, basePath);

        return new TextConfigurationProvider(fullPath);
    }
}
