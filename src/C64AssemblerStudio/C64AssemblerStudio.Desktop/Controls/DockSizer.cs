using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace C64AssemblerStudio.Desktop.Controls;
public class DockSizer : Panel
{
    public static readonly DirectProperty<DockSizer, DockSizerOrientation> OrientationProperty
        = AvaloniaProperty.RegisterDirect<DockSizer, DockSizerOrientation>(
           nameof(Orientation),
           o => o.Orientation,
           (o, v) => o.Orientation = v
        );
    public static readonly DirectProperty<DockSizer, double> MinSizedWidthProperty
    = AvaloniaProperty.RegisterDirect<DockSizer, double>(
       nameof(MinSizedWidth),
       o => o.MinSizedWidth,
       (o, v) => o.MinSizedWidth = v
    );
    private DockSizerOrientation _orientation = DockSizerOrientation.Horizontal;
    private double _minSizedWidth = 80;
    private Control? _related;
    private Panel? _parent;
    public DockSizerOrientation Orientation
    {
        get => _orientation;
        set
        {
            if (value != Orientation)
            {
                SetAndRaise(OrientationProperty, ref _orientation, value);
                UpdateCursor();
            }
        }
    }
    public double MinSizedWidth
    {
        get => _minSizedWidth;
        set => SetAndRaise(MinSizedWidthProperty, ref _minSizedWidth, value);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateCursor();
    }

    void UpdateCursor()
    {
        Cursor = Orientation switch
        {
            DockSizerOrientation.Vertical => new Cursor(StandardCursorType.SizeWestEast),
            _ => new Cursor(StandardCursorType.SizeNorthSouth)
        };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _parent = Parent as Panel;
        if (_parent is not null)
        {
            int index = _parent.Children.IndexOf(this);
            if (index > 0)
            {
                _related = _parent.Children[index - 1];
                e.Pointer.Capture(this);
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_related is not null && _parent is not null)
        {
            var newPosition = e.GetPosition(_parent);
            switch (Orientation)
            {
                case DockSizerOrientation.Vertical:
                    double newWidth = Math.Max(MinSizedWidth, newPosition.X);
                    if (newWidth > _parent.Bounds.Width + Width)
                    {
                        newWidth = _parent.Bounds.Width - Width;
                    }
                    _related.Width = newWidth;
                    break;
                case DockSizerOrientation.Horizontal:
                    double newHeight = Math.Max(MinSizedWidth, _parent.Bounds.Height - newPosition.Y);
                    if (newHeight > _parent.Bounds.Height - Height)
                    {
                        newHeight = _parent.Bounds.Height - Height;
                    }
                    _related.Height = newHeight;
                    break;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (Equals(e.Pointer.Captured, this))
        {
            e.Pointer.Capture(null);
            _parent = null;
            _related = null;
        }
    }
}

public enum DockSizerOrientation
{
    Horizontal,
    Vertical
}
