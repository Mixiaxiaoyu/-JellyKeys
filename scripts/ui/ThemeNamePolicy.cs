namespace JellyKeyboardOverlay.UI;

internal static class ThemeNamePolicy
{
	public static string NextAvailable(ISet<string> usedNames, string prefix)
	{
		for (var number = 1; ; number++)
		{
			var candidate = $"{prefix} {number}";
			if (!usedNames.Contains(candidate))
			{
				return candidate;
			}
		}
	}
}
