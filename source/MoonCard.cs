using Godot;
using System;

public partial class MoonCard : Sprite2D
{
	public int _phase{get;private set;} = 0;
	private string _inBoardOwner{get;set;} = "Server";

	//change moon phase id and icon
	public void SetMoonPhase(int phase)
	{
		_phase = phase % 8;	//phase is number from 0-7 this should not change anything
		this.Texture = GD.Load<Texture2D>($"res://aset/MoonCard_{phase}.webp");
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
