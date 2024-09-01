namespace Antlr4.Runtime;

public static class AntlrExtension
{
    public static int Length (this IToken token) => token.StopIndex-token.StartIndex+1;
}