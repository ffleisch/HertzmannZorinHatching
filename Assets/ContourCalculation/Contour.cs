using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GenerateCrosshatch;

interface IIntersectionProvider
{
    public List<Vector2> GraphNodes { get; }
    public List<(int, int, int)> GraphEdges { get; }//start index, end index, index of the originating segment

    public void sortEdgesbyLeftEndpoint();
}

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

        public int originalTriangleIndex;
        public Vector3 interpolatingRatios;
    }

    // Start is called before the first frame update
    MeshFilter mf;
    public ComputeShader cs;

    public BoundingVolumeHierarchy.BoundingVolumeHierarchy<AABBSegment> contourCollisionTree;

    private int kernelId;
    private ComputeBuffer triBuffer;
    private ComputeBuffer vertBuffer;
    private ComputeBuffer normalBuffer;


    private ComputeBuffer resultBuffer;

    private MeshCollider mc;


    int containsSegment;
    void Start()
    {


    }

    private Camera myCamera;
    Hatching h;
    public void init(Hatching h, Camera c)
    {
        myCamera = c;
        this.h = h;
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
        resultBuffer = new(mf.mesh.triangles.Length / 3, sizeof(int) * (2) + sizeof(float) * (5 * 3 + 2 * 2));


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




    private IIntersectionProvider intersections;


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
    /// 


    private const float rayCastStandoff = 0.05f;
    internal class RaycastSeed
    {
        public Vector3 position;
        public Vector3 normal;
        private bool wasEvaluated = false;
        private bool visible;
        private Contour superInstance;



        public int ogIndex;
        internal RaycastSeed(Vector3 position, Vector3 normal, Contour superInstance)
        {
            this.superInstance = superInstance;
            this.position = position;
            this.normal = normal;
        }


        public bool testVisibility(Camera c)
        {
            if (wasEvaluated) return visible;

            Vector3 root = superInstance.transform.TransformPoint(position + rayCastStandoff * normal);
            RaycastHit hit;
            Vector3 camPos = c.transform.position;
            Ray r = new Ray(root, camPos - root);
            visible = !superInstance.mc.Raycast(r, out hit, Vector3.Magnitude(camPos - root) + 0.01f);
            return visible;
        }
    }



    /// <summary>
    ///  create a corresponding raycast seed
    /// for this the original segment on which the point lies is used
    /// the normals and 3d positions are interpolated accoringly
    /// </summary>
    /// <param name="list"></param>
    /// <param name="point"></param>
    /// <param name="ogSegmentIndex"></param>
    RaycastSeed createRaycastSeed(int point, int ogSegmentIndex)
    {

        ResultBufferStruct ogSegment = rawContourSegments[ogSegmentIndex];

        Vector3 pos;
        Vector3 normal;

        Vector2 screenPoint = intersections.GraphNodes[point];
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

            //if (ratio < 0 || ratio > 1) {
            //Debug.Log("LOLOLOLOOL"+ratio);
            //Debug.Log(ogSegment.start+" "+ogSegment.end +" "+screenPoint);
            //Debug.Log((screenPoint - ogSegment.start).magnitude + " " + (ogSegment.end - ogSegment.start).magnitude);
            //}
            pos = Vector3.Lerp(ogSegment.objectStart, ogSegment.objectEnd, ratio);
            normal = Vector3.Lerp(ogSegment.normalStart, ogSegment.normalEnd, ratio);

        }
        RaycastSeed seed = new(pos, normal, this);
        seed.ogIndex = ogSegmentIndex;
        return seed;
    }


    public void CalcContourSegments()
    {
        uint threadGroupSizes;


        int numTriangles = triBuffer.count / 3;


        //pass the camera position in object coordinates to the shader for contour calculation
        cs.SetVector("cameraPos", transform.InverseTransformPoint(myCamera.transform.position));
        cs.SetMatrix("objectToClipSpace", myCamera.projectionMatrix * myCamera.worldToCameraMatrix * transform.localToWorldMatrix);

        cs.SetVector("wh_half", new Vector2(myCamera.pixelWidth, myCamera.pixelHeight) / 2.0f);

        cs.GetKernelThreadGroupSizes(kernelId, out threadGroupSizes, out _, out _);

        int numGroups = 1 + (int)(numTriangles / threadGroupSizes);

        cs.Dispatch(kernelId, numGroups, 1, 1);
        ResultBufferStruct[] results = new ResultBufferStruct[numTriangles];
        resultBuffer.GetData(results);


        //filter the triangles whicvh did not contain a contour segment
        //could be done faster on the gpu
        rawContourSegments = results.Where(x => x.containsSegment != 0).ToArray();


        var triangles = mf.mesh.triangles;
        var vertices = mf.mesh.vertices;
        Vector3 c = transform.InverseTransformPoint(myCamera.transform.position);
        for (int i = 0; i < rawContourSegments.Length; i++)
        {
            var s = rawContourSegments[i];
            int i1 = triangles[s.originalTriangleIndex * 3 + 0];
            int i2 = triangles[s.originalTriangleIndex * 3 + 1];
            int i3 = triangles[s.originalTriangleIndex * 3 + 2];
            float ratio1 = s.interpolatingRatios.x;
            float ratio2 = s.interpolatingRatios.y;
            int ratioCode = (int)s.interpolatingRatios.z;

            Vector3 v1 = c - vertices[i1];
            float c1 = h.k1[i1] * Mathf.Pow(Vector3.Dot(v1, h.e1[i1]), 2) + h.k2[i1] * Mathf.Pow(Vector3.Dot(v1, h.e2[i1]), 2);

            Vector3 v2 = c - vertices[i2];
            float c2 = h.k1[i2] * Mathf.Pow(Vector3.Dot(v2, h.e1[i2]), 2) + h.k2[i2] * Mathf.Pow(Vector3.Dot(v2, h.e2[i2]), 2);

            Vector3 v3 = c - vertices[i3];
            float c3 = h.k1[i3] * Mathf.Pow(Vector3.Dot(v3, h.e1[i3]), 2) + h.k2[i3] * Mathf.Pow(Vector3.Dot(v3, h.e2[i3]), 2);

            float cf1 = 0, cf2 = 0;

            if (ratioCode % 8 == 3)
            {//if (((ratioCode & 1) != 0)&&((ratioCode & 2) != 0)) {
                cf1 = Mathf.Lerp(c1, c2, ratio1);
                cf2 = Mathf.Lerp(c1, c3, ratio2);

                //rawContourSegments[i].objectStart = Vector3.Lerp(vertices[i1],vertices[i2],ratio1);
                //rawContourSegments[i].objectEnd= Vector3.Lerp(vertices[i1],vertices[i3],ratio2);
            }

            if (ratioCode % 8 == 6)
            {//if (((ratioCode & 2) != 0)&&((ratioCode & 4) != 0)) {
                cf1 = Mathf.Lerp(c1, c3, ratio1);
                cf2 = Mathf.Lerp(c2, c3, ratio2);

                //rawContourSegments[i].objectStart = Vector3.Lerp(vertices[i1],vertices[i3],ratio1);
                //rawContourSegments[i].objectEnd= Vector3.Lerp(vertices[i2],vertices[i3],ratio2);
            }

            if (ratioCode % 8 == 5)
            {//if (((ratioCode & 1) != 0)&&((ratioCode & 4) != 0)) {
                cf1 = Mathf.Lerp(c1, c2, ratio1);
                cf2 = Mathf.Lerp(c2, c3, ratio2);
                //rawContourSegments[i].objectStart = Vector3.Lerp(vertices[i1],vertices[i2],ratio1);
                //rawContourSegments[i].objectEnd= Vector3.Lerp(vertices[i2],vertices[i3],ratio2);
            }
            if (cf1 * cf2 < 0)
            {
                //Debug.Log("Cusp detected");
                ratioCode |= 8;
            }
            rawContourSegments[i].interpolatingRatios = new Vector3(ratio1, ratio2, ratioCode);
        }





        intersections = new AABBContourIntersection(rawContourSegments.Cast<ISegment>());
        //intersections = new BentleyOttmann.BentleyOttman(rawContourSegments.Cast<ISegment>());



        //TODO reconsider reusing the collisiontree
        /*if (intersections.GetType() == typeof(AABBContourIntersection))
        {
            contourCollisionTree = ((AABBContourIntersection)intersections).tree;
        }
        else {
            contourCollisionTree = new();
            //TODO untested
            foreach ((int s,int e,int o) in intersections.GraphEdges) {
                contourCollisionTree.Add(new AABBSegment(intersections.GraphNodes[s],intersections.GraphNodes[e]));
            }
        }*/



        int num = 0;


        //maybe sort the segments by theor left endpoint
        //intersections.sortEdgesbyLeftEndpoint();

        int[] connectedCount = new int[intersections.GraphNodes.Count];
        foreach ((var s, var e, var o) in intersections.GraphEdges)
        {
            connectedCount[s]++;
            connectedCount[e]++;
        }

        foreach ((var s, var e, var o) in intersections.GraphEdges){
            if ((((int)rawContourSegments[o].interpolatingRatios.z)&8)!=0) {
                if (intersections.GraphNodes[s]==rawContourSegments[o].start) {
                    connectedCount[s] = 1;
                    //Debug.Log("Snip snpip");
                }
                if (intersections.GraphNodes[e]==rawContourSegments[o].start) {
                    connectedCount[e] = 1;
                    //Debug.Log("Snip snpip 2");
                }

            }
        }
            
        


        var test = connectedCount.Where<int>(x => x != 2);

        outline = new();





        List<LinkedList<(int, RaycastSeed)>> openLists = new();

        Dictionary<int, LinkedList<(int, RaycastSeed)>> lineEnds = new();

        foreach ((int s, int e, int ind) in intersections.GraphEdges)
        {
            bool startContained = lineEnds.ContainsKey(s);
            bool endContained = lineEnds.ContainsKey(e);


            //there is no list ending or starting in s or e
            if (!startContained && !endContained)
            {

                openLists.Add(new());
                var newList = openLists.Last();

                newList.AddLast((s, createRaycastSeed(s, ind)));
                newList.AddLast((e, createRaycastSeed(e, ind)));

                if (connectedCount[s] == 2)
                {
                    lineEnds.Add(s, newList);
                }
                if (connectedCount[e] == 2)
                {
                    lineEnds.Add(e, newList);
                }
            }

            if (startContained && !endContained)
            {
                var myList = lineEnds[s];
                if (myList.First.Value.Item1 == s)
                {
                    myList.AddFirst((e, createRaycastSeed(e, ind)));
                }
                else
                {
                    myList.AddLast((e, createRaycastSeed(e, ind)));
                }
                lineEnds.Remove(s);
                if (connectedCount[e] == 2)
                {
                    lineEnds.Add(e, myList);
                }
            }
            if (!startContained && endContained)
            {
                var myList = lineEnds[e];
                if (myList.First.Value.Item1 == e)
                {
                    myList.AddFirst((s, createRaycastSeed(s, ind)));
                }
                else
                {
                    myList.AddLast((s, createRaycastSeed(s, ind)));
                }
                lineEnds.Remove(e);
                if (connectedCount[s] == 2)
                {
                    lineEnds.Add(s, myList);
                }

            }

            if (startContained && endContained)
            {
                var sList = lineEnds[s];
                var eList = lineEnds[e];
                lineEnds.Remove(s);
                lineEnds.Remove(e);
                openLists.Remove(sList);
                openLists.Remove(eList);


                IEnumerable<(int, RaycastSeed)> newListEnum;

                if (sList.First.Value.Item1 == s || sList.First.Value.Item1 == e)
                {
                    newListEnum = sList.Reverse();
                }
                else
                {
                    newListEnum = sList;
                }

                if (eList.First.Value.Item1 == s || eList.First.Value.Item1 == e)
                {
                    newListEnum = newListEnum.Concat(eList);
                }
                else
                {
                    newListEnum = newListEnum.Concat(eList.Reverse());
                }
                LinkedList<(int, RaycastSeed)> newList = new(newListEnum);


                var newListFirstPointIndex = newList.First.Value.Item1;
                if (connectedCount[newListFirstPointIndex] == 2)
                {
                    if (!lineEnds.ContainsKey(newListFirstPointIndex))
                    {
                        lineEnds.Add(newListFirstPointIndex, newList);
                    }
                    else
                    {
                        lineEnds[newListFirstPointIndex] = newList;
                    }
                }
                var newListLastPointIndex = newList.Last.Value.Item1;
                if (connectedCount[newListLastPointIndex] == 2)
                {
                    if (!lineEnds.ContainsKey(newListLastPointIndex))
                    {
                        lineEnds.Add(newListLastPointIndex, newList);
                    }
                    else
                    {
                        lineEnds[newListLastPointIndex] = newList;
                    }
                }



                openLists.Add(newList);

            }



        }

        foreach (var l in openLists)
        {
            outline.Add(l.ToList());
        }


        #region old line creation
        /*
        //keep a dictionary to track which lines are growing form the left
        //if a segments statrpoint is contained, remove it and add its endpoint and add it to the associated list
        //if two lists end in the same point (i.e. a crossing), instaed start a new list
        Dictionary<int, List<(int, RaycastSeed)>> openLists = new();
        HashSet<int> burned = new();
        HashSet<int> twiceBurned = new();
        foreach ((int s, int e, int ind) in intersections.GraphEdges)
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
            //Debug.Log(myCamera.ScreenToViewportPoint(new Vector2(myCamera.pixelHeight,myCamera.pixelWidth)));
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
        }*/
        #endregion


        //remove hidden lines

        visRatios = new();
        for (int i = outline.Count - 1; i >= 0; i--)
        {
            var l = outline[i];
            int visibleSum = 0;
            foreach ((_, RaycastSeed s) in l)
            {
                visibleSum += s.testVisibility(myCamera) ? 1 : 0;
            }

            float score = (float)visibleSum / l.Count;
            if (score < 0.5f)
            {
                outline.RemoveAt(i); //TODO uncomment
            }
            else
            {

                visRatios.Add(score);
            }
        }

        contourCollisionTree = new();

        foreach (var line in outline)
        {
            int last_p = -1;
            foreach ((int p, _) in line)
            {
                if (last_p == -1)
                {
                    last_p = p;
                    continue;
                }
                contourCollisionTree.Add(new AABBSegment(intersections.GraphNodes[last_p], intersections.GraphNodes[p]));
                last_p = p;
            }

        }

    }


    List<float> visRatios;
    private ResultBufferStruct[] rawContourSegments;

    private List<List<(int, RaycastSeed)>> outline = new();

    public IEnumerable<IEnumerable<Vector3>> getOutlineLines()
    {
        foreach (var l in outline)
        {

            yield return l.Select<(int, RaycastSeed), UnityEngine.Vector3>(item => (intersections.GraphNodes[item.Item1]));
        }


    }

    //append the vertices to the given array, such that they can be rendered by the custom line renderer
    public void addMyMixedVertices(ref List<Vector3> vertices, ref List<Vector2> endPoints)
    {
        float w = myCamera.pixelWidth;
        float h = myCamera.pixelHeight;


        foreach (var l in outline)
        {
            var testList = new List<Vector2>();
            int lastIndex = -1;
            int i = 0;
            int oldVertexLastIndex = vertices.Count;
            foreach (var p in l)
            {
                if (p.Item1 == lastIndex)
                {
                    continue;
                }

                endPoints.Add(Vector2.zero);
                testList.Add(Vector2.zero);


                Vector3 point = intersections.GraphNodes[p.Item1];
                vertices.Add(new Vector3(2f * (point.x / w) - 1, 1 - 2f * (point.y / h), 1));
                lastIndex = p.Item1;
                i++;
            }//todo teilweise sehr hacky

            int verticesAdded = vertices.Count - oldVertexLastIndex;

            if (verticesAdded < 2)
            {
                while (vertices.Count > oldVertexLastIndex)
                {
                    vertices.RemoveAt(vertices.Count - 1);
                    endPoints.RemoveAt(endPoints.Count - 1);

                }
                continue;
            }

            Vector2 zw = endPoints[oldVertexLastIndex];
            zw.x = (int)START_OR_ENDPOINT.START;
            endPoints[oldVertexLastIndex] = zw;

            zw = endPoints[vertices.Count - 1];
            zw.x = (int)START_OR_ENDPOINT.END;
            endPoints[vertices.Count - 1] = zw;


        }

    }

    [Range(0, 100)]
    public int outlineHighlighted;

    //Debug.Log(myCamera.ScreenToViewportPoint(new Vector2(myCamera.pixelHeight,myCamera.pixelWidth)));
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
            Handles.matrix = myCamera.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(1, myCamera.pixelHeight / (float)myCamera.pixelWidth, 1));

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


        if (intersections != null)
        {


            float w = myCamera.pixelWidth;
            float h = myCamera.pixelHeight;
            float mult = Mathf.Tan(Mathf.PI * myCamera.fieldOfView / 360f);
            Matrix4x4 flatMatrix = myCamera.cameraToWorldMatrix * Matrix4x4.Translate(-Vector3.forward) * Matrix4x4.Scale(new Vector3(myCamera.aspect * mult, mult, 1)); //* Matrix4x4.Scale(new Vector3(1, myCamera.pixelHeight / (float)myCamera.pixelWidth, 1));
                                                                                                                                                                         //flatMatrix =flatMatrix* Matrix4x4.Scale(new Vector3(myCamera.pixelWidth/2,myCamera.pixelHeight/2,1))*Matrix4x4.Translate(new Vector3(1,1,0));
                                                                                                                                                                         //flatMatrix = flatMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) *Matrix4x4.Scale(new Vector3(2 / (float)Camera.main.pixelWidth, 2 / (float)Camera.main.pixelHeight, 1));
            flatMatrix = flatMatrix * Matrix4x4.Translate(new Vector3(-1, -1, 0)) * Matrix4x4.Scale(new Vector3(2 / w, 2 / h, 1));

            Handles.matrix = flatMatrix;

            int i = 0;
            foreach (var l in outline)
            {
                bool firstLoop = true;
                int lastIndex = 0;
                foreach ((int ind, RaycastSeed s) in l)
                {
                    Handles.color = Color.HSVToRGB(((1 + i) % 10) / 10f, 1, 1);
                    //Handles.color = Color.HSVToRGB(visRatios[i], 1, 1);
                    //Handles.color = new Color(visRatios[i],visRatios[i],visRatios[i]);
                    /*Handles.matrix = transform.localToWorldMatrix;

					Handles.DrawLine(s.position, s.position + s.normal * 0.1f);*/


                    //draw final line lists infront of the camera
                    if (firstLoop)
                    {
                        firstLoop = false; lastIndex = ind; continue;
                    }
                    Handles.DrawLine(intersections.GraphNodes[lastIndex], intersections.GraphNodes[ind],2);
                    lastIndex = ind;



                }
                i += 1;
            }
            
            
            int n = 0;
            Handles.matrix = transform.localToWorldMatrix;
            var test = rawContourSegments.Select(x => x.interpolatingRatios.z);
            foreach (var s in rawContourSegments)
            {
                if ((((int)s.interpolatingRatios.z) & 8) != 0)
                {
                    Handles.color = Color.red;
                    Handles.DrawLine(s.objectStart, s.objectEnd, 2);
                }
                else
                {
                    Handles.color = Color.black;
                    Handles.DrawLine(s.objectStart, s.objectEnd, 2);

                }
                //Handles.color = Color.HSVToRGB(((1 + n) % 10) / 10f, 1, 1);
                //n++;	
            }

            
            int n_l = 0;
            foreach (var l in outline)
            {
                //n_l++;
                //if (visRatios[n_l-1] < 0.5f) {
                //	continue;
                //}
                int nVis = 0;
                if (outlineHighlighted % outline.Count != n_l)
                {
                    n_l++;
                    continue;
                }

                foreach ((var s, var rcs) in l)
                {
                    //var og = rawContourSegments[ intersections.GraphEdges[s].Item3];



                    var og = rawContourSegments[rcs.ogIndex];
                    Handles.matrix = transform.localToWorldMatrix;
                    Handles.color = Color.green;
                    Handles.DrawLine(rcs.position, og.objectStart);

                    Handles.color = Color.HSVToRGB(((1 + n_l) % 10) / 10f, 1, 1);
                    bool visible = rcs.testVisibility(myCamera);
                    Handles.color = visible ? Color.red : Color.blue;
                    nVis += visible ? 1 : 0;
                    Handles.DrawWireDisc(rcs.position, rcs.normal, rayCastStandoff * 2);
                    Handles.DrawLine(rcs.position, rcs.position + rayCastStandoff * rcs.normal);
                    Handles.DrawLine(og.objectStart, og.objectEnd, 3);

                    Handles.matrix = Matrix4x4.identity;
                    //Handles.DrawLine(transform.TransformPoint( rcs.position+rayCastStandoff*rcs.normal), myCamera.transform.position);
                    n++;
                }
                n_l++;
                //Debug.Log(l.Count + " " + ((float)nVis / l.Count));
            }



            /*int num = 0;
			Handles.color = Color.black;
			foreach (var p in intersections.GraphNodes)
			{
				Handles.DrawWireCube(p, Vector3.one * 0.01f * ((1 + num % 9) / 10f));
				num++;
			}*/



            /*
            int num = 0;
            foreach ((int s, int e,_) in intersections.GraphEdges) {
                //Handles.color = Color.HSVToRGB(((1 + num) % 5) / 5f, 1, 1);
                Handles.color = Color.red;
                if (outlineHighlighted == num%outline.Count)
                {
                    Handles.DrawLine((Vector3)intersections.GraphNodes[s],(Vector3)intersections.GraphNodes[e],30);

                }
                num++;
            }*/

            /*foreach (var l in outline)
            {
                int start = l[0].Item1;
                int end = l.Last().Item1;
                Handles.color = Color.green;
                Handles.DrawWireCube(intersections.GraphNodes[start], Vector3.one * 0.01f + Random.insideUnitSphere * 0.0025f);
                Handles.color = Color.red;
                Handles.DrawWireCube(intersections.GraphNodes[end], Vector3.one * 0.02f + Random.insideUnitSphere * 0.005f);


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
