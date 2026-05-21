using Microsoft.Extensions.Configuration;

namespace CampusHub.ConfigCenter.Configuration;

/// <summary>
/// Extension-метод для удобного подключения TextConfigurationSource
/// к IConfigurationBuilder в одну строку.
/// </summary>
public static class TextConfigurationExtensions
{
    /// <summary>
    /// Добавляет собственный провайдер конфигурации,
    /// читающий пары ключ/значение из текстового файла.
    /// </summary>
    /// <param name="builder">Текущий IConfigurationBuilder.</param>
    /// <param name="path">Путь к .txt файлу (относительный или абсолютный).</param>
    public static IConfigurationBuilder AddTextFile(
        this IConfigurationBuilder builder,
        string path)
    {
        return builder.Add(new TextConfigurationSource { FilePath = path });
    }
}
