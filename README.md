## Содержание

- [Описание](#описание)
- [Функциональность](#функциональность)
- [Структура проекта](#структура-проекта)
- [Установка](#установка)
- [Запуск](#запуск)
- [Пример использования](#пример-использования)
- [Грамматика языка](#грамматика-языка)

---

## Описание

Данный проект представляет собой **комплексный анализатор и интерпретатор** для модельного языка программирования, реализованный на языке C#.

Система включает:
- **Лексический анализатор** — токенизация исходного кода
- **Синтаксический анализатор** — рекурсивный спуск по грамматике
- **Семантический анализатор** — проверка типов и описаний
- **Генератор ПОЛИЗ** — построение промежуточного представления
- **Стековый интерпретатор** — выполнение ПОЛИЗ с поддержкой ввода/вывода

---

## Функциональность

### Анализ исходного кода
- Разбор ключевых слов, идентификаторов, чисел (десятичных, двоичных, восьмеричных, шестнадцатеричных)
- Поддержка комментариев `/* ... */`
- Обработка вещественных чисел с экспонентой

### Языковые конструкции
- **Описания**: `dim x, y integer;`
- **Типы данных**: `integer`, `real`, `boolean`
- **Присваивание**: `x := 5;`
- **Условные операторы**: `if (x > 0) ... else ...`
- **Циклы**: `while (x < 10) ...`, `for i := 1 to 10 ...`
- **Составные операторы**: `begin ... end`
- **Ввод/вывод**: `readln x;`, `writeln x;`

### Диагностика ошибок
- **Лексические ошибки** — неизвестные символы, некорректные числа
- **Синтаксические ошибки** — нарушение грамматики с указанием строки
- **Семантические ошибки** — неописанные переменные, несовместимость типов

### Генерация и выполнение
- Построение ПОЛИЗ в виде `List<string>`
- Стековый интерпретатор с поддержкой:
  - Арифметических операций: `+`, `-`, `*`, `/`
  - Сравнений: `<`, `>`, `==`, `!=`, `<=`, `>=`
  - Логических операций: `&&`, `||`, `!`
  - Условных и безусловных переходов: `$F`, `$`
  - Ввода/вывода: `R`, `W`
 
---

## Структура проекта

Lecser/

├── Lecser/

│ ├── Lexer.cs # Лексический анализатор

│ ├── SyntaxAnalyzer.cs # Синтаксический и семантический анализатор

│ ├── SimpleInterpreter.cs # Стековый интерпретатор ПОЛИЗ

│ ├── Token.cs # Структура токена

│ ├── SymbolInfo.cs # Информация об идентификаторе

│ ├── MainForm.cs # Главная форма приложения

│ └── Program.cs # Точка входа

├── Lecser.sln # Решение Visual Studio

└── README.md # Документация


---

## Установка

### Требования
- **.NET 8.0** или выше
- **Visual Studio 2022** (или другой совместимый редактор)

---

## Запуск 

Через Visual Studio
  - Открыть Lecser.sln
  - Нажать F5 или Debug - Start Debugging

---

## Пример использования

    {
    dim x, n, i integer;
    n := 3;
    for i := 1 to n
        begin
            readln x;
            if ((x >= 10) && (x < 20)) 
                writeln 1
            else 
                if ((x >= 20) && (x <= 30)) 
                    writeln 2
                else 
                    writeln 3
        end
    next;
    }

---

## Грамматика языка

Program→ {  ElementList; }

ElementList → Element | ElementList; Element

Element → DescriptionList | OperatorList

DescriptionList→ Description | DescriptionList; Description

Description→ dim IdentifierList Type

Type → integer | real | boolean

IdentifierList → Identifier| IdentifierList, Identifier

OperatorList → Operator | OperatorList; Operator

Operator → Compound | Assignment | Conditional | FixedLoop | ConditionalLoop | Input | Output

Compound → begin OperatorList end

Assignments → Identifier := Expression

Conditional → if ( Expression ) Operator else Operator | if ( Expression ) Operator

FixedLoop → for Assignment to Expression step Expression Operator next | for Assignment to Expression Operator next

ConditionalLoop → while ( Expression ) Operator

Input → readln IdentifierList

Output → writeln ExpressionList

ExpressionList → Expression | ExpressionList, Expression

Expression → Operand | Expression GroupRelationOperations Operand

Operand → Term | Operand GroupAdditionOperations  Term

Term → Factor | Term MultiplicationGroupOperations  Factor

Factor → Identifier | Number | BooleanConstant | UnaryOperation Factor | ( Expression )

GroupRelationOperations → != | == | < | <= | > | >=

GroupAdditionOperations → + | - | ||

MultiplicationGroupOperations → * | / | &&

UnaryOperation → !
 
BooleanConstant → true | false

Identifier → Letter | Identifier  Letter | Identifier Digit

Letter → a | b | c | d | e | f | g | h | i | j | k | l | m | n | o | p | q | r | s | t | u | v | w | x | y | z | 
             A | B | C | D | E | F | G | H | I | J | K | L | M | N | O | P | Q | R | S | T | U | V | W | X | Y | Z
             
Digit → 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9

Number → IntegerNumber | FloatNumber

IntegerNumber → BinaryNumber | OctalNumber | DecimalNumber | HexadecimalNumber

BinaryNumber → BinaryNumberList B | BinaryNumberList  b

BinaryNumberList → BinaryDigit | BinaryNumberList BinaryDigit 

BinaryDigit → 0 | 1

OctalNumber →  OctalNumberList  O | OctalNumberList o

OctalNumberList → OctalDigit | OctalNumberList OctalNumber 

OctalNumber → 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7

DecimalNumber → DecimalNumberList   D | DecimalNumberList d | DecimalNumberList    

DecimalNumberList → Digit | DecimalNumberList Digit 

HexadecimalNumber → HexadecimalNumberList H |HexadecimalNumberList  h

HexadecimalNumberList → Digit | HexadecimalNumberList  HexadecimalDigit 

HexadecimalDigit → Digit | a| b | c | d | e | f | A | B | C | D | E | F

RealNumber → DecimalNumber Exponent | DecimalNumber . DecimalNumber Exponent | . DecimalNumber Exponent | . DecimalNumber | DecimalNumber . DecimalNumber

Exponent → +E DecimalNumber | +e DecimalNumber | -E DecimalNumber | -e DecimalNumber | E DecimalNumber | e DecimalNumber

