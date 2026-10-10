namespace System.IO;

public static class StreamExtensions
{
	public static ushort ReadUShort(this Stream stream)
	{
		Span<byte> buffer = stackalloc byte[2];
		stream.ReadExactly(buffer);
		return BitConverter.ToUInt16(buffer);
	}
}