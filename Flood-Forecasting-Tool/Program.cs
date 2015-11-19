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

using Microsoft.VisualBasic.FileIO;
using OSGeo.GDAL;
using OSGeo.OGR;
using Python.Runtime;
using RAS41;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


class Program
{
    /// <summary>
    /// Flood Forecasting Tool Program
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {



        Console.WriteLine("**************************************************");
        Console.WriteLine("Flood Forecasting Tool");
        Console.WriteLine("**************************************************");
        Console.WriteLine("A flood inundation delineation forecasting tool using \n a rating curve library approach.");
        Console.WriteLine("This tool was developed as part of a project for the");
        Console.WriteLine("National Flood Interoperability Experiment (NFIE) Summer Institue");
        Console.WriteLine("held at the Univeristy of Alabama in Tuscaloosa");
        Console.WriteLine("from June 1st to July 17th 2015");
        Console.WriteLine("Contact: caleb.buahin@aggiemail.usu.edu");
        Console.WriteLine("**************************************************");
        Console.WriteLine("");

        Console.CancelKeyPress += Console_CancelKeyPress;

        ConfigureGdal();


        bool first = true;


        //Infinite loop to process arguments
        while (true)
        {
#if !DEBUG

            //try
            //{
#endif
            //if initially starting
            if (first)
            {
                first = false;
            }
            else
            {
                Console.Write("Type Commmand>>>>");

                string argString = Console.ReadLine();

                if (argString != null)
                {
                    //Parsing out arguments
                    using (var textStream = new StringReader(argString))
                    {
                        using (var parser = new TextFieldParser(textStream))
                        {
                            parser.Delimiters = new string[] { " " };
                            parser.HasFieldsEnclosedInQuotes = true;
                            args = parser.ReadFields();
                        }
                    }
                }

            }

            String projectFile = null;
            String outputShapefile = null;
            String ratingsCurveXMLLib = null;
            String forecastXML = null;
            String comidFile = null;

            bool verbose = true;
            bool run = false;
            bool exportToGIS = false;
            bool exp = false;
            bool createRatingsCurveLib = false;
            bool help = false;
            bool runForeCast = false;
            bool exportForeCast = false;
            bool expCOMID = false;

            //Read arguments
            if (args != null)
                for (int i = 0; i < args.Length; i++ /**/)
                {
                    switch (args[i].ToLower())
                    {
                        case "-help":

                            //Run hec ras model
                            //Console.WriteLine("");
                            //Console.WriteLine("run\tRuns current plan for project");
                            //Console.WriteLine("\tInput Arguments");
                            //Console.WriteLine("\t***************");
                            //Console.WriteLine("\trequired -prj [input project file .prj]");
                            //Console.WriteLine("\toptional -verbose [print messages and progress]");
                            //Console.WriteLine("\toptional -exp [export results to shapefile]");

                            ////export shapefile
                            //Console.WriteLine("");
                            //Console.WriteLine("expGIS\tExports profiles to inundation polygons");
                            //Console.WriteLine("\tInput Arguments");
                            //Console.WriteLine("\t***************");
                            //Console.WriteLine("\trequired -prj [input project file .prj]");
                            //Console.WriteLine("\trequired -shp [output shapefile]");
                            //Console.WriteLine("");

                            //create ratings curve library file
                            Console.WriteLine("");
                            Console.WriteLine("rclib\tCreate ratings curve library");
                            Console.WriteLine("\tInput Arguments");
                            Console.WriteLine("\t***************");
                            Console.WriteLine("\trequired -prj [input project file .prj]");
                            Console.WriteLine("\trequired -libxml [output library]");
                            Console.WriteLine("");


                            //create initial COMID mapping file
                            Console.WriteLine("");
                            Console.WriteLine("expcomid\tCreate intial COMID mapping file for rating curve");
                            Console.WriteLine("\tInput Arguments");
                            Console.WriteLine("\t***************");
                            Console.WriteLine("\trequired -libxml [Ratings curve library file]");
                            Console.WriteLine("\trequired -comid [output COMID mapping file]");
                            Console.WriteLine("");


                            //export forecast file
                            Console.WriteLine("");
                            Console.WriteLine("expfcfile\tExports forecast file based on ratings curve library file");
                            Console.WriteLine("\tInput Arguments");
                            Console.WriteLine("\t***************");
                            Console.WriteLine("\trequired -libxml [Ratings curve library file]");
                            Console.WriteLine("\trequired -comid [Comma separated file mapping comid to cross section and multiplication factors to apply to them. " +
                                "Every cross section must be mapped to a COMID and must have a valid multiplication factor. " +
                                "The Format of this file is [River Reach ID], [Cross Section ID], [COMID], [Multiplication Factor. " +
                                            "Use expCOMID command to export a COMID file you can start with]");
                            Console.WriteLine("\trequired -fxml [Path to output forecast file]");
                            Console.WriteLine("");

                            //run forecast file
                            Console.WriteLine("");
                            Console.WriteLine("runfcfile\tRun forecast file");
                            Console.WriteLine("\tInput Arguments");
                            Console.WriteLine("\t***************");
                            Console.WriteLine("\trequired -fxml [forecast xml file]");
                            Console.WriteLine("");


                            help = true;

                            break;
                        case "run":
                            run = true;
                            break;
                        case "expgis":
                            exportToGIS = true;
                            break;
                        case "rclib":
                            createRatingsCurveLib = true;
                            break;
                        case "expcomid":
                            expCOMID = true;
                            break;
                        case "expfcfile":
                            exportForeCast = true;
                            break;
                        case "runfcfile":
                            runForeCast = true;
                            break;
                        case "-verbose":
                            verbose = true;
                            break;
                        case "-prj":
                            if (i + 1 < args.Length)
                            {
                                projectFile = args[i + 1];
                                i++;
                            }
                            break;
                        case "-exp":
                            exp = true;
                            break;

                        case "-libxml":
                            if (i + 1 < args.Length)
                            {
                                ratingsCurveXMLLib = args[i + 1];
                                i++;
                            }
                            break;
                        case "-comid":
                            if (i + 1 < args.Length)
                            {
                                comidFile = args[i + 1];
                                i++;
                            }
                            break;
                        case "-fxml":
                            if (i + 1 < args.Length)
                            {
                                forecastXML = args[i + 1];
                                i++;
                            }
                            break;
                        case "-shp":
                            if (i + 1 < args.Length)
                            {
                                outputShapefile = args[i + 1];
                                i++;
                            }
                            break;

                    }
                }


            //Execution
            # region Execute HECRAS Model

            if (run == true)
            {
                if (projectFile != null && File.Exists(projectFile))
                {
                    Console.WriteLine("Executing HEC-RAS project " + projectFile + "...\n");

                    HecRasModel model = new HecRasModel(new FileInfo(projectFile));

                    //If verbose
                    if (verbose)
                    {
                        model.Controller.ComputeProgressBar += controller_ComputeProgressBar;
                        model.Controller.ComputeProgressMessage += hecController_ComputeProgressMessage;
                    }

                    int numMessage = 0;
                    Array messages = null;

                    //Run the model
                    model.Controller.Compute_CurrentPlan(ref numMessage, ref messages);

                    //Print model messages
                    for (int m = 0; m < numMessage; m++)
                    {
                        Console.WriteLine("Message [" + m + "] => " + messages.GetValue(m).ToString());
                    }



                    //write profiles to export as GIS
                    model.WriteProfilesToExportAsGIS();

                    //Export to shapefile
                    if (exp)
                    {
                        model = new HecRasModel(new FileInfo(projectFile));
                        model.SaveProfilesToShapeFile(new FileInfo(outputShapefile));
                    }

                    Console.WriteLine("Finished executing HEC-RAS project " + projectFile + "\n");
                }
                else
                {
                    Console.WriteLine("Requires a valid HEC-RAS input file .prj");
                }
            }
            # endregion

            # region Export HECRAS profiles to inundation polygons

            else if (exportToGIS)
            {
                if (projectFile != null)
                {
                    Console.WriteLine("Exporting HEC-RAS Profiles to shapefiles using " + projectFile + "...\n");

                    HecRasModel model = new HecRasModel(new FileInfo(projectFile));
                    model.WriteProfilesToExportAsGIS();

                    //save to be safe
                    //model.Controller.Project_Save();

                    if (outputShapefile != null)
                    {
                        model = new HecRasModel(new FileInfo(projectFile));
                        model.SaveProfilesToShapeFile(new FileInfo(outputShapefile));

                        Console.WriteLine("Finished exporting HEC-RAS profiles to shapefiles using " + projectFile + "\n");
                    }
                    else
                    {
                        Console.WriteLine("Please specifiy output shapefile");
                    }
                }
                else
                {
                    Console.WriteLine("Please specifiy project file to export");

                }
            }

            #endregion

            #region Create ratings curve library for lookup

            else if (createRatingsCurveLib)
            {
                if (projectFile != null)
                {
                    if (ratingsCurveXMLLib != null)
                    {

                        Console.WriteLine("Exporting HEC-RAS model to ratings curve library using " + projectFile + "...\n");

                        HecRasModel model = new HecRasModel(new FileInfo(projectFile));
                        model.WriteProfilesToExportAsGIS();

                        //save to be safe
                        model.ReadRatingsCurves();

                        using (TextWriter writer = new StreamWriter(ratingsCurveXMLLib))
                        {
                            XmlSerializer sr = new XmlSerializer(typeof(HecRasModel));
                            sr.Serialize(writer, model);
                        }

                        Console.WriteLine("Finished exporting HEC-RAS model to ratings curve library using " + projectFile + "\n");
                    }
                    else
                    {
                        Console.WriteLine("Please specifiy output library file");
                    }

                }
                else
                {
                    Console.WriteLine("Please specifiy project file to export");
                }
            }

            # endregion

            #region Export Initialization COMID file

            else if (expCOMID)
            {
                if (ratingsCurveXMLLib != null || !File.Exists(ratingsCurveXMLLib))
                {
                    if (comidFile != null)
                    {
                        Console.WriteLine("Exporting COMID mapping file using " + ratingsCurveXMLLib + "...\n");

                        using (TextReader reader = new StreamReader(ratingsCurveXMLLib))
                        {
                            XmlSerializer sr = new XmlSerializer(typeof(HecRasModel));
                            HecRasModel model = (HecRasModel)sr.Deserialize(reader);
                            model.OpenHECRASProjectFile();
                            model.ReadProfiles();
                            model.ReadSteadyStateFlowData();

                            List<River> rivers = model.Rivers.Values.ToList();

                            Console.WriteLine("\nAvailable Rivers or Tributaries");
                            Console.WriteLine("=================================");

                            for (int i = 0; i < model.Rivers.Count; i++)
                            {
                                Console.WriteLine("Index: " + i + " River Name: " + rivers[i].Name);
                            }

                            Console.WriteLine("Enter index for main river to use to derive multiplication factors");
                            string indexAsString = Console.ReadLine();
                            int riverIndex;

                            if (int.TryParse(indexAsString, out riverIndex) && riverIndex >= 0 && riverIndex < rivers.Count)
                            {
                                River river = rivers[riverIndex];
                                List<Reach> reaches = river.Reaches.Values.ToList();

                                Console.WriteLine("\nAvailable Steady State Profiles");
                                Console.WriteLine("=================================");


                                for (int i = 0; i < model.Profiles.Count; i++)
                                {
                                    Console.WriteLine("Index: " + i + " Profile Names: " + model.Profiles[i]);
                                }

                                Console.WriteLine("=================================");
                                Console.WriteLine("Enter index for profile to use");
                                indexAsString = Console.ReadLine();

                                int profileIndex;

                                if (int.TryParse(indexAsString, out profileIndex) && profileIndex >= 0 && profileIndex < model.Profiles.Count)
                                {
                                    string profileName = model.Profiles[profileIndex];
                                    Reach downstreamReach = reaches[reaches.Count - 1];
                                    List<XSection> xsections = downstreamReach.XSections.Values.ToList();

                                    double normalizationFactor = xsections[xsections.Count - 1].ProfileFlows[profileName];

                                    using (TextWriter writer = new StreamWriter(comidFile))
                                    {
                                        writer.WriteLine("River_Name,Reach_Name,XSection_Station_Name,COMID,MultiplicationFactor");

                                        for (int i = 0; i < rivers.Count; i++)
                                        {
                                            river = rivers[i];

                                            reaches = river.Reaches.Values.ToList();

                                            for (int m = 0; m < reaches.Count; m++)
                                            {
                                                Reach reach = reaches[m];

                                                xsections = reach.XSections.Values.ToList();

                                                for (int j = 0; j < xsections.Count; j++)
                                                {
                                                    XSection xsection = xsections[j];

                                                    writer.WriteLine(river.Name + "," + reach.Name + "," + xsection.StationName + ", [COMID]," + (xsection.ProfileFlows[profileName] / normalizationFactor));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        Console.WriteLine("Finished exporting COMID mapping file using " + ratingsCurveXMLLib + "\n");
                    }
                }
                else
                {
                    Console.WriteLine("\nPlease specify valid ratings curve library path\n");
                }
            }

            # endregion

            # region Export forecast file

            else if (exportForeCast)
            {
                if (ratingsCurveXMLLib != null && File.Exists(ratingsCurveXMLLib))
                {
                    if (comidFile != null && File.Exists(comidFile))
                    {
                        if (forecastXML != null)
                        {
                            Console.WriteLine("Exporting forecast file ...\n");

                            ForeCastConfiguration forecast = new ForeCastConfiguration(new FileInfo(ratingsCurveXMLLib));

                            using (TextReader reader = new StreamReader(comidFile))
                            {
                                string line = "";
                                string[] delim = new string[] { "," };

                                while ((line = reader.ReadLine()) != null)
                                {
                                    string[] cols = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                                    double outVal;

                                    if (cols.Length == 5 && double.TryParse(cols[4], out outVal))
                                    {
                                        forecast.SetCOMIDAndFlowFactor(cols[0], cols[1], cols[2], int.Parse(cols[3]), outVal);
                                    }
                                }

                                reader.Close();
                            }


                            forecast.SaveAs(new FileInfo(forecastXML));

                            Console.WriteLine("Finished exporting forecast file ...\n");

                        }
                        else
                        {
                            Console.WriteLine("\nPlease specify file to save forecast file\n");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nPlease specify valid COMID mapping file. Create a new mapping file using the expcomid command \n");
                    }
                }
                else
                {
                    Console.WriteLine("\nPlease specify valid ratings curve library path\n");
                }
            }

            #endregion

            # region Run forecast file

            else if (runForeCast)
            {
                if (forecastXML != null && File.Exists(forecastXML))
                {
                    using (TextReader reader = new StreamReader(forecastXML))
                    {
                        XmlSerializer sr = new XmlSerializer(typeof(ForeCastConfiguration));
                        ForeCastConfiguration forecast = (ForeCastConfiguration)sr.Deserialize(reader);
                        forecast.ForecastFile = forecastXML;
                        reader.Close();
                        reader.Dispose();
                        forecast.Start();
                    }
                }
                else
                {
                    Console.WriteLine("\nPlease specify valid forecast file");
                }
            }

            # endregion

            else if (args == null || (args.Length > 0 && args[0] != "-help" && !help))
            {
                Console.WriteLine("\nCommand was not recognized. Type -help for proper usage of commands\n");
            }
#if !DEBUG

            //}
            //catch (Exception ex)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;

            //    Console.WriteLine("Exceptions");
            //    Console.WriteLine("==================================");

            //    Console.WriteLine("\n" + ex.Message);

            //    Console.WriteLine("Inner Exceptions");
            //    Console.WriteLine("\t==================================");
            //    Exception tex = ex.InnerException;

            //    while (tex != null)
            //    {
            //        Console.WriteLine("\t" + tex.Message);
            //        tex = tex.InnerException;
            //    }

            //    Console.WriteLine("\tStackTrace");
            //    Console.WriteLine("\t==================================");
            //    Console.WriteLine("\t" + ex.StackTrace);

            //    Console.ResetColor();
            //}
#endif
        }


    }

    # region events

    static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {

        Console.WriteLine("Exiting...");
        Environment.Exit(0);
    }

    /// <summary>
    /// HEC-RAS Controller progress messages
    /// </summary>
    /// <param name="msg"></param>
    static void hecController_ComputeProgressMessage(string msg)
    {

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(msg);

    }

    /// <summary>
    /// HEC-RAS Controller progress
    /// </summary>
    /// <param name="Progress"></param>
    static void controller_ComputeProgressBar(float Progress)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("Progress = >" + Progress + "%");

    }

    #endregion

    # region configuration

    static void ConfigureGdal()
    {
        var executingAssemblyFile = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;
        var executingDirectory = Path.GetDirectoryName(executingAssemblyFile);

        if (string.IsNullOrEmpty(executingDirectory))
            throw new InvalidOperationException("cannot get executing directory");

        var gdalPath = Path.Combine(executingDirectory, "gdal");
        var nativePath = Path.Combine(gdalPath, GetPlatform());

        // Prepend native path to environment path, to ensure the
        // right libs are being used.
        var path = Environment.GetEnvironmentVariable("PATH");
        path = nativePath + ";" + Path.Combine(nativePath, "plugins") + ";" + path;
        Environment.SetEnvironmentVariable("PATH", path);

        // Set the additional GDAL environment variables.
        var gdalData = Path.Combine(gdalPath, "data");
        Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
        Gdal.SetConfigOption("GDAL_DATA", gdalData);

        var driverPath = Path.Combine(nativePath, "plugins");
        Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath);
        Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath);

        Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
        Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

        var projSharePath = Path.Combine(gdalPath, "share");
        Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath);
        Gdal.SetConfigOption("PROJ_LIB", projSharePath);


        Gdal.AllRegister();
        //Ogr.RegisterAll();

#if DEBUG
        //var num = Gdal.GetDriverCount();
        //for (var i = 0; i < num; i++)
        //{
        //    var driver = Gdal.GetDriver(i);
        //    Console.WriteLine(string.Format("GDAL {0}: {1}-{2}", i, driver.ShortName, driver.LongName));
        //}
#endif
    }

    private static string GetPlatform()
    {
        return IntPtr.Size == 4 ? "x86" : "x64";
    }

    # endregion

    
}

