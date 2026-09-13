using Godot;

namespace JellyKeyboardOverlay.UI;

// Owns tween cancellation and restores animated controls after interrupted transitions.
internal sealed class UiMotionScope(Control owner)
{
	private readonly Control _owner = owner;
	private readonly Dictionary<CanvasItem, (Color Modulate, Vector2? Position)> _original = new();
	private Tween? _root;
	private Tween? _items;

	public Tween StartRoot(bool parallel = true)
	{
		_root?.Kill();
		_root = _owner.CreateTween().SetParallel(parallel);
		return _root;
	}

	public Tween StartItems(bool parallel = true)
	{
		CancelItems();
		_items = _owner.CreateTween().SetParallel(parallel);
		return _items;
	}

	public void Remember(CanvasItem item, bool position = false)
	{
		_original[item] = (item.Modulate, position && item is Control control ? control.Position : null);
	}

	public void CancelAll()
	{
		_root?.Kill();
		_root = null;
		CancelItems();
	}

	public void CancelItems()
	{
		_items?.Kill();
		_items = null;
		RestoreItems();
	}

	public void CompleteRoot(Tween tween)
	{
		if (_root == tween)
		{
			_root = null;
		}
	}

	public void CompleteItems(Tween tween)
	{
		if (_items == tween)
		{
			RestoreItems();
			_items = null;
		}
	}

	public void RestoreItems()
	{
		foreach (var (item, state) in _original)
		{
			if (!GodotObject.IsInstanceValid(item))
			{
				continue;
			}

			item.Modulate = state.Modulate;
			if (state.Position is { } position && item is Control control)
			{
				control.Position = position;
			}
		}
		_original.Clear();
	}
}
