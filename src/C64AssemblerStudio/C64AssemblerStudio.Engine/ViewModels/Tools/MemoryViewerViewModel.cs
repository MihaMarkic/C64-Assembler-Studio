using System.Globalization;
using System.Runtime.CompilerServices;
using C64AssemblerStudio.Core;
using C64AssemblerStudio.Core.Common;
using Microsoft.Extensions.Logging;

namespace C64AssemblerStudio.Engine.ViewModels.Tools;

public class MemoryViewerViewModel: ViewModel, IToolView
{
    readonly ILogger<MemoryViewerViewModel> _logger;
    readonly ViceMemoryViewModel _memoryViewModel;
    public string Header => "Memory";
    public int RowSize { get; } = 16;
    public bool ShowOnlyRowsWithChanges { get; set; }
    public ImmutableArray<MemoryViewerRow> Rows { get; private set; }
    public ImmutableArray<MemoryViewerRow> FilteredRows { get; private set; }
    public RelayCommand GoToAddressCommand { get; }
    public event EventHandler<ScrollToItemEventArgs>? ScrollToRow; 
    public string? GoToAddressText { get; set; }

    public MemoryViewerViewModel(ILogger<MemoryViewerViewModel> logger, ViceMemoryViewModel memoryViewModel)
    {
        _logger = logger;
        _memoryViewModel = memoryViewModel;
        GoToAddressCommand = new RelayCommand(GoToAddress,
            () => ushort.TryParse(GoToAddressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _));
        memoryViewModel.MemoryContentChanged += MemoryViewModel_MemoryContentChanged;
        CreateRows();
        FilterRows();
    }

    private void OnScrollToRow(ScrollToItemEventArgs e) => ScrollToRow?.Invoke(this, e);
    private void GoToAddress()
    {
        if (ushort.TryParse(GoToAddressText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ushort address))
        {
            var row = FilteredRows.SingleOrDefault(r => r.Address <= address && r.Address + r.Cells.Length > address);
            if (row is not null)
            {
                OnScrollToRow(new (row));
            }
        }
    }
    private void MemoryViewModel_MemoryContentChanged(object? sender, EventArgs e)
    {
        FilterRows();
        foreach (var row in FilteredRows)
        {
            row.RaiseChanged();
        }
    }

    private void FilterRows()
    {
        if (ShowOnlyRowsWithChanges)
        {
            FilteredRows = [..Rows.Where(r => r.Cells.Any(c => c.HasChanges))];
        }
        else
        {
            FilteredRows = Rows;
        }
    }

    internal void CreateRows()
    {
        int allRows = ushort.MaxValue / RowSize;
        var rowsBuilder = ImmutableArray.CreateBuilder<MemoryViewerRow>();
        for (int r = 0; r < allRows; r++)
        {
            ushort start = (ushort)(r * RowSize);
            MemoryViewerCell[] cells = new MemoryViewerCell[RowSize];
            for (int j = 0; j < cells.Length; j++)
            {
                cells[j] = new MemoryViewerCell(_memoryViewModel, (ushort)(start + j));
            }
            rowsBuilder.Add(new MemoryViewerRow(start, [..cells]));
        }
        Rows = rowsBuilder.ToImmutable();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _memoryViewModel.MemoryContentChanged -= MemoryViewModel_MemoryContentChanged;
    }

    protected override void OnPropertyChanged([CallerMemberName] string name = default!)
    {
        base.OnPropertyChanged(name);
        switch (name)
        {
            case nameof(ShowOnlyRowsWithChanges):
                FilterRows();
                break;
            case nameof(GoToAddressText):
                GoToAddressCommand.RaiseCanExecuteChanged();
                break;
        }
    }
}

public class MemoryViewerRow
{
    public ushort Address { get; }
    public ImmutableArray<MemoryViewerCell> Cells { get; }
    public MemoryViewerRow(ushort address, ImmutableArray<MemoryViewerCell> cells)
    {
        Address = address;
        Cells = cells;
    }

    public void RaiseChanged()
    {
        foreach (var cell in Cells)
        {
            cell.RaiseChanged();
        }
    }
}

public class MemoryViewerCell: NotifiableObject
{
    readonly IViceMemory _owner;
    public ushort Address { get; }
    public MemoryViewerCell(IViceMemory owner, ushort address)
    {
        _owner = owner;
        Address = address;
    }
    public byte Value => _owner.Current.Span[Address];
    public byte PreviousValue => _owner.Previous.Span[Address];
    public bool HasChanges => Value != PreviousValue;
    public void RaiseChanged()
    {
        OnPropertyChanged(nameof(Value));
    }
}

public class ScrollToItemEventArgs : EventArgs
{
    public MemoryViewerRow Row { get; }

    public ScrollToItemEventArgs(MemoryViewerRow row) => Row = row;

}