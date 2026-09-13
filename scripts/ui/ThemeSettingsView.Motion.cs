using Godot;

namespace JellyKeyboardOverlay.UI;

public partial class ThemeSettingsView
{
	public void AnimateClose(Action onFinished, bool animate = true)
	{
		_animationsEnabled = false;
		_motionScope.CancelAll();
		if (!animate || !Visible)
		{
			Visible = false;
			Modulate = Colors.White;
			onFinished();
			return;
		}

		var items = GetMotionItems().Reverse().ToArray();
		var tween = _motionScope.StartRoot();
		for (var index = 0; index < items.Length; index++)
		{
			var item = items[index];
			var target = item.Modulate;
			var basePosition = item.Position;
			_motionScope.Remember(item, position: true);
			var delay = Math.Min(index * 0.007, 0.105);
			tween.TweenProperty(item, "position", basePosition + new Vector2(0f, -8f), 0.17)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
			tween.TweenProperty(item, "modulate:a", 0f, 0.14)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);
		}
		tween.TweenProperty(this, "modulate:a", 0f, 0.24)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		tween.Finished += () =>
		{
			_motionScope.RestoreItems();
			Visible = false;
			Modulate = Colors.White;
			_motionScope.CompleteRoot(tween);
			onFinished();
		};
	}

	private void AnimateOpen(bool animate)
	{
		_animationsEnabled = animate;
		if (!animate)
		{
			return;
		}

		Modulate = new Color(1f, 1f, 1f, 0f);
		var tween = _motionScope.StartRoot(parallel: false);
		tween.TweenProperty(this, "modulate:a", 1f, 0.18)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		tween.Finished += () => _motionScope.CompleteRoot(tween);
		AnimateItemsIn(GetMotionItems());
	}

	private IEnumerable<Control> GetMotionItems()
	{
		yield return _layoutTab;
		yield return _designTab;
		yield return _themesTab;
		foreach (var item in ActiveTabContent.GetChildren().OfType<Control>().Where(item => item.Visible))
		{
			yield return item;
		}
		yield return _aboutButton;
		yield return _closeButton;
	}

	private void AnimateItemsIn(IEnumerable<Control> controls)
	{
		var items = controls.ToArray();
		var tween = _motionScope.StartItems();
		for (var index = 0; index < items.Length; index++)
		{
			var item = items[index];
			var target = item.Modulate;
			var basePosition = item.Position;
			_motionScope.Remember(item, position: true);
			item.Position = basePosition + new Vector2(0f, 10f);
			item.Modulate = new Color(target.R, target.G, target.B, 0f);
			var delay = Math.Min(index * 0.01, 0.14);
			tween.TweenProperty(item, "position", basePosition, 0.24)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(item, "modulate:a", target.A, 0.18)
				.SetDelay(delay)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);
		}
		tween.Finished += () => _motionScope.CompleteItems(tween);
	}

}
