using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static Lecser.SyntaxAnalyzer;

namespace Lecser
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private StringBuilder stackLog = new StringBuilder();

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            stackLog.Clear();
            SyntaxAnalyzer.StackLog = stackLog;

            var lexer = new Lexer(txtInput.Text);
            List<Token> tokens;
            string lexResult;

            try
            {
                tokens = lexer.Tokenize();
                var errors = tokens.Count(t => t.TokenType == "error");
                if (errors == 0)
                {

                    lexResult = $"Лексический анализ: успешно\n";
       
                }
                else
                {
                    var errorList = tokens
                        .Where(t => t.TokenType == "error")
                        .Select(t => $"Строка {t.Line}: '{t.Lexeme}'")
                        .ToList();
                    lexResult = $" Лексический анализ: {errors} ошибок\n" +
                                $" Ошибки:\n      " + string.Join("\n      ", errorList);
                }
            }
            catch (Exception ex)
            {
                lexResult = $"Лексический анализ: ошибка\n      {ex.Message}";
                tokens = new List<Token>();
            }
            /*
            foreach (var t in tokens)
            {
                Debug.WriteLine($"[{t.Line}] {t.TokenCode} '{t.Lexeme}' = {t.Value} ({t.SemanticType})");
            }*/

            string fullReport = "";
            bool success = false;
            SyntaxAnalyzer syntax = null;
            string polizText = "";
            string interpretationResult = "";

            // Анализ и интерпретация
            if (tokens != null && tokens.All(t => t.TokenType != "error"))
            {
                try
                {
                    syntax = new SyntaxAnalyzer();
                    fullReport = syntax.Analyze(tokens);
                    success = true;

                    polizText = syntax.GetPolishNotation();
                    
                    // Запускаем интерпретатор
                    try
                    {
                        var poliz = syntax.GetPolishCode();
                        var symbols = syntax.GetSymbolTable();
                        var interpreter = new Interpreter(poliz, symbols);

                        // Ввод — через UI
                        Func<string> input = () =>
                        {
                            using (var form = new System.Windows.Forms.Form())
                            {
                                form.Text = "Ввод";
                                var tb = new System.Windows.Forms.TextBox { Dock = System.Windows.Forms.DockStyle.Top };
                                var btn = new System.Windows.Forms.Button
                                {
                                    Text = "OK",
                                    DialogResult = System.Windows.Forms.DialogResult.OK,
                                    Dock = System.Windows.Forms.DockStyle.Bottom
                                };
                                form.Controls.AddRange(new System.Windows.Forms.Control[] { tb, btn });
                                form.AcceptButton = btn;
                                return form.ShowDialog() == System.Windows.Forms.DialogResult.OK ? tb.Text : "0";
                            }
                        };

                        interpreter.Run(
                            output: s => interpreter.ExecutionLog.Add("[ВЫВОД] " + s),
                            input: input
                        );

                        interpretationResult = string.Join("\n", interpreter.ExecutionLog);
                    }
                    catch (Exception ex)
                    {
                        interpretationResult = $"Ошибка интерпретации:\n{ex.Message}";
                    }
                   
                }
                catch (SyntaxAnalyzer.SemanticException ex)
                {
                    fullReport = $" Семантический анализ: ошибка\n      {ex.Message}";
                }
                catch (Exception ex)
                {
                    fullReport = $"Синтаксический анализ: ошибка\n      {ex.Message}";
                }
            }
            else
            {
                fullReport = "Синтаксический и семантический анализ: пропущены (ошибки в лексическом)";
            }

            // Формируем полный отчёт через StringBuilder
            var report = new StringBuilder();

            report.AppendLine("ОБЩИЙ ОТЧЁТ\n");
            report.AppendLine(lexResult);
            
            report.AppendLine(fullReport);
            
            // ПОЛИЗ
            if (!string.IsNullOrWhiteSpace(polizText))
            {
                report.AppendLine("ПОЛИЗ");
                report.AppendLine(polizText);
            }
            
            // Результат интерпретации
            if (!string.IsNullOrEmpty(interpretationResult))
            {
                report.AppendLine("\nРЕЗУЛЬТАТ ИНТЕРПРЕТАЦИИ");
                report.AppendLine(interpretationResult);
            }

            // Выводим в txtReport
            txtReport.Text = report.ToString();

            // Заполняем таблицу
            if (tokens != null)
            {
                var keywords = tokens.Where(t => t.TokenType == "keyword").ToList();
                var identifiers = tokens.Where(t => t.TokenType == "identifier").ToList();
                var numbers = tokens.Where(t => t.TokenType == "number").ToList();
                var operators = tokens.Where(t => t.TokenType == "operator").ToList();
                var errors = tokens.Where(t => t.TokenType == "error").ToList();

                FillKeywordTable(dgvKeywords, lexer.TW, keywords);
                FillOperatorTable(dgvOperators, lexer.TL, operators);
                FillGrid(dgvIdentifiers, identifiers);
                FillGrid(dgvNumbers, numbers);
            }
        }

        // Таблица ключевых слов
        private void FillKeywordTable(DataGridView dgv, List<string> allKeywords, List<Token> tokens)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.Columns.Add("Lines", "Строки");
            dgv.Columns.Add("Lexeme", "Лексема");
            dgv.Columns.Add("Code", "Код токена");

            foreach (var keyword in allKeywords.OrderBy(k => k))
            {
                var found = tokens.Where(t => t.Lexeme == keyword).ToList();
                if (found.Any())
                {
                    var lines = string.Join(", ", found.Select(t => t.Line).OrderBy(x => x));
                    dgv.Rows.Add(lines, keyword, found.First().TokenCode);
                }
                else
                {
                    // Код: 1.номер (номер = индекс + 1)
                    int index = allKeywords.IndexOf(keyword);
                    dgv.Rows.Add("", keyword, $"1.{index + 1}");
                }
            }
        }

        private void FillOperatorTable(DataGridView dgv, List<string> allOperators, List<Token> tokens)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.Columns.Add("Lines", "Строки");
            dgv.Columns.Add("Lexeme", "Лексема");
            dgv.Columns.Add("Code", "Код токена");

            foreach (var op in allOperators.OrderBy(o => o))
            {
                var found = tokens.Where(t => t.Lexeme == op).ToList();
                if (found.Any())
                {
                    var lines = string.Join(", ", found.Select(t => t.Line).OrderBy(x => x));
                    dgv.Rows.Add(lines, op, found.First().TokenCode);
                }
                else
                {
                    int index = allOperators.IndexOf(op);
                    dgv.Rows.Add("", op, $"2.{index + 1}");
                }
            }
        }

        private void FillGrid(DataGridView dgv, List<Token> tokens)
        {
            dgv.Rows.Clear();
            dgv.Columns.Clear();
            dgv.Columns.Add("Lines", "Строки");
            dgv.Columns.Add("Lexeme", "Лексема");
            dgv.Columns.Add("Code", "Код токена");

            var grouped = tokens
                .GroupBy(t => new { t.Lexeme, t.TokenCode })
                .Select(g => new
                {
                    Lines = string.Join(", ", g.Select(t => t.Line).OrderBy(x => x)),
                    g.Key.Lexeme,
                    g.Key.TokenCode
                })
                .OrderBy(x => x.Lexeme);

            foreach (var item in grouped)
            {
                dgv.Rows.Add(item.Lines, item.Lexeme, item.TokenCode);
            }
        }

    }
}