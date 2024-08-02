using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using C64AssemblerStudio.Desktop.Converters;
using C64AssemblerStudio.Engine.ViewModels.Tools;

namespace C64AssemblerStudio.Desktop.Views.Tools;

public partial class MemoryViewer : UserControl
{
    private readonly HexValueConverter _hexValueConverter;
    private readonly PetsciiByteToCharConverter _petsciiByteToChar;
    private BoolToFontWeightConverter ChangedValueToBoldConverter { get; }
    private FontFamily _c64Font = FontFamily.Default;
    private MemoryViewerViewModel? _currentViewModel;
    private int _dataColumnsCount;

    public MemoryViewer()
    {
        InitializeComponent();
        DataContextChanged += MemoryViewer_DataContextChanged;
        _hexValueConverter = (HexValueConverter)Resources[nameof(HexValueConverter)].ValueOrThrow();
        _petsciiByteToChar = (PetsciiByteToCharConverter)Resources[nameof(PetsciiByteToCharConverter)].ValueOrThrow();
        ChangedValueToBoldConverter =
            (BoolToFontWeightConverter)Resources[nameof(ChangedValueToBoldConverter)].ValueOrThrow();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (this.TryFindResource("C64Mono", out object? fontResource) && fontResource is FontFamily font)
        {
            _c64Font = font;
        }
    }

    private void MemoryViewer_DataContextChanged(object? sender, EventArgs e)
    {
        if (_currentViewModel is not null)
        {
            _currentViewModel.PropertyChanged -= CurrentViewModel_PropertyChanged;
            _currentViewModel.ScrollToRow -= CurrentViewModelOnScrollToRow;
            RemoveDynamicColumns();
        }

        _currentViewModel = (MemoryViewerViewModel?)DataContext;
        if (_currentViewModel is not null)
        {
            _currentViewModel.PropertyChanged += CurrentViewModel_PropertyChanged;
            _currentViewModel.ScrollToRow += CurrentViewModelOnScrollToRow;
            _dataColumnsCount = _currentViewModel.RowSize;
            PopulateDynamicColumns(_dataColumnsCount);
        }
    }

    private void CurrentViewModelOnScrollToRow(object? sender, ScrollToItemEventArgs e)
    {
        Grid.ScrollIntoView(e.Row, null);
    }

    private void CurrentViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_currentViewModel is not null)
        {
            switch (e.PropertyName)
            {
                case nameof(MemoryViewerViewModel.RowSize):
                    RemoveDynamicColumns();
                    PopulateDynamicColumns(_currentViewModel.RowSize);
                    break;
            }
        }
    }

    /// <summary>
    /// Removes all but first column
    /// </summary>
    void RemoveDynamicColumns()
    {
        while (Grid.Columns.Count > 1)
        {
            Grid.Columns.RemoveAt(1);
        }
    }

    void PopulateDynamicColumns(int count)
    {
        PopulateValueColumns(count);
        PopulateCharColumns(count);
    }

    void PopulateValueColumns(int count)
    {
        if (_currentViewModel is not null)
        {
            Binding textBinding = new Binding($"{nameof(MemoryViewerCell.Value)}")
            {
                Converter = _hexValueConverter,
            };
            Binding previousTextBinding = new Binding($"{nameof(MemoryViewerCell.PreviousValue)}")
            {
                Converter = _hexValueConverter,
            };
            Binding fontWeightBinding = new Binding($"{nameof(MemoryViewerCell.HasChanges)}")
            {
                Converter = ChangedValueToBoldConverter,
            };
            MultiBinding previousOpacityBinding = new MultiBinding
            {
                Converter = new FuncMultiValueConverter<bool, double>(x => x.All(y => y) ? 1 : 0),
            };
            previousOpacityBinding.Bindings.Add(new Binding($"{nameof(MemoryViewerCell.HasChanges)}"));
            Binding previousVisibleBinding = new Binding
            {
                Source = _currentViewModel,
                Path = nameof(MemoryViewerViewModel.ShowOnlyRowsWithChanges),
            };
            for (int i = 0; i < count; i++)
            {
                Binding dataContextBinding = new Binding($"{nameof(MemoryViewerRow.Cells)}[{i}]");
                string headerText = $"{i:X2}";
                var column = new DataGridTemplateColumn
                {
                    Width = new DataGridLength(24),
                    CellTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                    {
                        var panel = new StackPanel
                        {
                            [!DataContextProperty] = dataContextBinding,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        };
                        panel.Children.Add(new TextBlock
                        {
                            [!TextBlock.TextProperty] = textBinding,
                            [!TextBlock.FontWeightProperty] = fontWeightBinding,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        });
                        panel.Children.Add(new TextBlock
                        {
                            [!TextBlock.TextProperty] = previousTextBinding,
                            [!OpacityProperty] = previousOpacityBinding,
                            [!IsVisibleProperty] = previousVisibleBinding,
                            Foreground = Brushes.Gray,
                            HorizontalAlignment = HorizontalAlignment.Center,
                        });
                        return panel;
                    }),
                    HeaderTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) => new TextBlock
                    {
                        Text = headerText,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    }),
                };
                Grid.Columns.Add(column);
            }
        }
    }

    void PopulateCharColumns(int count)
    {
        MultiBinding previousOpacityBinding = new MultiBinding
        {
            Converter = new FuncMultiValueConverter<bool, double>(x => x.All(y => y) ? 1 : 0),
        };
        previousOpacityBinding.Bindings.Add(new Binding($"{nameof(MemoryViewerCell.HasChanges)}"));
        Binding previousVisibleBinding = new Binding
        {
            Source = _currentViewModel,
            Path = nameof(MemoryViewerViewModel.ShowOnlyRowsWithChanges),
        };
        Binding textBinding = new Binding($"{nameof(MemoryViewerCell.Value)}")
        {
            Converter = _petsciiByteToChar,
        };
        Binding previousTextBinding = new Binding($"{nameof(MemoryViewerCell.PreviousValue)}")
        {
            Converter = _petsciiByteToChar,
        };
        for (int i = 0; i < count; i++)
        {
            Binding dataContextBinding = new Binding($"{nameof(MemoryViewerRow.Cells)}[{i}]");
            string headerText = $"{i:X2}";
            var column = new DataGridTemplateColumn
            {
                Width = new DataGridLength(12),
                CellTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) =>
                {
                    var panel = new StackPanel
                    {
                        [!DataContextProperty] = dataContextBinding,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    panel.Children.Add(new TextBlock
                    {
                        [!TextBlock.TextProperty] = textBinding,
                        FontFamily = _c64Font,
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                    panel.Children.Add(new TextBlock
                    {
                        [!TextBlock.TextProperty] = previousTextBinding,
                        FontFamily = _c64Font,
                        FontSize = 12,
                        [!OpacityProperty] = previousOpacityBinding,
                        [!IsVisibleProperty] = previousVisibleBinding,
                        Foreground = Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    });
                    return panel;
                }),
                HeaderTemplate = new FuncDataTemplate<MemoryViewerRow>((r, ns) => new TextBlock
                {
                    Text = headerText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                }),
            };
            Grid.Columns.Add(column);
        }
    }
}