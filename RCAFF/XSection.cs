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
    //private List<Point> elevationPoints;
    private SerializableDictionary<string, double> profileElevations;
    private SerializableDictionary<string, double> profileFlows;
    private List<FlowStagePair> ratingCurve;
    private double minZ;
    private bool scaleStationsAlongCutline = true;

    private double currentWaterSurfaceElevation;



    public XSection(string station)
    {
        stationName = station;
        cutLine = new List<Point>();
        //elevationPoints = new List<Point>();
        profileElevations = new SerializableDictionary<string, double>("ProfileElevation", "Profile", "Elevation");
        profileFlows = new SerializableDictionary<string, double>("ProfileFlow", "Profile", "Flow");
        ratingCurve = new List<FlowStagePair>();
    }

    public XSection()
    {
        stationName = "";
        cutLine = new List<Point>();
        //elevationPoints = new List<Point>();
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

    [XmlAttribute()]
    public string StationName
    {
        get { return stationName; }
        set { stationName = value; }
    }

    [XmlIgnore()]
    public double CurrentWaterSurfaceElevation
    {
        get { return currentWaterSurfaceElevation; }
    }
    
    public void ClearProfiles()
    {
        profileFlows.Clear();
        profileElevations.Clear();
        ratingCurve.Clear();
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
    
    public void SetElevationFromFlow(double flow)
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

        currentWaterSurfaceElevation = elevation;
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