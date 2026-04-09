using Godot;
using System;

public partial class PlayerScore : HBoxContainer
{
	public int Score{get;set;} = 0;
	private Label _label;
	public TextureRect _star{get;private set;}
	public Polygon2D _activeIcon{get;set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_star = GetNode<TextureRect>("Star");
		_activeIcon = GetNode<Polygon2D>("ActiveIcon");

		_activeIcon.VisibilityChanged += OnActiveIconVisibilityChanged;

		UpdateLabel();

		OnActiveIconVisibilityChanged();
	}

	//play shrink animation when active icon showed to scene
	private void OnActiveIconVisibilityChanged()
	{
		if(_activeIcon.Visible)
		{
			var tween = GetTree().CreateTween().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
			tween.TweenCallback(Callable.From(() =>
			{
				_activeIcon.Scale = new Vector2(2.0f, 2.0f);
			}));
			tween.TweenProperty(_activeIcon, "scale", new Vector2(1.0f, 1.0f), 0.5);			
		}
	}

	public Vector2 GetStarCenterPos()
	{
		return _star.GlobalPosition + new Vector2(_star.Size.X/2, _star.Size.Y/2);
		// return _star.GlobalPosition + new Vector2(20, 20);
	}

	// increase player score, then update UI
	public void IncreaseScore(int amount)
	{
		Score += amount;
		UpdateLabel();
	}

	// update player score label UI
	public void UpdateLabel()
	{
		if(Score < 10)
			_label.Text = $": 0{Score}";
		else
			_label.Text = $": {Score}";
	}

}
