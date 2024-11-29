namespace C64AssemblerStudio.Core.Common.Compiler;

/// <summary>
/// Provides a non-nullable argument carying potentially nullable value.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
/// Used with combination with <see cref="Activator.CreateInstance{T}"/> where specific arguments can not be nullable.
/// </remarks>
public class NullableArgument<T>
    where T : class
{
    public T? Value { get; }

    public NullableArgument(T? value)
    {
        Value = value;
    }
}