using Godot;
using System;

public partial class LobbyUi : Control
{
	private const int DefaultPort = 8910;
	private const int MaxClient = 1;
	private	ENetMultiplayerPeer _peer;
	private LineEdit _ipAddress;
	private LineEdit _playerName;
	private Label _statusOk;
	private Label _statusFail;
	private Button _hostButton;
	private Button _joinButton;

	public override void _Ready()
	{
		_ipAddress = GetNode<LineEdit>("%AddressEdit");
		_playerName = GetNode<LineEdit>("%NameEdit");
		_statusOk = GetNode<Label>("%StatusOk");
		_statusFail = GetNode<Label>("%StatusFail");
		_hostButton = GetNode<Button>("%HostButton");
		_joinButton = GetNode<Button>("%JoinButton");

		var multi = GetTree().GetMultiplayer();

		multi.PeerConnected += PlayerConnected;
		multi.PeerDisconnected += PlayerDisconnected;
		multi.ConnectedToServer += ConnectedOk;
		multi.ConnectionFailed += ConnectedFail;
		multi.ServerDisconnected += ServerDisconnected;
	}

	public override void _ExitTree()
	{
		base._ExitTree();

		var multi = GetTree().GetMultiplayer();
		if(multi is not null)
		{
			multi.PeerConnected -= PlayerConnected;
			multi.PeerDisconnected -= PlayerDisconnected;
			multi.ConnectedToServer -= ConnectedOk;
			multi.ConnectionFailed -= ConnectedFail;
			multi.ServerDisconnected -= ServerDisconnected;			
		}
	}

	//Display text to screen below lobby panel
	private void SetStatus(string text, bool isOk)
	{
		if(isOk)
		{
			_statusOk.Text = text;
			_statusFail.Text = "";
		}
		else
		{
			_statusOk.Text = "";
			_statusFail.Text = text;			
		}
	}
	// Callback from SceneTree
	private void PlayerConnected(long id)
	{		
		var gameScene = ResourceLoader.Load<PackedScene>("res://moon_game.tscn").Instantiate() as MoonGame;
		SetPlayerName(gameScene);
		gameScene.Connect("GameFinished", new Callable(this, nameof(EndGame)), (int)ConnectFlags.Deferred);
		GetTree().Root.AddChild(gameScene);
		Hide();
	}

	private void SetPlayerName(MoonGame gameScene)
	{
		var player = GetTree().GetMultiplayer().IsServer()?Player.Server:Player.Client;
		gameScene.SetPlayerName((int)player, _playerName.Text);
	}

	// Callback from SceneTree
	private void PlayerDisconnected(long id)
	{
		EndGame(GetTree().GetMultiplayer().IsServer()?"Server Disconnected":"Client Disconnected");
	}

	// Callback from SceneTree, only for clients (not server).
	private void ConnectedOk()
	{
		// GD.Print("Multiplayer CB: ConnectedOk");
	}

	// Callback from SceneTree, only for clients (not server).
	private void ConnectedFail()
	{
		SetStatus("Couldn't Connect.!", false);
		CleanUpPeer();
		_hostButton.Disabled = false;
		_joinButton.Disabled = false;
	}

	// Callback from SceneTree
	private void ServerDisconnected()
	{
		EndGame("Server Disconnected");
	}

	//create a server
	private void OnHostButtonPressed()
	{
		_peer = new ENetMultiplayerPeer();
		var err = _peer.CreateServer(DefaultPort, MaxClient);
		if(err != Error.Ok)
		{
			SetStatus($"Failed to setup networking! Error: {err}", false);
			return;
		}
		_peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

		GetTree().GetMultiplayer().MultiplayerPeer = _peer;

		_ipAddress.Text = GetLocalIpAddress();
		SetStatus("Waiting for other player.. .", true);

		_hostButton.Disabled = true;
		_joinButton.Disabled = true;
	}

	// get this device Ip, used when creating a host
	private string GetLocalIpAddress()
	{
		foreach (string ip in IP.GetLocalAddresses())
		{
			if (ip.Contains(".") && !ip.StartsWith("127."))
				return ip;
		}

		return "127.0.0.1"; //no local network IP is found		
	}


	//join a server with IP
	private void OnJoinButtonPressed()
	{
		var ip = _ipAddress.Text;
		if(!ip.IsValidIPAddress())
		{
			SetStatus("IP Address is invalid.!", false);
			return;
		}

		_peer = new ENetMultiplayerPeer();
		_peer.CreateClient(ip, DefaultPort);
		_peer.Host.Compress(ENetConnection.CompressionMode.RangeCoder);

		GetTree().GetMultiplayer().MultiplayerPeer = _peer;
		SetStatus("Connecting.. .", true);

		_hostButton.Disabled = true;
		_joinButton.Disabled = true;
	}

	private void EndGame(string withError="")
	{
		if(HasNode("/root/MoonGame"))
		{
			GetNode("/root/MoonGame").Free();	
			Show();
		}
		CleanUpPeer();

		_hostButton.Disabled = false;
		_joinButton.Disabled = false;

		SetStatus(withError, false);
	}

	//release peer singleton global and local
	private void CleanUpPeer()
	{
		if (_peer != null)
		{
			_peer.Close();
			_peer = null;
		}

		GetTree().GetMultiplayer().MultiplayerPeer = null;		
	}
}
