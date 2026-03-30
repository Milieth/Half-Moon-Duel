using Godot;
using System;
using System.Drawing;

public partial class MoonCard : Sprite2D
{
	private Sprite2D _frontIcon;
	private Sprite2D _backIcon;
	public int _phase{get;private set;} = 0;
	private string _inBoardOwner{get;set;} = "Server";


	// return card size, front icon and back icon have the same size
	public Vector2 GetSize()
	{
		return _frontIcon.Texture.GetSize();
	}

	//change moon phase id and icon
	public void SetMoonPhase(int phase)
	{
		_phase = phase % 8;	//phase is number from 0-7 this should not change anything
		_frontIcon.Texture = GD.Load<Texture2D>($"res://aset/MoonCard_{phase}.webp");
	}

	public void Init()
	{
		_frontIcon = GetNode<Sprite2D>("FrontIcon");
		_backIcon = GetNode<Sprite2D>("BackIcon");		
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Init();
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
