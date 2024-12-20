﻿namespace C64AssemblerStudio.Engine.Extensions;
public static class DataExtension
{
    public static ImmutableArray<byte> Copy(this ReadOnlySpan<byte> buffer, int? size = null)
    {
        if (size.HasValue)
        {
            if (size.Value > buffer.Length)
            {
                throw new Exception("Buffer does not have enough data");
            }
            buffer = buffer.Slice(0, size.Value);
        }
        return buffer.ToImmutableArray();
    }
}
