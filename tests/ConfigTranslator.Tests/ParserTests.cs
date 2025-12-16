using ConfigTranslator.Core;
using ConfigTranslator.Core.Models;
using Xunit;

namespace ConfigTranslator.Tests;

/// <summary>
/// Тесты парсера конфигурационного языка.
/// Покрывают все конструкции языка с учётом вложенности.
/// </summary>
public class ParserTests
{
    #region Числа
    
    [Fact]
    public void Parse_SimpleNumber_ReturnsCorrectValue()
    {
        var result = ConfigParser.ParseConfig("x = 3.14");
        
        Assert.Single(result.Entries);
        Assert.Equal(3.14, ((NumberValue)result.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_NumberWithoutIntegerPart_Works()
    {
        var result = ConfigParser.ParseConfig("x = .5");
        
        Assert.Equal(0.5, ((NumberValue)result.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_NumberWithZeroIntegerPart_Works()
    {
        var result = ConfigParser.ParseConfig("x = 0.123");
        
        Assert.Equal(0.123, ((NumberValue)result.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_LargeNumber_Works()
    {
        var result = ConfigParser.ParseConfig("x = 123456.789");
        
        Assert.Equal(123456.789, ((NumberValue)result.Entries["x"]).Value);
    }
    
    #endregion

    #region Множественные присваивания
    
    [Fact]
    public void Parse_MultipleAssignments_ReturnsAllEntries()
    {
        var result = ConfigParser.ParseConfig("a = 1.0, b = 2.0");
        
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(1.0, ((NumberValue)result.Entries["a"]).Value);
        Assert.Equal(2.0, ((NumberValue)result.Entries["b"]).Value);
    }
    
    [Fact]
    public void Parse_MultipleAssignmentsWithTrailingComma_Works()
    {
        var result = ConfigParser.ParseConfig("a = 1.0, b = 2.0,");
        
        Assert.Equal(2, result.Entries.Count);
    }
    
    #endregion

    #region Словари (struct)
    
    [Fact]
    public void Parse_SimpleStruct_ReturnsDict()
    {
        var result = ConfigParser.ParseConfig("obj = struct { x = 1.0 }");
        
        var obj = result.Entries["obj"] as DictValue;
        Assert.NotNull(obj);
        Assert.Equal(1.0, ((NumberValue)obj.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_EmptyStruct_Works()
    {
        var result = ConfigParser.ParseConfig("empty = struct { }");
        
        var empty = result.Entries["empty"] as DictValue;
        Assert.NotNull(empty);
        Assert.Empty(empty.Entries);
    }
    
    [Fact]
    public void Parse_StructWithMultipleFields_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            config = struct {
                port = 8080.0,
                timeout = 30.0,
                maxConn = 100.0
            }
        ");
        
        var config = result.Entries["config"] as DictValue;
        Assert.Equal(3, config!.Entries.Count);
        Assert.Equal(8080.0, ((NumberValue)config.Entries["port"]).Value);
        Assert.Equal(30.0, ((NumberValue)config.Entries["timeout"]).Value);
        Assert.Equal(100.0, ((NumberValue)config.Entries["maxConn"]).Value);
    }
    
    [Fact]
    public void Parse_StructWithTrailingComma_Works()
    {
        var result = ConfigParser.ParseConfig("obj = struct { x = 1.0, }");
        
        var obj = result.Entries["obj"] as DictValue;
        Assert.NotNull(obj);
        Assert.Single(obj.Entries);
    }
    
    #endregion

    #region Вложенные структуры
    
    [Fact]
    public void Parse_NestedStruct_ReturnsNestedDict()
    {
        var result = ConfigParser.ParseConfig("outer = struct { inner = struct { value = 42.0 } }");
        
        var outer = result.Entries["outer"] as DictValue;
        var inner = outer!.Entries["inner"] as DictValue;
        Assert.Equal(42.0, ((NumberValue)inner!.Entries["value"]).Value);
    }
    
    [Fact]
    public void Parse_DeeplyNestedStruct_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            a = struct {
                b = struct {
                    c = struct {
                        d = struct {
                            value = 1.0
                        }
                    }
                }
            }
        ");
        
        var a = result.Entries["a"] as DictValue;
        var b = a!.Entries["b"] as DictValue;
        var c = b!.Entries["c"] as DictValue;
        var d = c!.Entries["d"] as DictValue;
        Assert.Equal(1.0, ((NumberValue)d!.Entries["value"]).Value);
    }
    
    [Fact]
    public void Parse_NestedStructWithSiblings_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            parent = struct {
                child1 = struct { x = 1.0 },
                child2 = struct { y = 2.0 },
                value = 3.0
            }
        ");
        
        var parent = result.Entries["parent"] as DictValue;
        Assert.Equal(3, parent!.Entries.Count);
        
        var child1 = parent.Entries["child1"] as DictValue;
        var child2 = parent.Entries["child2"] as DictValue;
        
        Assert.Equal(1.0, ((NumberValue)child1!.Entries["x"]).Value);
        Assert.Equal(2.0, ((NumberValue)child2!.Entries["y"]).Value);
        Assert.Equal(3.0, ((NumberValue)parent.Entries["value"]).Value);
    }
    
    #endregion

    #region Константы (global)
    
    [Fact]
    public void Parse_GlobalConstant_CanBeReferenced()
    {
        var result = ConfigParser.ParseConfig("global PI = 3.14; value = [PI]");
        
        Assert.Single(result.Entries); // global не попадает в результат
        Assert.Equal(3.14, ((NumberValue)result.Entries["value"]).Value);
    }
    
    [Fact]
    public void Parse_GlobalConstantInStruct_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            global timeout = 30.0;
            server = struct {
                connectionTimeout = [timeout]
            }
        ");
        
        var server = result.Entries["server"] as DictValue;
        Assert.Equal(30.0, ((NumberValue)server!.Entries["connectionTimeout"]).Value);
    }
    
    [Fact]
    public void Parse_GlobalStructConstant_CanBeReferenced()
    {
        var result = ConfigParser.ParseConfig(@"
            global defaults = struct { x = 0.0, y = 0.0 };
            position = [defaults]
        ");
        
        var position = result.Entries["position"] as DictValue;
        Assert.NotNull(position);
        Assert.Equal(2, position.Entries.Count);
        Assert.Equal(0.0, ((NumberValue)position.Entries["x"]).Value);
        Assert.Equal(0.0, ((NumberValue)position.Entries["y"]).Value);
    }
    
    [Fact]
    public void Parse_MultipleGlobalConstants_Work()
    {
        var result = ConfigParser.ParseConfig(@"
            global a = 1.0;
            global b = 2.0;
            sum = struct { first = [a], second = [b] }
        ");
        
        var sum = result.Entries["sum"] as DictValue;
        Assert.Equal(1.0, ((NumberValue)sum!.Entries["first"]).Value);
        Assert.Equal(2.0, ((NumberValue)sum.Entries["second"]).Value);
    }
    
    [Fact]
    public void Parse_ConstantInNestedStruct_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            global val = 42.0;
            root = struct {
                level1 = struct {
                    level2 = struct {
                        deep = [val]
                    }
                }
            }
        ");
        
        var root = result.Entries["root"] as DictValue;
        var level1 = root!.Entries["level1"] as DictValue;
        var level2 = level1!.Entries["level2"] as DictValue;
        Assert.Equal(42.0, ((NumberValue)level2!.Entries["deep"]).Value);
    }
    
    #endregion

    #region Комментарии
    
    [Fact]
    public void Parse_Comment_IsIgnored()
    {
        var result = ConfigParser.ParseConfig(@"
            <!-- This is a comment -->
            x = 1.0
        ");
        
        Assert.Single(result.Entries);
        Assert.Equal(1.0, ((NumberValue)result.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_MultilineComment_IsIgnored()
    {
        var result = ConfigParser.ParseConfig(@"
            <!--
            This is a
            multiline comment
            with many lines
            -->
            x = 1.0
        ");
        
        Assert.Single(result.Entries);
    }
    
    [Fact]
    public void Parse_CommentBetweenAssignments_IsIgnored()
    {
        var result = ConfigParser.ParseConfig(@"
            a = 1.0,
            <!-- comment in the middle -->
            b = 2.0
        ");
        
        Assert.Equal(2, result.Entries.Count);
    }
    
    [Fact]
    public void Parse_CommentInsideStruct_IsIgnored()
    {
        var result = ConfigParser.ParseConfig(@"
            obj = struct {
                <!-- field comment -->
                x = 1.0,
                <!-- another comment -->
                y = 2.0
            }
        ");
        
        var obj = result.Entries["obj"] as DictValue;
        Assert.Equal(2, obj!.Entries.Count);
    }
    
    #endregion

    #region Синтаксические ошибки
    
    [Fact]
    public void Parse_UndefinedConstant_ThrowsException()
    {
        var ex = Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x = [undefined]"));
        
        Assert.Contains("undefined", ex.Message);
    }
    
    [Fact]
    public void Parse_MissingValue_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x = "));
    }
    
    [Fact]
    public void Parse_MissingEquals_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x 1.0"));
    }
    
    [Fact]
    public void Parse_UnclosedStruct_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x = struct { y = 1.0"));
    }
    
    [Fact]
    public void Parse_UnclosedConstantRef_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("global a = 1.0; x = [a"));
    }
    
    [Fact]
    public void Parse_InvalidNumber_ThrowsException()
    {
        // Число без точки невалидно по грамматике
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x = 123"));
    }
    
    [Fact]
    public void Parse_MissingSemicolonAfterGlobal_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("global a = 1.0 x = [a]"));
    }
    
    [Fact]
    public void Parse_InvalidIdentifier_ThrowsException()
    {
        Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("123abc = 1.0"));
    }
    
    [Fact]
    public void Parse_ErrorMessage_ContainsLineNumber()
    {
        var ex = Assert.Throws<ParseException>(() => 
            ConfigParser.ParseConfig("x = 1.0\ny = \n"));
        
        Assert.Contains("строке", ex.Message);
    }
    
    #endregion

    #region Пустой ввод и краевые случаи
    
    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyDict()
    {
        var result = ConfigParser.ParseConfig("");
        
        Assert.Empty(result.Entries);
    }
    
    [Fact]
    public void Parse_OnlyWhitespace_ReturnsEmptyDict()
    {
        var result = ConfigParser.ParseConfig("   \n\t\n   ");
        
        Assert.Empty(result.Entries);
    }
    
    [Fact]
    public void Parse_OnlyComments_ReturnsEmptyDict()
    {
        var result = ConfigParser.ParseConfig("<!-- comment only -->");
        
        Assert.Empty(result.Entries);
    }
    
    #endregion
}
