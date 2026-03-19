using System.Text;

namespace Lecser
{
    // Структуры для семантического анализа 
    public class SymbolInfo
    {
        public string Name { get; set; }
        public bool IsDeclared { get; set; } // 0 — не описан, 1 — описан
        public string Type { get; set; }     // "integer", "real", "boolean"
        public int Address { get; set; }     // условный адрес
    }


    public class SyntaxAnalyzer
    {
        // Синтаксический анализ
        private List<Token> tokenList; // храним полные Token для семантики
        private int position;

        // Генерация кода
        private static readonly List<string> PolishJumpOps = new()
{
    "$",   // 1 → безусловный переход (jump)
    "$F"   // 2 → условный переход по лжи (jump if false)
};
        private const string OP_READ = "R";
        private const string OP_WRITE = "W";
        private List<string> polishNotation = new(); // будет хранить строки: "x", "5", ":="
        private Stack<int> patchStack = new();

        public List<string> GetPolishCode() => new List<string>(polishNotation);
        public Dictionary<string, SymbolInfo> GetSymbolTable() => new Dictionary<string, SymbolInfo>(symbolTable);

        // Семантический анализ
        private Dictionary<string, SymbolInfo> symbolTable = new();
        private Stack<string> typeStack = new();
        private int nextAddress = 1000;
        public static StringBuilder StackLog { get; set; } // static

        // Таблица операций (TOP)
        private static readonly (string op, string type1, string type2, string resultType)[] OperationTable =
{
    // Арифметика
    ("+", "integer", "integer", "integer"),
    ("-", "integer", "integer", "integer"),
    ("*", "integer", "integer", "integer"),
    ("/", "integer", "integer", "integer"), 

    ("+", "real", "real", "real"),
    ("-", "real", "real", "real"),
    ("*", "real", "real", "real"),
    ("/", "real", "real", "real"), 

    // Смешанные арифметические операции
    ("+", "integer", "real", "real"),
    ("+", "real", "integer", "real"),
    ("-", "integer", "real", "real"),
    ("-", "real", "integer", "real"),
    ("*", "integer", "real", "real"),
    ("*", "real", "integer", "real"),
    ("/", "integer", "real", "real"),
    ("/", "real", "integer", "real"),

    // Сравнения
    ("==", "integer", "integer", "boolean"),
    ("==", "real", "real", "boolean"),
    ("==", "boolean", "boolean", "boolean"),
    ("==", "integer", "real", "boolean"),
    ("==", "real", "integer", "boolean"),

    ("!=", "integer", "integer", "boolean"),
    ("!=", "real", "real", "boolean"),
    ("!=", "boolean", "boolean", "boolean"),
    ("!=", "integer", "real", "boolean"),
    ("!=", "real", "integer", "boolean"),

    ("<", "integer", "integer", "boolean"),
    ("<", "real", "real", "boolean"),
    ("<", "integer", "real", "boolean"),
    ("<", "real", "integer", "boolean"),

    (">", "integer", "integer", "boolean"),
    (">", "real", "real", "boolean"),
    (">", "integer", "real", "boolean"),
    (">", "real", "integer", "boolean"),

    ("<=", "integer", "integer", "boolean"),
    ("<=", "real", "real", "boolean"),
    ("<=", "integer", "real", "boolean"),
    ("<=", "real", "integer", "boolean"),

    (">=", "integer", "integer", "boolean"),
    (">=", "real", "real", "boolean"),
    (">=", "integer", "real", "boolean"),
    (">=", "real", "integer", "boolean"),

    // Логические операции
    ("&&", "boolean", "boolean", "boolean"),
    ("||", "boolean", "boolean", "boolean"),

    // Унарное отрицание — уже обрабатывается отдельно в `CheckNotOperation`
};

        // Словари для расшифровки (для сообщений)
        private static readonly Dictionary<(int z, int n), string> TokenNames = new()
        {
            // Ключевые слова (z=1)
            {(1, 1), "dim"}, {(1, 2), "integer"}, {(1, 3), "real"}, {(1, 4), "boolean"},
            {(1, 5), "begin"}, {(1, 6), "end"}, {(1, 7), "if"}, {(1, 8), "else"},
            {(1, 9), "for"}, {(1, 10), "to"}, {(1, 11), "step"}, {(1, 12), "next"},
            {(1, 13), "while"}, {(1, 14), "readln"}, {(1, 15), "writeln"},
            {(1, 16), "true"}, {(1, 17), "false"},

            // Операторы (z=2)
            {(2, 1), "{"}, {(2, 2), "}"}, {(2, 3), "("}, {(2, 4), ")"},
            {(2, 5), ";"}, {(2, 6), ","}, {(2, 7), ":="}, {(2, 8), "=="}, {(2, 9), "!="},
            {(2, 10), "<="}, {(2, 11), ">="}, {(2, 12), "<"}, {(2, 13), ">"}, {(2, 14), "="},
            {(2, 15), "+"}, {(2, 16), "-"}, {(2, 17), "*"}, {(2, 18), "/"},
            {(2, 19), "&&"}, {(2, 20), "||"}, {(2, 21), "!"}
        };

        // Публичный метод анализа
        public string Analyze(List<Token> lexicalTokens)
        {
            tokenList = lexicalTokens.Where(t => t.TokenType != "error").ToList();
            position = 0;
            symbolTable.Clear();
            typeStack.Clear();
            nextAddress = 1000;

            try
            {
                Program();

                if (position < tokenList.Count)
                    throw Error($"Лишние символы после конца программы. Ожидалось '}}', получено: {CurrentToken()}");

                return BuildReport();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        // Синтаксический анализ с семантическими проверками 

        private void Program()
        {
            Expect(2, 1); // {
            while (!Match(2, 2)) // пока не }
            {
                if (position >= tokenList.Count)
                    throw Error("Ожидался '}' для завершения программы, достигнут конец файла");

                Element();

                if (!Match(2, 5)) // ;
                    throw Error($"Ожидалась ';' после оператора или описания. Получено: {CurrentToken()}");
                position++;
            }
            Expect(2, 2); // }
            GenOp(".");
        }

        private void Element()
        {
            if (Match(1, 1)) // dim
                Description();
            else
                Operator();
        }

        private void Description()
        {
            Expect(1, 1); // dim

            // Стек для номеров идентификаторов
            var idStack = new Stack<string>();
            idStack.Push("0"); // маркер конца

            // Первый идентификатор
            if (!Match(3))
                throw Error("Ожидался идентификатор после 'dim'");
            idStack.Push(GetCurrentLexeme());
            position++;

            // Дополнительные идентификаторы
            while (Match(2, 6)) // ,
            {
                position++;
                if (!Match(3))
                    throw Error("Ожидался идентификатор после ','");
                idStack.Push(GetCurrentLexeme());
                position++;
            }

            // Определяем тип
            string type;
            if (Match(1, 2)) { type = "integer"; }
            else if (Match(1, 3)) { type = "real"; }
            else if (Match(1, 4)) { type = "boolean"; }
            else throw Error("Ожидался тип: integer, real, boolean");
            position++;

            // dec(t): извлекаем из стека до 0 и заполняем таблицу
            string id;
            while ((id = idStack.Pop()) != "0")
            {
                if (symbolTable.TryGetValue(id, out var existing) && existing.IsDeclared)
                    throw SemanticError($"Идентификатор '{id}' уже описан");

                symbolTable[id] = new SymbolInfo
                {
                    Name = id,
                    IsDeclared = true,
                    Type = type,
                    Address = nextAddress++
                };
            }
        }

        private void Operator()
        {
            if (Match(1, 5)) // begin
                Compound();
            else if (Match(1, 9)) // for ← добавь эту проверку В САМОЕ НАЧАЛО
                FixedLoop();
            else if (Match(1, 7)) // if
                Conditional();
            else if (Match(3) && LookAhead(2, 7)) // идентификатор :=
                Assignment();
            else if (Match(1, 13)) // while
                ConditionalLoop();
            else if (Match(1, 14)) // readln
                Input();
            else if (Match(1, 15)) // writeln
                Output();
            else
                throw Error($"Неизвестный оператор: {CurrentToken()}");
        }

        private void Compound()
        {
            Expect(1, 5); // begin

            // Если сразу end — пустой блок
            if (Match(1, 6)) // end
            {
                position++;
                return;
            }

            OperatorList();

            // После OperatorList должен быть end
            if (!Match(1, 6))
                throw Error("Ожидался 'end' после составного оператора");
            position++; // съели end
        }

        private void SimpleOperand()
        {
            if (Match(4)) // число
            {
                GenNum(tokenList[position].Value);
                position++;
            }
            else if (Match(3)) // идентификатор
            {
                GenId(GetCurrentLexeme());
                position++;
            }
            else
                throw Error($"Ожидалось число или идентификатор, получено {CurrentToken()}");
        }

        private void OperatorList()
        {
            Operator();
            while (Match(2, 5)) // ;
            {
                position++; // съели ';'

                // Если следующий токен — 'end', выходим
                if (Match(1, 6)) // end
                    return;

                // Иначе — следующий оператор
                Operator();
            }
        }

        // Проверка присваивания
        private void Assignment()
        {
            if (!Match(3))
                throw SemanticError("Ожидался идентификатор в левой части присваивания");

            string varName = GetCurrentLexeme();
            var leftToken = tokenList[position];
            position++; // съели идентификатор
            GenId(leftToken.Lexeme, asAddress: true);
            Expect(2, 7); // :=

            // Теперь — полноценное выражение
            Expression(); // сгенерирует ВЕСЬ ПОЛИЗ правой части

            // Семантическая проверка типов 
            if (!symbolTable.TryGetValue(varName, out var info) || !info.IsDeclared)
                throw SemanticError($"Идентификатор '{varName}' не описан");

            if (typeStack.Count == 0)
                throw SemanticError("Выражение в правой части не имеет типа");
            string rightType = typeStack.Pop();

            if (info.Type != rightType)
                throw SemanticError($"Несовместимые типы в присваивании: {info.Type} := {rightType}");

            // Генерация: левый операнд (адрес!) + оператор присваивания
            GenOp(":=");
        }

        // Проверка условий
        private void Conditional()
        {
            Expect(1, 7); // if
            Expect(2, 3); // (

            Expression(); // B → x 0 >
            CheckBooleanCondition();

            // 1. Сгенерировать: ? $F 
            int p1MetkaSlot = polishNotation.Count; // индекс заглушки под метку
            polishNotation.Add("?");                // [?, ...]
            GenOp("$F");                            // [?, $F, ...]

            Expect(2, 4); // )
            Operator();   // S1 → x x 1 + :=

            if (Match(1, 8)) // else
            {
                // 2. Сгенерировать: ? $
                int p2MetkaSlot = polishNotation.Count;
                polishNotation.Add("?");
                GenOp("$");

                // 3. Запатчить p1: else начинается СЛЕДУЮЩИМ элементом (1-based!)
                int elseStart = polishNotation.Count + 1;
                polishNotation[p1MetkaSlot] = elseStart.ToString();

                position++; // 'else'
                Operator(); // S2 → x x 1 - :=

                // 4. Запатчить p2: после if
                int afterIf = polishNotation.Count + 1;
                polishNotation[p2MetkaSlot] = afterIf.ToString();
            }
            else
            {
                // Без else: p1 → за S1
                int afterIf = polishNotation.Count + 1;
                polishNotation[p1MetkaSlot] = afterIf.ToString();
            }

        }

        private void ConditionalLoop()
        {
            Expect(1, 13); // while
            Expect(2, 3); // (

            int loopStart = polishNotation.Count + 1; // 1-based начало условия
            Expression(); // B
            CheckBooleanCondition();



            // ? $F
            int p1MetkaSlot = polishNotation.Count;
            polishNotation.Add("?");
            GenOp("$F");

            Expect(2, 4); // )
            Operator(); // S

            // loopStart $
            GenNum(loopStart); // число — метка
            GenOp("$");

            // Запатчить p1: после тела цикла
            int afterLoop = polishNotation.Count + 1;
            polishNotation[p1MetkaSlot] = afterLoop.ToString();

        }
        private void FixedLoop()
        {
            Expect(1, 9); // for

            // i := a
            if (!Match(3)) throw SemanticError("Ожидался идентификатор в for");
            string counter = GetCurrentLexeme();
            position++;
            Expect(2, 7); // :=
            GenId(counter, asAddress: true);
            Expression(); // a
            GenOp(":=");

            Expect(1, 10); // to

            // ТЕЛО ЦИКЛА: сначала условие
            // Запоминаем НАЧАЛО УСЛОВИЯ
            int loopConditionStart = polishNotation.Count + 1;

            // i b >
            GenId(counter);
            SimpleOperand(); // b
            GenOp("<");
            int exitSlot = polishNotation.Count;
            polishNotation.Add("?");
            GenOp("$F");

            // Проверка на step
            long step = 1;
            if (Match(1, 11)) // step 
            {
                position++; // съесть 'step'

                // Поддержка только чисел
                if (Match(4)) // число
                {
                    var token = tokenList[position];
                    step = Convert.ToInt64(token.Value);
                    position++;
                }
                else
                {
                    throw SemanticError("После 'step' ожидается целое число");
                }
            }

            Operator(); // x := x + i

            // Шаг: i := i + step
            GenId(counter, asAddress: true);
            GenId(counter);
            GenNum(step);
            GenOp("+");
            GenOp(":=");

            // Прыжок
            GenNum(loopConditionStart);
            GenOp("$");

            // Патчинг
            int afterLoop = polishNotation.Count + 1;
            polishNotation[exitSlot] = afterLoop.ToString();

            Expect(1, 12); // next
        }
        private void Input()
        {
            Expect(1, 14); // readln

            do
            {
                if (!Match(3))
                    throw SemanticError("Ожидался идентификатор в readln");

                string id = GetCurrentLexeme();
                CheckIdentifier(id); // семантика: описан?
                position++;

                // Генерация: адрес переменной + R
                GenId(id, asAddress: true); // ← адрес!
                GenOp(OP_READ);             // R
            }
            while (Match(2, 6)); // , — продолжаем

        }
        private void Output()
        {
            Expect(1, 15); // writeln

            // Скобки необязательны
            bool hasParens = Match(2, 3);
            if (hasParens) position++;

            Expression();
            GenOp(OP_WRITE);

            if (hasParens)
                Expect(2, 4); // )
        }

        private void ExpressionList()
        {
            Expression();
            while (Match(2, 6)) // ,
            {
                position++;
                Expression();
            }
        }
        private void Expression()
        {
            LogicalExpr();
        }
        private void LogicalExpr()
        {
            RelationalExpr();
            while (Match(2, 19) || Match(2, 20)) // &&, ||
            {
                string op = GetCurrentLexeme();
                position++;
                RelationalExpr();
                CheckBinaryOperation(op);
                GenOp(op);
            }
        }
        private void RelationalExpr()
        {
            AdditiveExpr();
            while (Match(2, 8) || Match(2, 9) || Match(2, 12) || Match(2, 10) || Match(2, 13) || Match(2, 11))
            {
                string op = GetCurrentLexeme();
                position++;
                AdditiveExpr();
                CheckBinaryOperation(op);
                GenOp(op);
            }
        }

        private void AdditiveExpr()
        {
            Term();
            while (Match(2, 15) || Match(2, 16)) // +, -
            {
                string op = GetCurrentLexeme();
                position++;
                Term();
                CheckBinaryOperation(op);
                GenOp(op);
            }
        }

        private void Term()
        {
            Factor();
            while (Match(2, 17) || Match(2, 18)) // *, /
            {
                string op = GetCurrentLexeme();
                position++;
                Factor();
                CheckBinaryOperation(op);
                GenOp(op);
            }
        }

        private void Factor()
        {
            if (Match(3)) // идентификатор
            {
                string name = GetCurrentLexeme();
                position++;
                CheckIdentifier(name);
                GenId(name); // значение переменной
            }
            else if (Match(4)) // число
            {
                var token = tokenList[position];
                position++;
                string type = token.SemanticType; // уже есть в Token
                typeStack.Push(type);
                GenNum(token.Value); // число
            }
            else if (Match(1, 16) || Match(1, 17)) // true / false
            {
                string lex = GetCurrentLexeme();
                position++;
                typeStack.Push("boolean");
                GenId(lex); // true/false как идентификаторы
            }
            else if (Match(2, 21)) // !
            {
                position++;
                Factor();
                GenOp("!"); // ← после операнда
                CheckNotOperation(); // семантика остаётся
            }
            else if (Match(2, 3)) // (
            {
                position++;
                Expression();
                Expect(2, 4); // )
            }
            else
            {
                throw Error($"Неизвестный фактор: {CurrentToken()}");
            }
        }
        
        // Вспомогательные методы семантики 
        private void CheckIdentifier(string name)
        {
            if (!symbolTable.TryGetValue(name, out var info) || !info.IsDeclared)
                throw SemanticError($"Идентификатор '{name}' не описан");
            typeStack.Push(info.Type);
        }

        private void CheckBinaryOperation(string opLexeme)
        {
            if (StackLog != null)
                StackLog.AppendLine($"[Стек перед '{opLexeme}']: [{string.Join(", ", typeStack.Reverse())}]");

            if (typeStack.Count < 2)
                throw SemanticError($"Недостаточно операндов для операции '{opLexeme}'");

            string type2 = typeStack.Pop();
            string type1 = typeStack.Pop();

            var match = OperationTable.FirstOrDefault(r =>
                r.op == opLexeme && r.type1 == type1 && r.type2 == type2);

            if (match == default)
                throw SemanticError($"Несовместимые типы '{type1}' и '{type2}' в операции '{opLexeme}'");

            typeStack.Push(match.resultType);
            if (StackLog != null)
                StackLog.AppendLine($"[Стек после '{opLexeme}']: [{string.Join(", ", typeStack.Reverse())}]");
        }

        private void CheckNotOperation()
        {
            if (typeStack.Count < 1)
                throw SemanticError("Недостаточно операндов для операции '!'");

            string type = typeStack.Pop();
            if (type != "boolean")
                throw SemanticError($"Операция '!' применима только к boolean, а не к '{type}'");

            typeStack.Push("boolean");
        }

        private void CheckBooleanCondition()
        {
            if (typeStack.Count < 1)
                throw SemanticError("Условие не имеет типа");

            string type = typeStack.Pop();
            if (type != "boolean")
                throw SemanticError($"Условие должно быть boolean, а не '{type}'");
        }

        // Вспомогательные методы синтаксиса
        private bool Match(int z, int n) =>
            position < tokenList.Count &&
            tokenList[position].TokenCode == $"{z}.{n}";

        private bool Match(int z) =>
            position < tokenList.Count &&
            tokenList[position].TokenCode.StartsWith($"{z}.");

        private bool LookAhead(int z, int n)
        {
            bool result = position + 1 < tokenList.Count &&
                          tokenList[position + 1].TokenCode == $"{z}.{n}";
            //Debug.WriteLine($"LookAhead({z},{n}) at pos={position}: {result} | nextToken = '{tokenList[position + 1]?.Lexeme}' ({tokenList[position + 1]?.TokenCode})");
            return result;
        }

        private void Expect(int z, int n)
        {
            if (!Match(z, n))
                throw Error($"Ожидалось '{TokenName(z, n)}', получено: {CurrentToken()}");
            position++;
        }


        private string CurrentToken() =>
            position < tokenList.Count ? $"'{tokenList[position].Lexeme}'" : "конец файла";

        private string TokenName(int z, int n) =>
            TokenNames.TryGetValue((z, n), out var name) ? name : $"({z};{n})";

        private string GetCurrentLexeme() =>
            position < tokenList.Count ? tokenList[position].Lexeme : "";

        public class SemanticException : Exception
        {
            public SemanticException(string message) : base(message) { }
        }

        private Exception SemanticError(string message) =>
            new SemanticException($"Семантическая ошибка: Ошибка на строке {GetCurrentLine()}: {message}");


        private int GetCurrentLine()
        {
            // Если ошибка связана с отсутствием ожидаемого символа — смотрим на предыдущий токен
            if (position > 0 && position <= tokenList.Count)
                return tokenList[position - 1].Line;
            if (position < tokenList.Count)
                return tokenList[position].Line;
            return 1;
        }

        private Exception Error(string message)
        {
            int line = position > 0 && position <= tokenList.Count
        ? tokenList[position - 1].Line
        : (position < tokenList.Count ? tokenList[position].Line : 1);
            int pos = position; // можно оставить для отладки
            return new Exception($"Синтаксический анализ: Ошибка на строке {line} (позиция {pos}): {message}");
        }

        // Формирование отчёта
        private string BuildReport()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Синтаксический и семантический анализ пройдены успешно.");

            return sb.ToString();
        }

        // Вспомогательные методы генерации

        private void GenOp(string op)
        {
            polishNotation.Add(op);
            //Debug.WriteLine($"→ GenOp: '{op}' → ПОЛИЗ: [{string.Join(", ", polishNotation)}]");
        }

        private void GenId(string name, bool asAddress = false)
        {
            if (string.IsNullOrEmpty(name))
                throw new Exception("GenId вызван с пустым именем!");

            string token = asAddress ? "@" + name : name;
            polishNotation.Add(token);
            //Debug.WriteLine($"→ GenId: '{token}' → ПОЛИЗ: [{string.Join(", ", polishNotation)}]");
        }

        private void GenNum(object value)
        {
            string s = value switch
            {
                long l => l.ToString(),
                double d => d.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? "??"
            };
            polishNotation.Add(s);
            //Debug.WriteLine($"→ GenNum: {s} → ПОЛИЗ: [{string.Join(", ", polishNotation)}]");

        }
        public string GetPolishNotation()
        {
            var lines = new List<string>();
            var current = new List<string>();

            foreach (var item in polishNotation)
            {
                if (item == "\n")
                {
                    if (current.Count > 0)
                        lines.Add(string.Join(" ", current));
                    current.Clear();
                }
                else
                {
                    current.Add(item);
                }
            }
            if (current.Count > 0)
                lines.Add(string.Join(" ", current));

            return string.Join("\n", lines);
        }
    }
}