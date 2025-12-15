using System.Text.Json;
using ConfigTranslator.Core;

namespace ConfigTranslator.Tests;

public class IntegrationTests
{
    private readonly Translator _translator = new();
    
    [Fact]
    public void Example_WebServerConfig()
    {
        var input = @"
            <!--
            Конфигурация веб-сервера
            -->
            global defaultTimeout = 30.0;
            
            server = struct {
                port = 8080.0,
                timeout = [defaultTimeout]
            },
            
            logging = struct {
                level = 2.0
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        var server = doc.RootElement.GetProperty("server");
        Assert.Equal(8080.0, server.GetProperty("port").GetDouble());
        Assert.Equal(30.0, server.GetProperty("timeout").GetDouble());
    }
    
    [Fact]
    public void Example_DatabaseConfig()
    {
        var input = @"
            global poolSize = 10.0;
            
            database = struct {
                connectionPool = struct {
                    maxSize = [poolSize],
                    timeout = 60.0
                }
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        var pool = doc.RootElement
            .GetProperty("database")
            .GetProperty("connectionPool");
            
        Assert.Equal(10.0, pool.GetProperty("maxSize").GetDouble());
    }
    
    [Fact]
    public void Example_GameConfig()
    {
        var input = @"
            global baseHealth = 100.0;
            
            player = struct {
                stats = struct {
                    health = [baseHealth],
                    mana = 50.0
                },
                combat = struct {
                    damage = 15.0,
                    critChance = 0.15
                }
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        var stats = doc.RootElement
            .GetProperty("player")
            .GetProperty("stats");
            
        Assert.Equal(100.0, stats.GetProperty("health").GetDouble());
    }
}