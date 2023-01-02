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



public class PointGrid{

	List<Point>[,] array;
	float stride;
	int w, h;
	int nx, ny;
	public PointGrid(int w, int h, float stride) {
		this.stride = stride;
		this.w = w;
		this.h = h;
		nx =1+(int)(w/stride);
		ny =1+(int)(h/stride);
		array = new List<Point>[nx,ny];
	}


	private (int, int) coords(Vector2 pos) {
		return ((int)(pos.x/stride),(int)(pos.y/stride));
	}
	private bool inBounds(Vector2 p) {
		return (p.x>0)&&(p.x<w)&&(p.y>0)&&(p.y<h);
	}
	public void insert(Point p) {
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
	public IEnumerable<Point> neighborhoodEnumerator(Vector2 pos) {
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
