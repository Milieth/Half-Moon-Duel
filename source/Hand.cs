using Godot;
using System;

public partial class Hand : HBoxContainer
{
	private ButtonGroup _cardButtonGroup = new();
	private ReferenceRect _selectedBorder;
	private RandomNumberGenerator _rng= new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cardButtonGroup.AllowUnpress = true;
		_selectedBorder = GetNode<ReferenceRect>("SelectedBorder");

		_rng.Randomize();

		// DrawCardRandomly();
		// DrawCardRandomly();
		// DrawCardRandomly();
		DrawCard(0);
		DrawCard(1);
		DrawCard(2);		
	}

	//close cards face down prevent other player to see it
	public void HideAllCard(bool isDisable = true)
	{
		foreach(var button in _cardButtonGroup.GetButtons())
		{
			var card = button.GetNode<MoonCard>("MoonCard");
			if(card is not null)
				card.FlipFaceDown();

			button.Disabled = isDisable;
		}
	}

	//turn cards face up reveal its moon phase icon
	public void ShowAllCard(bool isDisable = false)
	{
		foreach(var button in _cardButtonGroup.GetButtons())
		{
			var card = button.GetNode<MoonCard>("MoonCard");
			if(card is not null)
				card.FlipFaceUp();

			button.Disabled = isDisable;
		}
	}

	//return currently selected moon card in hand, return null if no selected card
	public MoonCard GetSelectedCard()
	{
		var button = _cardButtonGroup.GetPressedButton();
		if(button is null)
			return null;
		
		return button.GetNode<MoonCard>("MoonCard");
	}

	//remove selected button in hand ButtonGroup immadiately
	public void RemoveSelectedButton()
	{
		_selectedBorder.Reparent(this, keepGlobalTransform: false);
		_selectedBorder.Hide();
		var button = _cardButtonGroup.GetPressedButton();
		if(button is not null)
		{
			button.ButtonPressed = false;
			button.Free();			
		}
	}
	
	//move the selected border to currently pressed button
	public void UpdateSelectedBorder()
	{
		var button = _cardButtonGroup.GetPressedButton();
		if(button is not null)
		{
			_selectedBorder.Size = button.Size;
			_selectedBorder.Reparent(button, keepGlobalTransform: false);
			_selectedBorder.Show();
		}
		else
		{
			_selectedBorder.Reparent(this, keepGlobalTransform: false);
			_selectedBorder.Hide();
		}
	}
	
	public void DrawCardRandomly()
	{
		DrawCard(_rng.RandiRange(0,7));		
	}
	//Draw moon card and add it to player hand
	public void DrawCard(int phase)
	{
		var packed = GD.Load<PackedScene>("res://moon_card.tscn");
		var card = packed.Instantiate<MoonCard>();
		var cardButton = new Button();
		cardButton.AddChild(card);
		AddChild(cardButton);

		card.SetMoonPhase(phase);
		card.Name = "MoonCard";

		cardButton.ToggleMode = true;
		cardButton.CustomMinimumSize = card.GetSize();
		cardButton.Pressed += OnButtonPressed;
		cardButton.ButtonGroup = _cardButtonGroup;

		card.Position = new Vector2(cardButton.CustomMinimumSize.X/2, cardButton.CustomMinimumSize.Y/2);

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnButtonPressed()
	{
		UpdateSelectedBorder();
	}
}
