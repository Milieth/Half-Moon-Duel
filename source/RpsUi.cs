using Godot;
using System;
using System.Collections.Generic;


public partial class RpsUi : Control
{
	public enum RPSResult
	{
		Draw,
		ServerWin,
		ServerLose		
	}

	public enum Card
	{
		None,
		Rock,
		Paper,
		Scissor
	}

    [Signal]
    public delegate void RPSFinishedEventHandler(int rpsWinner);

	private Dictionary<Player, Card> _chosenCard = new Dictionary<Player, Card>();
	private Dictionary<Player, int> _playerScore = new Dictionary<Player, int>();
	private HBoxContainer _rpsMenu;
	private Label _result;
	private Timer _nextRoundTimer;
	private Label _yourScore;
	private Label _enemyScore;
	private TextureRect _yourCard;
	private TextureRect _enemyCard;

	private const int WinRequirement = 1;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_rpsMenu = GetNode<HBoxContainer>("RPS_Menu");
		_result = GetNode<Label>("%Result");
		_yourScore = GetNode<Label>("%YourScoreLabel");
		_enemyScore = GetNode<Label>("%EnemyScoreLabel");
		_yourCard = GetNode<TextureRect>("%YourCard");
		_enemyCard = GetNode<TextureRect>("%EnemyCard");
		_yourCard.Hide();
		_enemyCard.Hide();

		_playerScore[Player.Server] = 0;
		_playerScore[Player.Client] = 0;

		_nextRoundTimer = GetNode<Timer>("NextRoundTimer");
		_nextRoundTimer.Timeout += RunNextRound;

		UpdatePlayerScoreLabel();
	}

	private void OnRockPressed()
	{
		OnCardPressed(Card.Rock);
	}

	private void OnPaperPressed()
	{
		OnCardPressed(Card.Paper);
	}

	private void OnScissorPressed()
	{
		OnCardPressed(Card.Scissor);
	}

	private void OnCardPressed(Card card)
	{
		_rpsMenu.Hide();
		if(GetTree().GetMultiplayer().IsServer())
		{
			CardSelected((int)Player.Server, (int)card);			
		}
		else
		{
			RpcId(1, nameof(CardSelected), (int)Player.Client, (int)card);
		}		
	}

	//event on card selected (Server only)
    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer)]
		private void CardSelected(int player, int card)
	{
		_chosenCard[(Player)player] = (Card)card;
		if(_chosenCard.Count == 2)
		{
			DetermineWinner();
		}
	}

	//check each player card and determine game result
	private void DetermineWinner()
	{
		RPSResult result;
		var serverCard = _chosenCard[Player.Server];
		var clientCard = _chosenCard[Player.Client];
		if(serverCard == clientCard)
		{
			result = RPSResult.Draw;
		}
		else if(serverCard == Card.Rock && clientCard == Card.Scissor ||
				serverCard == Card.Paper && clientCard == Card.Rock ||
				serverCard == Card.Scissor && clientCard == Card.Paper)
		{
			result = RPSResult.ServerWin;
		}
		else
		{
			result = RPSResult.ServerLose;
		}
		Rpc(nameof(SyncEndOfRound), (int)result, (int)serverCard, (int)clientCard);
		_nextRoundTimer.Start();
	}

	//sync the both client and server before go to next round
    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void SyncEndOfRound(int result, int serverCard, int clientCard)
	{
		DisplayWinnerResult(result, (Card)serverCard, (Card)clientCard);
		IncreasePlayerScore(result);
		UpdatePlayerScoreLabel();
	}

	//display game result to all player
	private void DisplayWinnerResult(int result, Card serverCard, Card clientCard)
	{
		string winResult = (RPSResult)result switch
		{
			RPSResult.Draw => "Draw",
			RPSResult.ServerWin when GetTree().GetMultiplayer().IsServer() => "Win",
			RPSResult.ServerWin when !GetTree().GetMultiplayer().IsServer() => "Lose",
			RPSResult.ServerLose when GetTree().GetMultiplayer().IsServer() => "Lose",
			RPSResult.ServerLose when !GetTree().GetMultiplayer().IsServer() => "Win",
			_ => "Unknown"
		};
		_result.Text = winResult;
		var yourCard = GetTree().GetMultiplayer().IsServer()?serverCard:clientCard;
		var enemyCard = GetTree().GetMultiplayer().IsServer()?clientCard:serverCard;
		var yourCardPath = "res://" + yourCard.ToString().ToLower() + ".jpg";
		var enemyCardPath = "res://" + enemyCard.ToString().ToLower() + ".jpg";
		_yourCard.Texture = GD.Load<Texture2D>(yourCardPath);
		_enemyCard.Texture = GD.Load<Texture2D>(enemyCardPath);
		_yourCard.Show();
		_enemyCard.Show();
	}

	//Increase player score if they win
	private void IncreasePlayerScore(int gameResult)
	{
		switch((RPSResult)gameResult)
		{
			case RPSResult.ServerWin:
				_playerScore[Player.Server]++;
				break;
			case RPSResult.ServerLose:
				_playerScore[Player.Client]++;
				break;
			default:
				break;
		}
	}
	
	//update player score label
	private void UpdatePlayerScoreLabel()
	{
		var yourScore = GetTree().GetMultiplayer().IsServer()?_playerScore[Player.Server]:_playerScore[Player.Client];
		var enemyScore = GetTree().GetMultiplayer().IsServer()?_playerScore[Player.Client]:_playerScore[Player.Server];
		_yourScore.Text = $"Your Score : {yourScore}";
		_enemyScore.Text = $"Enemy Score : {enemyScore}";
	}

	//this round ended, rematch or close the game
	private void RunNextRound()
	{
		if(isPlayerWinTheGame()) Rpc(nameof(CloseAllRPSGame));
		else Rpc(nameof(RematchRPS));
	}

    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void CloseAllRPSGame()
	{
		var rpsWinner = Player.Client;
		if(_playerScore[Player.Server] > _playerScore[Player.Client])
			rpsWinner = Player.Server;

		EmitSignal("RPSFinished", (int)rpsWinner);	
	}

	//rematch the game, reset rps game data
    [Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RematchRPS()
	{
		_result.Text = "";
		_chosenCard.Clear();
		_rpsMenu.Show();
		_yourCard.Hide();
		_enemyCard.Hide();
		// GD.Print("Rematch Occured");
	}

	//check if player score reach max score to win
	private bool isPlayerWinTheGame()
	{
		bool serverMetReq = _playerScore[Player.Server] >= WinRequirement;
		bool clientMetReq = _playerScore[Player.Client] >= WinRequirement;
		bool sameScore = _playerScore[Player.Server] == _playerScore[Player.Client];
		return (serverMetReq || clientMetReq) && !sameScore;
	}
}
