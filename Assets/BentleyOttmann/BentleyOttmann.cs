using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;


namespace BentleyOttmann
{
    public class BentleyOttman
    {


        //PriorityQueue<SweepEvent, SweepEvent> priorityQueue;
        SortedSet<SweepEvent> priorityQueue;
        List<Segment> segments;

        Tree<Segment> sweepLine;

        public List<Vector2> GraphNodes = new();
        public List<(int, int,int)> GraphEdges = new();


        public Color[] testColours;

        public BentleyOttman(List<(Vector2, Vector2)> segmentsRaw)
        {

            segments = new();


            foreach (var s in segmentsRaw)
            {
                var ns = new Segment(s.Item1, s.Item2);
                segments.Add(ns);
            }
            init();
        }

        public BentleyOttman(IEnumerable<ISegment> segments)
        {
            this.segments = new();

            int i=0;
            foreach (var s in segments)
            {
                var ns = new Segment(s.start, s.end);
                ns.initial_index = i;
                this.segments.Add(ns);
                i++;
            }
            init();
        }

        private void init()
        {
            segments.Sort(new InitialSegmentOrdering());

            priorityQueue = new(new PriorityQueueOrdering());
            foreach (var ns in segments)
            {
                SweepEvent start = new(ns.start, ns);
                SweepEvent end = new(ns.end);
                //priorityQueue.Enqueue(start, start);
                //priorityQueue.Enqueue(end, end);
                priorityQueue.Add(start);
                priorityQueue.Add(end);
            }

            sweepLine = new();


            if (testColours == null) {
                int n = 10;
                testColours = new Color[n];
                for (int i = 0; i < n; i++) {
                    testColours[i] =Color.HSVToRGB(i/(float)n,1,1);
                }
                
            }

        }

        private void addEvents(Segment a, Segment b)
        {
            if (b == null) return;
            if (a == null) return;
            var possibleEvent = a.getPossibleSweepEvent(b);
            if (possibleEvent != null)
            {
                possibleEvent.id = -1; //this is right proper cursed
                //this way all events for intersections have the same id
                //if they are compared and have the same position and now the same id, only the first will be present in the sorted set


                //priorityQueue.Enqueue(possibleEvent, possibleEvent);
                //if (a.associatedEvent != null)
                //{
                //    Debug.Log(a.associatedEvent.point + " " + possibleEvent.point);
                //}


                /*if (a.associatedEvent != null && a.associatedEvent.point == possibleEvent.point)
                {
                    removeAssociatedEvents(a);
                }
                if (b.associatedEvent != null && b.associatedEvent.point == possibleEvent.point)
                {
                    removeAssociatedEvents(b);
                }*/

                a.associatedEvent = possibleEvent;
                b.associatedEvent = possibleEvent;
                priorityQueue.Add(possibleEvent);
                debugEvents.Add(possibleEvent);
            }

        }

        private void removeAssociatedEvents(Segment a)
        {
            if (a != null && a.associatedEvent != null)
            {
                deletedElements.Add(a.associatedEvent);
                priorityQueue.Remove(a.associatedEvent);
            }

        }


        public void intersectSegments() {
            GraphEdges = new();
            GraphNodes = new();
            while (step())
            {
            }

            
        }


        public bool step()
        {
            //var currentEvent = priorityQueue.Dequeue();
            var currentEvent = priorityQueue.First();
            priorityQueue.Remove(currentEvent);

            sweepPos = currentEvent.point;

            //Step 1
            if (!(GraphNodes.Count > 0 && GraphNodes[GraphNodes.Count - 1] == sweepPos))
            {
                GraphNodes.Add(sweepPos);
            }



            //String str = "";
            /*Segment lastTestSeg = null;
            foreach (Segment s in sweepLine)
            {
                float y;
                bool isect;
                GeometricPrimitives.GetLineIntersectionX(s, poslast, out y, out isect);
                str += y;
                if (lastTestSeg != null)
                    str += lastTestSeg.CompareTo(s);
                lastTestSeg = s;
                str += "\n";
            }
            Debug.Log(str);*/


            //Step 2
            Segment.currentSweepPosition = sweepPos;//very important
            Segment requestSegment = new(sweepPos, sweepPos);
            if (currentEvent.segment == null)
            {
                sweeplineCanidates = sweepLine.FindAllEqual(requestSegment);
            }
            else
            {
                sweeplineCanidates = new List<Segment> { currentEvent.segment };
            }

           // Debug.Log(sweeplineCanidates);

            //Step 3

            int current_index = GraphNodes.Count - 1;
            foreach (Segment s in sweeplineCanidates)
            {
                if (s.LastIntersectionIndex != -1)
                {
                    GraphEdges.Add((s.LastIntersectionIndex, current_index,s.initial_index));
                }
                s.LastIntersectionIndex = current_index;
            }

            //step 4

                  //Debug.Log(sweepPos.y + " " + sweeplineCanidates.Count);
            //Debug.Log(str);

            if (currentEvent.segment == null)
            {
                sweepLine.DeleteAllEqual(requestSegment);
            }
                        //Debug.Log(str);

            sweeplineCanidates.RemoveAll(item => item.end == sweepPos);

            //step 5
            sweeplineCanidates.Reverse();
            //sweeplineCanidates.Sort(new SegmentAngleComparer());

            var nextLarger = sweepLine.NextLarger(requestSegment);
            var nextSmaller = sweepLine.NextSmaller(requestSegment);

            nextLargerTest = nextLarger;
            nextSmallerTest = nextSmaller;

            //step 6 
            //add all segments containing poslast to the sweepline
            foreach (var s in sweeplineCanidates)
            {
                sweepLine.Add(s);
            }

            //step 7 create new Events
            debugEvents.Clear();
            deletedElements.Clear();
            if (sweeplineCanidates.Count > 0)
            {
                var firstSegment = sweeplineCanidates[0];
                var lastSegment = sweeplineCanidates[sweeplineCanidates.Count - 1];
                removeAssociatedEvents(firstSegment);
                removeAssociatedEvents(lastSegment);
                addEvents(nextSmaller, firstSegment);
                addEvents(lastSegment, nextLarger);
            }
            else
            {
                addEvents(nextSmaller, nextLarger);
            }
            return priorityQueue.Count > 0;
        }


        Vector2 sweepPos = Vector2.positiveInfinity;
        List<Segment> sweeplineCanidates;
        Segment nextLargerTest;
        Segment nextSmallerTest;
        List<SweepEvent> debugEvents = new();
        List<SweepEvent> deletedElements = new();
        public void debugDraw()

        {
            //Debug.Log("Debug");


            Handles.color = Color.blue;

            /*for(int i = 0; i < segments.Count; i++)
            {
                Color c = new Color((i%5)/4f,((int)(i/5f)%5)/4f,0);
                Handles.color = c;
                Segment s = segments[i];
                Handles.DrawWireCube(s.start,Vector3.one * 0.1f);
            }*/


            Handles.DrawWireCube(sweepPos, Vector3.one * 0.1f);


            Handles.color = Color.yellow;
            Handles.DrawDottedLine(new Vector2(Segment.currentSweepPosition.x, -10), new Vector2(Segment.currentSweepPosition.x, 10), 1);
            Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int num = 0;

            /*
			foreach (var s in segments)
			{
				Vector2 pa = s.start;
				Vector2 pb = s.end;
				Vector2 delta = pb - pa;
				delta = delta.normalized;
				Handles.color = testColours[num % testColours.Length];//Color.cyan;
				Handles.DrawLine(pa + delta * 0.1f, pb - delta * 0.1f, 1);
				Handles.color = Color.green;
				Handles.DrawWireDisc(pa+delta*0.1f,Vector3.forward,0.1f);
				Handles.color = Color.red;
				Handles.DrawWireDisc(pb-delta*0.1f,Vector3.forward,0.1f);
				num++;
			}*/

            num = 0;
            foreach (var s in sweepLine)
            {
                float y = 0;
                bool intersects;
                if (GeometricPrimitives.GetLineIntersectionX(s, mouse, out y, out intersects))
                {



                    if (!intersects)
                    {
                        Handles.color = Color.blue;
                    }
                    else
                    {
                        Handles.color = Color.cyan;

                    }
                    Handles.DrawWireCube(new Vector2(mouse.x, y), Vector3.one * 0.1f);

                }

                /*if (GeometricPrimitives.GetLineIntersectionX(s, Segment.currentSweepPosition, out y, out intersects))
				{
					if (intersects)nextLarger
					{

                removeAssociatedEvents(nextSmaller);
					Segment reqSeg = new(poslast, poslast);
					int comp = reqSeg.CompareTo(s);

					if (comp < 0)
						Handles.color = Color.red;
					if (comp == 0)
						Handles.color = Color.yellow;
					if (comp > 0)
						Handles.color = Color.green;
					Handles.DrawLine(s.start, s.end, 2);

					}
				}*/

                Handles.color = testColours[num % testColours.Length];
                Handles.DrawLine(s.start, s.end, 4);
                if (s.associatedEvent != null)
                    Handles.DrawWireDisc(s.associatedEvent.point, Vector3.forward, 0.5f + 0.1f * (num % 5));
                num++;
            }

            /*if (sweeplineCanidates != null)
            {
                num = 0;
                foreach (var s in sweeplineCanidates)
                {
                    Handles.color = num == 0 ? new Color(1, 1, 1, 0.5f) : new Color(0, 0, 0, 0.5f);
                    Handles.DrawLine((Vector3)s.start + Vector3.back * 0.01f, (Vector3)s.end + Vector3.back * 0.01f, 7);nextLarger
                    num++;
                }
            }*/
            num = 0;
            foreach ((int a, int b,_) in GraphEdges)
            {
                Vector2 pa = GraphNodes[a];
                Vector2 pb = GraphNodes[b];
                Vector2 delta = pb - pa;
                //delta = delta.normalized;
                Handles.color = testColours[num % testColours.Length];//Color.cyan;
                                                                      //Handles.DrawLine(pa + delta * 0.1f, pb - delta * 0.1f, 1);
                Handles.DrawLine(pa, pb, 1);
                //Handles.DrawDottedLine(pa + delta * 0.1f, pb - delta * 0.1f, 1);
                num += 1;

            }
            foreach (SweepEvent s in debugEvents)
            {
                Handles.color = Color.green;
                Handles.DrawWireCube(s.point, Vector3.one * 0.3f);
            }
            foreach (SweepEvent s in deletedElements)
            {
                Handles.color = Color.red;
                Handles.DrawWireCube(s.point, Vector3.one * 0.3f);
            }

            num = 0;
            foreach (SweepEvent s in priorityQueue)
            {
                Handles.color = s.segment == null ? Color.yellow : Color.magenta;
                Handles.DrawWireCube(s.point, Vector3.one * 0.2f * (1 + (num % 5) / 5f));
                num++;
            }




            if (nextLargerTest != null)
            {

                Handles.color = new Color(1, 1, 1, 0.5f);
                Handles.DrawLine(nextLargerTest.start, nextLargerTest.end, 10);
            }
            if (nextSmallerTest != null)
            {

                Handles.color = new Color(1, 1, 1, 0.5f);
                Handles.DrawLine(nextSmallerTest.start, nextSmallerTest.end, 10);
            }

        }

    }

}

