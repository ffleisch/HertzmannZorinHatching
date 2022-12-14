using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Contour : MonoBehaviour
{
    struct ResultBufferStruct : ISegment
    {
        public Vector3 objectStart;
        public Vector3 objectEnd;
        public Vector3 normalStart;
        public Vector3 normalEnd;
        public Vector2 start { set; get; }
        public Vector2 end { set; get; }

        public int containsSegment;
    }

    // Start is called before the first frame update
    MeshFilter mf;
    public ComputeShader cs;
    private int kernelId;
    private ComputeBuffer triBuffer;
    private ComputeBuffer vertBuffer;
    private ComputeBuffer normalBuffer;


    private ComputeBuffer resultBuffer;

    private MeshCollider mc;


    int containsSegment;
    void Start()
    {


        //load the compute shader
        cs = Resources.Load<ComputeShader>("ContourShader");
        kernelId = cs.FindKernel("GenerateContourSegments");

        //write the mesh data to the compute buffers
        //could be potentially done fatser with Mesh.GetNativeVertexBufferPtr


        mc = GetComponent<MeshCollider>();
        if (mc == null)
        {
            mc = gameObject.AddComponent<MeshCollider>();
        }

        updateMeshData();





    }

    void updateMeshData()
    {
        mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("No MeshFilter found");
        }


        //create Buffers
        triBuffer = new(mf.mesh.triangles.Length, sizeof(int));
        vertBuffer = new(mf.mesh.vertices.Length, sizeof(float) * 3);
        normalBuffer = new(mf.mesh.normals.Length, sizeof(float) * 3);
        resultBuffer = new(mf.mesh.triangles.Length / 3, sizeof(int) + sizeof(float) * (3 + 3 + 3 + 3 + 2 + 2));


        //set buffer data
        triBuffer.SetData(mf.mesh.triangles);
        vertBuffer.SetData(mf.mesh.vertices);
        normalBuffer.SetData(mf.mesh.normals);

        //assign buffers to the compute shader
        cs.SetBuffer(kernelId, "triangles", triBuffer);
        cs.SetBuffer(kernelId, "vertices", vertBuffer);
        cs.SetBuffer(kernelId, "normals", normalBuffer);
        cs.SetBuffer(kernelId, "results", resultBuffer);


        cs.SetInt("numTriangles", triBuffer.count / 3);
    }




    private AABBContourIntersection aabbIntersections;


    void Update()
    {

        /*
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    CalcContourSegments();


                    bo = new BentleyOttmann.BentleyOttman(contours);

                }

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    bo.testStep();
                }
                if (Input.GetKey(KeyCode.RightShift))
                {
                    if (bo != null)
                    {
                        for (int i = 0; i < 300; i++)
                            bo.testStep();
                    }
               }*/

        //CalcContourSegments();
    }
    /// <summary>
    /// a point and a direction from which to check visibility
    /// stores visibiltiy check to avoifd recalculation
    /// </summary>
    internal class RaycastSeed
    {
        public Vector3 position;
        public Vector3 normal;
        private bool wasEvaluated = false;
        private bool visible;
        private Contour superInstance;
        internal RaycastSeed(Vector3 position, Vector3 normal, Contour superInstance)
        {
            this.superInstance = superInstance;
            this.position = position;
            this.normal = normal;
        }


        public bool testVisibility()
        {
            if (wasEvaluated) return visible;

            Vector3 root = superInstance.transform.TransformPoint(position + 0.01f * normal);
            RaycastHit hit;
            Vector3 camPos = Camera.main.transform.position;
            Ray r = new Ray(root, camPos - root);
            visible = !superInstance.mc.Raycast(r, out hit, Vector3.Magnitude(camPos - root) + 0.01f);
            return visible;
        }
    }


    
    /// <summary>
    /// add a point resulting from the intersection algorithm to a list and create a corresponding raycast seed
    /// for this the original segment on which the point lies is used
    /// the normals and 3d positions are interpolated accoringly
    /// </summary>
    /// <param name="list"></param>
    /// <param name="point"></param>
    /// <param name="ogSegmentIndex"></param>
    void addToList(List<(int, RaycastSeed)> list, int point, int ogSegmentIndex)
    {

        ResultBufferStruct ogSegment = rawContourSegments[ogSegmentIndex];

        Vector3 pos;
        Vector3 normal;

        Vector2 screenPoint = aabbIntersections.GraphNodes[point];
        //merge lists starting in the same point (thats not an intersection)


        if (screenPoint == ogSegment.start)
        {
            pos = ogSegment.objectStart;
            normal = ogSegment.normalStart;
        }
        else if (screenPoint == ogSegment.end)
        {
            pos = ogSegment.objectEnd;
            normal = ogSegment.normalEnd;
        }
        else
        {
            float ratio = ((screenPoint - ogSegment.start) / (ogSegment.end - ogSegment.start)).magnitude;
            pos = Vector3.Lerp(ogSegment.objectStart, ogSegment.objectEnd, ratio);
            normal = Vector3.Lerp(ogSegment.normalStart, ogSegment.normalEnd, ratio);

        }
        RaycastSeed seed = new(pos, normal,this);
        list.Add((point, seed));
    }


    public void CalcContourSegments()
    {
        uint threadGroupSizes;


        int numTriangles = triBuffer.count / 3;


        //pass the camera position in object coordinates to the shader for contour calculation
        cs.SetVector("cameraPos", transform.InverseTransformPoint(Camera.main.transform.position));
        cs.SetMatrix("objectToClipSpace", Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix * transform.localToWorldMatrix);

        cs.GetKernelThreadGroupSizes(kernelId, out threadGroupSizes, out _, out _);

        int numGroups = 1 + (int)(numTriangles / threadGroupSizes);

        cs.Dispatch(kernelId, numGroups, 1, 1);
        ResultBufferStruct[] results = new ResultBufferStruct[numTriangles];
        resultBuffer.GetData(results);


        //filter the triangles whicvh did not contain a contour segment
        //could be done faster on the gpu
        rawContourSegments = results.Where(x => x.containsSegment != 0).ToList();

        aabbIntersections = new(rawContourSegments.Cast<ISegment>());
        //aabbIntersections.intersectSegments();



        Dictionary<int, List<(int, RaycastSeed)>> openLists = new();
        HashSet<int> burned = new();
        HashSet<int> twiceBurned = new();
        outline = new();
        int num = 0;



        //the segments are sorted, due to the bentleyx ottmann
        //keep a dictionary to track which lines are growing form the left
        //if a segments statrpoint is contained, remove it and add its endpoint and add it to the associated list
        //if two lists end in the same point (i.e. a crossing), instaed start a new list
        foreach ((int s, int e, int ind) in aabbIntersections.GraphEdges)
        {
            if (openLists.ContainsKey(s) && (!burned.Contains(s)))
            {
                var l = openLists[s];
                addToList(l, e, ind);
                //l.Add(e);
                openLists.Remove(s);
                if (openLists.ContainsKey(e))
                {
                    openLists.Remove(e);
                    burned.Add(e);
                }
                else
                {
                    openLists[e] = l;
                }
            }
            else
            {
                List<(int, RaycastSeed)> l = new();
                outline.Add(l);
                addToList(l, s, ind);
                addToList(l, e, ind);
                openLists[e] = l;
            }
            if (burned.Contains(s))
            {
                twiceBurned.Add(s);
            }
            num++;
        }

        //merge lists starting in the same point (thats not an intersection)
        Dictionary<int, List<(int, RaycastSeed)>> lineStarts = new();
        List<List<int>> duplicates = new();
        for (int i = outline.Count - 1; i >= 0; i--)
        {
            var l = outline[i];
            var firstPointIndex = l.First().Item1;

            if (!burned.Contains(firstPointIndex))
            {
                if (lineStarts.ContainsKey(firstPointIndex))
                {
                    var oldLine = lineStarts[firstPointIndex];
                    lineStarts.Remove(firstPointIndex);
                    oldLine.Reverse();
                    oldLine.RemoveAt(oldLine.Count - 1);
                    oldLine.AddRange(l);
                    outline.RemoveAt(i);
                }
                else
                {
                    lineStarts[firstPointIndex] = l;
                }
            }
        }
        Dictionary<int, List<(int, RaycastSeed)>> lineEnds = new();

        //merge lists ending in the same point (thats not an intersection)
        for (int i = outline.Count - 1; i >= 0; i--)
        {
            var l = outline[i];
            var lastPointindex = l.Last().Item1;
            if (!twiceBurned.Contains(lastPointindex))
            {
                if (lineEnds.ContainsKey(lastPointindex))
                {
                    var oldLine = lineEnds[lastPointindex];
                    lineEnds.Remove(lastPointindex);
                    l.Reverse();
                    oldLine.RemoveAt(oldLine.Count - 1);
                    oldLine.AddRange(l);
                    outline.RemoveAt(i);
                }
                else
                {
                    lineEnds[lastPointindex] = l;
                }
            }
        }

        //remove hidden lines
        for (int i = outline.Count - 1; i >= 0; i--)
        {
            var l = outline[i];
            int visibleSum = 0;
            foreach ((_, RaycastSeed s) in l)
            {
                visibleSum += s.testVisibility() ? 1 : 0;
            }

            float score = (float)visibleSum / l.Count;

            if (score < 0.5f)
            {
                outline.RemoveAt(i);
            }
        }


    }
    private List<ResultBufferStruct> rawContourSegments;

    private List<List<(int, RaycastSeed)>> outline = new();

    private void OnDrawGizmos()
    {
        /*if (contours != null)
        {
            Handles.color = Color.red;
            Handles.matrix = transform.localToWorldMatrix;
            foreach (var c in contours)
            {
                Handles.DrawLine(c.start, c.end);
            }

        }*/

        /*if (bo != null)
        {
            //TODO this better
            Handles.matrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));

            //bo.debugDraw();
            Handles.color = Color.black;
            int i = 0;
            foreach ((int s, int e, _) in bo.GraphEdges)
            {
                Handles.color = Color.HSVToRGB((i % 10) / 10f, 1, 1);

                Handles.DrawLine(bo.GraphNodes[s], bo.GraphNodes[e], 1);

                i += 1;
            }
        }*/

        if (aabbIntersections != null)
        {
            Matrix4x4 flatMatrix = Camera.main.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(1, Camera.main.pixelHeight / (float)Camera.main.pixelWidth, 1));

            int i = 0;
            foreach (var l in outline)
            {
                bool firstLoop = true;
                int lastIndex = 0;
                foreach ((int ind, RaycastSeed s) in l)
                {
                    Handles.color = Color.HSVToRGB(((1 + i) % 10) / 10f, 1, 1);
                    /*Handles.matrix = transform.localToWorldMatrix;

                    Handles.DrawLine(s.position, s.position + s.normal * 0.1f);*/


                    //draw final line lists infront of the camera
                    if (firstLoop) { firstLoop = false; lastIndex = ind; continue; }
                    Handles.matrix = flatMatrix;
                    Handles.DrawLine(aabbIntersections.GraphNodes[lastIndex], aabbIntersections.GraphNodes[ind], 2);
                    lastIndex = ind;



                }
                i += 1;
            }

            /*int num = 0;
            Handles.color = Color.black;
            foreach (var p in bo.GraphNodes) {
                Handles.DrawWireCube(p,Vector3.one *0.01f*((num%10)/10f));
                num++;
            }*/
            
            
            /*Random.InitState(0);
            foreach (var l in connected)
            {
                int start = l[0].Item1;
                int end = l.Last().Item1;
                Handles.color = Color.green;
                Handles.DrawWireCube(bo.GraphNodes[start], Vector3.one * 0.01f + Random.insideUnitSphere * 0.0025f);
                Handles.color = Color.red;
                Handles.DrawWireCube(bo.GraphNodes[end], Vector3.one * 0.02f + Random.insideUnitSphere * 0.005f);


            }*/

        }
    }




    private void OnDestroy()
    {
        vertBuffer.Release();
        triBuffer.Release();
        normalBuffer.Release();
        resultBuffer.Release();
    }
}
