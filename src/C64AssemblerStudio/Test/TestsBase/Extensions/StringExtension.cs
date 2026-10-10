using System.Globalization;

namespace System;

public static class StringExtension
{
	/// <summary>
	/// Expects <paramref name="dataText"/> in comma-delimited byte texts in hex or decimal value.
	/// </summary>
	/// <param name="dataText"></param>
	/// <returns></returns>
	/// <example>
	/// $b5,$07,$9e,$20,$32,$30 in hex or,
	/// 140,130,5 in dec.
	/// </example>
	public static ReadOnlySpan<byte> ToBytes(this ReadOnlySpan<char> dataText)
	{
		var split = dataText.Split(',');
		ReadOnlySpan<Range> ranges = [.. split];
		Span<byte> data = new byte[dataText.Length];
		for (int i = 0; i < ranges.Length; i++)
		{
			ReadOnlySpan<char> text = dataText[ranges[i]].Trim();
			if (text.Length < 1)
			{
				throw new Exception("At least one element is empty");
			}

			if (text.StartsWith('$'))
			{
				if (byte.TryParse(text[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture.NumberFormat,
					    out byte b))
				{
					data[i] = b;
				}
				else
				{
					throw new Exception($"Failed parsing hex byte {text}");
				}
			}
			else
			{
				if (byte.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat,
					    out byte b))
				{
					data[i] = b;
				}
				else
				{
					throw new Exception($"Failed parsing dec byte {text}");
				}
			}
		}
		return data;
	}
}