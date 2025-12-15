namespace ConfigTranslator.Core.Models;

/// <summary>
/// Базовый класс для всех значений конфигурации
/// </summary>
public abstract class ConfigValue
{
    public abstract string ToJson(int indent = 0);
}

/// <summary>
/// Числовое значение
/// </summary>
public class NumberValue : ConfigValue
{
    public double Value { get; }
    
    public NumberValue(double value)
    {
        Value = value;
    }
    
    public override string ToJson(int indent = 0)
    {
        return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Словарь (struct)
/// </summary>
public class DictValue : ConfigValue
{
    public Dictionary<string, ConfigValue> Entries { get; } = new();
    
    public override string ToJson(int indent = 0)
    {
        if (Entries.Count == 0)
            return "{}";
            
        var indentStr = new string(' ', indent * 2);
        var innerIndent = new string(' ', (indent + 1) * 2);
        
        var entries = Entries.Select(kvp => 
            $"{innerIndent}\"{kvp.Key}\": {kvp.Value.ToJson(indent + 1)}");
            
        return "{\n" + string.Join(",\n", entries) + $"\n{indentStr}}}";
    }
}

/// <summary>
/// Ссылка на константу (временное значение при парсинге)
/// </summary>
public class ConstantReference : ConfigValue
{
    public string Name { get; }
    
    public ConstantReference(string name)
    {
        Name = name;
    }
    
    public override string ToJson(int indent = 0)
    {
        throw new InvalidOperationException($"Константа '{Name}' не была разрешена");
    }
}