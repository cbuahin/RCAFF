/****************************************************************************
**
**  Developer: Caleb Amoa Buahin, Utah State University
**  Email: caleb.buahin@aggiemailgmail.com
** 
**  This file is part of the Flood-Forecasting-Tool.exe, a flood inundation forecasting tool was created as part of a project for the National
**  Flood Interoperability Experiment (NFIE) Summer Institute held at the National Water Center at University of Alabama Tuscaloosa from June 1st through July 17.
**  Special thanks to the following project members who made significant contributed to the approaches used in this code and its testing.
**  Nikhil Sangwan, Purdue University, Indiana
**  Cassandra Fagan, University of Texas, Austin
**  Samuel Rivera, University of Illinois at Urbana-Champaign
**  Curtis Rae, Brigham Young University, Utah
**  Marc Girons-Lopez Uppsala University, Sweden
**  Special thanks to our advisors, Dr.Jeffery Horsburgh, Dr. Jim Nelson, and Dr. Maidment who were instrumetal to the success of this project
**  Flood-Forecasting-Tool.exe and its associated files is free software; you can redistribute it and/or modify
**  it under the terms of the Lesser GNU General Public License as published by
**  the Free Software Foundation; either version 3 of the License, or
**  (at your option) any later version.
**
**  Flood-Forecasting-Tool.exe and its associated files is distributed in the hope that it will be useful,
**  but WITHOUT ANY WARRANTY; without even the implied warranty of
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
**  Lesser GNU General Public License for more details.
**
**  You should have received a copy of the Lesser GNU General Public License
**  along with this program.  If not, see <http://www.gnu.org/licenses/>
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Point also used as vector for calculations
/// </summary>
public class Point
{
    private double x, y, z;

    /// <summary>
    /// Default constructor
    /// </summary>
    public Point()
    {
        x = y = z = 0;
    }

    /// <summary>
    /// Constructor with x, y, z specified
    /// </summary>
    /// <param name="x">x parameter</param>
    /// <param name="y">y parameter</param>
    /// <param name="z">z parametr</param>
    public Point(double x = 0, double y = 0, double z = 0)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// <summary>
    /// x
    /// </summary>
    public double X
    {
        get { return x; }
        set { x = value; }
    }

    /// <summary>
    /// y
    /// </summary>
    public double Y
    {
        get { return y; }
        set { y = value; }
    }

    public double Z
    {
        get { return z; }
        set { z = value; }
    }

    public double Length()
    {
        return Math.Sqrt(x * x + y * y + z * z);
    }

    public static Point operator +(Point p1, Point p2)
    {
        return new Point(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);
    }

    public static Point operator -(Point p1, Point p2)
    {
        return new Point(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);
    }

    public static Point operator *(Point p1, double p2)
    {
        return new Point(p1.x * p2, p1.y * p2, p1.z * p2);
    }

    public static Point operator *(double p2, Point p1)
    {
        return new Point(p1.x * p2, p1.y * p2, p1.z * p2);
    }

    public static Point operator /(Point p1, double p2)
    {
        return new Point(p1.x / p2, p1.y / p2, p1.z / p2);
    }

    public static Point Normalize(ref Point p1)
    {
        double length = p1.Length();

        return new Point(p1.x / length, p1.y / length, p1.z / length);
    }

    public static Point CrossProduct(ref Point u, ref Point v)
    {
        return new Point(u.y * v.z - u.z * v.y, u.z * v.x - u.x * v.z, u.x * v.y - u.y * v.x);
    }

    public static double DotProduct(ref Point u, ref Point v)
    {
        return u.x * v.x + u.y * v.y + u.z * v.z;
    }

    public void Normalize()
    {
        double length = this.Length();
        x = x / length;
        y = y / length;
        z = z / length;
    }

    public override string ToString()
    {
        return x + " , " + y + " , " + z;
    }

    //http://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/
    // Given three colinear points p, q, r, the function checks if
    // point q lies on line segment 'pr'
    public static bool OnSegment(Point p, Point q, Point r)
    {
        if (q.x <= Math.Max(p.x, r.x) && q.x >= Math.Min(p.x, r.x) &&
                q.y <= Math.Max(p.y, r.y) && q.y >= Math.Min(p.y, r.y))
            return true;
        return false;
    }

    // To find orientation of ordered triplet (p, q, r).
    // The function returns following values
    // 0 --> p, q and r are colinear
    // 1 --> Clockwise
    // 2 --> Counterclockwise
    public static int Orientation(Point p, Point q, Point r)
    {
        int val = (int)((q.y - p.y) * (r.x - q.x) -
                  (q.x - p.x) * (r.y - q.y));

        if (val == 0) return 0;  // colinear
        return (val > 0) ? 1 : 2; // clock or counterclock wise
    }

    // The function that returns true if line segment 'p1q1'
    // and 'p2q2' intersect.
    public static bool DoIntersect(Point p1, Point q1, Point p2, Point q2)
    {
        // Find the four orientations needed for general and
        // special cases
        int o1 = Orientation(p1, q1, p2);
        int o2 = Orientation(p1, q1, q2);
        int o3 = Orientation(p2, q2, p1);
        int o4 = Orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1
        if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

        // p1, q1 and p2 are colinear and q2 lies on segment p1q1
        if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2
        if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2
        if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases
    }

    // Returns true if the point p lies inside the polygon[] with n vertices
    public static bool IsInside(ref List<Point> polygon, ref Point p)
    {
        // There must be at least 3 vertices in polygon[]
        if (polygon.Count < 3) return false;

        // Create a point for line segment from p to infinite
        Point extreme = new Point(double.PositiveInfinity, p.y, 0);

        // Count intersections of the above line with sides of polygon
        int count = 0, i = 0;
        do
        {
            int next = (i + 1) % polygon.Count;

            // Check if the line segment from 'p' to 'extreme' intersects
            // with the line segment from 'polygon[i]' to 'polygon[next]'
            if (DoIntersect(polygon[i], polygon[next], p, extreme))
            {
                // If the point 'p' is colinear with line segment 'i-next',
                // then check if it lies on segment. If it lies, return true,
                // otherwise false
                if (Orientation(polygon[i], p, polygon[next]) == 0)
                    return OnSegment(polygon[i], p, polygon[next]);

                count++;
            }
            i = next;
        } while (i != 0);

        bool isInside = (count % 2 == 1);

        // Return true if count is odd, false otherwise
        return isInside;  // Same as 
    }

}
