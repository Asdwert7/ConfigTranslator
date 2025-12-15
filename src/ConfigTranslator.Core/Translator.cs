using ConfigTranslator.Core.Models;

namespace ConfigTranslator.Core;

/// <summary>
/// Главный класс для трансляции конфигурации в JSON
/// </summary>
public class Translator
{
    public string Translate(string input)
    {
        var root = ConfigParser.ParseConfig(input);
        return root.ToJson();
    }
    
    public string TranslateFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Файл не найден: {filePath}");
        }
        
        var content = File.ReadAllText(filePath);
        return Translate(content);
    }
}