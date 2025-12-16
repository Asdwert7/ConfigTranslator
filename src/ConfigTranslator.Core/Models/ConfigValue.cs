using System.Text.Json;

namespace ConfigTranslator.Core.Models;

/// <summary>
/// Базовый класс для всех значений конфигурации
/// </summary>
public abstract class ConfigValue
{
    /// <summary>
    /// Преобразует значение в JSON-представление
    /// </summary>
    public abstract void WriteJson(Utf8JsonWriter writer);
}

/// <summary>
/// Числовое значение формата \d*\.\d+
/// </summary>
public class NumberValue : ConfigValue
{
    public double Value { get; }
    
    public NumberValue(double value)
    {
        Value = value;
    }
    
    public override void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteNumberValue(Value);
    }
}

/// <summary>
/// Словарь (struct { ... })
/// </summary>
public class DictValue : ConfigValue
{
    public Dictionary<string, ConfigValue> Entries { get; } = new();
    
    public override void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        foreach (var (key, value) in Entries)
        {
            writer.WritePropertyName(key);
            value.WriteJson(writer);
        }
        writer.WriteEndObject();
    }
    
    /// <summary>
    /// Преобразует корневой словарь в JSON-строку
    /// </summary>
    public string ToJson()
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
        { 
            Indented = true 
        });
        
        WriteJson(writer);
        writer.Flush();
        
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }
}

/// <summary>
/// Ссылка на константу [имя] — временное значение при парсинге
/// </summary>
public class ConstantReference : ConfigValue
{
    public string Name { get; }
    
    public ConstantReference(string name)
    {
        Name = name;
    }
    
    public override void WriteJson(Utf8JsonWriter writer)
    {
        throw new InvalidOperationException($"Константа '{Name}' не была разрешена");
    }
}
