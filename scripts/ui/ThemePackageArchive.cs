using System.IO.Compression;

namespace JellyKeyboardOverlay.UI;

internal static class ThemePackageArchive
{
	private const string ThemeEntryName = "theme.json";
	public const string TextureEntryName = "text.png";

	public static void Write(string path, byte[] themeJson, byte[] texture)
	{
		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
		try
		{
			using (var file = File.Create(temporaryPath))
			using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
			{
				WriteEntry(archive, ThemeEntryName, themeJson);
				WriteEntry(archive, TextureEntryName, texture);
			}
			File.Move(temporaryPath, path, overwrite: true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	public static (byte[] ThemeJson, byte[] Texture) Read(
		string path, int maxThemeJsonBytes, int maxTextureBytes)
	{
		using var file = File.OpenRead(path);
		using var archive = new ZipArchive(file, ZipArchiveMode.Read);
		if (archive.Entries.Count != 2 ||
			archive.GetEntry(ThemeEntryName) is not { } themeEntry ||
			archive.GetEntry(TextureEntryName) is not { } textureEntry)
		{
			throw new InvalidDataException("主题包需包含 theme.json 和 text.png。");
		}

		return (ReadEntry(themeEntry, maxThemeJsonBytes), ReadEntry(textureEntry, maxTextureBytes));
	}

	private static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
	{
		var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
		using var output = entry.Open();
		output.Write(bytes);
	}

	private static byte[] ReadEntry(ZipArchiveEntry entry, int maximumBytes)
	{
		if (entry.Length <= 0 || entry.Length > maximumBytes)
		{
			throw new InvalidDataException($"主题包中的 {entry.FullName} 大小不合规。");
		}
		using var input = entry.Open();
		using var output = new MemoryStream((int)entry.Length);
		var buffer = new byte[81920];
		int count;
		while ((count = input.Read(buffer)) > 0)
		{
			if (output.Length + count > maximumBytes)
			{
				throw new InvalidDataException($"主题包中的 {entry.FullName} 过大。");
			}
			output.Write(buffer, 0, count);
		}
		return output.ToArray();
	}
}
