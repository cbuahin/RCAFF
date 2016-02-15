/****************************************************************************
**
**  Developer: Caleb Amoa Buahin, Utah State University
**  Email: caleb.buahin@aggiemailgmail.com
** 
**  This file is part of the RCAFF.exe, a flood inundation forecasting tool was created as part of a project for the National
**  Flood Interoperability Experiment (NFIE) Summer Institute held at the National Water Center at University of Alabama Tuscaloosa from June 1st through July 17.
**  Special thanks to the following project members who made significant contributed to the approaches used in this code and its testing.
**  Nikhil Sangwan, Purdue University, Indiana
**  Cassandra Fagan, University of Texas, Austin
**  Samuel Rivera, University of Illinois at Urbana-Champaign
**  Curtis Rae, Brigham Young University, Utah
**  Marc Girons-Lopez Uppsala University, Sweden
**  Special thanks to our advisors, Dr.Jeffery Horsburgh, Dr. Jim Nelson, and Dr. Maidment who were instrumetal to the success of this project
**  RCAFF.exe and its associated files are free software; you can redistribute it and/or modify
**  it under the terms of the Lesser GNU General Public License as published by
**  the Free Software Foundation; either version 3 of the License, or
**  (at your option) any later version.
**
**  RCAFF.exe and its associated files is distributed in the hope that it will be useful,
**  but WITHOUT ANY WARRANTY; without even the implied warranty of
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
**  Lesser GNU General Public License for more details.
**
**  You should have received a copy of the Lesser GNU General Public License
**  along with this program.  If not, see <http://www.gnu.org/licenses/>
**
****************************************************************************/


using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet;
using TriangleNet.Data;
using TriangleNet.Geometry;

public class WaterSurfacePolygon
{
    List<Point> triangleNormals, points;
    List<double> trianglePlaneEquationDs;
    double minX, minY, maxX, maxY, minZ, maxZ;
    Mesh mesh;
    List<TriangleNet.Data.Triangle> triangles;

    public WaterSurfacePolygon(List<Point> points)
    {
        mesh = new TriangleNet.Mesh();
        mesh.Behavior.Quality = true;

        this.points = points;

        triangleNormals = new List<Point>();
        trianglePlaneEquationDs = new List<double>();

        InputGeometry geomtery = new InputGeometry();

        for (int i = 0; i < points.Count; i++)
        {
            Point p = points[i];

            if (i == 0)
            {
                minX = maxX = p.X;
                minY = maxY = p.Y;
                minZ = maxZ = p.Z;
            }
            else
            {
                minX = Math.Min(p.X, minX);
                maxX = Math.Max(p.X, maxX);
                minY = Math.Min(p.Y, minY);
                maxY = Math.Max(p.Y, maxY);
                minZ = Math.Min(p.Z, minZ);
                maxZ = Math.Max(p.Z, maxZ);
            }

            geomtery.AddPoint(p.X, p.Y, 0, p.Z);

            //add segments
            if (i > 0)
            {
                geomtery.AddSegment(i - 1, i, 0);
            }

            if (i == points.Count - 1)
            {
                geomtery.AddSegment(i, 0, 0);
            }
        }

        mesh.Triangulate(geomtery);
        triangles = new List<TriangleNet.Data.Triangle>();

        foreach (TriangleNet.Data.Triangle tr in mesh.Triangles)
        {
            if (tr.P0 < points.Count && tr.P1 < points.Count && tr.P2 < points.Count)
            {
                triangles.Add(tr);
            }
        }

        calculateNormalsAndDs();
    }

    public void calculateNormalsAndDs()
    {
        triangleNormals.Clear();
        trianglePlaneEquationDs.Clear();

        foreach (TriangleNet.Data.Triangle t in triangles)
        {
            Point p0 = points[t.P0];
            Point p1 = points[t.P1];
            Point p2 = points[t.P2];

            Point v1 = p1 - p0;
            Point v2 = p2 - p0;

            Point normal = Point.CrossProduct(ref v1, ref v2);
            normal.Normalize();
            triangleNormals.Add(normal);
            trianglePlaneEquationDs.Add(-normal.X * points[t.P0].X - normal.Y * points[t.P0].Y - normal.Z * points[t.P0].Z);
        }

    }

    public double MinX
    {
        get { return minX; }
        set { minX = value; }
    }

    public double MaxX
    {
        get { return maxX; }
        set { maxX = value; }
    }

    public double MinY
    {
        get { return minY; }
        set { minY = value; }
    }

    public double MaxY
    {
        get { return maxY; }
        set { maxY = value; }
    }

    public double MinZ
    {
        get { return minZ; }
        set { minZ = value; }
    }

    public double MaxZ
    {
        get { return maxZ; }
        set { maxZ = value; }
    }

    public List<Point> Points
    {
        get { return points; }
        set { points = value; }
    }

    public Mesh Mesh
    {
        get { return mesh; }
        set { mesh = value; }
    }

    public List<TriangleNet.Data.Triangle> Triangles
    {
        get { return triangles; }
        set { triangles = value; }
    }

    public bool Contains(Point p)
    {

        if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY)
        {
            return false;
        }

        int i, j;
        bool c = false;

        for (i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            Point vi = points[i];
            Point vj = points[j];

            if (((vi.Y > p.Y) != (vj.Y > p.Y)) &&
             (p.X < (vj.X - vi.X) * (p.Y - vi.Y) / (vj.Y - vi.Y) + vi.X))
                c = !c;
        }

        return c;
    }

    public int FindTriangleThatContains(double x, double y)
    {
        for (int i = 0; i < triangles.Count; i++)
        {
            TriangleNet.Data.Triangle ts = triangles[i];

            if (Contains(ref ts, x, y))
            {
                return i;
            }
        }

        return -1;
    }

    public bool Contains(double x, double y)
    {

        if (x < minX || x > maxX || y < minY || y > maxY)
        {
            return false;
        }

        int i, j;
        bool c = false;

        for (i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            Point vi = points[i];
            Point vj = points[j];

            if (((vi.Y > y) != (vj.Y > y)) &&
             (x < (vj.X - vi.X) * (y - vi.Y) / (vj.Y - vi.Y) + vi.X))
                c = !c;
        }

        return c;
    }

    public double GetZ(double x, double y)
    {
        double value = -99999999999999999.00;
        int count = 0;


        foreach (var ts in triangles)
        {
            TriangleNet.Data.Triangle tr = ts;

            if (Contains(ref tr, x, y))
            {
                Point normal = triangleNormals[count];
                double d = trianglePlaneEquationDs[count];
                return (normal.X * x + normal.Y * y + d) / -normal.Z;
            }
        }

        return value;
    }

    public double GetZ(int index, double x, double y)
    {
        if (index < triangles.Count)
        {
            TriangleNet.Data.Triangle tr = triangles[index];

            if (Contains(ref tr, x, y))
            {
                Point normal = triangleNormals[index];
                double d = trianglePlaneEquationDs[index];
                double value = (normal.X * x + normal.Y * y + d) / -normal.Z;
                return value;
            }
        }

        return double.MinValue;
    }

    public List<Polygon> GetPolygons()
    {
        List<Polygon> polygons = new List<Polygon>();

        foreach (var ts in mesh.Triangles)
        {
            TriangleNet.Data.Triangle tr = ts;

            polygons.Add(GetPolygon(ref tr));
        }
        
        return polygons;
    }

    public static bool Contains(ref TriangleNet.Data.Triangle triangle, double x, double y)
    {
        Vertex v1 = triangle.GetVertex(0);
        Vertex v2 = triangle.GetVertex(1);
        Vertex v3 = triangle.GetVertex(2);
        Vertex pt = new Vertex(x, y);
        bool b1, b2, b3;

        b1 = Sign(ref pt, ref v1, ref v2) < 0.0f;
        b2 = Sign(ref pt, ref v2, ref v3) < 0.0f;
        b3 = Sign(ref pt, ref v3, ref v1) < 0.0f;

        return ((b1 == b2) && (b2 == b3));
    }

    public static List<Point> GetTriangleAsPoints(ref TriangleNet.Data.Triangle triangle)
    {
        Vertex v1 = triangle.GetVertex(0);
        Vertex v2 = triangle.GetVertex(1);
        Vertex v3 = triangle.GetVertex(2);

        List<Point> points = new List<Point>();

        points.Add(new Point(v1.X, v1.Y, v1.Attributes[0]));
        points.Add(new Point(v2.X, v2.Y, v2.Attributes[0]));
        points.Add(new Point(v3.X, v3.Y, v3.Attributes[0]));

        return points;
    }

    public static Polygon GetPolygon(ref TriangleNet.Data.Triangle triangle)
    {
        List<Coordinate> coords = new List<Coordinate>();
        Vertex v1 = triangle.GetVertex(0);
        coords.Add(new Coordinate(v1.X, v1.Y));
        v1 = triangle.GetVertex(1);
        coords.Add(new Coordinate(v1.X, v1.Y));
        v1 = triangle.GetVertex(2);
        coords.Add(new Coordinate(v1.X, v1.Y));

        return new Polygon(coords);
    }

    private static double Sign(ref Vertex p1, ref Vertex p2, ref Vertex p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

}
