using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public struct Point {
	public Vector2 pos;
	public Vector2 dir;

	public Point(Vector2 pos, Vector2 dir)
	{
		this.pos = pos;
		this.dir = dir;
	}

}

public class StreamlinePoint
{

	public Vector2 pos;
	public Vector2 dir;
	public Vector2 d1;
	public Vector2 d2;




	float[,] distort = new float[2, 2];
	static float[,] rotate_45_right = { { 0.70710678118f, 0.70710678118f }, { -0.70710678118f, 0.70710678118f } };
	static float[,] rotate_45_left = { { 0.70710678118f, -0.70710678118f }, { 0.70710678118f, 0.70710678118f } };
	public bool inQueue;
	public bool marked;
	public int brightnessLevel;


	public StreamlinePoint(Vector2 pos, Vector2 dir,Vector2 d1, Vector2 d2) : this(pos,d1,d2)
	{
		this.dir = dir;
	}
	public StreamlinePoint(Vector2 pos,Vector2 d1, Vector2 d2)
	{
		this.pos = pos;

		this.dir = d1;

		float determinant = d1.x * d2.y - d1.x * d2.x;
		distort[0, 0] = d2.y / determinant;
		distort[0, 1] = -d2.x / determinant;
		distort[1, 0] = -d1.y / determinant;
		distort[1, 1] = d1.x / determinant;

	}

	public bool checkParallelToFirstCrossVector(Vector2 dir)//unused i think
	{
		float dotp_1 = dir.x * (-d1.y - d2.y) + dir.y * (d1.x + d2.x);//Vector2.Dot(d1, dir_last) / d1.magnitude;
		float dotp_2 = dir.x * (d1.y - d2.y) + dir.y * (-d1.x + d2.x);//Vector2.Dot(d2, dir_last) / d2.magnitude;
																	  //if (Mathf.Abs(dotp_1) < Mathf.Abs(dotp_2))
		return (dotp_1 * dotp_2 > 0);
	}

	public bool Parallel(Vector2 p1, Vector2 p2)
	{
		float p_dash_1_x = distort[0, 0] * p1.x + distort[0, 1] * p1.y;
		float p_dash_1_y = distort[1, 0] * p1.x + distort[1, 1] * p1.y;
		float p_dash_2_x = distort[0, 0] * p2.x + distort[0, 1] * p2.y;
		float p_dash_2_y = distort[1, 0] * p2.x + distort[1, 1] * p2.y;

		Vector2 left = new Vector2(p_dash_1_x * rotate_45_left[0, 0] + p_dash_1_y * rotate_45_left[0, 1], p_dash_1_x * rotate_45_left[1, 0] + p_dash_1_y * rotate_45_left[1, 1]);
		Vector2 right = new Vector2(p_dash_1_x * rotate_45_right[0, 0] + p_dash_1_y * rotate_45_right[0, 1], p_dash_1_x * rotate_45_right[1, 0] + p_dash_1_y * rotate_45_right[1, 1]);
		Vector2 p_dash_2 = new Vector2(p_dash_2_x, p_dash_2_y);
		return Vector2.Dot(left, p_dash_2) * Vector2.Dot(right, p_dash_2) > 0;
	}

	public bool Parallel(StreamlinePoint other)
	{
		return Parallel(dir, other.dir);
	}

}


public class PointGrid{

	List<StreamlinePoint>[,] array;
	float stride;
	int w, h;
	int nx, ny;
	public PointGrid(int w, int h, float stride) {
		this.stride = stride;
		this.w = w;
		this.h = h;
		nx =1+(int)(w/stride);
		ny =1+(int)(h/stride);
		array = new List<StreamlinePoint>[nx,ny];
	}


	private (int, int) coords(Vector2 pos) {
		return ((int)(pos.x/stride),(int)(pos.y/stride));
	}
	private bool inBounds(Vector2 p) {
		return (p.x>0)&&(p.x<w)&&(p.y>0)&&(p.y<h);
	}
	public void insert(StreamlinePoint p) {
		if (inBounds(p.pos)) {
			(int x, int y) = coords(p.pos);
			if (array[x, y] == null) {
				array[x, y] = new();
			}
			array[x, y].Add(p);
		}
	
	}

	private bool inBoundsInt(int x ,int y) {
		return (x>0)&&(x<nx)&&(y>0)&&(y<nx);
	}
	public IEnumerable<StreamlinePoint> neighborhoodEnumerator(Vector2 pos) {
		if (!inBounds(pos)) {
			yield break;
		}

		(int x, int y) = coords(pos);
		for (int i = -1; i < 1; i++) {
			for (int j = -1; i < 1; i++)
			{
				if ((x>0)&&(x<nx)&&(y>0)&&(y<nx)) {
					if (array[x+i, y+j] != null) {
						foreach (var p in array[x+i, y+j]) {
							yield return p;
						}
					}
				}
			}
		}
		
	}
}
