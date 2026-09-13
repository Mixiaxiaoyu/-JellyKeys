namespace JellyKeyboardOverlay.Input;

public readonly record struct RawKeyEvent(uint VirtualKey, uint ScanCode, bool Extended, bool Pressed, bool SystemKey);

