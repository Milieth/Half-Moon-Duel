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
	private Player MultiplayerAuth = Player.Server;
	private Board _board;
	private Dictionary<Player, PlayerScore> _score;
	private Dictionary<Player, Hand> _hand = new();
	private Dictionary<Player, MoonCard.Color> _playerColor = new();

	private Player _activePlayer = Player.None;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_board = GetNode<Board>("Board");
		foreach(var node in _board._nodes.Values)
			node.OnBoardNodePressed += OnNodePressed;

		_playerColor = new()
		{
			{Player.Server, MoonCard.Color.Bright},
			{Player.Client, MoonCard.Color.Dark}
		};
		_score = new()
        {
            { Player.Server, GetNode<PlayerScore>($"{_playerColor[Player.Server]}Score") },
            { Player.Client, GetNode<PlayerScore>($"{_playerColor[Player.Client]}Score") }
        };
		_hand = new()
		{
            { Player.Server, GetNode<Hand>($"{_playerColor[Player.Server]}Hand") },
            { Player.Client, GetNode<Hand>($"{_playerColor[Player.Client]}Hand") }
		};

		InitLayout(MultiplayerAuth);
		SetGameTurn(MultiplayerAuth);
	}

	//setting game layout for this player, wheter use bright side or dark side
	private void InitLayout(Player front)
	{
		foreach(var (player, hand) in _hand)
		{
			if(player == front)
			{
				hand.Position = new Vector2(640, 630);
			}
			else
			{
				hand.Position = new Vector2(640, 80);
				hand.HideAllCard();
			}
		}

		foreach(var (player, score) in _score)
		{
			if(player == front)
			{
				score.Position = new Vector2(62, 489);
			}
			else
			{
				score.Position = new Vector2(1088, 193);
			}
		}
	}

	//wheter this is your turn or opponent turn
	private bool IsYourTurn()
	{
		return MultiplayerAuth == _activePlayer;
	}

	//change player turn to the next player
	private void SwitchPlayerTurn()
	{
		if(_activePlayer == Player.Server) SetGameTurn(Player.Client);
		else SetGameTurn(Player.Server);
	}

	//change game turn owner
	private void SetGameTurn(Player player)
	{
		foreach(var playerScore in _score.Values)
			playerScore._activeIcon.Hide();

		_activePlayer = player;
		if(IsYourTurn())
		{
			_hand[MultiplayerAuth].ShowAllCard();
			_score[MultiplayerAuth]._activeIcon.Show();
			_board.Disabled(false);
		}
		else
		{
			_hand[MultiplayerAuth].ShowAllCard(true);
			var opponent = (MultiplayerAuth == Player.Server)? Player.Client : Player.Server;
			_score[opponent]._activeIcon.Show();
			_board.Disabled(true);
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
					_score[MultiplayerAuth].IncreaseScore(matchEvent._score);
					_board.SetCardsColor(matchEvent._nodes, _playerColor[MultiplayerAuth]);
					_board.SetConnectionLineByEvent(matchEvent);
				}
			}
		}

		if(_board.IsBoardFull())
		{
			GainBonusPoint(MultiplayerAuth);
		}
		_hand[MultiplayerAuth].DrawCardRandomly();
	}	

	//Player gain bonus point during end of game
	private void GainBonusPoint(Player player)
	{
		var bonus = _board.CountCardsByColor(_playerColor[player]);
		_score[player].IncreaseScore(bonus);
		GD.Print($"{player.ToString()} Gain Bonus:{bonus}");
	}

	//move card from player hand to board node
	private bool PlaceCard(string nodeID)
	{
		GD.Print($"Node Pressed:{nodeID}");
		var card = _hand[MultiplayerAuth].GetSelectedCard();
		if(card is not null)
		{
			_board.PlaceCard(nodeID, card);
			_hand[MultiplayerAuth].RemoveSelectedButton();
			return true;
		}

		return false;		
	}
}
