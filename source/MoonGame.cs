using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CardMatchEvent
{
	public MoonCard.Pattern _pattern{get;set;}
	public List<string> _nodes{get;set;}
	public int _score{get;set;}
	public Player _owner{get;set;}

	static public Godot.Collections.Array CreateRPCsPackedArr(List<CardMatchEvent> matchEventList)
	{
		Godot.Collections.Array matchEventArr = new();
		
		foreach(var matchEvent in matchEventList)
			matchEventArr.Add(matchEvent.CreateRPCsPacked());
		
		return matchEventArr;
	}

	static public List<CardMatchEvent> UnpackingRPCsPacked(Godot.Collections.Array matchEventArr)
	{
		List<CardMatchEvent> matchEventList = new();
		foreach(Godot.Collections.Dictionary dict in matchEventArr)
		{
			var matchEvent = new CardMatchEvent
			(
				(int)dict["Pattern"], 
				(string)dict["Nodes"], 
				(int)dict["Score"], 
				(int)dict["Owner"]
			);
			matchEventList.Add(matchEvent);
		}

		return matchEventList;		
	}


	public CardMatchEvent()
	{
	}

	public CardMatchEvent(MoonCard.Pattern pattern, List<string> nodes, int score, Player owner)
	{
		_pattern = pattern;
		_nodes = nodes;
		_score = score;
		_owner = owner;
	}

	//use this for unboxing RPCs packed
	public CardMatchEvent(int pattern, string nodes, int score, int owner)
	{
		_pattern = (MoonCard.Pattern)pattern;
		_nodes = nodes.Select(c => c.ToString()).ToList();
		_score = score;
		_owner = (Player)owner;
	}

	//packed card match event data for RPCs
	public Godot.Collections.Dictionary CreateRPCsPacked()
	{
		var dict = new Godot.Collections.Dictionary
        {
            ["Pattern"] = (int)_pattern,
            ["Nodes"] = string.Join("", _nodes),
            ["Score"] = _score,
            ["Owner"] = (int)_owner
        };
		return dict;
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
	[Signal]
	public delegate void GameFinishedEventHandler(string withError);
	private Player _multiplayerAuth = Player.Server;
	private Board _board;
	private Dictionary<Player, PlayerScore> _score = new();
	private Dictionary<Player, Hand> _hand = new();
	private Dictionary<Player, MoonCard.Color> _playerColor = new();
	private Player _activePlayer = Player.None;
	private Label _prompt;
	private Button _exitButton;

	private RandomNumberGenerator _rng= new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_multiplayerAuth = GetTree().GetMultiplayer().IsServer()? Player.Server : Player.Client;
		_rng.Randomize();

		_exitButton = GetNode<Button>("ExitButton");
		_exitButton.Pressed += OnExitButtonPressed; 
		_prompt = GetNode<Label>("Prompt");
		_prompt.Text = "";

		if(_multiplayerAuth == Player.Server)
		{
			int boardID = _rng.RandiRange(1, Board.MaxID);
			Rpc(nameof(InitBoard), boardID);		
		}

		Hide();
		var rpsScene = GD.Load<PackedScene>("res://rps_ui.tscn").Instantiate<RpsUi>();
		GetTree().Root.AddChild(rpsScene);
		rpsScene.RPSFinished += OnRPSFinished;
	}

	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void InitBoard(int boardID)
	{
		_board = GD.Load<PackedScene>($"res://Board{boardID}.tscn").Instantiate<Board>();
		AddChild(_board);
		foreach(var node in _board._nodes.Values)
			node.OnBoardNodePressed += OnNodePressed;		
	}

	private void OnRPSFinished(int rpsWinner)
	{
		//start the game
		InitPlayerColor((Player)rpsWinner);

		if(_multiplayerAuth == Player.Server)
		{
			for(int i = 0; i < 2; i++)
			{
				DrawCardRandomly(Player.Server);
				DrawCardRandomly(Player.Client);
			}			
			DrawCardRandomly((Player)rpsWinner);
		}

		InitLayout(_multiplayerAuth);
		if(GetTree().GetMultiplayer().IsServer())
			Rpc(nameof(SetGameTurn), rpsWinner);
		Show();

		if(HasNode("/root/RPS_UI"))
			GetNode("/root/RPS_UI").QueueFree();
	}

	//setting game object ownership based on rps winner
	private void InitPlayerColor(Player rpsWinner)
	{
		var rpsLoser = (rpsWinner == Player.Server)? Player.Client : Player.Server;
		_playerColor[rpsWinner] = MoonCard.Color.Bright;
		_playerColor[rpsLoser] = MoonCard.Color.Dark;

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
				hand.SetRevealMode(Hand.RevealMode.FaceDown);
			}
		}

		foreach(var (player, score) in _score)
		{
			if(player == front) score.Position = new Vector2(62, 489);
			else score.Position = new Vector2(1088, 193);
		}
	}

	// (Server Only) Draw card randomly to target player hand
	private void DrawCardRandomly(Player player)
	{
		int phase = _rng.RandiRange(0,7);
		Rpc(nameof(DrawCard), (int)player, phase);
	}

	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void DrawCard(int player, int phase)
	{
		_hand[(Player)player].DrawCard(phase);
	}

	//wheter this is your turn or opponent turn
	private bool IsYourTurn()
	{
		return _multiplayerAuth == _activePlayer;
	}

	//return the opposite of current active player
	private Player GetNextPlayerTurn()
	{
		return (_activePlayer == Player.Server)? Player.Client : Player.Server;
	}

	//change game turn owner
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SetGameTurn(int player)
	{
		foreach(var playerScore in _score.Values)
			playerScore._activeIcon.Hide();

		_activePlayer = (Player)player;
		if(IsYourTurn())
		{
			// GD.Print($"{_multiplayerAuth}: Set Your Turn");
			_hand[_multiplayerAuth].ShowAllCard();
			_score[_multiplayerAuth]._activeIcon.Show();
			_board.Disabled(false, true);
		}
		else if(_activePlayer == Player.None)
		{
			// GD.Print($"{_multiplayerAuth}: Set None Turn");
			//waiting server to finish processing
			_hand[_multiplayerAuth].ShowAllCard(true);
			_board.Disabled(true, true);
		}
		else
		{
			//opponent turn
			// GD.Print($"{_multiplayerAuth}: Set Opponent Turn");
			_hand[_multiplayerAuth].ShowAllCard(true);
			_score[_activePlayer]._activeIcon.Show();
			_board.Disabled(true, true);
		}
	}

	//construct phase pair event from target node, if there are no event occured return null
	private List<CardMatchEvent> GetPhasePairEvent(string targetNodeID, Player player)
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
				owner: player
			);
			pairEvent.Add(matchEvent);
		}

		return pairEvent;		
	}

	private List<CardMatchEvent> GetFullPhaseEvent(string targetNodeID, Player player)
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
				owner: player
			);
			fullEvent.Add(matchEvent);
		}

		return fullEvent;
	}

	private List<CardMatchEvent> GetPhaseSequenceEvent(string targetNodeID, Player player)
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
				owner: player
			);
			sequenceEvent.Add(matchEvent);
		}

		return sequenceEvent;
	}

	//construct all card events that detected, if there are no event occured return null
	private List<CardMatchEvent> GetCardEvents(string targetNodeID, Player player)
	{
		var pair = GetPhasePairEvent(targetNodeID, player);
		var full = GetFullPhaseEvent(targetNodeID, player);
		var sequence = GetPhaseSequenceEvent(targetNodeID, player);

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
		GD.Print($"Node Pressed:{nodeID}");
		if(!GetTree().GetMultiplayer().IsServer())
		{
			SetGameTurn((int)Player.None);
			RpcId(1, nameof(ValidateMove), nodeID, _hand[Player.Client].GetSelectedButtonIndex(), (int)Player.Client);
		}
		else
		{
			ValidateMove(nodeID, _hand[Player.Server].GetSelectedButtonIndex(), (int)Player.Server);
		}
	}	

	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private bool ValidateMove(string nodeID, int cardIndex, int sender)
	{
		bool validCard = _hand[(Player)sender].ValidCard(cardIndex);
		bool nodeIsEmpty = _board._nodes[nodeID]._card is null;
		bool validPlayer = _activePlayer == (Player)sender;
		bool valid = validCard && nodeIsEmpty && validPlayer;
		if(valid)
		{
			ExecuteMove(nodeID, cardIndex, sender);
		}
		else
		{
			GD.Print("Invalid Move, Try Again");
			Rpc(nameof(SetGameTurn), (int)_activePlayer);
		}
		return valid;
	}

	//execute player move and its corresponding event
	private void ExecuteMove(string nodeID, int cardIndex, int sender)
	{
		Rpc(nameof(PlaceCard), nodeID, cardIndex, sender);

		var matchEvent = GetCardEvents(nodeID, (Player)sender);
		if(matchEvent is not null)
			Rpc(nameof(ImplementCardEvents), CardMatchEvent.CreateRPCsPackedArr(matchEvent));
		
		//next phase
		if(!_board.IsBoardFull())
		{
			DrawCardRandomly(GetNextPlayerTurn());
			Rpc(nameof(SetGameTurn), (int)GetNextPlayerTurn());
		}
		else
		{
			Rpc(nameof(SetGameTurn), (int)Player.None);
			GainBonusPoint();
			DetermineWinner();
		}
	}

	//(Server) determine winner of the game and display it
	private void DetermineWinner()
	{
		var server = _score[Player.Server].Score;
		var client = _score[Player.Client].Score;
		Player winner = Player.None;
		if(server > client) winner = Player.Server;
		else if(server < client) winner = Player.Client;
		
		Rpc(nameof(DisplayWinner), (int)winner);
	}

	//display winner prompt and exit button
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void DisplayWinner(int player)
	{
		if((Player)player != Player.None)
		{
			var color = _playerColor[(Player)player];
			_prompt.Text = $"{color} Moon Win";
			_prompt.SelfModulate = (color == MoonCard.Color.Bright)? 
			new Godot.Color(1.0f, 1.0f, 1.0f):
			new Godot.Color(0.0f, 0.0f, 0.0f);
		}
		else
		{
			_prompt.Text = "The Game Turned Draw.!!";
		}

		_exitButton.Show();		
	}

	//Close the moon game and return to lobby
	private void OnExitButtonPressed()
	{
		EmitSignal("GameFinished", "Game Finished");
	}

	//move card from player hand to board node
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PlaceCard(string nodeID, int cardIndex, int sender)
	{
		var card = _hand[(Player)sender].GetCard(cardIndex);
		card.FlipFaceUp();
		_board.PlaceCard(nodeID, card);
		_hand[(Player)sender].RemoveButton(cardIndex);
	}

	//Implement card pattern matching events
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ImplementCardEvents(Godot.Collections.Array matchEventArr)
	{
		foreach(var matchEvent in CardMatchEvent.UnpackingRPCsPacked(matchEventArr))
		{
			matchEvent.print();
			_score[matchEvent._owner].IncreaseScore(matchEvent._score);
			_board.SetCardsColor(matchEvent._nodes, _playerColor[matchEvent._owner]);
			_board.SetConnectionLineByEvent(matchEvent);
		}
	}

	//(Server) Player gain bonus point based on many card they claimed at the end of game
	private void GainBonusPoint()
	{
		int serverBonus = _board.CountCardsByColor(_playerColor[Player.Server]);
		int clientBonus = _board.CountCardsByColor(_playerColor[Player.Client]);
		Rpc(nameof(ImplementBonusPoint), serverBonus, clientBonus);
	}

	//increase each player point
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ImplementBonusPoint(int serverBonus, int clientBonus)
	{
		_score[Player.Server].IncreaseScore(serverBonus);
		GD.Print($"{Player.Server} Gain Bonus:{serverBonus}");		
		_score[Player.Client].IncreaseScore(clientBonus);
		GD.Print($"{Player.Client} Gain Bonus:{clientBonus}");
	}
}
