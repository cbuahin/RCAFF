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
using System.Xml.Serialization;

public class XSection
{
    private List<Point> cutLine;
    private string stationName;
    private List<Point> elevationPoints;
    private SerializableDictionary<string, double> profileElevations;
    private SerializableDictionary<string, double> profileFlows;
    private List<FlowStagePair> ratingCurve;
    private double minZ;
    private bool scaleStationsAlongCutline = true;

    public XSection(string station)
    {
        stationName = station;
        cutLine = new List<Point>();
        elevationPoints = new List<Point>();
        profileElevations = new SerializableDictionary<string, double>("ProfileElevation", "Profile", "Elevation");
        profileFlows = new SerializableDictionary<string, double>("ProfileFlow", "Profile", "Flow");
        ratingCurve = new List<FlowStagePair>();
    }

    public XSection()
    {
        stationName = "";
        cutLine = new List<Point>();
        elevationPoints = new List<Point>();
        profileElevations = new SerializableDictionary<string, double>("ProfileElevation", "Profile", "Elevation");
        profileFlows = new SerializableDictionary<string, double>("ProfileFlow", "Profile", "Flow");
        ratingCurve = new List<FlowStagePair>();
    }

    public List<FlowStagePair> RatingCurve
    {
        get { return ratingCurve; }
        set
        {
            if (value != null)
            {
                ratingCurve = (from n in value
                               orderby n.Flow ascending
                               select n).ToList();
            }
        }
    }

    public List<Point> XSCutLine
    {
        get { return cutLine; }
        set
        {
            cutLine = value;
        }
    }

    [XmlIgnore]
    public List<Point> ElevationPoints
    {
        get { return elevationPoints; }
        set
        {
            elevationPoints = value;
            SetMinZ();
        }
    }

    [XmlIgnore]
    public SerializableDictionary<string, double> ProfileElevations
    {
        get { return profileElevations; }
        set { profileElevations = value; }
    }

    [XmlIgnore]
    public SerializableDictionary<string, double> ProfileFlows
    {
        get { return profileFlows; }
        set { profileFlows = value; }
    }

    public void ClearProfiles()
    {
        profileFlows.Clear();
        profileElevations.Clear();
        ratingCurve.Clear();
    }

    public void SetElevationPoints(List<double> xpathElevations)
    {
        elevationPoints.Clear();

        List<double> stations = new List<double>();
        for (int i = 0; i < xpathElevations.Count; i = i + 2)
        {
            stations.Add(xpathElevations[i]);
        }

        double maxstation = stations.Max();

        double maxCutline = 0;

        for (int i = 1; i < cutLine.Count; i++)
        {
            maxCutline += (cutLine[i] - cutLine[i - 1]).Length();
        }

        for (int i = 0; i < xpathElevations.Count; i = i + 2)
        {
            double x = xpathElevations[i] * maxCutline / maxstation;
            double z = xpathElevations[i + 1];

            double length = 0;

            for (int j = 1; j < cutLine.Count; j++)
            {
                Point vector = cutLine[j] - cutLine[j - 1];

                double curLength = vector.Length();
                double prevLength = length;
                length += curLength;

                if (x >= prevLength && x <= length)
                {
                    double factor = (curLength - (length - x)) / curLength;
                    Point p = cutLine[j - 1] + vector * factor;
                    p.Z = z;
                    elevationPoints.Add(p);
                    break;
                }
                else if (j == cutLine.Count - 1)
                {
                    Point end = cutLine[j];
                    Point p = new Point(end.X, end.Y, z);
                    elevationPoints.Add(p);
                }
            }
        }

        SetMinZ();
    }

    public void CreateRatingCurve()
    {

        if (profileFlows.Count > 0)
        {
            List<string> sortedProfiles = (from n in profileFlows
                                           orderby n.Value ascending
                                           select n.Key).ToList();

            foreach (string s in sortedProfiles)
            {
                double value = profileFlows[s];

                FlowStagePair p = (from n in ratingCurve
                                   where n.Flow == value
                                   select n).FirstOrDefault();

                if (p == null)
                    ratingCurve.Add(new FlowStagePair() { Flow = profileFlows[s], Stage = profileElevations[s] });
            }

        }
    }

    [XmlAttribute()]

    public string StationName
    {
        get { return stationName; }
        set { stationName = value; }
    }

    public Point LeftBankPoint(double elevation, out int elevationPointIndex)
    {
        if (elevation > elevationPoints[0].Z)
        {
            Point end = elevationPoints[0];
            elevationPointIndex = 0;
            return new Point(end.X, end.Y, elevation);
        }
        else
        {
            Point temp = null;

            for (int i = 0; i < elevationPoints.Count - 1; i++)
            {
                Point p1 = elevationPoints[i];
                Point p2 = elevationPoints[i + 1];
                if (ZBetweeen(elevation, ref p1, ref p2, out temp))
                {
                    elevationPointIndex = i;
                    temp.Z = elevation;
                    return temp;
                }
            }

            elevationPointIndex = -1;

            return temp;
        }
    }

    public Point RightBankPoint(double elevation, out int elevationPointIndex)
    {
        if (elevation > elevationPoints[elevationPoints.Count - 1].Z)
        {
            Point end = elevationPoints[elevationPoints.Count - 1];
            elevationPointIndex = elevationPoints.Count - 1;
            return new Point(end.X, end.Y, elevation);
        }
        else
        {
            Point temp = null;

            for (int i = elevationPoints.Count - 1; i > 0; i--)
            {
                Point p1 = elevationPoints[i - 1];
                Point p2 = elevationPoints[i];

                if (ZBetweeen(elevation, ref p1, ref p2, out temp))
                {
                    elevationPointIndex = i;
                    temp.Z = elevation;
                    return temp;
                }
            }

            elevationPointIndex = -1;
            return temp;
        }
    }

    public List<Point> GetLineAlongCutLine(double elevation)
    {
        int start = 0, end = 0;
        Point left = null, right = null;

        List<Point> points = new List<Point>();

        left = LeftBankPoint(elevation, out start);
        right = RightBankPoint(elevation, out end);

        if (left != null && right != null)
        {
            points.Add(left);

            if (start + 1 < end)
            {
                for (int i = start + 1; i < end; i++)
                {
                    Point p = elevationPoints[i];

                    points.Add(new Point(p.X, p.Y, elevation));
                }

            }
            points.Add(right);
        }

        return points;
    }

    public double GetElevationFromFlow(double flow)
    {
        double elevation = 0;

        if (flow < ratingCurve[0].Flow)
        {
            double m = -1;
            int i = 0;

            while (m < 0 && i + 1 < ratingCurve.Count)
            {
                double f2 = ratingCurve[i + 1].Flow; double h2 = ratingCurve[i + 1].Stage;
                double f1 = ratingCurve[i].Flow; double h1 = ratingCurve[i].Stage;
                m = (h2 - h1) / (f2 - f1);
                double c = h2 - m * f2;
                elevation = m * flow + c;
                i++;
            }

        }
        else if (flow > ratingCurve[ratingCurve.Count - 1].Flow)
        {
            double m = -1;
            int i = ratingCurve.Count - 1;

            while (m < 0 && i - 1 >= 0)
            {
                double f2 = ratingCurve[i].Flow; double h2 = ratingCurve[i].Stage;
                double f1 = ratingCurve[i - 1].Flow; double h1 = ratingCurve[i - 1].Stage;
                m = (h2 - h1) / (f2 - f1);
                double c = h2 - m * f2;
                elevation = m * flow + c;
                i--;
            }
        }
        else
        {
            for (int i = 1; i < ratingCurve.Count; i++)
            {
                double flow1 = ratingCurve[i - 1].Flow;
                double flow2 = ratingCurve[i].Flow;

                if (flow >= flow1 && flow <= flow2)
                {
                    double elev1 = ratingCurve[i - 1].Stage;
                    double elev2 = ratingCurve[i].Stage;
                    elevation = elev1 + ((flow - flow1) * (elev2 - elev1) / (flow2 - flow1));
                }
                else if (flow >= flow2 && flow <= flow1)
                {

                    double elev1 = ratingCurve[i - 1].Stage;
                    double elev2 = ratingCurve[i].Stage;
                    elevation = elev2 + ((flow - flow2) * (elev1 - elev2) / (flow1 - flow2));
                }
            }
        }

        if (elevation <= minZ)
            elevation = minZ + 0.5;

        return elevation;
    }

    bool ZBetweeen(double elevation, ref Point p1, ref Point p2, out Point location)
    {
        location = null;

        if (elevation >= p1.Z && elevation <= p2.Z)
        {
            double factor = (elevation - p1.Z) / (p2.Z - p1.Z);
            location = p1 + (p2 - p1) * factor;
            return true;
        }
        else if (elevation >= p2.Z && elevation <= p1.Z)
        {
            double factor = (elevation - p2.Z) / (p1.Z - p2.Z);
            location = p2 + (p1 - p2) * factor;
            return true;
        }

        return false;
    }

    public void SetMinZ()
    {
        if (elevationPoints.Count > 0)
            minZ = (from n in elevationPoints select n.Z).Min();

    }
}

public class FlowStagePair
{
    double flow, stage;

    public double Flow
    {
        get { return flow; }
        set { flow = value; }
    }

    public double Stage
    {
        get { return stage; }
        set { stage = value; }
    }
}