using Sprache;
using ConfigTranslator.Core.Models;

namespace ConfigTranslator.Core;

/// <summary>
/// Парсер конфигурационного языка на основе библиотеки Sprache
/// </summary>
public static class ConfigParser
{
    /// <summary>
    /// Многострочный комментарий: <!-- ... -->
    /// </summary>
    private static readonly Parser<string> Comment =
        from open in Sprache.Parse.String("<!--")
        from content in Sprache.Parse.AnyChar.Until(Sprache.Parse.String("-->")).Text()
        select content;

    /// <summary>
    /// Пробелы и комментарии (пропускаются)
    /// </summary>
    private static readonly Parser<IEnumerable<char>> Whitespace =
        Sprache.Parse.WhiteSpace.Many();

    private static Parser<T> Tok<T>(this Parser<T> parser) =>
        from leading in Whitespace
        from comments in Comment.Then(_ => Whitespace).Many()
        from item in parser
        from trailing in Whitespace
        from trailingComments in Comment.Then(_ => Whitespace).Many()
        select item;

    /// <summary>
    /// Число: \d*\.\d+
    /// </summary>
    private static readonly Parser<ConfigValue> Number =
        from intPart in Sprache.Parse.Digit.Many().Text()
        from dot in Sprache.Parse.Char('.')
        from fracPart in Sprache.Parse.Digit.AtLeastOnce().Text()
        select (ConfigValue)new NumberValue(
            double.Parse($"{intPart}.{fracPart}", 
            System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>
    /// Имя: [a-zA-Z][a-zA-Z0-9]*
    /// </summary>
    private static readonly Parser<string> Identifier =
        from first in Sprache.Parse.Letter
        from rest in Sprache.Parse.LetterOrDigit.Many().Text()
        select first + rest;

    /// <summary>
    /// Ссылка на константу: [имя]
    /// </summary>
    private static readonly Parser<ConfigValue> ConstantRef =
        from open in Sprache.Parse.Char('[').Tok()
        from name in Identifier.Tok()
        from close in Sprache.Parse.Char(']').Tok()
        select (ConfigValue)new ConstantReference(name);

    /// <summary>
    /// Значение: число, структура или ссылка на константу
    /// </summary>
    private static readonly Parser<ConfigValue> Value =
        Number.Tok()
            .Or(Sprache.Parse.Ref(() => Struct))
            .Or(ConstantRef);

    /// <summary>
    /// Присваивание: имя = значение
    /// </summary>
    private static readonly Parser<KeyValuePair<string, ConfigValue>> Assignment =
        from name in Identifier.Tok()
        from eq in Sprache.Parse.Char('=').Tok()
        from value in Value
        select new KeyValuePair<string, ConfigValue>(name, value);

    /// <summary>
    /// Структура: struct { имя = значение, ... }
    /// </summary>
    private static readonly Parser<ConfigValue> Struct =
        from keyword in Sprache.Parse.String("struct").Tok()
        from open in Sprache.Parse.Char('{').Tok()
        from entries in Assignment.DelimitedBy(Sprache.Parse.Char(',').Tok()).Optional()
        from trailing in Sprache.Parse.Char(',').Tok().Optional()
        from close in Sprache.Parse.Char('}').Tok()
        select (ConfigValue)new DictValue()
            .WithEntries(entries.GetOrElse(Enumerable.Empty<KeyValuePair<string, ConfigValue>>()));

    /// <summary>
    /// Объявление константы: global имя = значение;
    /// </summary>
    private static readonly Parser<KeyValuePair<string, ConfigValue>> GlobalDeclaration =
        from keyword in Sprache.Parse.String("global").Tok()
        from name in Identifier.Tok()
        from eq in Sprache.Parse.Char('=').Tok()
        from value in Value
        from semi in Sprache.Parse.Char(';').Tok()
        select new KeyValuePair<string, ConfigValue>(name, value);

    /// <summary>
    /// Элемент верхнего уровня: global или присваивание
    /// </summary>
    private static readonly Parser<(bool isGlobal, KeyValuePair<string, ConfigValue> entry)> TopLevelItem =
        GlobalDeclaration.Select(g => (true, g))
            .Or(Assignment.Select(a => (false, a)));

    /// <summary>
    /// Полный документ
    /// </summary>
    private static readonly Parser<List<(bool isGlobal, KeyValuePair<string, ConfigValue> entry)>> Document =
        from leading in Whitespace
        from comments in Comment.Then(_ => Whitespace).Many()
        from items in TopLevelItem.DelimitedBy(Sprache.Parse.Char(',').Tok().Optional()).Optional()
        from trailing in Sprache.Parse.Char(',').Tok().Optional()
        from end in Whitespace.End()
        select items.GetOrElse(Enumerable.Empty<(bool, KeyValuePair<string, ConfigValue>)>()).ToList();

    /// <summary>
    /// Парсит входной текст и возвращает корневой словарь
    /// </summary>
    public static DictValue ParseConfig(string input)
    {
        var result = Document.TryParse(input);
        
        if (!result.WasSuccessful)
        {
            throw new ParseException($"Ошибка синтаксиса в позиции {result.Remainder.Position}: {result.Message}");
        }

        var constants = new Dictionary<string, ConfigValue>();
        var root = new DictValue();

        foreach (var (isGlobal, entry) in result.Value)
        {
            var resolvedValue = ResolveConstants(entry.Value, constants);
            
            if (isGlobal)
            {
                constants[entry.Key] = resolvedValue;
            }
            else
            {
                root.Entries[entry.Key] = resolvedValue;
            }
        }

        return root;
    }

    /// <summary>
    /// Разрешает ссылки на константы
    /// </summary>
    private static ConfigValue ResolveConstants(ConfigValue value, Dictionary<string, ConfigValue> constants)
    {
        switch (value)
        {
            case ConstantReference constRef:
                if (!constants.TryGetValue(constRef.Name, out var resolved))
                {
                    throw new ParseException($"Неизвестная константа: '{constRef.Name}'");
                }
                return resolved;
                
            case DictValue dict:
                var newDict = new DictValue();
                foreach (var entry in dict.Entries)
                {
                    newDict.Entries[entry.Key] = ResolveConstants(entry.Value, constants);
                }
                return newDict;
                
            default:
                return value;
        }
    }
}

/// <summary>
/// Расширение для заполнения словаря
/// </summary>
public static class DictValueExtensions
{
    public static DictValue WithEntries(this DictValue dict, IEnumerable<KeyValuePair<string, ConfigValue>> entries)
    {
        foreach (var entry in entries)
        {
            dict.Entries[entry.Key] = entry.Value;
        }
        return dict;
    }
}

/// <summary>
/// Исключение при ошибке парсинга
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}