namespace JellyKeyboardOverlay.Input;

public static class KeyMapper
{
    public static bool TryMap(RawKeyEvent keyEvent, out string keyId)
    {
        if (!keyEvent.Extended && TryMapNumpadScanCode(keyEvent.ScanCode, out keyId))
        {
            return true;
        }

        var virtualKey = keyEvent.VirtualKey;

        if (virtualKey is >= 0x41 and <= 0x5A)
        {
            keyId = $"Key{(char)virtualKey}";
            return true;
        }

        if (virtualKey is >= 0x30 and <= 0x39)
        {
            keyId = $"Digit{(char)virtualKey}";
            return true;
        }

        if (virtualKey is >= 0x70 and <= 0x7B)
        {
            keyId = $"F{virtualKey - 0x6F}";
            return true;
        }

        if (virtualKey is >= 0x60 and <= 0x69)
        {
            keyId = $"Numpad{virtualKey - 0x60}";
            return true;
        }

        keyId = virtualKey switch
        {
            0x1B => "Escape",
            0xC0 => "Backquote",
            0xBD => "Minus",
            0xBB => "Equal",
            0x08 => "Backspace",
            0x09 => "Tab",
            0xDB => "BracketLeft",
            0xDD => "BracketRight",
            0xDC => "Backslash",
            0x14 => "CapsLock",
            0xBA => "Semicolon",
            0xDE => "Quote",
            0xBC => "Comma",
            0xBE => "Period",
            0xBF => "Slash",
            0x20 => "Space",
            0x5B => "MetaLeft",
            0x5C => "MetaRight",
            0x5D => "ContextMenu",
            0x2C => "PrintScreen",
            0x91 => "ScrollLock",
            0x13 => "Pause",
            0x2D => "Insert",
            0x24 => "Home",
            0x21 => "PageUp",
            0x2E => "Delete",
            0x23 => "End",
            0x22 => "PageDown",
            0x26 => "ArrowUp",
            0x25 => "ArrowLeft",
            0x28 => "ArrowDown",
            0x27 => "ArrowRight",
            0x90 => "NumLock",
            0x6F => "NumpadDivide",
            0x6A => "NumpadMultiply",
            0x6D => "NumpadSubtract",
            0x6B => "NumpadAdd",
            0x6E => "NumpadDecimal",
            0xAD => "AudioVolumeMute",
            0xAE => "AudioVolumeDown",
            0xAF => "AudioVolumeUp",
            0xB3 => "MediaPlayPause",
            0x0D => keyEvent.Extended ? "NumpadEnter" : "Enter",
            0x10 or 0xA0 or 0xA1 => keyEvent.ScanCode == 0x36 || virtualKey == 0xA1 ? "ShiftRight" : "ShiftLeft",
            0x11 or 0xA2 or 0xA3 => keyEvent.Extended || virtualKey == 0xA3 ? "ControlRight" : "ControlLeft",
            0x12 or 0xA4 or 0xA5 => keyEvent.Extended || virtualKey == 0xA5 ? "AltRight" : "AltLeft",
            _ => string.Empty,
        };

        return keyId.Length > 0;
    }

    private static bool TryMapNumpadScanCode(uint scanCode, out string keyId)
    {
        keyId = scanCode switch
        {
            0x47 => "Numpad7",
            0x48 => "Numpad8",
            0x49 => "Numpad9",
            0x4B => "Numpad4",
            0x4C => "Numpad5",
            0x4D => "Numpad6",
            0x4F => "Numpad1",
            0x50 => "Numpad2",
            0x51 => "Numpad3",
            0x52 => "Numpad0",
            0x53 => "NumpadDecimal",
            _ => string.Empty,
        };

        return keyId.Length > 0;
    }
}

