using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class ColorGradientTrack : Control
{
    private (float Offset, Color Color)[] _stops =
    {
        (0f, Colors.Black),
        (1f, Colors.White),
    };

    public ColorGradientTrack()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public void SetStops(params (float Offset, Color Color)[] stops)
    {
        if (stops.Length < 2)
        {
            return;
        }

        _stops = stops.OrderBy(stop => stop.Offset).ToArray();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var width = Mathf.Max(1, Mathf.CeilToInt(Size.X));
        var height = Mathf.Max(1f, Size.Y);
        var radius = Mathf.Min(height * 0.5f, 8f);

        for (var pixel = 0; pixel < width; pixel++)
        {
            var x = pixel + 0.5f;
            var amount = width <= 1 ? 0f : pixel / (float)(width - 1);
            var inset = RoundedEndInset(x, width, radius);
            DrawLine(
                new Vector2(x, inset),
                new Vector2(x, height - inset),
                Sample(amount),
                1.1f,
                true);
        }
    }

    private Color Sample(float amount)
    {
        for (var index = 1; index < _stops.Length; index++)
        {
            var right = _stops[index];
            if (amount > right.Offset)
            {
                continue;
            }

            var left = _stops[index - 1];
            var span = Mathf.Max(0.0001f, right.Offset - left.Offset);
            return left.Color.Lerp(right.Color, (amount - left.Offset) / span);
        }

        return _stops[^1].Color;
    }

    private static float RoundedEndInset(float x, float width, float radius)
    {
        var centerDistance = x < radius
            ? radius - x
            : x > width - radius
                ? x - (width - radius)
                : 0f;
        if (centerDistance <= 0f)
        {
            return 0f;
        }

        return radius - Mathf.Sqrt(Mathf.Max(0f, radius * radius - centerDistance * centerDistance));
    }
}
