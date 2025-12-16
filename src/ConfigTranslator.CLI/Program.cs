using ConfigTranslator.Core;

namespace ConfigTranslator.CLI;

/// <summary>
/// Точка входа CLI-приложения для трансляции конфигурации в JSON
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }
        
        string? inputFile = null;
        
        for (int i = 0; i < args.Length; i++)
        {
            if ((args[i] == "-i" || args[i] == "--input") && i + 1 < args.Length)
            {
                inputFile = args[i + 1];
                i++;
            }
            else if (!args[i].StartsWith("-"))
            {
                inputFile = args[i];
            }
        }
        
        if (string.IsNullOrEmpty(inputFile))
        {
            Console.Error.WriteLine("Ошибка: не указан входной файл");
            PrintUsage();
            return 1;
        }
        
        try
        {
            var translator = new Translator();
            var json = translator.TranslateFile(inputFile);
            Console.WriteLine(json);
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"Ошибка: {ex.Message}");
            return 1;
        }
        catch (ParseException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Неожиданная ошибка: {ex.Message}");
            return 1;
        }
    }
    
    static void PrintUsage()
    {
        Console.WriteLine("ConfigTranslator - трансляция конфигурационного языка в JSON");
        Console.WriteLine();
        Console.WriteLine("Использование: ConfigTranslator.CLI [опции] <входной_файл>");
        Console.WriteLine();
        Console.WriteLine("Опции:");
        Console.WriteLine("  -i, --input <файл>  Путь к входному файлу конфигурации");
        Console.WriteLine("  -h, --help          Показать эту справку");
        Console.WriteLine();
        Console.WriteLine("Примеры:");
        Console.WriteLine("  ConfigTranslator.CLI config.conf");
        Console.WriteLine("  ConfigTranslator.CLI -i config.conf > output.json");
    }
}
