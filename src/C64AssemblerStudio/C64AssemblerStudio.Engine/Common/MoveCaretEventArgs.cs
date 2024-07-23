namespace C64AssemblerStudio.Engine.Common;

public sealed class MoveCaretEventArgs: EventArgs
{
    public int Line { get; }
    public int Column { get; }
    public MoveCaretEventArgs(int row, int column)
    {
        Line = row;
        Column = column;
    }
}