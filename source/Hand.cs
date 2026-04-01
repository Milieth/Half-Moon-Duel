using Godot;
using System;
using System.Linq;

public partial class Hand : HBoxContainer
{
	public ButtonGroup _cardButtonGroup{get;set;} = new();
	private ReferenceRect _selectedBorder;
	private RandomNumberGenerator _rng= new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_cardButtonGroup.AllowUnpress = true;
		_selectedBorder = GetNode<ReferenceRect>("SelectedBorder");

		_rng.Randomize();
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

	//validate wheter card actually exist in hand
	public bool ValidCard(int index)
	{
		var button = _cardButtonGroup.GetButtons().ElementAtOrDefault(index);
		return button != null;
	}

	//return selected button/card index in array
	public int GetSelectedButtonIndex()
	{
		var pressedButton = _cardButtonGroup.GetPressedButton();
		if(pressedButton is not null)
			return _cardButtonGroup.GetButtons().IndexOf(pressedButton);
		else
			return -1;
	}

	//return card in hand by its index
	public MoonCard GetCard(int index)
	{
		var button = _cardButtonGroup.GetButtons().ElementAtOrDefault(index);
		if(button is null)
			return null;
		
		return button.GetNode<MoonCard>("MoonCard");
	}

	// remove button in hand by index, can be used together with GetSelectedButtonIndex()
	public void RemoveButton(int index)
	{
		var button = _cardButtonGroup.GetButtons().ElementAtOrDefault(index);
		if(button is not null)
		{
			if(button.ButtonPressed)
			{
				_selectedBorder.Reparent(this, keepGlobalTransform: false);		//secure it before deletion
				_selectedBorder.Hide();			
			}
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
	
	//Draw 1 moon card randomly
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

	public void OnButtonPressed()
	{
		UpdateSelectedBorder();
	}
}
