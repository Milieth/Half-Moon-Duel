using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Board : Control
{
	public const int MaxID = 5;
	private int _id = 0;
	public Dictionary<string, BoardNode> _nodes{get;set;} = new();
	public Dictionary<string, ConnectionLine> _edgeConnection{get;set;} = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ZIndex = -1;
		MouseFilter = MouseFilterEnum.Ignore;

		foreach(Node child in GetChildren())
		{
			if(child is BoardNode node)
			{
				_nodes.Add(node._id, node);	
			}
		}
		
		ConnectEdgeOfEveryNodes();
	}

	//if disabled, prevent user to click board node
	public void Disabled(bool isDisabled, bool onlyEmptyNode)
	{
		foreach (var node in _nodes.Values)
		{
			if (!onlyEmptyNode || node._card is null)
				node.Disabled = isDisabled;
		}
	}

	//return true if no more card can be attached in board, false otherwise
	public bool IsBoardFull()
	{
		return CountEmptyNodes() == 0;
	}

	//return many nodes with no card attach to it
	public int CountEmptyNodes()
	{
		int manyNodes = 0;
		foreach(var node in _nodes.Values)
			if(node._card is null)
				manyNodes++;

		return manyNodes;
	}

	//return many cards in board with given color
	public int CountCardsByColor(MoonCard.Color colorType)
	{
		return GetCardByColor(colorType).Count;
	}

	//search a list of cards with the same color(owner)
	public List<string> GetCardByColor(MoonCard.Color colorType)
	{
		var nodeIDs = new List<string>();
		foreach(var node in _nodes.Values)
			if(node._card is not null && node._card._color == colorType)
					nodeIDs.Add(node._id);
		
		return nodeIDs;
	}

	//setting cards color bright or dark
	public void SetCardsColor(List<string> nodes, MoonCard.Color color)
	{
		foreach(var id in nodes)
			_nodes[id]._card.SetColor(color);
	}

	//reset connection line to only board connection line(grey line)
	public void ResetConnectionLine(List<string> lineNameList)
	{
		foreach(var lineName in lineNameList)
		{
			var sortedName = new string(lineName.OrderBy(c => c).ToArray());
			_edgeConnection[sortedName].SetMatchPattern(MoonCard.Pattern.None);			
		}		
	}

	//tweening connection line event based on data in CardMatchEvent
	public Tween GetConnectionLineEventAnim(CardMatchEvent matchEvent)
	{
		var tween = GetTree().CreateTween();
		foreach(var lineName in ConnectionLine.GetLineNameFromNodes(matchEvent._nodes))
		{
			if(_edgeConnection.TryGetValue(lineName, out var connectionLine))
			{
				tween.TweenSubtween(connectionLine.GetConnectionAnim(matchEvent._pattern));
			}
			else
			{
				var sortedName = new string(lineName.OrderBy(c => c).ToArray());
				tween.TweenSubtween(_edgeConnection[sortedName].GetConnectionAnim(matchEvent._pattern, reversedDirection:true));
			}
		}

		return tween;
	}

	//set connection line type based on occured event
	public void SetConnectionLineByEvent(CardMatchEvent matchEvent)
	{
		foreach(var lineName in ConnectionLine.GetLineNameFromNodes(matchEvent._nodes))
		{
			var sortedName = new string(lineName.OrderBy(c => c).ToArray());
			_edgeConnection[sortedName].SetMatchPattern(matchEvent._pattern);			
		}
	}

    // find possible lunar sequence chain from target node
	public List<List<string>> GetPhaseSequenceList(string targetNodeID)
	{
		var backwardBranch = FindLunarBranch(targetNodeID, true);
		var forwardBranch = FindLunarBranch(targetNodeID, false);

		var allPaths = backwardBranch.Concat(forwardBranch).ToList();
		var uniquePaths = new List<List<string>>();
		var addedPath = new HashSet<HashSet<string>>(HashSet<string>.CreateSetComparer());
		foreach(var path in allPaths)
		{
			var hashPath = new HashSet<string>(path);
			if(!addedPath.Contains(hashPath) && path.Count >= 3)
			{
				uniquePaths.Add(path);
				addedPath.Add(hashPath);		//ensure the same haseSet not added twice
			}
		}

		return uniquePaths;
	}

	//detect lunar full phase pair from target node, then return it
	public List<List<string>> GetFullPhaseList(string targetNodeID)
	{
		Func<int, int, bool> fullPhaseCondition = (targetPhase, adjacentPhase) =>
		{
			return Math.Abs(targetPhase - adjacentPhase) == 4;
		};
		return GetPhasePair(targetNodeID, fullPhaseCondition);
	}

	//detect lunar phase pair from target node, then return it
	public List<List<string>> GetPhasePairList(string targetNodeID)
	{
		return GetPhasePair(targetNodeID, (targetPhase, adjacentPhase) => targetPhase == adjacentPhase);
	}

	//tweening move a card from hand to board, will flip it face up if it currently face down
	public Tween PlaceCardAnim(string nodeID, MoonCard card)
	{
		var nodePos = _nodes[nodeID].GetCenterPosition();
		
		var tween = GetTree().CreateTween();
		tween.TweenProperty(card, "global_position", nodePos, 0.2);
		if(!card.IsFaceUp())
			tween.TweenSubtween(card.GetFlipFaceUpAnim());

		return tween;
	}

	//place moon card in node
	public void PlaceCard(string nodeID, MoonCard card)
	{
		if(card is not null)
			_nodes[nodeID].AddCard(card);
	}

	//Remove moon card from a node in board
	public void RemoveCard(string nodeID)
	{
		_nodes[nodeID].RemoveCard();
	}

    // Helper: search possible lunar phase sequence from one direction(backward or forward) using depth first search allogaritm
	private List<List<string>> DepthFirstSearch(string targetNodeID, int direction, HashSet<string> visitedID)
	{
		var targetNode = _nodes[targetNodeID];
		if(targetNode._card is null)
			return new();
		visitedID.Add(targetNodeID);
		var searchPath = new List<List<string>>();

		var neighborIDs = targetNode._adjacent;
		foreach(var neighborID in neighborIDs)
		{
			var neighbor = _nodes[neighborID];
			if(!visitedID.Contains(neighborID) && neighbor._card is not null)
			{
				int expectedPhase = (targetNode._card._phase + direction + 8) % 8;
				if(neighbor._card._phase == expectedPhase)
				{
					var subPath = DepthFirstSearch(neighborID, direction, visitedID);
					if(searchPath.Count == 0)
					{
						searchPath = subPath;		//no branch yet
					}
					else
					{
						foreach(List<string> path in subPath)	//branching detected
							searchPath.Add(path);
					}
				}
			}
		}

		visitedID.Remove(targetNodeID);
		if (searchPath.Count == 0)
		{
			searchPath.Add(new List<string>{targetNodeID});
		}
		else
		{
			foreach(List<string> path in searchPath)
				path.Insert(0, targetNodeID);
		}
		return searchPath;
	}

    // Helper: find possible path branch of lunar phase sequence, seeking from backward first or forward first
	private List<List<string>> FindLunarBranch(string targetNodeID, bool isStartFromBackward)
	{
		int direction = 1;
		if(isStartFromBackward)
			direction = -1;

		var fullPathBranchs = new List<List<string>>();
		var firstBranchs = DepthFirstSearch(targetNodeID, direction, new());
		foreach(var firstBranch in firstBranchs)
		{
			firstBranch.Remove(targetNodeID);
			firstBranch.Reverse();
			var visited = new HashSet<string>(firstBranch);
			var secondBranchs = DepthFirstSearch(targetNodeID, -direction, visited);
			foreach(var secondBranch in secondBranchs)
				fullPathBranchs.Add(firstBranch.Concat(secondBranch).ToList());
			
		}

		return fullPathBranchs;
	}

	//Helper: get phase pair from target node with specified pattern condition
	private List<List<string>> GetPhasePair(string targetNodeID, Func<int, int, bool> phaseCondition)
	{
		var moonPair = new List<List<string>>();
		var targetNode = _nodes[targetNodeID];
		var neighbors = targetNode._adjacent;
		foreach(var neighbor in neighbors)
		{
			if(_nodes[neighbor]._card is not null)
			{
				// var distance = Math.Abs(targetNode._card._phase - _nodes[neighbor]._card._phase);
				if(phaseCondition(targetNode._card._phase, _nodes[neighbor]._card._phase))
				{
					var pair = new List<string>{targetNode._id, _nodes[neighbor]._id};
					moonPair.Add(pair);
				}
			}
		}

		return moonPair;		
	}

	//Add ConnectionLine for each connection of nodes
	private void ConnectEdgeOfEveryNodes()
	{
		HashSet<string> finishedNode = new();
		foreach(var (id, node) in _nodes)
		{
			finishedNode.Add(id);
			foreach(string neighborID in node._adjacent)
			{
				if(!finishedNode.Contains(neighborID))
					ConnectEdge(id, neighborID);
			}
		}
	}

	//helper to Add ConnectionLine between 2 nodes by specify their ID
	private void ConnectEdge(string nodeID1, string nodeID2)
	{
		ConnectEdge(_nodes[nodeID1], _nodes[nodeID2]);
	}

	//helper to Add ConnectionLine between 2 nodes
	private void ConnectEdge(BoardNode node1, BoardNode node2)
	{
		string lineName = node1._id + node2._id;
		string sorted = new string(lineName.OrderBy(c => c).ToArray());

		var packed = GD.Load<PackedScene>("res://connection_line.tscn");
		var edge = packed.Instantiate<ConnectionLine>();
		AddChild(edge);
		_edgeConnection[sorted] = edge;

		edge.ZIndex = -1;
		var id1 = sorted[0].ToString();
		var id2 = sorted[1].ToString();
		edge.ConnectEdge(_nodes[id1].GetCenterPosition(), _nodes[id2].GetCenterPosition());
		edge.SetMatchPattern(MoonCard.Pattern.None);
	}
}
