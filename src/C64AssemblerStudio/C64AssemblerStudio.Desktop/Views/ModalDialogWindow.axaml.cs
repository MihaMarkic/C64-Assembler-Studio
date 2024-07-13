﻿using Avalonia;
using Avalonia.Controls;
using C64AssemblerStudio.Engine.Messages;

namespace C64AssemblerStudio.Desktop.Views;

public partial class ModalDialogWindow : Window
{
    ShowModalDialogMessageCore? message;
    public ModalDialogWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (message is not null)
        {
            message.Close -= Message_Close;
        }
        message = DataContext as ShowModalDialogMessageCore;
        if (message is not null)
        {
            message.Close += Message_Close;
        }
        base.OnDataContextChanged(e);
    }

    private void Message_Close(object? sender, EventArgs e)
    {
        Close();
    }
}
