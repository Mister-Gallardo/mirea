namespace CampusHub.ConfigCenter.Configuration;

/// <summary>
/// Собственный провайдер конфигурации.
/// Читает файл в формате: строка с ключом, следующая строка — значение.
/// Ключи содержат двоеточие для вложенных секций (например Custom:AppVersion).
/// </summary>
public class TextConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
{
    private readonly string _filePath;

    public TextConfigurationProvider(string filePath)
    {
        _filePath = filePath;
    }

    /// <summary>
    /// Загружает конфигурацию из текстового файла.
    /// Формат: нечётные строки — ключ, чётные строки — значение.
    /// Пустые строки и строки, начинающиеся с #, игнорируются.
    /// </summary>
    public override void Load()
    {
        if (!File.Exists(_filePath))
        {
            // Если файл не найден — оставляем пустую коллекцию
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        var lines = File
            .ReadAllLines(_filePath)
            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith('#'))
            .ToArray();

        // Пары: чётный индекс = ключ, нечётный = значение
        for (int i = 0; i + 1 < lines.Length; i += 2)
        {
            var key = lines[i].Trim();
            var value = lines[i + 1].Trim();

            if (!string.IsNullOrEmpty(key))
            {
                result[key] = value;
            }
        }

        Data = result;
    }
}
