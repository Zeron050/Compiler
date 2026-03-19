public class Token
{
    public int Line { get; set; }
    public string Lexeme { get; set; }
    public string TokenType { get; set; } // keyword, identifier, number, operator, error
    public string TokenCode { get; set; } // "1.1", "2.5", ...
    public object Value { get; set; } // null по умолчанию; для чисел — int/long/double; для true/false — bool
    public string SemanticType { get; set; } // "integer", "real", "boolean"
}