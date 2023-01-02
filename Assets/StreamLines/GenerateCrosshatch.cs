using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GenerateCrosshatch : MonoBehaviour
{
	Texture2D directionImage;
	Hatching h;
	BoundingVolumeHierarchy.BoundingVolumeHierarchy<AABBSegment> contourTree;


	Vector2 screenSize;
	// Start is called before the first frame update
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			Debug.Log("Adding streamline");
			contourTree = h.contour.contourCollisionTree;

			screenSize = new Vector2(directionImage.width, directionImage.height);
			foreach ((var node, _) in contourTree.EnumerateNodes())
			{
				if (node.Object != null)
				{
					node.Object.calcScreenPostions(screenSize);
				}
			}

			streamlines.Add(generateStreamLine(Input.mousePosition, Vector2.up));
		}
	}

	PointGrid grid;

	public void init(Hatching h, Texture2D directionImage, BoundingVolumeHierarchy.BoundingVolumeHierarchy<AABBSegment> contourTree)
	{
		this.contourTree = contourTree;

		Vector2 size = new Vector2(directionImage.width, directionImage.height);
		foreach ((var node, _) in contourTree.EnumerateNodes())
		{
			if (node.Object != null)
			{
				node.Object.calcScreenPostions(size);
			}
		}

		this.directionImage = directionImage;
		this.h = h;
		grid = new(directionImage.width, directionImage.height, h.dSep);

		streamlines = new();
	}


	(Vector2, Vector2) GetDirectionsFromImage(Vector2 p)
	{
		if (p.x >= 0 && p.x < directionImage.width && p.y >= 0 && p.y < directionImage.height)
		{
			Vector4 c = directionImage.GetPixel((int)p[0], (int)p[1]);

			return (new Vector2(c.x, c.y), new Vector2(c.z, c.w));
		}
		else
		{
			return (Vector2.zero, Vector2.zero);
		}
	}

	Vector2 GetAlignedVectorFromImage(Vector2 p, Vector2 dir, out Vector2 d1, out Vector2 d2)
	{
		(d1, d2) = GetDirectionsFromImage(p);
		float dotp_1 = dir.x * (-d1.y - d2.y) + dir.y * (d1.x + d2.x);
		float dotp_2 = dir.x * (d1.y - d2.y) + dir.y * (-d1.x + d2.x);
		Vector2 dir_out = dotp_1 * dotp_2 > 0 ? d1 : d2;

		int same_dir = Vector2.Dot(dir, dir_out) > 0 ? 1 : -1;
		return same_dir * dir_out;
	}

	bool checkNeighborClear(Vector2 p, Vector2 dir, float dist)
	{
		foreach (var sp in grid.neighborhoodEnumerator(p))
		{
			if (Vector2.Dot(dir, sp.pos - p) > 0)//punkt ist vor der Linie
			{
				if ((p - sp.pos).magnitude < dist)//punkt abstand ist kleiner dtest
				{
					if (sp.Parallel(sp.dir, dir))//Sind nicht "senkrecht"
					{
						if (!checkCriticalCurveIntersection(p, sp.pos)) //es gbt keine Linie dazwischen
						{ return false; }
					}
				}
			}
		}



		return true;

	}



	bool checkCriticalCurveIntersection(Vector2 p1, Vector2 p2)
	{
		AABBSegment tester = new(p1, p2,screenSize);

		foreach (var other in contourTree.EnumerateOverlappingLeafNodes(tester.GetBounds()))
		{
			if (tester.intersectsUsingScreenCoord(other.Object))
			{
				return true;
			}
		}
		return false;
	}


	List<List<StreamlinePoint>> streamlines;

	private void OnDrawGizmos()
	{
		(Vector2 d1, Vector2 d2) = GetDirectionsFromImage(Input.mousePosition);

		float mult = Mathf.Tan(Mathf.PI * Camera.main.fieldOfView / 360f);
		float w = Camera.main.scaledPixelWidth;
		float h = Camera.main.scaledPixelHeight;

		Matrix4x4 flatMatrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(Camera.main.aspect * mult, mult, 1)); //* Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));
		flatMatrix = flatMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) * Matrix4x4.Scale(new Vector3(2 / w, 2 / h, 1));

		Handles.matrix = flatMatrix;


		Handles.color = Color.red;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d1 * 200);
		Handles.color = Color.green;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d2 * 200);

		//var mouseLine=generateStreamLine(Input.mousePosition,Vector2.up);

		Handles.color = Color.black;
		/*foreach (StreamlinePoint p in grid.neighborhoodEnumerator(Input.mousePosition))
		{
			//Handles.DrawLine(p.pos,p.pos+p.dir,1);
			//Handles.DrawWireCube(p.pos, Vector2.one * 0.1f);
			Debug.Log("Point " + (p.pos - (Vector2)Input.mousePosition));
			Handles.DrawLine(p.pos, p.pos + Vector2.down, 0.1f);
		}*/
		Handles.color = Color.red;
		var tester = new AABBSegment(Vector2.zero, Input.mousePosition,screenSize);
		foreach (var node in contourTree.EnumerateOverlappingLeafNodes(tester.GetBounds()))
		{
			if (node.Object.intersectsUsingScreenCoord(tester))
			{
				Handles.DrawLine(node.Object.screenStart, node.Object.screenEnd, 5);
			}
		}

		Handles.color = Color.green;
		foreach ((var node, _) in contourTree.EnumerateNodes())
		{
			if (node.Object != null)
			{
				Handles.DrawLine(node.Object.screenStart, node.Object.screenEnd, 5);
			}
		}

		Handles.color = Color.black;
		foreach (var l in streamlines)
		{

			StreamlinePoint pLast = null;
			foreach (var p in l)
			{
				if (pLast == null)
				{
					pLast = p;
					continue;

				}
				Handles.DrawLine(p.pos, pLast.pos);

				pLast = p;
			}

		}
	}


	int maxits = 10000;
	List<StreamlinePoint> generateStreamLine(Vector2 start, Vector2 startDir)
	{
		var pointList = new List<StreamlinePoint>();
		float stepSize = h.dSep / 2f;
		Vector2 pos = start;
		Vector2 posLast = start;
		Vector2 dir_last = startDir;

		for (int i = 0; i < maxits; i++)
		{
			Vector2 dir = GetAlignedVectorFromImage(pos, dir_last, out Vector2 d1, out Vector2 d2);

			Vector2 step = dir.normalized * stepSize;
			pos += step;
			if (!checkCriticalCurveIntersection(posLast, pos))
			{
				if (checkNeighborClear(pos, dir, h.dSep * h.dTest))
				{
					var sp = new StreamlinePoint(pos, dir, d1, d2);
					pointList.Add(sp);
					grid.insert(sp);
				}
				else
				{
					Debug.Log("Neighbor no clear");
					break;
				}
			}
			else
			{
				Debug.Log("Over the line");
				break;
			}

			dir_last = dir;
			posLast = pos;
			if (dir == Vector2.zero)
			{
				break;
			}
		}
		return pointList;
	}


}
