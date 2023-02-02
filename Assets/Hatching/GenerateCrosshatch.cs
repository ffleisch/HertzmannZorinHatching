using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class GenerateCrosshatch : MonoBehaviour
{
	Texture2D directionImage;
	Texture2D brightnessImage;
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
		if (Input.GetKey(KeyCode.Space))
		{
			/*contourTree = h.contour.contourCollisionTree;

            while (true)
            {
                (var p, var dir) = seedQueue.Dequeue();
                if (exploreSeed(p, dir))
                {
                    break;
                }
            }*/
			generateHatches();
		}

		if (Input.GetMouseButtonDown(0))
		{
			seedQueue.Enqueue((Input.mousePosition, Vector2.up));
		}
	}

	PointGrid grid;

	public void init(Hatching h, Texture2D directionImage, Texture2D brightnessImage)
	{
		this.contourTree = h.contour.contourCollisionTree;


		this.directionImage = directionImage;
		this.brightnessImage = brightnessImage;
		this.h = h;
		grid = new(directionImage.width, directionImage.height, h.dSep);

		streamlines = new();

		seedQueue = new();
	}
	public void generateHatches()
	{
		grid = new(directionImage.width, directionImage.height, h.dSep);

		streamlines = new();

		seedQueue = new();

		contourTree = h.contour.contourCollisionTree;
		exploreFromGridSeedpoints();
		reduceHatches();
	}

	private void exploreFromGridSeedpoints()
	{
		int nx = 50;
		int ny = 50;
		float wx = directionImage.width / nx;
		float wy = directionImage.height / ny;

		for (float i = 0; i < directionImage.width; i += wx)
		{
			for (float j = 0; j < directionImage.height; j += wy)
			{

				Vector2 p = new Vector2(i, j);
				(Vector2 d1, Vector2 d2) = GetDirectionsFromImage(p);
				if (d1 == Vector2.zero && d2 == Vector2.zero)
				{
					continue;
				}
				//Debug.Log(i+" "+j+" "+d1+" "+d2);
				//Debug.Log(p+" in qeue already"+seed_queue.Count);
				seedQueue.Enqueue((p, d1));

				seedQueue.Enqueue((p, d2));
				exploreAllFromQueue();
			}
		}
	}


	private void exploreAllFromQueue()
	{
		while (seedQueue.Count > 0)
		{
			(Vector2 pos, Vector2 dir) = seedQueue.Dequeue();
			exploreSeed(pos, dir);
		}

	}

	Queue<(Vector2, Vector2)> seedQueue;


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
		AABBSegment tester = new(p1, p2);

		foreach (var other in contourTree.EnumerateOverlappingLeafNodes(tester.GetBounds()))
		{
			if (tester.intersectsSegment(other.Object, out _, out _))
			{
				return true;
			}
		}
		return false;
	}

	Vector2 closestCriticalCurveIntersection(Vector2 start, Vector2 end)
	{
		AABBSegment tester = new(start, end);
		Vector2 result = end;
		float minRatio = 1;
		foreach (var other in contourTree.EnumerateOverlappingLeafNodes(tester.GetBounds()))
		{
			if (tester.intersectsSegment(other.Object, out Vector2 isect, out float ratio))
			{
				if (ratio < minRatio)
				{
					result = isect;
					minRatio = ratio;
				}
			}
		}

		return result;
	}



	List<LinkedList<StreamlinePoint>> streamlines;

	private void OnDrawGizmos()
	{
		(Vector2 d1, Vector2 d2) = GetDirectionsFromImage(Input.mousePosition);

		float mult = Mathf.Tan(Mathf.PI * Camera.main.fieldOfView / 360f);
		float w = Camera.main.scaledPixelWidth;
		float h = Camera.main.scaledPixelHeight;

		Matrix4x4 flatMatrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(Camera.main.aspect * mult, mult, 1)); //* Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));
		flatMatrix = flatMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) * Matrix4x4.Scale(new Vector3(2 / w, 2 / h, 1));

		Handles.matrix = flatMatrix;


		/*Handles.color = Color.red;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d1 * 200);
		Handles.color = Color.green;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)d2 * 200);
		*/
		StreamlinePoint s = new(Input.mousePosition, Vector2.left, d1, d2);
		Handles.color = Color.red;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)s.dir * 200);
		Handles.color = Color.green;
		Handles.DrawLine(Input.mousePosition, Input.mousePosition + (Vector3)s.getHatchPerpendicularVector(s.dir) * 200);


		//var mouseLine=generateStreamLine(Input.mousePosition,Vector2.up);

		Handles.color = Color.red;
		foreach (StreamlinePoint p in grid.neighborhoodEnumerator(Input.mousePosition))
		{
			//Handles.DrawLine(p.pos,p.pos+p.dir,1);
			//Handles.DrawWireCube(p.pos, Vector2.one * 0.1f);
			Handles.DrawSolidDisc(p.pos, Vector3.forward, 1.5f);
		}


		Handles.color = Color.black;
		foreach ((var node, _) in contourTree.EnumerateNodes())
		{
			if (node.Object != null)
			{
				//Handles.DrawLine(node.Object.start, node.Object.end);
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
				Handles.color =Color.HSVToRGB((p.testIndex%7)/7f,1,1);

				//if (!p.marked && !pLast.marked)
				//{

				//if(p.testIndex!=0)
					//Handles.DrawLine(p.pos, pLast.pos);

				//}

				pLast = p;
			}

		}


	}


	const int maxits = 10000;
	LinkedList<StreamlinePoint> generateStreamLine(Vector2 start, Vector2 startDir)
	{
		var pointList = new LinkedList<StreamlinePoint>();
		float stepSize = h.dTest * h.dSep;



		for (int d = 0; d < 2; d++)
		{

			Vector2 dir_last = startDir * (d == 0 ? 1 : -1);
			Vector2 pos = start;
			Vector2 posLast = start;

			for (int i = 0; i < maxits; i++)
			{
				Vector2 dir = GetAlignedVectorFromImage(pos, dir_last, out Vector2 d1, out Vector2 d2);

				Vector2 step = dir.normalized * stepSize;
				pos += step;
				if (checkNeighborClear(pos, dir, h.dSep * h.dTest))
				{
					bool doneFlag = false;
					//if we go over the line do still draw to it
					if (checkCriticalCurveIntersection(posLast, pos))
					{
						pos = closestCriticalCurveIntersection(posLast, pos);
						doneFlag = true;
					}


					var sp = new StreamlinePoint(pos, dir, d1, d2);
					if (d == 0)
					{
						pointList.AddLast(sp);
					}
					else
					{
						if (i != 0)
						{
							sp.dir *= -1;
							pointList.AddFirst(sp);
						}
					}
					grid.insert(sp);


					if (doneFlag)
					{
						break;
					}
				}
				else
				{


					break;
				}

				dir_last = dir;
				posLast = pos;
				if (dir == Vector2.zero)
				{
					break;
				}
			}
		}
		return pointList;
	}





	bool exploreSeed(Vector2 pos, Vector2 dir)
	{
		//seed has to be inside image (and on the object)
		if (GetAlignedVectorFromImage(pos, dir, out _, out _) == Vector2.zero)
		{
			return false;
		}
		//neighborhood has to be clear
		if (!checkNeighborClear(pos, dir, h.dSep * 0.999f))
		{
			return false;
		}

		var pointList = generateStreamLine(pos, dir);

		if (pointList.Count > 0)
		{
			streamlines.Add(pointList);

		}
		else
		{
			return false;
		}


		//add new seeds to the queue

		foreach (var p in pointList)
		{
			var pDir = p.dir.normalized * h.dSep;
			var pDirPerp = p.getHatchPerpendicularVector(dir).normalized * h.dSep;


			/*switch (Random.Range(0, 4))
			{
				case 0:
					seedQueue.Enqueue((p.pos + pDirPerp, pDir));
					break;

				case 1:
					seedQueue.Enqueue((p.pos + pDirPerp, pDirPerp));
					break;
				case 2:
					seedQueue.Enqueue((p.pos - pDirPerp, pDir));
					break;
				case 3:
					seedQueue.Enqueue((p.pos - pDirPerp, pDirPerp));
					break;

			}*/

			seedQueue.Enqueue((p.pos + pDirPerp, pDir));
			seedQueue.Enqueue((p.pos + pDirPerp, pDirPerp));
			seedQueue.Enqueue((p.pos - pDirPerp, pDir));
			seedQueue.Enqueue((p.pos - pDirPerp, pDirPerp));

		}
		return true;

	}

	Color getColorFromImage(Vector2 p)
	{
		if (p.x >= 0 && p.x < brightnessImage.width && p.y >= 0 && p.y < brightnessImage.height)
		{
			return brightnessImage.GetPixel((int)p[0], (int)p[1]);

		}
		else
		{
			return Color.black;
		}
	}


	public List<Vector3[]> generateLinerendererPoints()
	{
		List<Vector3[]> outp = new();
		float mult = Mathf.Tan(Mathf.PI * Camera.main.fieldOfView / 360f);
		float w = Camera.main.scaledPixelWidth;
		float h = Camera.main.scaledPixelHeight;

		Matrix4x4 viewMatrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(Camera.main.aspect * mult, mult, 1)); //* Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));
		viewMatrix = viewMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) * Matrix4x4.Scale(new Vector3(2 / w, 2 / h, 1));


		foreach (var l in streamlines)
		{
			List<Vector3> openList = new();
			foreach (var sp in l)
			{
				if (sp.marked)
				{
					if (openList.Count >= 2)//one signle point does not make a line
					{
						outp.Add(openList.ToArray());
					}
					if (openList.Count > 0)
					{
						openList = new();
					}
				}
				else
				{
					openList.Add(sp.pos);
				}

			}
			if (openList.Count >= 2)
			{
				outp.Add(openList.ToArray());
			}

		}

		foreach (var l in this.h.contour.getOutlineLines())
		{
			outp.Add(l.ToArray());
		}


		foreach (var arr in outp)
		{
			for (int i = 0; i < arr.Length; i++)
			{
				Vector4 zw = new Vector4(arr[i].x, arr[i].y, arr[i].z, 1);
				//arr[i]=viewMatrix.MultiplyPoint(arr[i]);			
				zw = viewMatrix * zw;
				arr[i] = new Vector3(zw.x, zw.y, zw.z) / zw.w;
			}


		}


		return outp;
	}

	internal enum START_OR_ENDPOINT {
		START = 1,
		END=2
	
	}

	public Mesh generateMixedLineMesh()
	{
		Mesh m = new Mesh();

		List<Vector3> vertices = new();
		List<Vector4> mixedAdjacency = new();
		List<Vector2> endPiece = new();

		List<bool> allMarkings = new();
		
		
		float w = directionImage.width;
		float h = directionImage.height;
		foreach (var l in streamlines)
		{
			if (l.Count < 2)
			{
				continue;
			}
			int i = 0;
			StreamlinePoint lastStreamlinePoint = null;
			StreamlinePoint currentStreamlinePoint = null;
			StreamlinePoint nextStreamlinePoint = null;

			void addPointToVertexList(StreamlinePoint sp, int endPieceVal)
			{
				vertices.Add(new Vector3(2 * sp.pos.x / (w) - 1, -2 * sp.pos.y / (h) + 1,sp.lineWidth * (endPieceVal != 0 ? 0 : 1)));//,
				//vertices.Add(new Vector3(sp.pos.x ,1-sp.pos.y,sp.lineWidth * (endPieceVal != 0 ? 0 : 1)));//,
				endPiece.Add(new Vector2(endPieceVal, 0));
				allMarkings.Add(sp.marked);
			}


			int numVerticesAtStart = vertices.Count;
			if (!l.First().marked)
			{
				addPointToVertexList(l.First(),(int) START_OR_ENDPOINT.START);
			}
			
			foreach (var sp in l)
			{
				nextStreamlinePoint = sp;
				if (i <=1)
				{
					i++;
					lastStreamlinePoint = currentStreamlinePoint;
					currentStreamlinePoint = nextStreamlinePoint;
					continue;
				}
				//bool isEndpiece =(i==0||i==(l.Count-1));
				bool isStartpiece = (lastStreamlinePoint.marked) && (!currentStreamlinePoint.marked);
				bool IsEndPiece = (!currentStreamlinePoint.marked) && (nextStreamlinePoint.marked);
				int endVal = isStartpiece ? (int)START_OR_ENDPOINT.START : (IsEndPiece ? (int)START_OR_ENDPOINT.END : 0);
				if (!currentStreamlinePoint.marked)
				{
					addPointToVertexList(currentStreamlinePoint, endVal);
				}

				i++;
				lastStreamlinePoint = currentStreamlinePoint;
				currentStreamlinePoint = nextStreamlinePoint;
			}
			if (!l.Last().marked)
			{
				addPointToVertexList(l.Last(), (int)START_OR_ENDPOINT.END);
			}
			if (vertices.Count - numVerticesAtStart==1) {
				vertices.RemoveAt(vertices.Count-1);
				endPiece.RemoveAt(endPiece.Count-1);
				allMarkings.RemoveAt(allMarkings.Count-1);
			}




		}

		this.h.contour.addMyMixedVertices(ref vertices,ref endPiece);

		for (int i = 0; i < vertices.Count; i++)
		{
			Vector3 nextPoint = endPiece[i].x==(int)START_OR_ENDPOINT.END ?  vertices[i] + (vertices[i] - vertices[i - 1]):vertices[i+1];
			Vector3 lastPoint = endPiece[i].x==(int)START_OR_ENDPOINT.START ? vertices[i] + (vertices[i] - vertices[i + 1]):vertices[i-1];
			Vector4 combinedVector = new Vector4(lastPoint.x, lastPoint.y, nextPoint.x, nextPoint.y);
			mixedAdjacency.Add(combinedVector);
		}
		



		

		int[] indices = new int[vertices.Count];
		for (int i = 0; i < indices.Length; i++)
		{
			indices[i] = i;
		}

		m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

		m.vertices = vertices.ToArray();
		m.tangents = mixedAdjacency.ToArray();
		m.uv2 = endPiece.ToArray();
		m.SetIndices(indices, MeshTopology.LineStrip, 0);
		return m;

	}



	int brightnessTransferFunction(Color c)
	{
		int zw = 0;
		float brightness = (c[0] + c[1] + c[2]) / 3.0f;
		zw += brightness > h.lowerLimit ? 1 : 0;
		zw += brightness > h.upperLimit ? 1 : 0;
		return zw;
	}

	float map(float v, float la, float ua, float lb, float ub)
	{
		return lb + (ub - lb) * (v - la) / (ua - la);

	}

	float brightnessToWidth(Color c)
	{

		float brightness = (c[0] + c[1] + c[2]) / 3.0f;

		if (brightness < h.lowerLimit) { return map(brightness, 0, h.lowerLimit, 1, 0.5f); }
		if (brightness < h.upperLimit) { return map(brightness, h.lowerLimit, h.upperLimit, 0.5f, 0f); }

		return 0.1f;
	}

	public void reduceHatches()
	{
		foreach (var l in streamlines)
		{
			foreach (var sp in l)
			{
				sp.inQueue = false;
				sp.marked = false;
				Color col = getColorFromImage(sp.pos);

				sp.brightnessLevel = brightnessTransferFunction(col);
				sp.lineWidth = brightnessToWidth(col);
			}
		}
		int testIndex = 0;
		foreach (var l in streamlines)
		{
			foreach (var sp in l)
			{
				if (sp.inQueue)
				{
					continue;
				}
				testIndex += 1;
				Queue<StreamlinePoint> bfs = new();
				//var closest = queryGrid(Input.mousePosition, Vector2.up, true);
				//var closestPoint = closest.OrderByDescending(x => (x.pos - (Vector2)Input.mousePosition).magnitude).First();

				StreamlinePoint init = new StreamlinePoint(sp.pos, sp.d2, sp.d1, sp.d2);
				//StreamlinePoint init = allStreamlinePoints[0][0];
				bfs.Enqueue(init);
				init.marked = true;
				init.inQueue = true;
				while (bfs.Count > 0)
				{
					StreamlinePoint current = bfs.Dequeue();
					//Debug.Log(current.pos);

					foreach (var n in grid.neighborhoodEnumerator(current.pos))
					{
						/*if (!n.Parallel(current)) {
							continue;
						}*/
						if (!n.inQueue)
						{

							if ((current.pos - n.pos).magnitude < h.dSep*2f)//TODO i removed a *2
							{
								if (!checkCriticalCurveIntersection(current.pos, n.pos))
								{
									//Debug.Log("yay");
									/*if (current.marked && current.Parallel(n))
                                    {
                                        n.marked = true;
                                    };*/

									if (n.brightnessLevel == 1)
										n.marked = current.marked == n.Parallel(current);
									if (n.brightnessLevel == 2)
										n.marked = true;

									if (n.marked&&n.brightnessLevel==1)
									{
										n.testIndex = testIndex;
										bfs.Enqueue(n);
									}
									n.inQueue = true;
								}
							}
						}


					}
				}
			}
		}
		int n_seen = 0;
		foreach (var l in streamlines)
		{
			foreach (var sp in l)
			{
				if (sp.inQueue)
				{
					n_seen += 1;
				}
			}
		}
		//Debug.Log("touched " + n_seen + " points");

	}


}
