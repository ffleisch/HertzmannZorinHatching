using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoundingVolumeHierarchy;
using static Math3d;

public class SegmentAABB:IBVHClientObject{
    public Vector2 start;
    public Vector2 end;

    Bounds myBounds;

    public Vector3 Position { get; }

    public Vector3 PreviousPosition { get; }
    private Vector2 Abs(Vector2 v2){
        return new Vector2(Mathf.Abs(v2.x), Mathf.Abs(v2.y));
    }

    public SegmentAABB(Vector2 start,Vector2 end) {
        this.start = start;
        this.end = end;
        Position = (start+end)/2;
        PreviousPosition = Position;
        myBounds = new Bounds(Position,Abs(end-start));
    }


    public bool intersects(SegmentAABB other) {
        return AreLineSegmentsCrossing(start,end,other.start,other.end);
    }


    public Bounds GetBounds()
    {
        return myBounds;
    }
}