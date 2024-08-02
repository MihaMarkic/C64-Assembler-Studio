using Avalonia;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace C64AssemblerStudio.Desktop.Behaviors;

public abstract class ToggleMultiClassOnEnumChangeBehavior<TEnum> : AvaloniaObject
    where TEnum : struct, Enum
{
    public static readonly AttachedProperty<TEnum> TriggerProperty =
        AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, TEnum>(
            "Trigger", default!, false, BindingMode.OneWay, coerce: ValidateTrigger);

    public static readonly AttachedProperty<IDictionary<TEnum, string>> ClassesProperty =
        AvaloniaProperty.RegisterAttached<ToggleClassOnBoolChangeBehavior, Interactive, IDictionary<TEnum, string>>(
            "Classes", default!, false, BindingMode.OneWay, coerce: ValidateClasses);

    public static void SetTrigger(AvaloniaObject element, bool value) => element.SetValue(TriggerProperty, value);
    public static TEnum GetTrigger(AvaloniaObject element) => element.GetValue(TriggerProperty);

    public static void SetClasses(AvaloniaObject element, IDictionary<TEnum, string> value) =>
        element.SetValue(ClassesProperty, value);

    public static IDictionary<TEnum, string>? GetClasses(AvaloniaObject element) => element.GetValue(ClassesProperty);

    public static TEnum ValidateTrigger(AvaloniaObject element, TEnum value)
    {
        UpdateTarget(element, value, GetClasses(element));
        return value;
    }

    public static IDictionary<TEnum, string> ValidateClasses(AvaloniaObject element, IDictionary<TEnum, string> value)
    {
        UpdateTarget(element, GetTrigger(element), value);
        return value;
    }

    internal static void UpdateTarget(AvaloniaObject element, TEnum value, IDictionary<TEnum, string>? classes)
    {
        if (classes is not null && element is StyledElement styled)
        {
            foreach (var e in Enum.GetValues<TEnum>())
            {
                if (classes.TryGetValue(e, out var className))
                {
                    if (value.HasFlag(e))
                    {
                        styled.Classes.Add(className);
                    }
                    else
                    {
                        styled.Classes.Remove(className);
                    }
                }
            }
        }
    }
}