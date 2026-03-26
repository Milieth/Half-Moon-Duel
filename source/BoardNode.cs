using Godot;
using System;

public partial class BoardNode : TextureButton
{
	public string _id{get;private set;} = "A";
	[Export]
	public string[] _adjacent{get;set;}
	[Export]
	private MoonCard _card;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_id = this.Name;
	}

	//return the center position relative to screen
	public Vector2 GetCenterPosition()
	{
		var centerOffset = new Vector2(this.Size.X/2, this.Size.Y/2);
		return this.GlobalPosition + centerOffset;
	}

	public void AddCard(MoonCard card)
	{
		if(card is null)
			return;

		RemoveCard();
		card.Position = new Vector2(this.Size.X/2, this.Size.Y/2);
		AddChild(card);
		_card = card;
	}

	public void RemoveCard()
	{
		if(_card is not null)
		{
			RemoveChild(_card);
			_card = null;					
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
