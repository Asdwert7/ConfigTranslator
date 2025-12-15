using System.Text.Json;
using ConfigTranslator.Core;

namespace ConfigTranslator.Tests;

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
}