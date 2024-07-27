using System.Xml;
using Avalonia.Controls;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace C64AssemblerStudio.Desktop.Views.Dialogs;

public partial class BreakpointDetails : UserControl
{
    public BreakpointDetails()
    {
        InitializeComponent();
        InitConditionsEditors();
    }
    void InitConditionsEditors()
    {
        var assembly = typeof(BreakpointDetails).Assembly;
        using (Stream s = assembly.GetManifestResourceStream("C64AssemblerStudio.Desktop.Resources.breakpoint-condition.xshd")!)
        {
            using (var reader = new XmlTextReader(s))
            {
                Conditions.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            }
        }
    }
}