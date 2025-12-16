# ConfigTranslator

Инструмент командной строки для трансляции учебного конфигурационного языка в JSON.

## Что делает программа

- Читает файл на учебном конфигурационном языке
- Преобразует его в JSON
- Выводит результат в стандартный вывод
- При синтаксических ошибках показывает строку и позицию ошибки

## Быстрый старт

```bash
# Клонировать
git clone <url>
cd ConfigTranslator

# Собрать
dotnet build

# Запустить
dotnet run --project src/ConfigTranslator.CLI -- examples/web_server.conf
```

## Установка

### Требования

- .NET 8.0+ (проверено на .NET 10.0)

### Сборка из исходников

```bash
git clone <url>
cd ConfigTranslator
dotnet restore
dotnet build
```

### Запуск тестов

```bash
dotnet test
```

## Использование

```bash
# Базовый запуск
dotnet run --project src/ConfigTranslator.CLI -- <файл>

# С ключом -i
dotnet run --project src/ConfigTranslator.CLI -- -i <файл>

# Сохранить в файл
dotnet run --project src/ConfigTranslator.CLI -- examples/game_config.conf > output.json

# Справка
dotnet run --project src/ConfigTranslator.CLI -- --help
```
Если присутствует ошибка для test скорее всего версия .NET старая и не поддерживается для решения проблемы советую компилировать из под .NET 10 или в файлах csproj изменить мануально версию на свою .NET так как я не написал автоматическую смену версии для .NET
### Пример

Входной файл `config.conf`:
```
global timeout = 30.0;

server = struct {
    port = 8080.0,
    timeout = [timeout]
}
```

Команда:
```bash
dotnet run --project src/ConfigTranslator.CLI -- config.conf
```

Результат:
```json
{
  "server": {
    "port": 8080,
    "timeout": 30
  }
}
```

## Синтаксис языка

### Числа
Формат: `\d*\.\d+` — обязательна точка и дробная часть
```
3.14
.5
0.0
100.0
```

### Имена
Формат: `[a-zA-Z][a-zA-Z0-9]*`
```
myVar
server1
maxConnections
```

### Словари
```
struct {
    ключ = значение,
    ключ = значение
}
```

### Константы
Объявление:
```
global имя = значение;
```

Использование:
```
[имя]
```

### Комментарии
```
<!--
Многострочный
комментарий
-->
```

## Примеры конфигураций

В папке `examples/` три примера из разных предметных областей:

| Файл | Область |
|------|---------|
| `web_server.conf` | DevOps/Backend |
| `database.conf` | Администрирование БД |
| `game_config.conf` | Gamedev/RPG |

## Структура проекта

```
ConfigTranslator/
├── ConfigTranslator.sln
├── README.md
├── .gitignore
├── src/
│   ├── ConfigTranslator.CLI/         # Точка входа
│   │   ├── ConfigTranslator.CLI.csproj
│   │   └── Program.cs
│   └── ConfigTranslator.Core/        # Логика
│       ├── ConfigTranslator.Core.csproj
│       ├── ConfigParser.cs           # Парсер (Sprache)
│       ├── Translator.cs             # Фасад
│       └── Models/
│           └── ConfigValue.cs
├── tests/
│   └── ConfigTranslator.Tests/
│       ├── ParserTests.cs            # 34 теста
│       ├── TranslatorTests.cs
│       └── IntegrationTests.cs
└── examples/
    ├── web_server.conf
    ├── database.conf
    └── game_config.conf
```

## Технологии

| Технология | Назначение |
|------------|------------|
| .NET 8.0+ | Платформа |
| Sprache 2.3.1 | Парсер-комбинаторы |
| xUnit | Тестирование |
| System.Text.Json | Генерация JSON |

## Покрытие тестами

Все конструкции языка покрыты тестами (44 теста):

- ✅ Числа (`3.14`, `.5`, `0.0`)
- ✅ Словари (`struct { }`)
- ✅ Вложенные словари (до 5 уровней)
- ✅ Константы (`global`)
- ✅ Ссылки на константы (`[имя]`)
- ✅ Комментарии (`<!-- -->`)
- ✅ Синтаксические ошибки

## Обработка ошибок

```
Синтаксическая ошибка в строке 5, позиция 12: unexpected ';'
```

```
Неизвестная константа: 'undefined'
```

## Архитектура

### Парсер-комбинаторы

Грамматика описана декларативно с помощью библиотеки Sprache:

```csharp
private static readonly Parser<ConfigValue> Number =
    from intPart in Parse.Digit.Many().Text()
    from dot in Parse.Char('.')
    from fracPart in Parse.Digit.AtLeastOnce().Text()
    select (ConfigValue)new NumberValue(
        double.Parse($"{intPart}.{fracPart}"));
```

### Этапы трансляции

```
Входной текст → Sprache Parser → AST → Resolve Constants → JSON
```

## Автор

asdwert7
