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

using DotSpatial.Data;
using DotSpatial.Topology;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class SDFtoShape
{
    public static Dictionary<string, int> months = new Dictionary<string, int>();

    static SDFtoShape()
    {
        months.Add("JAN", 1);
        months.Add("FEB", 2);
        months.Add("MAR", 3);
        months.Add("APR", 4);
        months.Add("MAY", 5);
        months.Add("JUN", 6);
        months.Add("JUL", 7);
        months.Add("AUG", 8);
        months.Add("SEP", 9);
        months.Add("OCT", 10);
        months.Add("NOV", 11);
        months.Add("DEC", 12);
    }

    public static void exportSDFtoShapeFile(string inputsdfFile, string outputShapefile)
    {
        using (TextReader reader = new StreamReader(inputsdfFile))
        {
            string line;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim() == "BEGIN BOUNDS:")
                {
                    FeatureSet fs = new FeatureSet(FeatureType.Polygon);
                    fs.DataTable.Columns.AddRange(new DataColumn[]
                        {
                          new DataColumn("ProfileName" , typeof(string)),
                          new DataColumn("ProfileDate" , typeof(string)),
                        });

                    fs.AddFid();

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Trim() == "END BOUNDS:")
                        {
                            fs.SaveAs(outputShapefile, true);

                            break;
                        }
                        else if (line.Trim() == "PROFILE LIMITS:")
                        {

                            string profileName = "";
                            bool isDate = false;
                            DateTime dateTime = DateTime.Now;
                            IList<Coordinate> coordinates = new List<Coordinate>();


                            while ((line = reader.ReadLine()) != null)
                            {
                                string[] cols;
                                double x, y, z;

                                if (line.Trim() == "END:")
                                {
                                    Polygon p = new Polygon(coordinates);

                                    IFeature f = fs.AddFeature(p);

                                    f.DataRow.BeginEdit();

                                    f.DataRow["ProfileName"] = profileName;

                                    if (isDate)
                                    {
                                        f.DataRow["ProfileDate"] = dateTime.ToString("yyyy/MM/dd HH:mm:ss");
                                    }

                                    f.DataRow.EndEdit();

                                    break;
                                }
                                else if (line.Trim() == "POLYGON:")
                                {

                                }
                                else if ((cols = line.Trim().Split(new string[] { ":", "," }, StringSplitOptions.RemoveEmptyEntries)).Length > 1)
                                {
                                    if (cols[0].Trim() == "PROFILE ID")
                                    {
                                        profileName = cols[1].Trim();

                                        cols = profileName.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                                        int day, year;

                                        if (cols.Length == 2 && int.TryParse(cols[0].Substring(0, 2), out day) && int.TryParse(cols[0].Substring(5, 4), out year) && months.ContainsKey(cols[0].Substring(2, 3)))
                                        {
                                            isDate = true;
                                            dateTime = new DateTime(year, months[cols[0].Substring(2, 3)], day);
                                            dateTime = dateTime.AddHours(double.Parse(cols[1].Substring(0, 2)));
                                            dateTime = dateTime.AddMinutes(double.Parse(cols[1].Substring(2, 2)));

                                        }

                                    }
                                    else if (
                                             double.TryParse(cols[0], out x) &&
                                             double.TryParse(cols[1], out y) &&
                                             double.TryParse(cols[2], out z)
                                             )
                                    {
                                        coordinates.Add(new Coordinate(x, y, z) { Z = z });
                                    }
                                }
                            }
                        }
                    }

                }
            }

        }
    }
}
