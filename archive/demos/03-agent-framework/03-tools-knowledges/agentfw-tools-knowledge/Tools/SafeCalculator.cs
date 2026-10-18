namespace AgentFwToolsKnowledge.Tools;

/// <summary>
/// Safe mathematical expression evaluator that only allows basic arithmetic operations.
/// This prevents code injection vulnerabilities that can occur with DataTable.Compute().
/// </summary>
public static class SafeCalculator
{
    public static string Evaluate(string expression)
    {
        try
        {
            // Remove whitespace
            expression = expression.Replace(" ", "");

            // Basic validation - only allow numbers, operators, parentheses, and decimal point
            if (!System.Text.RegularExpressions.Regex.IsMatch(expression, @"^[\d+\-*/().]+$"))
            {
                return $"Error: Invalid characters in expression '{expression}'. Only numbers and basic operators (+, -, *, /, parentheses) are allowed.";
            }

            // Evaluate the expression using a safe parser
            var result = EvaluateExpression(expression);
            return $"Result: {result}";
        }
        catch (Exception ex)
        {
            return $"Error: Could not calculate '{expression}' - {ex.Message}";
        }
    }

    private static double EvaluateExpression(string expression)
    {
        // Simple recursive descent parser for arithmetic expressions
        var index = 0;
        return ParseExpression(expression, ref index);
    }

    private static double ParseExpression(string expr, ref int index)
    {
        var left = ParseTerm(expr, ref index);

        while (index < expr.Length)
        {
            var op = expr[index];
            if (op != '+' && op != '-')
                break;

            index++;
            var right = ParseTerm(expr, ref index);
            left = op == '+' ? left + right : left - right;
        }

        return left;
    }

    private static double ParseTerm(string expr, ref int index)
    {
        var left = ParseFactor(expr, ref index);

        while (index < expr.Length)
        {
            var op = expr[index];
            if (op != '*' && op != '/')
                break;

            index++;
            var right = ParseFactor(expr, ref index);
            
            if (op == '/' && right == 0)
                throw new DivideByZeroException("Division by zero");

            left = op == '*' ? left * right : left / right;
        }

        return left;
    }

    private static double ParseFactor(string expr, ref int index)
    {
        // Handle negative numbers
        if (index < expr.Length && expr[index] == '-')
        {
            index++;
            return -ParseFactor(expr, ref index);
        }

        // Handle parentheses
        if (index < expr.Length && expr[index] == '(')
        {
            index++;
            var result = ParseExpression(expr, ref index);
            if (index >= expr.Length || expr[index] != ')')
                throw new FormatException("Mismatched parentheses");
            index++;
            return result;
        }

        // Parse number
        var start = index;
        while (index < expr.Length && (char.IsDigit(expr[index]) || expr[index] == '.'))
        {
            index++;
        }

        if (start == index)
            throw new FormatException($"Expected number at position {index}");

        return double.Parse(expr.Substring(start, index - start));
    }
}
