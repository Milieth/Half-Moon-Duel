using Godot;
using System;
using System.Drawing;

public partial class MoonCard : Sprite2D
{
	public enum Pattern
	{
		None,
		PhasePair,
		FullPhase,
		PhaseSequence
	}

	public enum Color
	{
		None,
		Bright,
		Dark
	}

	private Sprite2D _frontIcon;
	private Sprite2D _backIcon;
	public int _phase{get;private set;} = 0;
	[Export]
	public Color _color{get;set;} = Color.None;
	private AudioStreamPlayer _collectingStarSFX;

	public void Init()
	{
		_collectingStarSFX = GetNode<AudioStreamPlayer>("CollectingStar");
		_frontIcon = GetNode<Sprite2D>("FrontIcon");
		_backIcon = GetNode<Sprite2D>("BackIcon");		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Init();
	}

	//tweening flip card face up, assume the card is in face down
	public Tween GetFlipFaceUpAnim()
	{
		var tween = GetTree().CreateTween();
		tween.TweenCallback(new Callable(this, nameof(FlipFaceDown)));
		tween.TweenProperty(this, "scale:x", 0.0f, 0.15)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		tween.TweenCallback(new Callable(this, nameof(FlipFaceUp)));

		tween.TweenProperty(this, "scale:x", 1.0f, 0.15)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		
		return tween;
	}

	//tweening collecting star animation, display star in card, move it to target position then disappear
	public Tween GetCollectingStarAnim(Vector2 finalPosition, Color color)
	{
		string starColor = (color == Color.Bright)? "white" : "black";
		var star = new Sprite2D();
		AddChild(star);
		star.Hide();
		star.ZIndex = 3;
		star.Texture = GD.Load<Texture2D>($"res://aset/{starColor} star.png");
		star.Position = new Vector2(-22, -22) * Scale.X;

		bool moveToRight = finalPosition > star.GlobalPosition;
		var hMove = new Vector2(100, 0);
		var hMoveOpposite = new Vector2(-50, 0);
		if(!moveToRight)
			hMove = -hMove;
			hMoveOpposite = -hMoveOpposite;
		
		var starPos1 = star.Position + hMove;
		var starPos2 = starPos1 + hMoveOpposite;
		
		var tween = GetTree().CreateTween();
		tween.TweenCallback(Callable.From(star.Show));
		tween.TweenProperty(star, "position", starPos1, 0.2);
		tween.TweenProperty(star, "position", starPos2, 0.1);
		tween.TweenCallback(Callable.From(() =>{_collectingStarSFX.Play();}));
		tween.TweenProperty(star, "global_position", finalPosition, 0.5)
			.SetDelay(0.3)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.In);
		tween.TweenCallback(Callable.From(star.QueueFree));

		return tween;
	}

	// return card size, front icon and back icon have the same size
	public Vector2 GetSize()
	{
		return _frontIcon.Texture.GetSize();
	}

	//return default colortype, change this to tweak card color setting
	static public Godot.Color GetColor(MoonCard.Color colorType)
	{
		var color = colorType switch
		{
			Color.Bright => new Godot.Color(1.0f, 1.0f, 1.0f) * 2.0f,
			Color.Dark => new Godot.Color(1.0f, 1.0f, 1.0f, 2.0f) * 0.5f,
			_ => new Godot.Color(1.0f, 1.0f, 1.0f)
		};
		return color;
	}

	// change moon card color
	public void SetColor(Color colorType)
	{
		_frontIcon.SelfModulate = MoonCard.GetColor(colorType);
		_color = colorType;
	}

	//change moon phase id and icon
	public void SetMoonPhase(int phase)
	{
		_phase = (phase + 8) % 8;	//phase is number from 0-7 this should not change anything, and prevent invalid numb
		_frontIcon.Texture = GD.Load<Texture2D>($"res://aset/MoonCard_{phase}.webp");
	}

	//check wheter card currently face up or face down
	public bool IsFaceUp()
	{
		return _frontIcon.Visible;
	}

	//turn moon card to face up, showing moon phase
	public void FlipFaceUp()
	{
		_frontIcon.Show();
		_backIcon.Hide();
	}

	//turn moon card to face down, showing card back icon
	public void FlipFaceDown()
	{
		_backIcon.Show();
		_frontIcon.Hide();
	}

	//turn moon card face down if it currently face up and vice versa
	public void FlipUp()
	{
		if(_frontIcon.Visible)
		{
			FlipFaceDown();
		}
		else
		{
			FlipFaceUp();
		}
	}
}
