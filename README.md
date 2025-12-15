## README.md

```markdown
# ConfigTranslator

Инструмент командной строки для трансляции учебного конфигурационного языка в JSON.

## Описание

Программа преобразует текст из входного конфигурационного формата в JSON. Синтаксические ошибки выявляются с выдачей сообщений с указанием позиции ошибки.

Для синтаксического разбора используется специализированный инструмент **Sprache** — библиотека парсер-комбинаторов для C#.

## Синтаксис конфигурационного языка

### Многострочные комментарии
```
<!--
Это многострочный
комментарий
-->
```

### Числа
Формат: `\d*\.\d+` (обязательная десятичная точка и дробная часть)
```
3.14
.5
0.0
```

### Имена
Формат: `[a-zA-Z][a-zA-Z0-9]*`
```
myVariable
server1
maxConnections
```

### Словари
```
struct {
    имя = значение,
    имя = значение
}
```

### Объявление константы
```
global имя = значение;
```

### Вычисление константы
```
[имя]
```

## Структура проекта

```
ConfigTranslator/
├── ConfigTranslator.sln
├── README.md
├── src/
│   ├── ConfigTranslator.CLI/          # Консольное приложение
│   │   └── Program.cs
│   └── ConfigTranslator.Core/         # Библиотека с логикой
│       ├── ConfigParser.cs            # Парсер на основе Sprache
│       ├── Translator.cs              # Фасад
│       └── Models/
│           └── ConfigValue.cs         # Модели значений
├── tests/
│   └── ConfigTranslator.Tests/        # Тесты
│       ├── ParserTests.cs
│       ├── TranslatorTests.cs
│       └── IntegrationTests.cs
└── examples/                          # Примеры конфигураций
    ├── web_server.conf
    ├── database.conf
    └── game_config.conf
```

## Используемые технологии

- **.NET 8.0** — платформа разработки
- **Sprache** — библиотека парсер-комбинаторов для синтаксического разбора
- **xUnit** — фреймворк для тестирования

## Установка и сборка

```bash
# Клонировать репозиторий
git clone <url>
cd ConfigTranslator

# Собрать проект
dotnet build

# Запустить тесты
dotnet test
```

## Использование

### Базовый запуск

```bash
dotnet run --project src/ConfigTranslator.CLI -- <путь_к_файлу>
```

### Примеры команд

```bash
# Транслировать файл конфигурации веб-сервера
dotnet run --project src/ConfigTranslator.CLI -- examples/web_server.conf

# Транслировать с явным указанием ключа -i
dotnet run --project src/ConfigTranslator.CLI -- -i examples/database.conf

# Транслировать с ключом --input
dotnet run --project src/ConfigTranslator.CLI -- --input examples/game_config.conf

# Сохранить результат в файл
dotnet run --project src/ConfigTranslator.CLI -- examples/web_server.conf > output.json

# Показать справку
dotnet run --project src/ConfigTranslator.CLI -- --help
```

### Пример входного файла

```
global timeout = 30.0;

server = struct {
    port = 8080.0,
    timeout = [timeout]
}
```

### Пример выходного JSON

```json
{
  "server": {
    "port": 8080.0,
    "timeout": 30.0
  }
}
```

## Запуск тестов

```bash
# Запустить все тесты
dotnet test

# Запустить с подробным выводом
dotnet test --verbosity normal
```

## Покрытие тестами

Все конструкции языка покрыты тестами:

| Конструкция | Тест |
|-------------|------|
| Числа `\d*\.\d+` | `Parse_SimpleNumber_ReturnsCorrectValue` |
| Числа без целой части `.5` | `Parse_NumberWithoutIntegerPart_Works` |
| Идентификаторы | `Parse_SimpleStruct_ReturnsDict` |
| Словари `struct {}` | `Parse_SimpleStruct_ReturnsDict` |
| Вложенные словари | `Parse_NestedStruct_ReturnsNestedDict` |
| Глубокая вложенность | `Parse_DeeplyNestedStruct_Works` |
| Пустой словарь | `Parse_EmptyStruct_Works` |
| Константы `global` | `Parse_GlobalConstant_CanBeReferenced` |
| Ссылки `[имя]` | `Parse_GlobalConstantInStruct_Works` |
| Комментарии `<!-- -->` | `Parse_Comment_IsIgnored` |
| Многострочные комментарии | `Parse_MultilineComment_IsIgnored` |
| Ошибка: неизвестная константа | `Parse_UndefinedConstant_ThrowsException` |

## Обработка ошибок

Программа выводит сообщения об ошибках с указанием позиции:

```
Ошибка синтаксиса в позиции 15: unexpected ';'
```

```
Неизвестная константа: 'undefined'
```

## Примеры конфигураций

В папке `examples/` находятся 3 примера из разных предметных областей:

1. **web_server.conf** — конфигурация веб-сервера (DevOps/Backend)
2. **database.conf** — конфигурация базы данных (Администрирование БД)
3. **game_config.conf** — конфигурация игры (Gamedev/RPG)

## Архитектура

### Подход: Парсер-комбинаторы (Sprache)

Библиотека Sprache позволяет описывать грамматику языка декларативно, комбинируя простые парсеры в более сложные:

```csharp
// Парсер числа: \d*\.\d+
private static readonly Parser<ConfigValue> Number =
    from intPart in Sprache.Parse.Digit.Many().Text()
    from dot in Sprache.Parse.Char('.')
    from fracPart in Sprache.Parse.Digit.AtLeastOnce().Text()
    select (ConfigValue)new NumberValue(
        double.Parse($"{intPart}.{fracPart}"));
```

### Этапы трансляции

```
Исходный текст → Sprache Parser → AST → Resolve Constants → JSON
```

1. **ConfigParser** — парсит входной текст, строит дерево значений
2. **ResolveConstants** — подставляет значения констант
3. **ToJson()** — генерирует JSON из дерева

### Компоненты

- **ConfigParser** — парсер на основе Sprache с правилами грамматики
- **Translator** — фасад, объединяющий парсинг и генерацию JSON
- **Models** — модели данных (NumberValue, DictValue, ConstantReference)

## Автор

Выполнено в рамках учебного задания.
```
