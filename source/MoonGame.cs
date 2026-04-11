using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

using GDArray = Godot.Collections.Array;

public class CardMatchEvent
{
	public MoonCard.Pattern _pattern{get;set;}
	public List<string> _nodes{get;set;}
	public int _score{get;set;}
	public Player _owner{get;set;}

	static public GDArray CreateRPCsPackedArr(List<CardMatchEvent> matchEventList)
	{
		GDArray matchEventArr = new();
		
		foreach(var matchEvent in matchEventList)
			matchEventArr.Add(matchEvent.CreateRPCsPacked());
		
		return matchEventArr;
	}

	static public List<CardMatchEvent> UnpackingRPCsPacked(GDArray matchEventArr)
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
	private AudioStreamPlayer _firstDraw;
	private AudioStreamPlayer _bgm;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_multiplayerAuth = GetTree().GetMultiplayer().IsServer()? Player.Server : Player.Client;
		_rng.Randomize();

		_exitButton = GetNode<Button>("ExitButton");
		_exitButton.Pressed += OnExitButtonPressed; 
		_prompt = GetNode<Label>("Prompt");
		_prompt.Text = "";

		_firstDraw = GetNode<AudioStreamPlayer>("FirstDraw");
		_bgm = GetNode<AudioStreamPlayer>("BGM");
		var bgmFiles = new List<string>
		{
			"res://aset/audio/Edgeworth-kuiper belt.mp3",
			"res://aset/audio/Fearsome_dark_space.mp3",
			"res://aset/audio/moon walk.mp3"
		};
		_bgm.Stream = GD.Load<AudioStream>(bgmFiles[_rng.RandiRange(0, bgmFiles.Count-1)]);
		if (_bgm.Stream is AudioStreamMP3 mp3Stream)
			mp3Stream.Loop = true;
		

		if(_multiplayerAuth == Player.Server)
		{
			// AudioServer.SetBusMute(0, true);
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
		InitLayout(_multiplayerAuth);

		_firstDraw.Play();
		if(_multiplayerAuth == Player.Server)
		{
			for(int i = 0; i < 2; i++)
			{
				DrawCardRandomly(Player.Server, true);
				DrawCardRandomly(Player.Client, true);
			}			
			DrawCardRandomly((Player)rpsWinner, true);
		}

		if(GetTree().GetMultiplayer().IsServer())
			Rpc(nameof(SetGameTurn), rpsWinner);
		Show();

		if(HasNode("/root/RPS_UI"))
			GetNode("/root/RPS_UI").QueueFree();

		_bgm.Play();
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
	private void DrawCardRandomly(Player player, bool withAnim)
	{
		int phase = _rng.RandiRange(0,7);
		Rpc(nameof(DrawCard), (int)player, phase, withAnim);
	}

	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void DrawCard(int player, int phase, bool withAnim)
	{
		_hand[(Player)player].DrawCard(phase, withAnim);
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
		if((Player)player == Player.None)
			return;

		foreach(var playerScore in _score.Values)
			playerScore._activeIcon.Hide();

		_activePlayer = (Player)player;
		if(IsYourTurn())
		{
			_hand[_multiplayerAuth].ShowAllCard();
			_score[_multiplayerAuth]._activeIcon.Show();
			_board.Disabled(false, true);
		}
		else
		{
			//opponent turn
			_hand[_multiplayerAuth].ShowAllCard(true);
			_score[_activePlayer]._activeIcon.Show();
			_board.Disabled(true, true);
		}
	}

	//disable any input, until SetGameTurn() called
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StopInput()
	{
		_hand[_multiplayerAuth].ShowAllCard(true);
		_board.Disabled(true, true);
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

	//construct full phase event from target node, if there are no event occured return null
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

	//construct lunar phase cycle event from target node, if there are no event occured return null
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
				score: nodes.Count,
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

	//player pressed board node and request a move
	private void OnNodePressed(string nodeID)
	{
		GD.Print($"Node Pressed:{nodeID}");
		StopInput();
		if(!GetTree().GetMultiplayer().IsServer())
			RpcId(1, nameof(ValidateMove), nodeID, _hand[Player.Client].GetSelectedButtonIndex(), (int)Player.Client);
		else
			ValidateMove(nodeID, _hand[Player.Server].GetSelectedButtonIndex(), (int)Player.Server);
		
	}	

	//check if place a card action is possible
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private bool ValidateMove(string nodeID, int cardIndex, int sender)
	{
		bool validCard = _hand[(Player)sender].ValidCard(cardIndex);
		bool nodeIsEmpty = _board._nodes[nodeID]._card is null;
		bool validPlayer = _activePlayer == (Player)sender;
		bool valid = validCard && nodeIsEmpty && validPlayer;
		if(valid)
		{
			Rpc(nameof(PlaceCard), nodeID, cardIndex, sender);
		}
		else
		{
			GD.Print("Invalid Move, Try Again");
			Rpc(nameof(SetGameTurn), (int)_activePlayer);
		}
		return valid;
	}

	//place card from hand to board, note: card in hand are destroyed in tweening
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void PlaceCard(string nodeID, int cardIndex, int sender)
	{
		var cardInHand = _hand[(Player)sender].GetCard(cardIndex);
		var phase = cardInHand._phase;

		var cardInBoard = GD.Load<PackedScene>("res://moon_card.tscn").Instantiate<MoonCard>();
		AddChild(cardInBoard);
		cardInBoard.Hide();
		cardInBoard.SetMoonPhase(phase);
		_board.PlaceCard(nodeID, cardInBoard);

		if(_multiplayerAuth == Player.Server)
		{
			var matchEvent = GetCardEvents(nodeID, (Player)sender);
			if(matchEvent is not null)
				Rpc(nameof(RunPlaceCardEventAnim), nodeID, cardIndex, sender, CardMatchEvent.CreateRPCsPackedArr(matchEvent));
		}
	}

	//tweening place card animation and card matching event animation
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RunPlaceCardEventAnim(string nodeID, int cardIndex, int sender, GDArray matchEventArr)
	{
		var cardInHand = _hand[(Player)sender].GetCard(cardIndex);
		var tween = GetTree().CreateTween();
		tween.TweenSubtween(_board.PlaceCardAnim(nodeID, cardInHand));
		tween.TweenCallback(Callable.From(() =>
		{
			_board._nodes[nodeID]._card.Show();
			_hand[(Player)sender].RemoveButton(cardIndex);
		}));

		tween.TweenSubtween(ImplementCardEvents(matchEventArr));
		if(_multiplayerAuth == Player.Server)
			tween.TweenCallback(new Callable(this, nameof(EndPhase))).SetDelay(0.5); //prevent server to finish early
	}

	//(Server) determine winner of the game and display it
	private void DetermineWinner()
	{
		if(_multiplayerAuth == Player.Client)
			return;

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

	//prepare to the next turn or end the game
	private void EndPhase()
	{
		if(_multiplayerAuth == Player.Client)
			return;
		
		if(!_board.IsBoardFull())
		{
			DrawCardRandomly(GetNextPlayerTurn(), true);
			Rpc(nameof(SetGameTurn), (int)GetNextPlayerTurn());
		}
		else
		{
			//end game
			Rpc(nameof(StopInput));
			GainBonusPoint();
		}
	}
	
	//Implement card pattern matching events
	private Tween ImplementCardEvents(GDArray matchEventArr)
	{
		var tween = GetTree().CreateTween();
		tween.TweenInterval(1.0f);
		foreach(var matchEvent in CardMatchEvent.UnpackingRPCsPacked(matchEventArr))
		{
			matchEvent.print();
			tween.TweenSubtween(CardMatchEventAnim(matchEvent));	
		}

		return tween;
	}

	//tweening zoom animation of target cards
	private Tween ZoomCardAnim(List<string> nodeIDs, Vector2 scale, double duration = 0.5)
	{
		var tween = GetTree().CreateTween();
		tween.SetParallel(true);
		foreach(var id in nodeIDs)
		{
			var card = _board._nodes[id]._card;
			tween.TweenProperty(card, "scale", scale, duration);
		}

		return tween;
	}

	//tweeening set color animation of target cards
	private Tween RecolorCardAnim(List<string> nodeIDs, MoonCard.Color colorType)
	{
		var tween = GetTree().CreateTween();
		tween.SetParallel(true);

		foreach(var id in nodeIDs)
		{
			var card = _board._nodes[id]._card;
			tween.TweenProperty(card, "modulate", MoonCard.GetColor(colorType), 0.2);
			card._color = colorType;
		}

		return tween;
	}

	//reset card color to original color, neither bright nor dark. and reset connection between nodes
	private Tween ResetColorAndConnection(List<string> nodeIDs)
	{
		var tween = GetTree().CreateTween();
		tween.SetParallel(true);
		tween.TweenCallback(Callable.From(() =>
		{
			_board.ResetConnectionLine(ConnectionLine.GetLineNameFromNodes(nodeIDs));
		}));
		foreach(var id in nodeIDs)
		{
			var card = _board._nodes[id]._card;
			tween.TweenCallback(Callable.From(() =>
			{
				card.Modulate = MoonCard.GetColor(MoonCard.Color.None);		//doesn't change actual color type
			}));
		}

		return tween;
	}

	//tweening collecting star animation from card list in board
	private Tween CollectingStarAnim(List<string> nodeIDs, Player player, double delay = 0.7)
	{
		var tween = GetTree().CreateTween();
		foreach(var id in nodeIDs)
			tween.TweenSubtween(CardCollectingStarAnim(id, player, delay));

		return tween;
	}

	//tweening collecting star animation from card in board
	private Tween CardCollectingStarAnim(string nodeID, Player player, double delay = 0.7)
	{
		var tween = GetTree().CreateTween();
		var card = _board._nodes[nodeID]._card;
		var scorePos = _score[player].GetStarCenterPos();
		tween.TweenCallback(Callable.From(()=>{card.GetCollectingStarAnim(scorePos, _playerColor[player]);}))
			.SetDelay(delay);
		tween.TweenCallback(Callable.From(() =>
		{
			_score[player].IncreaseScore(1);
		}));

		return tween;
	}

	// tweening card pattern matching event
	public Tween CardMatchEventAnim(CardMatchEvent matchEvent)
	{
		var playerColor = _playerColor[matchEvent._owner];

		var tween = GetTree().CreateTween();
		tween.TweenSubtween(ResetColorAndConnection(matchEvent._nodes));
		tween.TweenSubtween(ZoomCardAnim(matchEvent._nodes, new Vector2(1.2f, 1.2f)));
		tween.TweenSubtween(RecolorCardAnim(matchEvent._nodes, playerColor));
		tween.TweenSubtween(_board.GetConnectionLineEventAnim(matchEvent));
		for(int i = 0; i < matchEvent._nodes.Count; i++)
		{
			if(!(matchEvent._pattern == MoonCard.Pattern.PhasePair && i == 1))			
				tween.TweenSubtween(CardCollectingStarAnim(matchEvent._nodes[i], matchEvent._owner));
		}
		tween.TweenSubtween(ZoomCardAnim(matchEvent._nodes, new Vector2(1.0f, 1.0f), 1.0));

		return tween;
	}

	//(Server) Player gain bonus point based on many card they claimed at the end of game
	private void GainBonusPoint()
	{
		var serverBonus = _board.GetCardByColor(_playerColor[Player.Server]);
		var clientBonus = _board.GetCardByColor(_playerColor[Player.Client]);
		var claimedCards = new Godot.Collections.Dictionary<Player, string>
		{
            [Player.Server] = string.Join("", serverBonus),
            [Player.Client] = string.Join("", clientBonus)
		};

		Rpc(nameof(ImplementBonusPoint), claimedCards);
	}

	//increase each player point at bonus poin phase
	[Rpc(mode:MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ImplementBonusPoint(Godot.Collections.Dictionary<Player, string> claimedCards)
	{
		var tween = GetTree().CreateTween();
		foreach(var (player, nodeStr) in claimedCards)
		{
			var nodeIDList = nodeStr.Select(c => c.ToString()).ToList();
			if(nodeIDList.Count != 0)
			{
				tween.TweenSubtween(ZoomCardAnim(nodeIDList, new Vector2(1.2f, 1.2f)));
				tween.TweenSubtween(CollectingStarAnim(nodeIDList, player));			
				tween.TweenSubtween(ZoomCardAnim(nodeIDList, new Vector2(1.0f, 1.0f), 0.2));
			}
		}

		tween.TweenCallback(new Callable(this, nameof(DetermineWinner)));

		GD.Print($"{Player.Server} Gain Bonus:{claimedCards[Player.Server].Length}");		
		GD.Print($"{Player.Client} Gain Bonus:{claimedCards[Player.Client].Length}");
	}
}
