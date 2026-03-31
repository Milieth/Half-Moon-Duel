using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CardMatchEvent
{
	public MoonCard.Pattern _pattern{get;set;}
	public List<string> _nodes{get;set;}
	public int _score{get;set;}
	public string _owner{get;set;}

	public CardMatchEvent()
	{
	}

	public CardMatchEvent(MoonCard.Pattern pattern, List<string> nodes, int score, string owner)
	{
		_pattern = pattern;
		_nodes = nodes;
		_score = score;
		_owner = owner;
	}

	//print card match event data to console
	public void print()
	{
		var nodes = string.Join(" ", _nodes);
		GD.Print($"{_pattern.ToString()} : {nodes} : {_score} : {_owner}");
	}
}

public enum Player
{
	None,
	Server,
	Client
}

public partial class MoonGame : Control
{
	private Dictionary<Player, PlayerScore> _score;
	private Board _board;
	private Hand _hand;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_score = new()
        {
            { Player.Server, GetNode<PlayerScore>("WhiteScore") },
            { Player.Client, GetNode<PlayerScore>("BlackScore") }
        };

		_board = GetNode<Board>("Board");
		_hand = GetNode<Hand>("Hand");

		foreach(var node in _board._nodes.Values)
		{
			node.OnBoardNodePressed += OnNodePressed;
		}
	}

	//construct phase pair event from target node, if there are no event occured return null
	private List<CardMatchEvent> GetPhasePairEvent(string targetNodeID)
	{
		var phasePair = _board.GetPhasePairList(targetNodeID);
		if(phasePair.Count == 0)
			return null;
		
		var pairEvent = new List<CardMatchEvent>();
		foreach(var nodes in phasePair)
		{
			var matchEvent = new CardMatchEvent(
				pattern: MoonCard.Pattern.PhasePair,
				nodes: nodes,
				score: 1,
				owner: "Server"
			);
			pairEvent.Add(matchEvent);
		}

		return pairEvent;		
	}

	private List<CardMatchEvent> GetFullPhaseEvent(string targetNodeID)
	{
		var fullPhase = _board.GetFullPhaseList(targetNodeID);
		if(fullPhase.Count == 0)
			return null;
		
		var fullEvent = new List<CardMatchEvent>();
		foreach(var nodes in fullPhase)
		{
			var matchEvent = new CardMatchEvent(
				pattern: MoonCard.Pattern.FullPhase,
				nodes: nodes,
				score: 2,
				owner: "Server"
			);
			fullEvent.Add(matchEvent);
		}

		return fullEvent;
	}

	private List<CardMatchEvent> GetPhaseSequenceEvent(string targetNodeID)
	{
		var phaseSequence = _board.GetPhaseSequenceList(targetNodeID);
		if(phaseSequence.Count == 0)
			return null;

		var sequenceEvent = new List<CardMatchEvent>();
		foreach(var nodes in phaseSequence)
		{
			var matchEvent = new CardMatchEvent(
				pattern: MoonCard.Pattern.PhaseSequence,
				nodes: nodes,
				score: nodes.Count + 7*(nodes.Count / 8),
				owner: "Server"
			);
			sequenceEvent.Add(matchEvent);
		}

		return sequenceEvent;
	}

	//construct all card events that detected, if there are no event occured return null
	private List<CardMatchEvent> GetCardEvents(string targetNodeID)
	{
		var pair = GetPhasePairEvent(targetNodeID);
		var full = GetFullPhaseEvent(targetNodeID);
		var sequence = GetPhaseSequenceEvent(targetNodeID);

		var allEvents = new List<CardMatchEvent>();
		if(pair is not null)
			allEvents.AddRange(pair);
		if(full is not null)
			allEvents.AddRange(full);
		if(sequence is not null)
			allEvents.AddRange(sequence);
		
		return (allEvents is not null)? allEvents : null;
	}

	private void OnNodePressed(string nodeID)
	{
		if(PlaceCard(nodeID))
		{
			var matchEvents = GetCardEvents(nodeID);
			if(matchEvents is not null)
			{
				foreach(var matchEvent in matchEvents)
				{
					matchEvent.print();
					_score[Player.Server].IncreaseScore(matchEvent._score);
					_board.SetCardsColor(matchEvent._nodes, MoonCard.Color.Bright);
					_board.SetConnectionLineByEvent(matchEvent);
				}
			}
		}

		_hand.DrawCardRandomly();
	}

	//move card from player hand to board node
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
