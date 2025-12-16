using ConfigTranslator.Core.Models;

namespace ConfigTranslator.Core;

/// <summary>
/// Фасад для трансляции конфигурационного языка в JSON.
/// Объединяет парсинг и генерацию JSON.
/// </summary>
public class Translator
{
    /// <summary>
    /// Транслирует текст конфигурации в JSON
    /// </summary>
    /// <param name="input">Текст на конфигурационном языке</param>
    /// <returns>JSON-строка</returns>
    /// <exception cref="ParseException">При синтаксической ошибке</exception>
    public string Translate(string input)
    {
        var root = ConfigParser.ParseConfig(input);
        return root.ToJson();
    }
    
    /// <summary>
    /// Транслирует файл конфигурации в JSON
    /// </summary>
    /// <param name="filePath">Путь к файлу конфигурации</param>
    /// <returns>JSON-строка</returns>
    /// <exception cref="FileNotFoundException">Если файл не найден</exception>
    /// <exception cref="ParseException">При синтаксической ошибке</exception>
    public string TranslateFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл не найден: {filePath}", filePath);
        }
        
        var content = File.ReadAllText(filePath);
        return Translate(content);
    }
}
