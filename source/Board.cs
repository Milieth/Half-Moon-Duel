using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Board : Control
{
	private int _id = 0;
	public Dictionary<string, BoardNode> _nodes{get;set;} = new();
	private Line2D _edgeLine;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach(Node child in GetChildren())
		{
			if(child is BoardNode node)
			{
				_nodes.Add(node._id, node);	
				// node.OnBoardNodePressed += 				
			}
		}
		
		_edgeLine = GetNode<Line2D>("EdgeLine");
		DrawEdges();
	}

    // find possible lunar sequence chain from target node
	public List<List<string>> GetLunarSequenceList(string targetNodeID)
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

	//draw edge line each adjacent nodes
	private void DrawEdges()
	{
		HashSet<string> finishedNode = new();
		foreach(var (id, node) in _nodes)
		{
			finishedNode.Add(id);
			foreach(string neighborID in node._adjacent)
			{
				if(!finishedNode.Contains(neighborID))
				{
					DrawLine(id, neighborID);
				}
			}
		}
	}

	//helper to draw edge line from 2 board nodeID
	private void DrawLine(string nodeID1, string nodeID2)
	{
		DrawLine(_nodes[nodeID1], _nodes[nodeID2]);
	}

	//helper to draw edge line from 2 board node
	private void DrawLine(BoardNode node1, BoardNode node2)
	{
		var newLine = (Line2D)_edgeLine.Duplicate();

		newLine.AddPoint(node1.GetCenterPosition());
		newLine.AddPoint(node2.GetCenterPosition());

		_edgeLine.AddChild(newLine);
	}

}
