using Sprache;
using ConfigTranslator.Core.Models;

namespace ConfigTranslator.Core;

/// <summary>
/// Парсер конфигурационного языка на основе библиотеки Sprache.
/// Грамматика:
///   Комментарий:  &lt;!-- ... --&gt;
///   Число:        \d*\.\d+
///   Имя:          [a-zA-Z][a-zA-Z0-9]*
///   Словарь:      struct { имя = значение, ... }
///   Константа:    global имя = значение;
///   Ссылка:       [имя]
/// </summary>
public static class ConfigParser
{
    #region Базовые парсеры
    
    /// <summary>
    /// Многострочный комментарий: &lt;!-- ... --&gt;
    /// </summary>
    private static readonly Parser<string> Comment =
        from open in Parse.String("<!--")
        from content in Parse.AnyChar.Until(Parse.String("-->")).Text()
        select content;

    /// <summary>
    /// Пробельные символы (пробелы, табы, переводы строк)
    /// </summary>
    private static readonly Parser<IEnumerable<char>> Whitespace =
        Parse.WhiteSpace.Many();

    /// <summary>
    /// Токенизатор: пропускает пробелы и комментарии вокруг парсера
    /// </summary>
    private static Parser<T> Tok<T>(this Parser<T> parser) =>
        from leading in Whitespace
        from comments in Comment.Then(_ => Whitespace).Many()
        from item in parser
        from trailing in Whitespace
        from trailingComments in Comment.Then(_ => Whitespace).Many()
        select item;

    #endregion

    #region Значения
    
    /// <summary>
    /// Число: \d*\.\d+ (обязательная точка и дробная часть)
    /// Примеры: 3.14, .5, 0.0, 123.456
    /// </summary>
    private static readonly Parser<ConfigValue> Number =
        from intPart in Parse.Digit.Many().Text()
        from dot in Parse.Char('.')
        from fracPart in Parse.Digit.AtLeastOnce().Text()
        select (ConfigValue)new NumberValue(
            double.Parse($"{intPart}.{fracPart}", 
            System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>
    /// Имя (идентификатор): [a-zA-Z][a-zA-Z0-9]*
    /// </summary>
    private static readonly Parser<string> Identifier =
        from first in Parse.Letter
        from rest in Parse.LetterOrDigit.Many().Text()
        select first + rest;

    /// <summary>
    /// Ссылка на константу: [имя]
    /// </summary>
    private static readonly Parser<ConfigValue> ConstantRef =
        from open in Parse.Char('[').Tok()
        from name in Identifier.Tok()
        from close in Parse.Char(']').Tok()
        select (ConfigValue)new ConstantReference(name);

    /// <summary>
    /// Значение: число | словарь | ссылка на константу
    /// </summary>
    private static readonly Parser<ConfigValue> Value =
        Number.Tok()
            .Or(Parse.Ref(() => Struct))
            .Or(ConstantRef);

    #endregion

    #region Структуры
    
    /// <summary>
    /// Присваивание: имя = значение
    /// </summary>
    private static readonly Parser<KeyValuePair<string, ConfigValue>> Assignment =
        from name in Identifier.Tok()
        from eq in Parse.Char('=').Tok()
        from value in Value
        select new KeyValuePair<string, ConfigValue>(name, value);

    /// <summary>
    /// Словарь: struct { имя = значение, имя = значение, ... }
    /// </summary>
    private static readonly Parser<ConfigValue> Struct =
        from keyword in Parse.String("struct").Tok()
        from open in Parse.Char('{').Tok()
        from entries in Assignment.DelimitedBy(Parse.Char(',').Tok()).Optional()
        from trailing in Parse.Char(',').Tok().Optional()
        from close in Parse.Char('}').Tok()
        select (ConfigValue)new DictValue()
            .WithEntries(entries.GetOrElse(Enumerable.Empty<KeyValuePair<string, ConfigValue>>()));

    #endregion

    #region Верхний уровень
    
    /// <summary>
    /// Объявление константы: global имя = значение;
    /// </summary>
    private static readonly Parser<KeyValuePair<string, ConfigValue>> GlobalDeclaration =
        from keyword in Parse.String("global").Tok()
        from name in Identifier.Tok()
        from eq in Parse.Char('=').Tok()
        from value in Value
        from semi in Parse.Char(';').Tok()
        select new KeyValuePair<string, ConfigValue>(name, value);

    /// <summary>
    /// Элемент верхнего уровня: объявление константы или присваивание
    /// </summary>
    private static readonly Parser<(bool isGlobal, KeyValuePair<string, ConfigValue> entry)> TopLevelItem =
        GlobalDeclaration.Select(g => (true, g))
            .Or(Assignment.Select(a => (false, a)));

    /// <summary>
    /// Полный документ: последовательность элементов верхнего уровня через запятую
    /// </summary>
    /// <summary>
/// Элемент верхнего уровня с опциональной запятой после обычных присваиваний
/// </summary>
private static readonly Parser<(bool isGlobal, KeyValuePair<string, ConfigValue> entry)> TopLevelItemWithSeparator =
    GlobalDeclaration.Select(g => (true, g))
        .Or(from assignment in Assignment
            from comma in Parse.Char(',').Tok().Optional()
            select (false, assignment));

/// <summary>
/// Полный документ: последовательность элементов верхнего уровня
/// </summary>
private static readonly Parser<List<(bool isGlobal, KeyValuePair<string, ConfigValue> entry)>> Document =
    from leading in Whitespace
    from comments in Comment.Then(_ => Whitespace).Many()
    from items in TopLevelItemWithSeparator.Many()
    from end in Whitespace.End()
    select items.ToList();
    #endregion

    #region Публичный API
    
    /// <summary>
    /// Парсит входной текст и возвращает корневой словарь.
    /// Константы (global) разрешаются и не попадают в результат.
    /// </summary>
    /// <param name="input">Текст на конфигурационном языке</param>
    /// <returns>Корневой словарь со всеми значениями</returns>
    /// <exception cref="ParseException">При синтаксической ошибке или неизвестной константе</exception>
    public static DictValue ParseConfig(string input)
    {
        var result = Document.TryParse(input);
        
        if (!result.WasSuccessful)
        {
            var position = GetLineColumn(input, result.Remainder.Position);
            throw new ParseException(
                $"Синтаксическая ошибка в строке {position.line}, позиция {position.column}: " +
                $"{result.Message}");
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

    #endregion

    #region Вспомогательные методы
    
    /// <summary>
    /// Разрешает ссылки на константы рекурсивно
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

    /// <summary>
    /// Преобразует абсолютную позицию в строку и колонку
    /// </summary>
    private static (int line, int column) GetLineColumn(string input, int position)
    {
        int line = 1;
        int column = 1;
        
        for (int i = 0; i < position && i < input.Length; i++)
        {
            if (input[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }
        
        return (line, column);
    }

    #endregion
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
/// Исключение при ошибке парсинга или разрешения констант
/// </summary>
public class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
