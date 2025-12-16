using System.Text.Json;
using ConfigTranslator.Core;
using Xunit;

namespace ConfigTranslator.Tests;

/// <summary>
/// Интеграционные тесты - проверка полных примеров конфигураций
/// из разных предметных областей
/// </summary>
public class IntegrationTests
{
    private readonly Translator _translator = new();
    
    /// <summary>
    /// Пример 1: Конфигурация веб-сервера (DevOps/Backend)
    /// </summary>
    [Fact]
    public void Example_WebServerConfig()
    {
        var input = @"
            <!--
            Конфигурация веб-сервера
            Предметная область: DevOps/Backend
            -->
            global defaultTimeout = 30.0;
            global maxRetries = 3.0;
            
            server = struct {
                port = 8080.0,
                maxConnections = 10000.0,
                timeout = [defaultTimeout],
                keepAlive = 1.0
            },
            
            ssl = struct {
                enabled = 1.0,
                port = 443.0,
                certValidityDays = 365.0
            },
            
            loadBalancer = struct {
                algorithm = 1.0,
                healthCheckInterval = 10.0,
                retries = [maxRetries]
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        // Проверяем server
        var server = doc.RootElement.GetProperty("server");
        Assert.Equal(8080.0, server.GetProperty("port").GetDouble());
        Assert.Equal(30.0, server.GetProperty("timeout").GetDouble()); // разрешённая константа
        
        // Проверяем ssl
        var ssl = doc.RootElement.GetProperty("ssl");
        Assert.Equal(443.0, ssl.GetProperty("port").GetDouble());
        
        // Проверяем loadBalancer
        var lb = doc.RootElement.GetProperty("loadBalancer");
        Assert.Equal(3.0, lb.GetProperty("retries").GetDouble()); // разрешённая константа
    }
    
    /// <summary>
    /// Пример 2: Конфигурация базы данных (Администрирование БД)
    /// </summary>
    [Fact]
    public void Example_DatabaseConfig()
    {
        var input = @"
            <!--
            Конфигурация базы данных
            Предметная область: Администрирование БД
            -->
            global connectionPoolSize = 20.0;
            global queryTimeoutSeconds = 60.0;
            
            primary = struct {
                port = 5432.0,
                poolSize = [connectionPoolSize],
                timeout = [queryTimeoutSeconds],
                maxIdleTime = 300.0
            },
            
            replica = struct {
                port = 5433.0,
                poolSize = 10.0,
                timeout = [queryTimeoutSeconds],
                syncDelay = 0.5
            },
            
            backup = struct {
                enabled = 1.0,
                intervalHours = 24.0,
                retentionDays = 30.0,
                compressionLevel = 9.0
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        // Проверяем primary
        var primary = doc.RootElement.GetProperty("primary");
        Assert.Equal(5432.0, primary.GetProperty("port").GetDouble());
        Assert.Equal(20.0, primary.GetProperty("poolSize").GetDouble());
        Assert.Equal(60.0, primary.GetProperty("timeout").GetDouble());
        
        // Проверяем replica
        var replica = doc.RootElement.GetProperty("replica");
        Assert.Equal(60.0, replica.GetProperty("timeout").GetDouble()); // та же константа
        
        // Проверяем backup
        var backup = doc.RootElement.GetProperty("backup");
        Assert.Equal(4, backup.EnumerateObject().Count());
    }
    
    /// <summary>
    /// Пример 3: Конфигурация игры (Gamedev/RPG)
    /// </summary>
    [Fact]
    public void Example_GameConfig()
    {
        var input = @"
            <!--
            Конфигурация игры
            Предметная область: Gamedev/RPG
            -->
            global basePlayerHealth = 100.0;
            global baseEnemyDamage = 10.0;
            
            player = struct {
                stats = struct {
                    health = [basePlayerHealth],
                    mana = 50.0,
                    stamina = 80.0,
                    level = 1.0
                },
                combat = struct {
                    baseDamage = 15.0,
                    critMultiplier = 2.0,
                    critChance = 0.1,
                    attackSpeed = 1.0
                },
                physics = struct {
                    walkSpeed = 5.0,
                    runSpeed = 10.0,
                    jumpForce = 8.5,
                    gravity = 9.81
                }
            },
            
            enemy = struct {
                goblin = struct {
                    health = 30.0,
                    damage = [baseEnemyDamage],
                    speed = 3.0,
                    expReward = 25.0
                },
                dragon = struct {
                    health = 500.0,
                    damage = 50.0,
                    speed = 7.0,
                    expReward = 1000.0
                }
            },
            
            world = struct {
                dayLengthSeconds = 1200.0,
                weatherChangeInterval = 300.0,
                spawnRadius = 50.0
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        // Проверяем player.stats
        var stats = doc.RootElement
            .GetProperty("player")
            .GetProperty("stats");
        Assert.Equal(100.0, stats.GetProperty("health").GetDouble());
        Assert.Equal(50.0, stats.GetProperty("mana").GetDouble());
        
        // Проверяем player.combat
        var combat = doc.RootElement
            .GetProperty("player")
            .GetProperty("combat");
        Assert.Equal(0.1, combat.GetProperty("critChance").GetDouble());
        
        // Проверяем enemy.goblin с константой
        var goblin = doc.RootElement
            .GetProperty("enemy")
            .GetProperty("goblin");
        Assert.Equal(10.0, goblin.GetProperty("damage").GetDouble()); // разрешённая константа
        
        // Проверяем глубокую вложенность
        var physics = doc.RootElement
            .GetProperty("player")
            .GetProperty("physics");
        Assert.Equal(9.81, physics.GetProperty("gravity").GetDouble());
    }
    
    /// <summary>
    /// Тест на глубокую вложенность структур
    /// </summary>
    [Fact]
    public void DeepNesting_WorksCorrectly()
    {
        var input = @"
            global depth = 5.0;
            
            level1 = struct {
                level2 = struct {
                    level3 = struct {
                        level4 = struct {
                            level5 = struct {
                                value = [depth]
                            }
                        }
                    }
                }
            }
        ";
        
        var json = _translator.Translate(input);
        var doc = JsonDocument.Parse(json);
        
        var value = doc.RootElement
            .GetProperty("level1")
            .GetProperty("level2")
            .GetProperty("level3")
            .GetProperty("level4")
            .GetProperty("level5")
            .GetProperty("value")
            .GetDouble();
            
        Assert.Equal(5.0, value);
    }
}
