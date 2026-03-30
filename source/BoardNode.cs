using Godot;
using System;

public partial class BoardNode : TextureButton
{
	[Signal]
	public delegate void OnBoardNodePressedEventHandler(string nodeID);
	public string _id{get;private set;} = "A";
	[Export]
	public string[] _adjacent{get;set;}
	[Export]
	public MoonCard _card{get;set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_id = this.Name;
		this.Pressed += OnPressed;
	}

	public void OnPressed()
	{
		EmitSignal("OnBoardNodePressed", _id);
	}

	//return the center position relative to screen
	public Vector2 GetCenterPosition()
	{
		var centerOffset = new Vector2(this.Size.X/2, this.Size.Y/2);
		return this.GlobalPosition + centerOffset;
	}

	//attach moon card to this node
	public void AddCard(MoonCard card)
	{
		if(card is null)
			return;

		RemoveCard();

		card.Position = new Vector2(this.Size.X/2, this.Size.Y/2);
		if(card.GetParent() is null)
		{
			AddChild(card);		
		}
		else
		{
			card.Reparent(this, keepGlobalTransform:false);
		}

		_card = card;

		this.Disabled = true;
	}

	//remove attached mooon card from this node
	public void RemoveCard()
	{
		if(_card is not null)
		{
			RemoveChild(_card);
			_card = null;					
		}

		this.Disabled = false;
	}
}
