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

using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


public class River
{

    private string name ;
    private SerializableDictionary<string, Reach> reaches;



    public River(string name = "")
    {

        this.name = name;
        reaches = new SerializableDictionary<string, Reach>("XSectionDictionaryItem", "Key", "Value");

    }

    public River()
    {
        this.name = "";
        reaches = new SerializableDictionary<string, Reach>("XSectionDictionaryItem", "Key", "Value");
    }

    [XmlAttribute()]
    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public SerializableDictionary<string, Reach> Reaches
    {
        get { return reaches; }
        set { reaches = value; }
    }
}



public class Reach
{
    private Polygon boundingPolygon;
    private List<WaterSurfacePolygon> waterSurfaces;
    private SerializableDictionary<string, XSection> xsections;
    private string name;

    public Reach(string name = "")
    {

        this.name = name;
        xsections = new SerializableDictionary<string, XSection>("XSectionDictionaryItem", "Key", "Value");
        waterSurfaces = new List<WaterSurfacePolygon>();
    }

    public Reach()
    {
        this.name = "";
        xsections = new SerializableDictionary<string, XSection>("XSectionDictionaryItem", "Key", "Value");
        waterSurfaces = new List<WaterSurfacePolygon>();
    }

    [XmlAttribute()]
    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    [XmlIgnore]
    public List<WaterSurfacePolygon> WaterSurfaces
    {
        get { return waterSurfaces; }
        set { waterSurfaces = value; }
    }

    [XmlIgnore]
    public Polygon BoundingPolygon
    {
        get { return boundingPolygon; }
        set { boundingPolygon = value; }
    }

    public SerializableDictionary<string, XSection> XSections
    {
        get { return xsections; }
        set
        {
            xsections = value;
            CreateBoundingPolygon();
        }
    }

    public void ClearProfiles()
    {
        foreach (XSection xs in xsections.Values)
        {
            xs.ClearProfiles();
        }
    }

    public void CreateBoundingPolygon()
    {
        List<Coordinate> coordinates = new List<Coordinate>();
        List<XSection> xsectionstemp = xsections.Values.ToList();

        for (int i = 0; i < xsectionstemp.Count; i++)
        {
            XSection xsec = xsectionstemp[i];
            Point lbank = xsec.XSCutLine[0];
            coordinates.Add(new Coordinate(lbank.X, lbank.Y));
        }


        for (int i = xsectionstemp.Count - 1; i >= 0; i--)
        {
            XSection xsec = xsectionstemp[i];
            Point lbank = xsec.XSCutLine[xsec.XSCutLine.Count - 1];
            coordinates.Add(new Coordinate(lbank.X, lbank.Y));
        }

        boundingPolygon = new Polygon(coordinates);
    }


    public void CreateTriangulationForWaterSurface()
    {
        waterSurfaces.Clear();

        List<XSection> xss = xsections.Values.ToList();

        for (int i = 1; i < xss.Count; i++)
        {
            List<Point> watersurfacePoints = new List<Point>();
            XSection xsection = xss[i - 1];

            for (int j = 0; j < xsection.XSCutLine.Count; j++)
            {
                Point p = xsection.XSCutLine[j];
                watersurfacePoints.Add(new Point(p.X, p.Y, p.Z));
            }


            xsection = xss[i];

            for (int j = xsection.XSCutLine.Count - 1; j >= 0; j--)
            {
                Point p = xsection.XSCutLine[j];
                watersurfacePoints.Add(new Point(p.X, p.Y, p.Z));
            }

            WaterSurfacePolygon wsurface = new WaterSurfacePolygon(watersurfacePoints);
            waterSurfaces.Add(wsurface);
        }

    }


    public void setWaterDepthsFromFlow(ref Dictionary<string, double> flowsForXSection)
    {

        List<XSection> xss = xsections.Values.ToList();

        for (int i = 1; i < xss.Count; i++)
        {

            WaterSurfacePolygon waterSurface = waterSurfaces[i - 1];

            XSection xsection = xss[i - 1];
            double uplevel = xsection.GetElevationFromFlow(flowsForXSection[xsection.StationName]);


            for (int j = 0; j < xsection.XSCutLine.Count; j++)
            {
                waterSurface.Points[j].Z = uplevel;//.Add(new Point(p.X, p.Y, uplevel));
            }

            int start = xsection.XSCutLine.Count;

            xsection = xss[i];
            double downLevel = xsection.GetElevationFromFlow(flowsForXSection[xsection.StationName]);

            for (int j = start; j < waterSurface.Points.Count; j++)
            {
                waterSurface.Points[j].Z = downLevel;//.Add(new Point(p.X, p.Y, uplevel));
            }

            waterSurface.calculateNormalsAndDs();
        }

    }
}
