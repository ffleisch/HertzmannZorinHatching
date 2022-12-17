using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoundingVolumeHierarchy;
using System;
using System.Linq;

class AABBSegmentCompareHelper
{
	public static int angleCompare(AABBSegment a, AABBSegment b)
	{
		if (a.dx == 0 || b.dx == 0)
		{
			if (a.dx == 0 && b.dx == 0)
			{
				return a.end.y.CompareTo(b.end.y);
			}
			else
			{
				return a.dx.CompareTo(b.dx);
			}
		}
		else
		{
			//Debug.Log("yeet " + a.dy / a.dx + " " + b.dy / b.dx + " " + (a.dy / a.dx).CompareTo(b.dy / b.dx));
			return ((a.dy / a.dx).CompareTo(b.dy / b.dx));
		}
	}
	public static int twoPointsCompare(Vector2 a, Vector2 b)
	{
		int res = a.x.CompareTo(b.x);

		if (res == 0)
		{
			return a.y.CompareTo(b.y);
		}
		else
		{
			return res;
		}
	}

}




class AABBSegment : IBVHClientObject, IComparable<AABBSegment>{
	public Vector2 start;
	public Vector2 end;
	public float dx;
	public float dy;
	Bounds myBounds;
	public int id;


	public int startIndex;
	public int endIndex;
	public int originatingIndex = -1;

	public Vector3 Position { get; }

	public Vector3 PreviousPosition { get; }
	private Vector2 Abs(Vector2 v2)
	{
		return new Vector2(Mathf.Abs(v2.x), Mathf.Abs(v2.y));
	}

	static int idCounter = 0;
	public void init(Vector2 a, Vector2 b)
	{
		id = idCounter;
		idCounter++;
		if (AABBSegmentCompareHelper.twoPointsCompare(a, b) < 0)
		{
			start = a;
			end = b;
		}
		else
		{
			start = b;
			end = a;
		}
		dx = end.x - start.x;
		dy = end.y - start.y;
		myBounds = new Bounds(Position, Abs(end - start));
	}

	public AABBSegment(ISegment s)
	{

		Position = (s.start + s.end) / 2;
		PreviousPosition = Position;
		init(s.start, s.end);
	}
	public AABBSegment(Vector2 p1, Vector2 end)
	{
		Position = (p1 + end) / 2;
		PreviousPosition = Position;
		init(p1, end);

	}

	public bool intersectsSegment(AABBSegment other, out Vector2 isect, out float ratio)
	{
		//a.dx = p1_x - a.start.x; a.dy = p1_y - a.start.y;
		//b.dx = p3_x - b.start.x; b.dy = p3_y - b.start.y;


		float s, t;
		s = (-dy * (start.x - other.start.x) + dx * (start.y - other.start.y)) / (-other.dx * dy + dx * other.dy);
		t = (other.dx * (start.y - other.start.y) - other.dy * (start.x - other.start.x)) / (-other.dx * dy + dx * other.dy);

		if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
		{
			// Collision detected
			isect = new Vector2(start.x + (t * dx), start.y + (t * dy));
			ratio = t;
			return true;
		}
		isect = Vector2.zero;
		ratio = 0;
		return false; // No collision
	}


	public Bounds GetBounds()
	{
		return myBounds;
	}
	public int CompareTo(AABBSegment other)
	{
		int res = GeometricPrimitives.twoPointsCompare(start, other.start);
		if (res == 0)
		{
			return AABBSegmentCompareHelper.angleCompare(this, other);
		}
		return res;
	}

}

public class AABBContourIntersection :IIntersectionProvider

{
	public List<Vector2> GraphNodes { get; }
	public List<(int, int, int)> GraphEdges { get; }//start index, end index, index of the originating segment

	public AABBContourIntersection(IEnumerable<ISegment> input)
	{
		GraphNodes = new();
		GraphEdges = new();

		BoundingVolumeHierarchy<AABBSegment> tree = new();

		Dictionary<Vector2, int> verticesSeen = new();
		Dictionary<int, AABBSegment> segments = new();
		int GetVertexIndex(Vector2 vert)
		{
			if (verticesSeen.ContainsKey(vert))
			{
				return verticesSeen[vert];
			}
			else
			{
				int index = GraphNodes.Count;
				GraphNodes.Add(vert);
				verticesSeen.Add(vert, index);
				return index;
			}
		}
		int i = 0;
		foreach (ISegment p in input)
		{
			AABBSegment s = new(p);
			s.startIndex = GetVertexIndex(s.start);
			s.endIndex = GetVertexIndex(s.end);
			s.originatingIndex = i;

			List<(float, Vector2, AABBSegment)> intersections = new();
			foreach (var possibleNode in tree.EnumerateOverlappingLeafNodes(s.GetBounds()))
			{

				AABBSegment possibleSegment = possibleNode.Object;

				if (s.startIndex == possibleSegment.startIndex || s.endIndex == possibleSegment.endIndex || s.startIndex == possibleSegment.endIndex || s.endIndex == possibleSegment.startIndex) continue;

				Vector2 isect;
				if (possibleSegment.intersectsSegment(s, out isect, out float ratio))
				{


					//create new vertices and segments
					segments.Remove(possibleSegment.id);
					tree.Remove(possibleSegment);
					intersections.Add((ratio, isect, possibleSegment));
					//GetVertexIndex(isect, out int isectIndex);

					/*AABBSegment s1 = new(s.start, isect);
                    s1.startIndex = s.startIndex;
                    s1.endIndex = isectIndex;
                    s1.originatingIndex = s.originatingIndex;

                    AABBSegment s2 = new(isect, s.end);
                    s2.startIndex = isectIndex;
                    s2.endIndex = s.endIndex;
                    s2.originatingIndex = s.originatingIndex;

                    AABBSegment s3 = new(possibleSegment.start, isect);
                    s3.startIndex = possibleSegment.startIndex;
                    s3.endIndex = isectIndex;
                    s3.originatingIndex = possibleSegment.originatingIndex;

                    AABBSegment s4 = new(isect, possibleSegment.end);
                    s4.startIndex = isectIndex;
                    s4.endIndex = possibleSegment.endIndex;
                    s4.originatingIndex = possibleSegment.originatingIndex;*/

					/*segments.Add(s1.id, s1);
                    segments.Add(s2.id, s2);
                    segments.Add(s3.id, s3);
                    segments.Add(s4.id, s4);

                    tree.Add(s1);
                    tree.Add(s2);
                    tree.Add(s3);
                    tree.Add(s4);*/
				}
			}

			intersections.Sort((x, y) => x.Item1.CompareTo(y.Item1));

			int lastPointIndex = GetVertexIndex(s.start);
			Vector2 lastPointVert = s.start;
			foreach ((float ratio, Vector2 isect, AABBSegment other) in intersections)
			{
				int isectIndex = GetVertexIndex(isect);
				AABBSegment s1 = new(lastPointVert, isect);
				s1.startIndex = lastPointIndex;
				s1.endIndex = isectIndex;
				s1.originatingIndex = s.originatingIndex;


				AABBSegment s3 = new(other.start, isect);
				s3.startIndex = other.startIndex;
				s3.endIndex = isectIndex;
				s3.originatingIndex = other.originatingIndex;

				AABBSegment s4 = new(isect, other.end);
				s4.startIndex = isectIndex;
				s4.endIndex = other.endIndex;
				s4.originatingIndex = other.originatingIndex;
				segments.Add(s1.id, s1);
				segments.Add(s3.id, s3);
				segments.Add(s4.id, s4);

				tree.Add(s1);
				tree.Add(s3);
				tree.Add(s4);
				lastPointIndex = isectIndex;
				lastPointVert = isect;
			}




			if (intersections.Count==0)
			{
				segments.Add(s.id, s);
				tree.Add(s);
			}
			else
			{
				AABBSegment s2 = new(lastPointVert, s.end);
				s2.startIndex = lastPointIndex;
				s2.endIndex = s.endIndex;
				s2.originatingIndex = s.originatingIndex;


				segments.Add(s2.id, s2);
				tree.Add(s2);

			}
			i++;
		}

		var segmentList = segments.Values.ToList();
		segmentList.Sort();
		foreach (var s in segmentList)
		{
			GraphEdges.Add((s.startIndex, s.endIndex, s.originatingIndex));
		}

	}

}


