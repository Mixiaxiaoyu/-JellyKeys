using Godot;

namespace JellyKeyboardOverlay.Layout;

public sealed record KeyDefinition(string Id, string Label, Rect2 Rect);

public sealed record KeyboardLayoutData(IReadOnlyList<KeyDefinition> Keys, Vector2 ContentSize);

