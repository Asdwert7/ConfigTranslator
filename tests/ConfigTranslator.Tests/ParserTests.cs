using ConfigTranslator.Core;
using ConfigTranslator.Core.Models;

namespace ConfigTranslator.Tests;

public class ParserTests
{
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
    public void Parse_MultipleAssignments_ReturnsAllEntries()
    {
        var result = ConfigParser.ParseConfig("a = 1.0, b = 2.0");
        
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(1.0, ((NumberValue)result.Entries["a"]).Value);
        Assert.Equal(2.0, ((NumberValue)result.Entries["b"]).Value);
    }
    
    [Fact]
    public void Parse_SimpleStruct_ReturnsDict()
    {
        var result = ConfigParser.ParseConfig("obj = struct { x = 1.0 }");
        
        var obj = result.Entries["obj"] as DictValue;
        Assert.NotNull(obj);
        Assert.Equal(1.0, ((NumberValue)obj.Entries["x"]).Value);
    }
    
    [Fact]
    public void Parse_NestedStruct_ReturnsNestedDict()
    {
        var result = ConfigParser.ParseConfig("outer = struct { inner = struct { value = 42.0 } }");
        
        var outer = result.Entries["outer"] as DictValue;
        var inner = outer!.Entries["inner"] as DictValue;
        Assert.Equal(42.0, ((NumberValue)inner!.Entries["value"]).Value);
    }
    
    [Fact]
    public void Parse_GlobalConstant_CanBeReferenced()
    {
        var result = ConfigParser.ParseConfig("global PI = 3.14; value = [PI]");
        
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
            -->
            x = 1.0
        ");
        
        Assert.Single(result.Entries);
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
    public void Parse_DeeplyNestedStruct_Works()
    {
        var result = ConfigParser.ParseConfig(@"
            a = struct {
                b = struct {
                    c = struct {
                        d = 1.0
                    }
                }
            }
        ");
        
        var a = result.Entries["a"] as DictValue;
        var b = a!.Entries["b"] as DictValue;
        var c = b!.Entries["c"] as DictValue;
        Assert.Equal(1.0, ((NumberValue)c!.Entries["d"]).Value);
    }
    
    [Fact]
    public void Parse_UndefinedConstant_ThrowsException()
    {
        Assert.Throws<ParseException>(() => ConfigParser.ParseConfig("x = [undefined]"));
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
    }
}