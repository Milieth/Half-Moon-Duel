using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Board : Control
{
	private int _id = 0;
	private Dictionary<string, BoardNode> _nodes = new();
	private Line2D _edgeLine;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		foreach(Node child in GetChildren())
		{
			if(child is BoardNode node)
				_nodes.Add(node._id, node);	
		}
		
		_edgeLine = GetNode<Line2D>("EdgeLine");
		DrawEdges();
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

}
