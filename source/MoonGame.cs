using Godot;
using System;

public partial class MoonGame : Control
{
	private Board _board;
	private Hand _hand;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_board = GetNode<Board>("Board");
		_hand = GetNode<Hand>("Hand");

		foreach(var node in _board._nodes.Values)
		{
			node.OnBoardNodePressed += OnNodePressed;
		}
	}

	private void OnNodePressed(string nodeID)
	{
		if(PlaceCard(nodeID))
		{
			var phasePair = _board.GetPhasePairList(nodeID);
			var fullPair = _board.GetFullPhaseList(nodeID);
			var lunarSequences = _board.GetLunarSequenceList(nodeID);

			var score = 0;
			//print
			if(phasePair.Count != 0)
			{
				GD.Print("Moon Phase Pair: ");
				foreach(var sequence in phasePair)
				{
					GD.Print(string.Join(", ", sequence));
				}
				GD.Print(phasePair.Count);
				score += phasePair.Count;
			}

			if(fullPair.Count != 0)
			{
				GD.Print("Moon Full Pair: ");
				foreach(var sequence in fullPair)
				{
					GD.Print(string.Join(", ", sequence));
				}
				GD.Print(fullPair.Count * 2);				
				score += fullPair.Count * 2;
			}

			// GD.Print($"lunar sequence count = {lunarSequences.Count}");
			if(lunarSequences.Count != 0)
			{
				GD.Print("Moon Phase Sequence: ");
				foreach(var sequence in lunarSequences)
				{
					GD.Print(string.Join(", ", sequence));
				}
				GD.Print(lunarSequences.Count);
			}
			
			if(score != 0)
				GD.Print(score);
		}

		_hand.DrawCardRandomly();
	}

	private bool PlaceCard(string nodeID)
	{
		GD.Print($"Node Pressed:{nodeID}");
		var card = _hand.GetSelectedCard();
		if(card is not null)
		{
			_board.PlaceCard(nodeID, card);
			_hand.RemoveSelectedButton();
			return true;
		}

		return false;		
	}
}
