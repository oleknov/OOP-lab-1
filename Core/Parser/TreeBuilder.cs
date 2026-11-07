using System.Numerics;
using Antlr4.Runtime;
using SpreadsheetEditor.Core.Tree;

namespace SpreadsheetEditor.Core.Parser;

internal sealed class TreeBuilder : FormulaBaseVisitor<Node>
{
    public override Node VisitFormula(FormulaParser.FormulaContext context) => Visit(context.expression());

    public override Node VisitParentheses(FormulaParser.ParenthesesContext context) => Visit(context.expression());

    public override Node VisitNumber(FormulaParser.NumberContext context) =>
        new NumberNode(BigInteger.Parse(context.GetText()));

    public override Node VisitReference(FormulaParser.ReferenceContext context)
    {
        var name = context.Start;
        return CellAddress.TryParse(name.Text, out var address)
            ? new ReferenceNode(address, name.StartIndex)
            : throw new ExpressionException(name.StartIndex, $"невірна адреса клітинки '{name.Text}'");
    }

    public override Node VisitUnary(FormulaParser.UnaryContext context) =>
        new UnaryNode(Operations.TryGetPrefix(context.sign.Text, out var operation) ? operation : throw Unknown(context.sign),
            Visit(context.expression()), context.sign.StartIndex);

    public override Node VisitFunction(FormulaParser.FunctionContext context) =>
        new UnaryNode(Operations.TryGetFunction(context.name.Text, out var function) ? function : throw Unknown(context.name),
            Visit(context.expression()), context.name.StartIndex);

    public override Node VisitPower(FormulaParser.PowerContext context) =>
        Binary(context.expression(), context.POW().Symbol);

    public override Node VisitMultiplicative(FormulaParser.MultiplicativeContext context) =>
        Binary(context.expression(), context.op);

    public override Node VisitAdditive(FormulaParser.AdditiveContext context) =>
        Binary(context.expression(), context.op);

    private BinaryNode Binary(FormulaParser.ExpressionContext[] operands, IToken symbol) =>
        new(Operations.TryGetBinary(symbol.Text, out var operation) ? operation : throw Unknown(symbol),
            Visit(operands[0]), Visit(operands[1]), symbol.StartIndex);

    private static ExpressionException Unknown(IToken symbol) =>
        new(symbol.StartIndex, $"невідома операція '{symbol.Text}'");
}
