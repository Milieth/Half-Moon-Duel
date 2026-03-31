using Godot;
using System;
using System.Collections.Generic;

public partial class ConnectionLine : Node2D
{
	private Line2D _edge;
	public Sprite2D _pairIcon{get;private set;}
	public MoonCard.Pattern _matchPattern{get;private set;} = MoonCard.Pattern.None;

	//get a list of connection line name from node list
	static public List<string> GetLineNameFromNodes(List<string> nodeIDs)
	{
		var lineNameList = new List<string>();
		for(int i = 0; i < nodeIDs.Count-1; i++)
			lineNameList.Add(nodeIDs[i]+nodeIDs[i+1]);

		return lineNameList;		
	}
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_edge = GetNode<Line2D>("Edge");
		_pairIcon = GetNode<Sprite2D>("PairIcon");
	}

	//connect 2 pos with a line
	public void ConnectEdge(Vector2 pos1, Vector2 pos2)
	{
		_edge.ClearPoints();
		_edge.AddPoint(pos1);
		_edge.AddPoint(pos2);

		_pairIcon.GlobalPosition = new Vector2((pos1.X + pos2.X)/2.0f, (pos1.Y + pos2.Y)/2.0f);
	}

	//change connection line type by specified Mooncard pattern between node
	public void SetMatchPattern(MoonCard.Pattern pattern)
	{
		_matchPattern = pattern;
		switch(_matchPattern)
		{
			case MoonCard.Pattern.None:
				_edge.DefaultColor = new Color(0.58f, 0.58f, 0.58f);
				_pairIcon.Hide();
				break;
			case MoonCard.Pattern.PhasePair:
				_edge.DefaultColor = new Color(0.58f, 0.58f, 0.58f);
				_pairIcon.Show();
				_pairIcon.Texture = GD.Load<Texture2D>("res://aset/white pair.png");
				break;
			case MoonCard.Pattern.FullPhase:
				_edge.DefaultColor = new Color(0.58f, 0.58f, 0.58f);
				_pairIcon.Show();
				_pairIcon.Texture = GD.Load<Texture2D>("res://aset/white dot.png");
				break;
			case MoonCard.Pattern.PhaseSequence:
				_edge.DefaultColor = new Color(1.0f, 1.0f, 1.0f);
				_pairIcon.Hide();
				break;
			default:
				break;
		}
	}
}
