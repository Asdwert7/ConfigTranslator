using System.Text.Json;
using ConfigTranslator.Core;
using Xunit;

namespace ConfigTranslator.Tests;

/// <summary>
/// Тесты транслятора (фасад)
/// </summary>
public class TranslatorTests
{
    private readonly Translator _translator = new();
    
    [Fact]
    public void Translate_SimpleConfig_ReturnsValidJson()
    {
        var input = "name = 1.0";
        var json = _translator.Translate(input);
        
        var doc = JsonDocument.Parse(json);
        Assert.Equal(1.0, doc.RootElement.GetProperty("name").GetDouble());
    }
    
    [Fact]
    public void Translate_ComplexConfig_ReturnsCorrectJson()
    {
        var input = @"
            global defaultPort = 8080.0;
            
            server = struct {
                port = [defaultPort],
                timeout = 30.5
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        var server = doc.RootElement.GetProperty("server");
        Assert.Equal(8080.0, server.GetProperty("port").GetDouble());
        Assert.Equal(30.5, server.GetProperty("timeout").GetDouble());
    }
    
    [Fact]
    public void Translate_OutputIsValidJson()
    {
        var input = @"
            a = 1.0,
            b = struct {
                c = 2.0,
                d = struct { e = 3.0 }
            }
        ";
        
        var json = _translator.Translate(input);
        
        // Проверяем, что результат парсится как валидный JSON
        var doc = JsonDocument.Parse(json);
        Assert.NotNull(doc);
    }
    
    [Fact]
    public void TranslateFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => 
            _translator.TranslateFile("/nonexistent/path/file.conf"));
    }
    
    [Fact]
    public void TranslateFile_ValidFile_ReturnsJson()
    {
        // Создаём временный файл
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "x = 1.0, y = 2.0");
            
            var json = _translator.TranslateFile(tempFile);
            var doc = JsonDocument.Parse(json);
            
            Assert.Equal(1.0, doc.RootElement.GetProperty("x").GetDouble());
            Assert.Equal(2.0, doc.RootElement.GetProperty("y").GetDouble());
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
    
    [Fact]
    public void Translate_SmallNumbers_FormattedCorrectly()
    {
        var input = "x = 0.0001";
        var json = _translator.Translate(input);
        
        // Проверяем что число не в научной нотации
        Assert.Contains("0.0001", json);
        Assert.DoesNotContain("E", json);
    }
}
