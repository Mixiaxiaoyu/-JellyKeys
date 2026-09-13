using System.Buffers.Binary;
using Godot;
using JellyKeyboardOverlay.Layout;

namespace JellyKeyboardOverlay.UI;

internal static class KeyboardTextAtlasValidator
{
	private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

	public static void Validate(byte[] png, KeyboardLayoutData layout)
	{
		if (png.Length < 24 || !png.AsSpan(0, 8).SequenceEqual(PngSignature) ||
			!png.AsSpan(12, 4).SequenceEqual("IHDR"u8))
		{
			throw new InvalidDataException("贴图必须是 PNG 文件。");
		}

		var expected = KeyboardTextAtlas.GetAtlasSize(layout);
		var width = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16, 4));
		var height = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20, 4));
		if (width != expected.X || height != expected.Y)
		{
			throw new InvalidDataException($"贴图尺寸需为 {expected.X} × {expected.Y}，当前为 {width} × {height}。");
		}

		var image = new Image();
		if (image.LoadPngFromBuffer(png) != Error.Ok || image.GetSize() != expected)
		{
			throw new InvalidDataException("PNG 贴图无法读取或尺寸不正确。");
		}
	}
}
