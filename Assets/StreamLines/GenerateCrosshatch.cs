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

    public void init(Hatching h, Texture2D directionImage)
    {
        this.contourTree = h.contour.contourCollisionTree;


        this.directionImage = directionImage;
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
                Handles.DrawLine(node.Object.start, node.Object.end);
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


    const int maxits = 10000;
    LinkedList<StreamlinePoint> generateStreamLine(Vector2 start, Vector2 startDir)
    {
        var pointList = new LinkedList<StreamlinePoint>();
        float stepSize = h.dTest * h.dSep / 2f;



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
                if (!checkCriticalCurveIntersection(posLast, pos))
                {
                    if (checkNeighborClear(pos, dir, h.dSep * h.dTest))
                    {
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
                    }
                    else
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


            switch (Random.Range(0, 4))
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

            }
            /*
            seedQueue.Enqueue((p.pos + pDirPerp, pDir));
            seedQueue.Enqueue((p.pos + pDirPerp, pDirPerp));
            seedQueue.Enqueue((p.pos - pDirPerp, pDir));
            seedQueue.Enqueue((p.pos - pDirPerp, pDirPerp));*/

        }
        return true;

    }



}
