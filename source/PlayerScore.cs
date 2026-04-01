using Godot;
using System;

public partial class PlayerScore : HBoxContainer
{
	public int Score{get;set;} = 0;
	private Label _label;
	private TextureRect _star;
	public Polygon2D _activeIcon{get;set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_label = GetNode<Label>("Label");
		_star = GetNode<TextureRect>("Star");
		_activeIcon = GetNode<Polygon2D>("ActiveIcon");

		UpdateLabel();
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
