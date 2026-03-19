using System.Diagnostics;

namespace Lecser
{
    public class Interpreter
    {
        private List<string> program;
        private Stack<object> stack = new Stack<object>();
        private Dictionary<string, object> memory = new Dictionary<string, object>();
        private int ip = 0;

        // Для отладки — лог выполнения
        public List<string> ExecutionLog { get; } = new List<string>();

        public Interpreter(List<string> poliz, Dictionary<string, SymbolInfo> symbolTable)
        {
            program = poliz ?? throw new ArgumentNullException(nameof(poliz));
            foreach (var s in symbolTable.Values)
            {
                memory[s.Name] = s.Type switch
                {
                    "integer" => 0L,
                    "real" => 0.0,
                    "boolean" => false,
                    _ => null
                };
            }
        }

        public void Run(Action<string> output = null, Func<string> input = null)
        {
            output ??= s => ExecutionLog.Add("[OUT] " + s);
            input ??= () => { ExecutionLog.Add("[IN] ?"); return "0"; };
            ip = 0;
            stack.Clear();
            ExecutionLog.Clear();


            while (ip < program.Count)
            {
                if (ip < 0 || ip >= program.Count)
                    throw new Exception($"ip={ip} вышел за пределы ПОЛИЗа (0..{program.Count - 1})");

                string token = program[ip];
                ip++;

                /*
                // ДЕТАЛЬНЫЙ ДЕБАГ: стек ДО выполнения
                var stackDump = string.Join(", ", stack.Reverse().Select(x =>
                    x is string s ? $"\"{s}\"" :
                    x is bool b ? b.ToString().ToLower() :
                    x.ToString()));
                ExecutionLog.Add($"[{ip,2}] {token,-8} | стек: [{stackDump}]");
                Debug.WriteLine($"[{ip,2}] {token,-8} | стек: [{stackDump}]");*/


                try
                {
                    // 1. Адрес: &x
                    if (token.StartsWith("@"))
                    {
                        string name = token.Substring(1);
                        stack.Push(name);
                        continue;
                    }

                    // 2. Число
                    if (double.TryParse(token, out double num))
                    {
                        stack.Push(num == (long)num ? (object)(long)num : num);
                        continue;
                    }

                    // 3. true / false
                    if (token == "true" || token == "false")
                    {
                        stack.Push(token == "true");
                        continue;
                    }

                    // 4. Идентификатор
                    if (memory.ContainsKey(token))
                    {
                        stack.Push(memory[token]);
                        continue;
                    }

                    // 5. Операции
                    switch (token)
                    {
                        case ":=":
                            if (stack.Count < 2) throw new Exception("Нужны: адрес, значение");
                            var val = stack.Pop();
                            var addr = (string)stack.Pop();
                            memory[addr] = val;
                            break;

                        case "+": BinaryOp((a, b) => a + b); break;
                        case "-": BinaryOp((a, b) => a - b); break;
                        case "*": BinaryOp((a, b) => a * b); break;
                        case "/":
                            var bObj = PopNumberObj();
                            var aObj = PopNumberObj();
                            if (aObj is long al && bObj is long bl)
                                stack.Push(al / bl);
                            else
                                stack.Push(ToDouble(aObj) / ToDouble(bObj));
                            break;

                        case "<": NumericCmp((a, b) => a < b); break;
                        case ">": NumericCmp((a, b) => a > b); break;
                        case "<=": NumericCmp((a, b) => a <= b); break;
                        case ">=": NumericCmp((a, b) => a >= b); break;
                        case "==": NumericCmp((a, b) => a == b); break;
                        case "!=": NumericCmp((a, b) => a != b); break;

                        case "!":
                            if (stack.Count < 1) throw new Exception("Нет операнда для !");
                            stack.Push(!(bool)stack.Pop());
                            break;

                        case "&&":
                            if (stack.Count < 2) throw new Exception("Нужны 2 булевых для &&");
                            var r = stack.Pop();
                            var l = stack.Pop();
                            //ExecutionLog.Add($"    &&: l={l} ({l?.GetType()}), r={r} ({r?.GetType()})");
                            Debug.WriteLine($"    &&: l={l} ({l?.GetType()}), r={r} ({r?.GetType()})");

                            stack.Push((bool)l && (bool)r);
                            break;

                        case "||":
                            if (stack.Count < 2) throw new Exception("Нужны 2 булевых для ||");
                            r = stack.Pop();
                            l = stack.Pop();
                            //ExecutionLog.Add($"    ||: l={l} ({l?.GetType()}), r={r} ({r?.GetType()})");
                            Debug.WriteLine($"    ||: l={l} ({l?.GetType()}), r={r} ({r?.GetType()})");

                            stack.Push((bool)l || (bool)r);
                            break;

                        case "R":
                            if (stack.Count < 1) throw new Exception("Нет адреса для R");
                            var addrR = (string)stack.Pop();
                            string text = input();
                            output($"[ввод] {addrR} = {text} ");

                            memory[addrR] = ParseInput(text, memory[addrR]);
                            break;

                        case "W":
                            if (stack.Count < 1) throw new Exception("Нет значения для W");
                            output(stack.Pop().ToString());
                            break;

                        case "$F":
                            if (stack.Count < 2) throw new Exception("Нужны: условие, метка для $F");
                            var lblStr = stack.Pop().ToString();
                            if (!int.TryParse(lblStr, out int lbl)) throw new Exception($"Метка не число: '{lblStr}'");
                            var cond = stack.Pop();
                            //ExecutionLog.Add($"    $F: cond={cond} ({cond?.GetType()}), lbl={lbl}");
                            Debug.WriteLine($"    $F: cond={cond} ({cond?.GetType()}), lbl={lbl}");


                            if (!(cond is bool))
                                throw new Exception($"Условие $F не bool, а {cond?.GetType()}: {cond}");
                            if (!(bool)cond) ip = lbl - 1;
                            break;

                        case "$":
                            if (stack.Count < 1) throw new Exception("Нужна метка для $");
                            lblStr = stack.Pop().ToString();
                            if (!int.TryParse(lblStr, out lbl)) throw new Exception($"Метка не число: '{lblStr}'");
                            //ExecutionLog.Add($"    $: переход на {lbl}");
                            Debug.WriteLine($"    $: переход на {lbl}");

                            ip = lbl - 1;
                            break;

                        case ".":
                            ip = program.Count;
                            break;

                        default:
                            throw new Exception($"Неизвестный токен: '{token}'");
                    }
                }
                catch (Exception ex)
                {
                    //ExecutionLog.Add($"ОШИБКА на [{ip}] '{token}': {ex.Message}");
                    throw;
                }
            }

            var finalStack = string.Join(", ", stack.Reverse().Select(x => x is string s ? $"\"{s}\"" : x.ToString()));
            //ExecutionLog.Add($"Завершено. Стек: [{finalStack}]");
            Debug.WriteLine($"Завершено. Стек: [{finalStack}]");
        }

        private double PopNumber()
        {
            var obj = stack.Pop();
            return obj is long l ? l : Convert.ToDouble(obj);
        }

        private void BinaryOp(Func<double, double, double> op)
        {
            var b = PopNumber(); var a = PopNumber();
            var res = op(a, b);
            stack.Push(res == (long)res ? (object)(long)res : res);
        }

        private void NumericCmp(Func<double, double, bool> comparer)
        {
            if (stack.Count < 2) throw new Exception("Недостаточно операндов для сравнения");
            var b = PopNumber(); // → double
            var a = PopNumber(); // → double
            stack.Push(comparer(a, b));
        }

        private object ParseInput(string s, object sample)
        {
            return sample switch
            {
                bool _ => bool.Parse(s),
                long _ => long.Parse(s),
                double _ => double.Parse(s),
                _ => s
            };          
        }
        private object PopNumberObj()
        {
            return stack.Pop();
        }

        private double ToDouble(object obj)
        {
            return obj is long l ? l : Convert.ToDouble(obj);
        }
    }
}