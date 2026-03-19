namespace Lecser
{
    public class Lexer
    {
        private string input;
        private int position;
        private int line;
        private string S;
        private long B;
        private string CS;
        private int z;
        public List<string> TW;  
        public List<string> TL;
        private List<string> TI;
        private List<string> TN;
        private List<Token> tokens;

        // Таблицы ограничителей
        private readonly Dictionary<string, int> OperatorToCode = new Dictionary<string, int>
        {
            { "{", 1 }, { "}", 2 }, { "(", 3 }, { ")", 4 }, { ";", 5 }, { ",", 6 }, { ":=", 7 },
            { "==", 8 }, { "!=", 9 }, { "<=", 10 }, { ">=", 11 }, { "<", 12 }, { ">", 13 }, 
            { "=", 14 }, { "+", 15 }, { "-", 16 }, { "*", 17 }, { "/", 18 }, { "&&", 19 }, 
            { "||", 20 }, { "!", 21 }
        };

        public Lexer(string code)
        {
            input = code.Replace("\r\n", "\n").Replace("\r", "\n"); 
            position = 0;
            line = 1;
            S = "";
            B = 0;
            CS = "H";
            z = 0;
            tokens = new List<Token>();

            // Таблицы
            TW = new List<string> { "dim", "integer", "real", "boolean", "begin", "end", "if", "else", "for", "to", "step", "next", "while", "readln", "writeln", "true", "false" };
            TL = new List<string> { "{", "}", "(", ")", ";", ",", ":=", "==", "!=", "<=", ">=", "<", ">", "=", "+", "-", "*", "/", "&&", "||", "!" };
            TI = new List<string>();
            TN = new List<string>();
        }
        public string GetKeywordTokenCode(string keyword)
        {
            int index = TW.IndexOf(keyword);
            return index >= 0 ? $"1.{index + 1}" : "0.0";
        }

        public string GetOperatorTokenCode(string op)
        {
            int index = TL.IndexOf(op);
            return index >= 0 ? $"2.{index + 1}" : "0.0";
        }

        private char gc()
        {
            if (position >= input.Length) return '\0';
            char ch = input[position];
            if (ch == '\n') line++;
            position++;
            return ch;
        }

        private bool let(char c) => char.IsLetter(c);
        private bool digit(char c) => char.IsDigit(c);
        private void nill() => S = "";
        private void add(char c)
        {
            if (c == '\r') return; // игнорируем \r
            if (c != '\n')
                S += c;
        }

        private int look(List<string> t)
        {
            z = t.IndexOf(S) + 1;
            return z;
        }

        private int put(List<string> t)
        {
            if (!t.Contains(S))
                t.Add(S);
            z = t.IndexOf(S) + 1;
            return z;
        }

        private void out_(string tokenType, string lexeme, string code, object value = null, string semanticType = null)
        {
            tokens.Add(new Token
            {
                Line = line,
                Lexeme = lexeme,
                TokenType = tokenType,
                TokenCode = code,
                SemanticType = semanticType ?? GetDefaultType(tokenType, lexeme),
                Value = value  // вот сюда кладём значение
            });
        }
        private string GetDefaultType(string tokenType, string lexeme)
        {
            if (tokenType == "number")
            {
                if (lexeme.Contains('.') || lexeme.Contains('e') || lexeme.Contains('E'))
                    return "real";
                return "integer";
            }
            if (tokenType == "keyword" && (lexeme == "true" || lexeme == "false"))
                return "boolean";
            return ""; // для идентификаторов — определяется при описании
        }

        private bool TryTranslate(int baseNum)
        {
            if (string.IsNullOrEmpty(S)) return false;

            char suffix = char.ToLower(S[S.Length - 1]);
            string digits = S.Substring(0, S.Length - 1);
            if (string.IsNullOrEmpty(digits)) return false;

            try
            {
                // Проверка цифр
                foreach (char c in digits)
                {
                    int val = char.IsDigit(c) ? c - '0' : char.ToLower(c) - 'a' + 10;
                    if (val >= baseNum) return false;
                }

                long value = Convert.ToInt64(digits, baseNum);
                S = value.ToString();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void translate(int baseNum)
        {
            //Debug.WriteLine($"[translate] Вход: S = '{S}'");
            if (string.IsNullOrEmpty(S)) { CS = "ER"; return; }

            char suffix = char.ToLower(S[S.Length - 1]);
            string digits = S.Substring(0, S.Length - 1);

            if (string.IsNullOrEmpty(digits)) { CS = "ER"; return; }

            try
            {
                // Проверка допустимости цифр
                foreach (char c in digits)
                {
                    int val = char.IsDigit(c) ? c - '0' : char.ToLower(c) - 'a' + 10;
                    if (val >= baseNum) throw new Exception();
                }

                long value = Convert.ToInt64(digits, baseNum);
                this.S = value.ToString();
                //Debug.WriteLine($"[translate] Выход: S = '{S}'");
            }
            catch
            {
                out_("error", $"Некорректное {GetBaseName(baseNum)} число: '{digits}'", "0.0");
                CS = "H"; // продолжаем анализ
            }
        }

        private string GetBaseName(int baseNum)
        {
            return baseNum switch
            {
                2 => "двоичное",
                8 => "восьмеричное",
                10 => "десятичное",
                16 => "шестнадцатеричное",
                _ => $"{baseNum}-ричное"
            };
        }

        private void convert()
        {
            // Для вещественных и экспоненты — оставляем как строку
            // Или парсим:
            try
            {
                double value = double.Parse(S, System.Globalization.NumberStyles.Float,
                                           System.Globalization.CultureInfo.InvariantCulture);
                S = value.ToString("G", System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                // оставляем как есть
            }
        }

        public List<Token> Tokenize()
        {
            // Защита от бесконечного цикла
            int safetyCounter = 0;
            const int MAX_STEPS = 100000;

            while (CS != "V" && safetyCounter++ < MAX_STEPS)
            {
                switch (CS)
                {
                    // Основные состояния
                    case "H": State_H(); break;
                    case "I": State_I(); break;
                    case "N2": State_N2(); break;   // основное состояние чисел (вместо N)

                    case "B": State_B(); break;   // после b
                    case "HX": State_HX(); break;   // после h
                    case "D": State_D(); break;   // после d
                    case "O": State_O(); break;   // после o

                    // Вещественные числа
                    case "E11": State_E11(); break; // .
                    case "E12": State_E12(); break; // .цифры
                    case "E21": State_E21(); break; // e/E
                    case "E22": State_E22(); break; // e[+/-]цифры

                    // Комментарии
                    case "C1": State_C1(); break;   // /
                    case "C2": State_C2(); break;   // /* ... *
                    case "C3": State_C3(); break;   // /* ... *

                    // Многосимвольные операторы
                    case "M1": State_M1(); break;   // <
                    case "M2": State_M2(); break;   // >
                    case "M3": State_M3(); break;   // :
                    case "M4": State_M4(); break;   // =
                    case "M5": State_M5(); break;   // !
                    case "M6": State_M6(); break;   // &
                    case "M7": State_M7(); break;   // |

                    // Ограничители
                    case "OG": State_OG(); break;   // {

                    // Финальные состояния
                    case "ER": State_ER(); break;   // ошибка
                    case "V": State_V(); break;     // выход

                    // Неизвестное состояние - ошибка
                    default:
                        out_("error", $"Неизвестное состояние: {CS}", "0.0");
                        CS = "V";
                        break;
                }
            }

            if (safetyCounter >= MAX_STEPS)
            {
                out_("error", "Превышено максимальное число шагов", "0.0");
            }

            // Обработка "висячих" состояний (если анализ не дошёл до V)
            if (CS == "I")
            {
                look(TW);
                if (z != 0) out_("keyword", S, $"1.{z}");
                else { put(TI); out_("identifier", S, $"3.{z}"); }
            }
            else if (CS == "N2" || CS == "B" || CS == "O" || CS == "D" || CS == "HX" || CS == "E12" || CS == "E22")
            {
                // Завершаем число
                if (CS == "B") translate(2);
                else if (CS == "O") translate(8);
                else if (CS == "D") translate(10);
                else if (CS == "HX") translate(16);
                // E12/E22 — вещественное, convert() не требуется
                put(TN);
                out_("number", S, $"4.{z}");
            }

            return tokens;
        }

        private void FinalizeNumber()
        {
            if (string.IsNullOrEmpty(S)) { CS = "H"; return; }

            // Попытка распознать как число со суффиксом
            if (S.Length >= 2)
            {
                char last = char.ToLower(S[S.Length - 1]);
                string digits = S.Substring(0, S.Length - 1);

                if (last == 'b' && digits.All(c => c == '0' || c == '1'))
                {
                    try
                    {
                        long value = Convert.ToInt64(digits, 2);  // уже число!
                        string stringValue = value.ToString();
                        int idx = put(TN);
                        out_("number", stringValue, $"4.{idx}", value, "integer");
                        //Debug.WriteLine($"→ Token: \"number\" {value} (\"integer\") ");

                        CS = "H";
                        return;
                    }
                    catch (OverflowException)
                    {
                        out_("error", $"Число слишком велико: {S}", "0.0");
                        CS = "H";
                        return;
                    }
                }
                if (last == 'o' && digits.All(c => c >= '0' && c <= '7'))
                {
                    try
                    {
                        long value = Convert.ToInt64(digits, 8);  // уже число!
                        string stringValue = value.ToString();
                        int idx = put(TN);
                        out_("number", stringValue, $"4.{idx}", value, "integer");
                        //Debug.WriteLine($"→ Token: \"number\" {value} (\"integer\") ");

                        CS = "H";
                        return;
                    }
                    catch (OverflowException)
                    {
                        out_("error", $"Число слишком велико: {S}", "0.0");
                        CS = "H";
                        return;
                    }
                }
                if (last == 'd' && digits.All(char.IsDigit))
                {
                    if (int.TryParse(S, out int intValue))
                    {
                        put(TN);
                        out_("number", S, $"4.{z}", intValue, "integer");
                        //Debug.WriteLine($"→ Token: \"number\" {intValue} (\"integer\") ");

                        CS = "H";
                        return;
                    }
                }
                if (last == 'h')
                {
                    try
                    {
                        try
                        {
                            long value = Convert.ToInt64(digits, 16);  // ← уже число!
                            string stringValue = value.ToString();
                            int idx = put(TN);
                            out_("number", stringValue, $"4.{idx}", value, "integer");
                            //Debug.WriteLine($"→ Token: \"number\" {value} (\"integer\") ");

                            CS = "H";
                            return;
                        }
                        catch (OverflowException)
                        {
                            out_("error", $"Число слишком велико: {S}", "0.0");
                            CS = "H";
                            return;
                        }
                    }
                    catch { }
                }
                if (S.Contains('.') || S.Contains('e') || S.Contains('E'))
                {
                    if (double.TryParse(S, System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, out double doubleValue))
                    {
                        put(TN);
                        out_("number", S, $"4.{z}", doubleValue, "real");
                        //Debug.WriteLine($"→ Token: \"number\" {doubleValue} (\"integer\") ");

                        CS = "H";
                        return;
                    }
                }

            }

            if (S.All(char.IsDigit))
            {
                if (long.TryParse(S, out long intValue))
                {
                    put(TN);
                    out_("number", S, $"4.{z}", intValue, "integer");
                    CS = "H";
                    return;
                }
            }

            // Если не число — ошибка
            out_("error", $"Некорректное число: {S}", "0.0");
            CS = "H";
        }

        private void ProcessInN2(char CH)
        {
            if (digit(CH) ||
        (CH >= 'a' && CH <= 'f') ||
        (CH >= 'A' && CH <= 'F'))
            {
                add(CH);
                CS = "N2";
                return;
            }

            // Суффиксы — снова переходим в специальные состояния
            if (CH == 'h' || CH == 'H') { add(CH); CS = "HX"; return; }

            // Иначе — завершаем число
            position--; if (CH == '\n') line--;
            FinalizeNumber();
            CS = "H";
        }

        

        // СОСТОЯНИЯ

        private void State_H()
        {
            char CH = gc();
            if (CH == '\0') { CS = "V"; return; }

            // Игнорируем пробелы и табуляцию
            if (CH == ' ' || CH == '\t' || CH == '\r' || CH == '\n')
            {
                CS = "H";  // просто пропускаем и возвращаемся в H
                return;
            }
            if (let(CH)) { nill(); add(CH); CS = "I"; return; }
            if (digit(CH))
            {
                nill();
                add(CH);
                CS = "N2";
                return;
            }
            if (CH == '{') { nill(); add(CH); CS = "OG"; return; }
            if (CH == '}') { out_("operator", "}", "2.2"); CS = "V"; return; }
            if (CH == ':') { nill(); add(CH); CS = "M3"; return; }
            if (CH == '/')
            {
                // Следующий символ (не считывая его!)
                char next = (position < input.Length) ? input[position] : '\0';
                if (next == '*')
                {
                    // Будет обработано как комментарий
                    nill(); add(CH); CS = "C1";
                    return;
                }
                else
                {
                    out_("operator", "/", "2.18");
                    CS = "H";
                    return;
                }
            }
            if (CH == '<') { nill(); add(CH); CS = "M1"; return; }
            if (CH == '>') { nill(); add(CH); CS = "M2"; return; }
            if (CH == '=') { nill(); add(CH); CS = "M4"; return; }
            if (CH == '!') { nill(); add(CH); CS = "M5"; return; }
            if (CH == '&') { nill(); add(CH); CS = "M6"; return; }
            if (CH == '|') { nill(); add(CH); CS = "M7"; return; }
            if (CH == '.') { nill(); add(CH); CS = "E11"; return; }

            // Односимвольные операторы
            if (OperatorToCode.ContainsKey(CH.ToString()))
            {
                out_("operator", CH.ToString(), $"2.{OperatorToCode[CH.ToString()]}");
                CS = "H";
                return;
            }

            out_("error", CH.ToString(), "0.0");
            CS = "V";
        }

        private void State_N2()
        {
            char CH = gc();
            //Debug.WriteLine($"{CH}, {S}");
            if (CH == '\0' || char.IsWhiteSpace(CH))
            {
                FinalizeNumber();
                CS = (CH == '\0') ? "V" : "H";
                return;
            }

            // Суффиксы
            if (CH == 'b' || CH == 'B') { add(CH); CS = "B"; return; }
            if (CH == 'h' || CH == 'H') { add(CH); CS = "HX"; return; }
            if (CH == 'd' || CH == 'D') { add(CH); CS = "D"; return; }
            if (CH == 'o' || CH == 'O') { add(CH); CS = "O"; return; }
            if ((CH == 'e' || CH == 'E') && S.Length > 0 && S.All(c => char.IsDigit(c)))
            {
                add(CH); CS = "E21"; return;
            }
            // Цифры
            if (digit(CH)) { add(CH); CS = "N2"; return; }

            // Буквы a-f/A-F
            if ((CH >= 'a' && CH <= 'f') || (CH >= 'A' && CH <= 'F'))
            {
                add(CH); CS = "N2"; return;
            }



            // Точка
            if (CH == '.' && position < input.Length && digit(input[position]))
            {
                add(CH); CS = "E11"; return;
            }

            // Иначе — завершаем число
            position--; if (CH == '\n') line--;
            FinalizeNumber();
            CS = "H";
        }

        private void State_I()
        {
            char CH = gc();
            if (let(CH) || digit(CH))
            {
                add(CH);
                CS = "I";
                return;
            }
            position--; // вернуть символ
            if (CH == '\n') line--;

            look(TW);
            if (z != 0) out_("keyword", S, $"1.{z}");
            else {
                put(TI);
                out_("identifier", S, $"3.{z}", value: S);

            }
            CS = "H";
        }


        private void State_C1()
        {
            char CH = gc();
            if (CH == '*') { CS = "C2"; return; }
            position--; if (CH == '\n') line--;
            out_("error", "/", "0.0");
            CS = "V";
        }

        private void State_C2()
        {
            char CH = gc();
            if (CH == '\0')
            {
                out_("error", "/* (незакрыт)", "0.0");
                CS = "V";
                return;
            }

            if (CH == '*')
            {
                CS = "C3";
                return;
            }

            if (CH == '\n')
                line++;

            CS = "C2";
        }

        private void State_C3()
        {
            char CH = gc();
            if (CH == '\0')
            {
                out_("error", "/* (незакрыт)", "0.0");
                CS = "V";
                return;
            }

            if (CH == '/')
            {
                CS = "H";
                return;
            }

            if (CH == '*')
            {
                CS = "C3";
                return;
            }

            if (CH == '\n')
                line++;

            CS = "C2";
        }

        private void State_M1() // <
        {
            char CH = gc();
            if (CH == '=') { out_("operator", "<=", "2.10"); CS = "H"; return; }
            if (CH == '>') { out_("operator", "!=", "2.9"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("operator", "<", "2.12"); CS = "H";
        }

        private void State_M2() // >
        {
            char CH = gc();
            if (CH == '=') { out_("operator", ">=", "2.11"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("operator", ">", "2.13"); CS = "H";
        }

        private void State_M3() // :
        {
            char CH = gc();
            if (CH == '=')
            {
                out_("operator", ":=", "2.7");
                CS = "H";
                return;
            }

            // Одиночный ':' — ошибка (нет в TL)
            position--; // возврат символа
            if (CH == '\n') line--;
            out_("error", ":", "0.0");
            CS = "V";
        }

        private void State_M4() // =
        {
            char CH = gc();
            if (CH == '=') { out_("operator", "==", "2.8"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("operator", "=", "2.14"); CS = "H";
        }

        private void State_M5() // !
        {
            char CH = gc();
            if (CH == '=') { out_("operator", "!=", "2.9"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("operator", "!", "2.21"); CS = "H";
        }

        private void State_M6() // &
        {
            char CH = gc();
            if (CH == '&') { out_("operator", "&&", "2.19"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("error", "&", "0.0");
            CS = "V";
        }

        private void State_M7() // |
        {
            char CH = gc();
            if (CH == '|') { out_("operator", "||", "2.20"); CS = "H"; return; }
            position--; if (CH == '\n') line--;
            out_("error", "|", "0.0"); CS = "V";
        }

        // После 'b' или 'B'
        private void State_B()
        {
            char CH = gc();

            // Если после 'b' — конец числа (пробел, ;, ), }, +, -, *, /, \0)
            if (CH == '\0' || char.IsWhiteSpace(CH) ||
                CH == ';' || CH == ',' || CH == ')' || CH == '}' ||
                CH == '+' || CH == '-' || CH == '*' || CH == '/' ||
                CH == '<' || CH == '>' || CH == '=')
            {
                // Попытка перевести как двоичное
                if (TryTranslate(2))                {
                    
                    int.TryParse(S, out int value);
                    put(TN);
                    out_("number", S, $"4.{z}",value, "integer");
                }
                else
                {
                    // ❗ Ошибка: выводим в ошибки
                    out_("error", $"Некорректное двоичное число: {S}", "0.0");
                }

                position--; if (CH == '\n') line--;
                CS = "H";
                return;
            }

            S = S.Substring(0, S.Length - 1); // убираем 'b'
            add('b'); // добавляем как символ (цифру 11)
            ProcessInN2(CH); // обрабатываем CH 
        }

        // После 'h' или 'H'
        private void State_HX()
        {
            char CH = gc();

            if (CH == '\0' || char.IsWhiteSpace(CH) ||
                CH == ';' || CH == ',' || CH == ')' || CH == '}' ||
                CH == '+' || CH == '-' || CH == '*' || CH == '/' ||
                CH == '<' || CH == '>' || CH == '=')
            {
                // Попытка перевести как двоичное
                if (TryTranslate(16))
                {
                    int.TryParse(S, out int value);
                    put(TN);
                    out_("number", S, $"4.{z}", value, "integer");

                }
                else
                {
                    out_("error", $"Некорректное шестнадцатиричное число: {S}", "0.0");
                }

                position--; if (CH == '\n') line--;
                CS = "H";
                return;
            }

            out_("error", $"Недопустимый символ 'o' в числе: {S}", "0.0");
            CS = "H";
        }

        // После 'd' или 'D'
        private void State_D()
        {
            char CH = gc();

            if (CH == '\0' || char.IsWhiteSpace(CH) ||
                CH == ';' || CH == ',' || CH == ')' || CH == '}' ||
                CH == '+' || CH == '-' || CH == '*' || CH == '/' ||
                CH == '<' || CH == '>' || CH == '=')
            {
                // Попытка перевести как двоичное
                if (TryTranslate(10))
                {
                    int.TryParse(S, out int value);
                    put(TN);
                    out_("number", S, $"4.{z}", value, "integer");

                }
                else
                {
                    out_("error", $"Некорректное десятичное число: {S}", "0.0");
                }

                position--; if (CH == '\n') line--;
                CS = "H";
                return;
            }

            // d — цифра hex (13)
            S = S.Substring(0, S.Length - 1);
            add('d');
            ProcessInN2(CH);
        }

        // После 'o' или 'O'
        private void State_O()
        {
            char CH = gc();

            if (CH == '\0' || char.IsWhiteSpace(CH) ||
                CH == ';' || CH == ',' || CH == ')' || CH == '}' ||
                CH == '+' || CH == '-' || CH == '*' || CH == '/')
            {
                // Попытка перевести как двоичное
                if (TryTranslate(8))
                {
                    int.TryParse(S, out int value);
                    put(TN);
                    out_("number", S, $"4.{z}", value, "integer");
                }
                else
                {
                    out_("error", $"Некорректное восьмиричное число: {S}", "0.0");
                }

                position--; if (CH == '\n') line--;
                CS = "H";
                return;
            }

            // o — недопустимо в hex (нет цифры 'o') - ошибка
            out_("error", $"Недопустимый символ 'o' в числе: {S}", "0.0");
            CS = "H";
        }

        private void State_E11() // .
        {
            char CH = gc();
            if (digit(CH)) { add(CH); CS = "E12"; return; }
            position--; if (CH == '\n') line--;
            out_("error", ".", "0.0"); CS = "V";
        }

        private void State_E12() // .число
        {
            char CH = gc();
            if (digit(CH)) { add(CH); CS = "E12"; return; }
            if (CH == 'e' || CH == 'E') { add(CH); CS = "E21"; return; }
            position--; if (CH == '\n') line--;
            convert();
            double.TryParse(S, out double value);
            put(TN);
            out_("number", S, $"4.{z}", value, "real");
            CS = "H";
        }

        private void State_E21() // e
        {
            char CH = gc();
            if (CH == 'h' || CH == 'H')
            {
                add(CH);
                CS = "HX";
                return;
            }
            if (CH == '+' || CH == '-') { add(CH); CS = "E22"; return; }
            if (digit(CH)) { add(CH); CS = "E22"; return; }
            position--; if (CH == '\n') line--;
            CS = "ER";
        }

        private void State_E22()
        {
            char CH = gc();
            if (digit(CH))
            {
                add(CH);
                CS = "E22";
                return;
            }
            if ((CH >= 'a' && CH <= 'f') || (CH >= 'A' && CH <= 'F'))
            {
                add(CH); CS = "N2"; return;
            }

            if (CH == 'h' || CH == 'H')
            {
                add(CH);
                CS = "HX";
                return;
            }
            // Завершаем экспоненту
            position--; if (CH == '\n') line--;

            convert(); double.TryParse(S, out double value);
            put(TN);
            out_("number", S, $"4.{z}", value, "real"); CS = "H";
        }

        private void State_OG() // {
        {
            look(TL);
            if (z != 0) { out_("operator", "{", "2.1"); CS = "H"; return; }
            CS = "ER";
        }

        private void State_ER()
        {
            out_("error", "ERR", "0.0");
            CS = "V";
        }

        private void State_V() { }
    }
}