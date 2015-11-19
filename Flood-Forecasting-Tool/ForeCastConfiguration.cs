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
using System.IO;
using System.Timers;
using Python.Runtime;
using System.Xml.Serialization;
using System.IO.Compression;
using Microsoft.Research.Science.Data;
using SharpCompress.Reader;
using System.Globalization;
using DotSpatial.Data;
using OSGeo.GDAL;
using System.Threading;
using OSGeo.OGR;
using System.Data;
using DotSpatial.Topology;
using SharpCompress.Writer;
using System.Diagnostics;

public class ForeCastConfiguration
{
    # region variables

    string ratingCurveLibraryFile = "";
    string elevationRaster = "";
    string forecastFile = "";
    double refreshTimeInHours;
    string localWorkspace = "";
    string archiveWorkspace = "";
    DateTime previousForeCastDate;
    string name = "";
    string description = "";
    string iRODShost = "";
    string iRODSuserName = "";
    string iRODSpassword = "";
    int iRODSport = 1247;
    string iRODSzone = "";
    string iRODScollection = "";
    HecRasModel model;
    SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, int>>> riverReachXSectionCOMIDMapping;
    SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, double>>> riverReachXSectionFlowFactorsMapping;
    System.Timers.Timer timer;
    static PyObject iRODSClientModule, uploadToGeoserver;
    static string[] iPathDel = new string[] { "/" };
    static string[] ifDel = new string[] { "_", ".", "-" };
    static string[] itDel = new string[] { "-" };
    static string[] iEnsDel = new string[] { "-" };
    static string[] lPathDel = new string[] { "\\" };
    bool deleteNetCDFAfterForecast = true;
    List<string> filesUsedInPreviousForecast;
    IntPtr enginePtr;
    ForeCastMode forecastMode;
    DateTime minForeCastDate;
    DateTime maxForeCastDate;
    bool isRunning = false;
    DateTime previousDate;
    int xSize, ySize;
    string rasterDriver;
    double[] geoTransformation = new double[6];
    string projection;
    float noData;
    float[][] elevationData;
    DataType dataType;
    int[][] rasterWaterSurfaceMapping;
    int[][] rasterTriangleMapping;
    bool uploadResultsToGeoserver;
    bool archiveResultsAfterForecast;
    string timeSeriesCSVFile = "";
    string geoServerURI = "http://apps.nfie.org/first-responder";
    string geoServerRestServiceEndpoint = "http://nfie-team2.cloudapp.net:8181/geoserver/rest";
    string geoServerWorkSpace = "first_responder";
    string geoServerPassword = "";
    string geoServerUserName = "";
    List<EnsembleForecastFile> localNetCDFFilesToRun;
    double flowConversionFactor = 35.3147;

    #endregion

    # region enums

    public enum ForeCastMode
    {
        Latest,
        BetweenSpecifiedDates,
        FromLocalCSV,
        FromLocalNetCDF
    }

    #endregion enums

    # region constructors

    public ForeCastConfiguration(FileInfo ratingsCurveLibrary, string name = "Untitled Reach", string description = "Untitled Reach")
    {
        riverReachXSectionCOMIDMapping = new SerializableDictionary<string,SerializableDictionary<string,SerializableDictionary<string,int>>>("RiverReachXSectionCOMIDMap", "River", "ReachXSectionCOMIDMap");
        riverReachXSectionFlowFactorsMapping = new SerializableDictionary<string,SerializableDictionary<string,SerializableDictionary<string,double>>>("RiverReachXSectionFlowFactorMap", "River", "ReachXSectionFlowFactorMap");
        filesUsedInPreviousForecast = new List<string>();

        this.name = name; this.description = description;
        previousForeCastDate = DateTime.MinValue;

        this.iRODShost = "nfie.hydroshare.org";
        this.iRODSport = 1247;
        this.iRODSzone = "nfiehydroZone";
        this.iRODScollection = "/nfiehydroZone/home/public/byu/rapid_output/nfie_texas_gulf_region";
        this.refreshTimeInHours = 2.0;
        this.localWorkspace = "C:\\";


        if (File.Exists(ratingsCurveLibrary.FullName))
            this.ratingCurveLibraryFile = ratingsCurveLibrary.FullName;
        else
            throw new FileNotFoundException("Rating Curve File was not found", ratingCurveLibraryFile);

        timer = new System.Timers.Timer();
        timer.Elapsed += Timer_Elapsed;

        localNetCDFFilesToRun = new List<EnsembleForecastFile>();
        localNetCDFFilesToRun.Add(new EnsembleForecastFile() { ForecastDate = DateTime.Now, Path = "C:\\temp.nc" });

        InitializeFromRatingsCurve();

    }

    public ForeCastConfiguration()
    {
        riverReachXSectionCOMIDMapping = new SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, int>>>("RiverReachXSectionCOMIDMap", "River", "ReachXSectionCOMIDMap");
        riverReachXSectionFlowFactorsMapping = new SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, double>>>("RiverReachXSectionFlowFactorMap", "River", "ReachXSectionFlowFactorMap");
        filesUsedInPreviousForecast = new List<string>();

        this.name = "Untitled Reach"; this.description = "Untitled Reach";
        previousForeCastDate = DateTime.MinValue;

        this.iRODShost = "nfie.hydroshare.org";
        this.iRODSport = 1247;
        this.iRODSzone = "nfieHydroZone";
        this.iRODScollection = "/nfiehydroZone/home/public/byu/rapid_output/nfie_texas_gulf_region";
        this.refreshTimeInHours = 2.0;
        timer = new System.Timers.Timer();
        timer.Elapsed += Timer_Elapsed;
        this.localWorkspace = "C:\\";


    }

    #endregion

    #region properties

    public string Name
    {
        get { return name; }
        set { name = value; }
    }

    public string Discription
    {
        get { return description; }
        set { description = value; }
    }

    public ForeCastMode ForecastMode
    {
        get { return forecastMode; }
        set { forecastMode = value; }
    }

    public string RatingCurveLibraryFile
    {
        get { return ratingCurveLibraryFile; }
        set
        {
            if (File.Exists(value))
                ratingCurveLibraryFile = value;
            else
                throw new FileNotFoundException("File " + value + " not found", value);

            InitializeFromRatingsCurve();
        }
    }

    public string ElevationRaster
    {
        get { return elevationRaster; }
        set
        {
            if (File.Exists(value))
            {
                elevationRaster = value;
                ReadElevationRaster();
            }
            else
                throw new FileNotFoundException("File " + value + " not found", value);
        }
    }

    public string iRODSHost
    {
        get { return iRODShost; }
        set { iRODShost = value; }
    }

    public string iRODSZone
    {
        get { return iRODSzone; }
        set { iRODSzone = value; }
    }

    public int iRODSPort
    {
        get { return iRODSport; }
        set { iRODSport = value; }
    }

    public string iRODSUserName
    {
        get { return iRODSuserName; }
        set { iRODSuserName = value; }
    }

    public string iRODSPassword
    {
        get { return iRODSpassword; }
        set { iRODSpassword = value; }
    }

    public string iRODSRootSearchCollection
    {
        get { return iRODScollection; }
        set { iRODScollection = value; }
    }

    public double RefreshTimeInHours
    {
        get { return refreshTimeInHours; }
        set
        {
            if (value >= 0)
            {
                refreshTimeInHours = value;
                timer.Interval = refreshTimeInHours * 60 * 60 * 1000;
            }
        }
    }

    public DateTime PreviousForeCastDate
    {
        get { return previousForeCastDate; }
        set { previousForeCastDate = value; }
    }

    public DateTime MinForeCastDate
    {
        get { return minForeCastDate; }
        set { minForeCastDate = value; }
    }

    public DateTime MaxForeCastDate
    {
        get { return maxForeCastDate; }
        set { maxForeCastDate = value; }
    }

    public List<string> FilesUsedInPreviousForecast
    {
        get { return filesUsedInPreviousForecast; }
        set { filesUsedInPreviousForecast = value; }
    }

    public string TimeSeriesCSVFile
    {
        get { return timeSeriesCSVFile; }
        set { timeSeriesCSVFile = value; }
    }

    public List<EnsembleForecastFile> LocalNetCDFilesToRun
    {
        get { return localNetCDFFilesToRun; }
        set { localNetCDFFilesToRun = value; }
    }

    public double FlowConversionFactor
    {
        get { return flowConversionFactor; }
        set { flowConversionFactor = value; }
    }

    public string LocalWorkSpace
    {
        get { return localWorkspace; }
        set
        {
            if (Directory.Exists(value))
            {
                localWorkspace = value;
            }
            else
            {
                throw new DirectoryNotFoundException(value + " worskpace was not found");
            }

        }
    }

    public bool ArchiveResultsAfterForecast
    {
        get { return archiveResultsAfterForecast; }
        set { archiveResultsAfterForecast = value; }
    }

    public string ArchiveWorkspace
    {
        get { return archiveWorkspace; }
        set { archiveWorkspace = value; }
    }

    public bool DeleteNetCDFAfterForecast
    {
        get { return deleteNetCDFAfterForecast; }
        set { deleteNetCDFAfterForecast = value; }
    }

    public bool UploadResultsToGeoserver
    {
        get { return uploadResultsToGeoserver; }
        set { uploadResultsToGeoserver = value; }
    }

    public string GeoServerURI
    {
        get { return geoServerURI; }
        set { geoServerURI = value; }
    }

    public string GeoServerRestServiceEndpoint
    {
        get { return geoServerRestServiceEndpoint; }
        set { geoServerRestServiceEndpoint = value; }
    }

    public string GeoServerWorkSpace
    {
        get { return geoServerWorkSpace; }
        set { geoServerWorkSpace = value; }
    }

    public string GeoServerUserName
    {
        get { return geoServerUserName; }
        set { geoServerUserName = value; }
    }

    public string GeoServerPassword
    {
        get { return geoServerPassword; }
        set { geoServerPassword = value; }
    }


    [XmlIgnore]
    public string ForecastFile
    {
        get { return forecastFile; }
        set
        {
            if (File.Exists(value))
                forecastFile = value;
            else
                throw new FileNotFoundException("File " + value + " not found", value);
        }
    }

    public SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, int>>> RiverReachXSectionCOMIDMapping
    {
        get { return riverReachXSectionCOMIDMapping; }
        set { riverReachXSectionCOMIDMapping = value; }
    }

    public SerializableDictionary<string, SerializableDictionary<string, SerializableDictionary<string, double>>> RiverReachXSectionFlowFactorsMapping
    {
        get { return riverReachXSectionFlowFactorsMapping; }
        set { riverReachXSectionFlowFactorsMapping = value; }
    }

    #endregion properties

    # region functions

    public void InitializeFromRatingsCurve()
    {
        using (TextReader reader = new StreamReader(ratingCurveLibraryFile))
        {
            XmlSerializer sr = new XmlSerializer(typeof(HecRasModel));
            model = (HecRasModel)sr.Deserialize(reader);

            riverReachXSectionCOMIDMapping.Clear();
            riverReachXSectionFlowFactorsMapping.Clear();

            List<River> rivers = model.Rivers.Values.ToList();

            for (int i = 0; i < rivers.Count; i++)
            {
                River river = rivers[i];

                SerializableDictionary<string, SerializableDictionary<string, int>> reachCOMIDXSectionMap = new SerializableDictionary<string,SerializableDictionary<string,int>>("ReachXSectionCOMID", "Reach", "XSectionCOMID");
                SerializableDictionary<string, SerializableDictionary<string, double>> reachXSectionFlowFactorsMap = new SerializableDictionary<string,SerializableDictionary<string,double>>("ReachXSectionFlowFactor", "Reach", "XSectionFlowFactor");

                List<Reach> reaches = river.Reaches.Values.ToList();

                for (int j = 0; j < reaches.Count; j++)
                {
                    Reach reach = reaches[j];

                    SerializableDictionary<string, int> COMIDXSectionMap = new SerializableDictionary<string, int>("XSectionCOMID", "XSection", "COMID");
                    SerializableDictionary<string, double> xSectionFlowFactorsMap = new  SerializableDictionary<string, double>("XSectionFlowFactor", "XSection", "FlowFactor");

                    List<XSection> xsections = reach.XSections.Values.ToList();

                    for (int k = 0; k < xsections.Count; k++)
                    {
                        XSection section = xsections[k];
                        section.SetMinZ();

                        COMIDXSectionMap.Add(section.StationName, -1);
                        xSectionFlowFactorsMap.Add(section.StationName, 1.0);
                    }

                    reachCOMIDXSectionMap.Add(reach.Name, COMIDXSectionMap);
                    reachXSectionFlowFactorsMap.Add(reach.Name, xSectionFlowFactorsMap);
                }

                riverReachXSectionCOMIDMapping.Add(river.Name, reachCOMIDXSectionMap);
                riverReachXSectionFlowFactorsMapping.Add(river.Name, reachXSectionFlowFactorsMap);
            }

        }
    }

    public void Start()
    {
        if (forecastMode == ForeCastMode.Latest)
        {
            timer.Interval = refreshTimeInHours * 60 * 60 * 1000;
            timer.Start();
        }

        MapRasterToTriangulation();

        Run();

        ArchiveFiles();
    }

    void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (!isRunning)
            Run();
    }

    public void Stop()
    {
        timer.Stop();
    }

    public void Save()
    {
        using (TextWriter writer = new StreamWriter(forecastFile))
        {
            XmlSerializer sr = new XmlSerializer(typeof(ForeCastConfiguration));
            sr.Serialize(writer, this);
        }
    }

    private void Run()
    {
        isRunning = true;

        switch (forecastMode)
        {
            case ForeCastMode.Latest:
                RunLatest();
                break;
            case ForeCastMode.BetweenSpecifiedDates:
                RunBetweenSpecifiedDates();
                break;
            case ForeCastMode.FromLocalCSV:
                RunLocalNetCSV();
                break;
            case ForeCastMode.FromLocalNetCDF:
                RunLocalNetCDFs();
                break;
        }


        Console.ResetColor();

        isRunning = false;
    }

    private void ReadElevationRaster()
    {

        Console.WriteLine("Reading elevation DEM...\n");

        Dataset rasterElevationGeotiff = Gdal.Open(elevationRaster, Access.GA_ReadOnly);
        rasterDriver = rasterElevationGeotiff.GetDriver().ShortName;
        projection = rasterElevationGeotiff.GetProjection();
        xSize = rasterElevationGeotiff.RasterXSize;
        ySize = rasterElevationGeotiff.RasterYSize;


        rasterElevationGeotiff.GetGeoTransform(geoTransformation);

        Band band = rasterElevationGeotiff.GetRasterBand(1);
        dataType = band.DataType;

        elevationData = new float[xSize][];

        for (int i = 0; i < xSize; i++)
        {
            elevationData[i] = new float[ySize];
            band.ReadRaster(i, 0, 1, ySize, elevationData[i], 1, ySize, 0, 0);

            double progress = (i + 1.0) * 100.0 / (xSize * 1.0);

            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("Progress => " + progress.ToString("###.00") + " %");
        }

        int hasVal;
        double tempV;
        band.GetNoDataValue(out tempV, out hasVal);
        noData = (float)tempV;

        rasterElevationGeotiff.Dispose();
        rasterElevationGeotiff = null;
        Console.WriteLine("\n");
        Console.WriteLine("Finished reading elevation DEM !\n");
    }

    private void MapRasterToTriangulation()
    {
        Console.WriteLine("Mapping elevation raster pixels to triangulation...\n");

        List<River> rivers = model.Rivers.Values.ToList();

        List<WaterSurfacePolygon> waterSurfacePolygons = new List<WaterSurfacePolygon>();

        double a = geoTransformation[0];
        double b = geoTransformation[1];
        double c = geoTransformation[2];
        double d = geoTransformation[3];
        double e = geoTransformation[4];
        double f = geoTransformation[5];

        //Create triangulation for each river
        for (int k = 0; k < rivers.Count; k++)
        {
            River river = rivers[k];

            foreach (Reach reach in river.Reaches.Values)
            {
                reach.CreateBoundingPolygon();
                reach.CreateTriangulationForWaterSurface();

                foreach(WaterSurfacePolygon polygon in reach.WaterSurfaces)
                {
                    waterSurfacePolygons.Add(polygon);
                }
            }
        }

        float[] mapping = new float[xSize * ySize];

        rasterWaterSurfaceMapping = new int[xSize][];
        rasterTriangleMapping = new int[xSize][];

        for (int i = 0; i < xSize; i++)
        {
            rasterWaterSurfaceMapping[i] = new int[ySize];
            rasterTriangleMapping[i] = new int[ySize];

            for (int j = 0; j < ySize; j++)
            {
                rasterWaterSurfaceMapping[i][j] = -1;
                rasterTriangleMapping[i][j] = -1;
                mapping[j * xSize + i] = noData;
            }
        }

        int count = 0;
        int foundCount = 0;
        int stepSize = (int)Math.Floor(xSize * ySize * 0.01);


        Parallel.For(0, xSize, i =>
        {
            for (int j = 0; j < ySize; j++)
            {

                Interlocked.Increment(ref count);

                if (count % stepSize == 0)
                {
                    double progress = count * 100.0 / (xSize * ySize * 1.0);

                    lock (Console.Out)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write("Progress => " + progress.ToString("###") + " %");
                    }
                }

                double elevation = elevationData[i][j];

                if (elevation != noData)
                {
                    double xlocation = a + i * b + j * c;
                    double ylocation = d + i * e + j * f;

                    for(int k = 0 ; k < waterSurfacePolygons.Count ; k++)
                    {
                        WaterSurfacePolygon watersurfacePolygon = waterSurfacePolygons[k];

                        lock(watersurfacePolygon)
                        {
                            if(watersurfacePolygon.Contains(xlocation , ylocation))
                            {
                                int m = watersurfacePolygon.FindTriangleThatContains(xlocation, ylocation);

                                if(m > -1)
                                {
                                    rasterWaterSurfaceMapping[i][j] = k;
                                    mapping[j * xSize + i] = k;
                                    rasterTriangleMapping[i][j] = m;
                                    break;
                                }
                            }
                        }
                    }
                    
                }
            }
        }
        );


        OSGeo.GDAL.Driver driver = Gdal.GetDriverByName(rasterDriver);
        Dataset newRaster = driver.Create(localWorkspace + "\\" + name + "_mapping.tif", xSize, ySize, 1, dataType, new string[] { "TFW=YES", "COMPRESS=LZW" });
        newRaster.GetRasterBand(1).SetNoDataValue(noData);
        newRaster.SetGeoTransform(geoTransformation);
        newRaster.SetProjection(projection);



        Band newRasterBand = newRaster.GetRasterBand(1);

        newRasterBand.WriteRaster(0, 0, xSize, ySize, mapping, xSize, ySize, 0, 0);

        double min, max, mean, stdev;
        newRasterBand.GetStatistics(0, 1, out min, out max, out mean, out stdev);

        newRasterBand.FlushCache();
        newRaster.FlushCache();

        newRaster.Dispose();
        newRaster = null;

        driver.Dispose();
        driver = null;

        using (IFeatureSet fs = new FeatureSet(DotSpatial.Topology.FeatureType.Polygon))
        {
            fs.DataTable.Columns.AddRange(new DataColumn[]
                    {
                      new DataColumn("Identifier" , typeof(int)),

                    });

            int tcount = 0;


            for (int k = 0; k < waterSurfacePolygons.Count; k++)
                {
                    WaterSurfacePolygon surface = waterSurfacePolygons[k];

                    foreach (TriangleNet.Data.Triangle pgon in surface.Triangles)
                    {
                        TriangleNet.Data.Triangle ts = pgon;
                        List<Coordinate> vertices = new List<Coordinate>();

                        Point p0 = surface.Points[ts.P0];
                        Point p1 = surface.Points[ts.P1];
                        Point p2 = surface.Points[ts.P2];

                        Coordinate c1 = new Coordinate(p0.X, p0.Y, p0.Z);
                        Coordinate c2 = new Coordinate(p1.X, p1.Y, p1.Z);
                        Coordinate c3 = new Coordinate(p2.X, p2.Y, p2.Z);

                        vertices.Add(c1);
                        vertices.Add(c2);
                        vertices.Add(c3);

                        Polygon polygon = new Polygon(vertices);

                        IFeature fset = fs.AddFeature(polygon);

                        fset.DataRow.BeginEdit();

                        fset.DataRow["Identifier"] = k;

                        fset.DataRow.EndEdit();

                        tcount++;
                    }
                }

            fs.SaveAs(localWorkspace + "\\" + name + "_polygon.shp", true);
            fs.Close();
            fs.Dispose();
        }


        double temp = 100;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine("Progress => " + temp.ToString("###") + " % \n");

        temp = foundCount * 100.0 / (xSize * ySize * 1.0);
        Console.WriteLine(temp.ToString("###.0") + " %  of pixels were found in triangulation \n");

        Console.WriteLine("Finished mapping elevation raster pixels to triangulation !\n");

    }

    private void RunLatest()
    {
        Console.WriteLine("Previous forecast time => " + previousForeCastDate.ToString("yyyyMMddTHHmmZ") + "...\n");

        bool endedSession = false;

        StartPythonSession();

        Console.WriteLine("Reading available collections...\n");

        Dictionary<DateTime, List<string>> availableCollections = GetAvailableForecastList();


        Console.WriteLine("Checking if new forecast is available....\n");

        KeyValuePair<DateTime, List<string>> newForecast = GetLatestForeCast(ref availableCollections);

        if (newForecast.Key > previousForeCastDate)
        {

            Console.WriteLine("New forecast is available for DateTime Starting " + newForecast.Key.ToString("yyyyMMddTHHmmZ") + " Let's rock and roll !");


            List<FileInfo> downloads = DownloadAndUnzipForecastsLocally(newForecast.Value);

            EndPythonSession();
            endedSession = true;


            Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>> timeSeriesForCOMID = new Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>>();

            Console.WriteLine("Reading flows from  forecasts...\n");

            //Changed to read all timeseries at once so that probabilities can be calculated per timesetp
            for (int i = 0; i < downloads.Count; i++)
            {
                FileInfo currentEnsemble = downloads[i];

                DateTime forecastDate = GetLocalFileDate(currentEnsemble);
                String ensemble = GetLocalEnsembleID(currentEnsemble.FullName);

                RetriveTimeSeriesForCOMIDs(forecastDate, ensemble, currentEnsemble, ref timeSeriesForCOMID);
            }

            Console.WriteLine("Finished reading flows from  forecasts\n");

            List<DateTime> dateTimes = timeSeriesForCOMID.Keys.ToList();


            //Dictionary<DateTime, Dictionary<string, FileInfo>> ensembleRasters = new Dictionary<DateTime, Dictionary<string, FileInfo>>();
            Dictionary<DateTime, FileInfo> probability = new Dictionary<DateTime, FileInfo>();

            Console.WriteLine("Running forecasts...\n");


            for (int i = 0; i < dateTimes.Count; i++)
            {
                DateTime dateTime = dateTimes[i];
                Dictionary<string, Dictionary<int, double>> valuesForEnsemble = timeSeriesForCOMID[dateTime];
                List<string> ensembles = valuesForEnsemble.Keys.ToList();

                List<FileInfo> filesForProbability = new List<FileInfo>();

                Console.WriteLine("Calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");


                for (int j = 0; j < ensembles.Count; j++)
                {
                    string ensemble = ensembles[j];

                    string fname = localWorkspace + "\\" + name + "_" + ensemble + "_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif";
                    FileInfo outputRasterFile = new FileInfo(fname);

                    Console.WriteLine("\tCalculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                    Dictionary<int, double> flowsByCOMID = valuesForEnsemble[ensemble];


                    List<River> rivers = model.Rivers.Values.ToList();

                    List<WaterSurfacePolygon> inundationPolygons = new List<WaterSurfacePolygon>();

                    for (int k = 0; k < rivers.Count; k++)
                    {
                        River river = rivers[k];

                        List<Reach> reaches = river.Reaches.Values.ToList();

                        for (int m = 0; m < reaches.Count; m++)
                        {
                            Reach reach = reaches[m];

                            List<string> xsections = (from n in reach.XSections.Values
                                                      select n.StationName).ToList();

                            Dictionary<string, double> dflows = new Dictionary<string, double>();

                            for (int l = 0; l < xsections.Count; l++)
                            {
                                string xsection = xsections[l];
                                int comid = riverReachXSectionCOMIDMapping[river.Name][reach.Name][xsection];
                                double factor = riverReachXSectionFlowFactorsMapping[river.Name][reach.Name][xsection];

#if DEBUG
                                double xflow = factor * flowsByCOMID[comid] * flowConversionFactor;
#else
                                double xflow = factor * flowsByCOMID[comid] * flowConversionFactor;
#endif

                                dflows.Add(xsection, xflow);
                            }

                            reach.setWaterDepthsFromFlow(ref dflows);
                            inundationPolygons.AddRange(reach.WaterSurfaces);
                        }
                    }

                    CalculateInundationDepthRaster(ref inundationPolygons, outputRasterFile);
                    filesForProbability.Add(outputRasterFile);
                    Console.WriteLine("\tFinished calculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                }



                Console.WriteLine("Finished calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");


                Console.WriteLine("Calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");

                List<FileInfo> files = probability.Values.ToList();

                FileInfo probabilityFile = new FileInfo(localWorkspace + "\\" + name + "_PF_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif");

                CalculateProbabilityOfInundationRaster(filesForProbability, probabilityFile);

                probability.Add(dateTime, probabilityFile);

                Console.WriteLine("Finished calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "\n");
            }

            Console.WriteLine("Finished running forecasts\n");


            DeleteNetCDFs(downloads);

            //Update current forecast
            previousDate = previousForeCastDate;
            previousForeCastDate = newForecast.Key;

            filesUsedInPreviousForecast = filesUsedInPreviousForecast = downloads.Select(p => p.FullName).ToList();

            //Export 
            UploadForecastsToGeoServer(probability);


            //Save myself
            Save();


            Console.WriteLine("Finished running forecast for DateTime Starting " + newForecast.Key.ToString("yyyyMMddTHHmmZ"));


        }
        else
        {
            Console.WriteLine("No new forecast is available :(");
        }


        if (!endedSession)
            EndPythonSession();
    }

    private void RunBetweenSpecifiedDates()
    {
        Console.WriteLine("Previous forecast time => " + previousForeCastDate.ToString("yyyyMMddTHHmmZ") + "...\n");

        Console.WriteLine("Reading available collections...\n");

        StartPythonSession();

        Dictionary<DateTime, List<string>> availableCollections = GetAvailableForecastList();

        EndPythonSession();

        Console.WriteLine("Checking if new forecast is available....\n");



        if (availableCollections.Count > 0)
        {
            List<DateTime> dates = (from n in availableCollections
                                    where n.Key >= minForeCastDate && n.Key <= maxForeCastDate
                                    orderby n.Key ascending
                                    select n.Key).Distinct().ToList();

            Console.WriteLine("Checking if forecast is available for specified date....\n");

            if (dates.Count > 0)
            {

                Console.WriteLine("Forecasts are available for specified date....\n");


                for (int m = 0; m < dates.Count; m++)
                {
                    DateTime dt = dates[m];
                    Console.WriteLine("Attempting to download forecast  for DateTime Starting " + dt.ToString("yyyyMMddTHHmmZ"));


                    StartPythonSession();

                    List<FileInfo> downloads = DownloadAndUnzipForecastsLocally(availableCollections[dt]);

                    EndPythonSession();

                    Dictionary<string, Dictionary<DateTime, FileInfo>> ensembleRasters = new Dictionary<string, Dictionary<DateTime, FileInfo>>();
                    Dictionary<DateTime, FileInfo> probability = new Dictionary<DateTime, FileInfo>();

                    for (int i = 0; i < downloads.Count; i++)
                    {
                        FileInfo currentEnsemble = downloads[i];
                        string ensemble = GetLocalEnsembleID(currentEnsemble.FullName);
                        ensembleRasters.Add(ensemble, RunForeCast(currentEnsemble));
                    }

                    List<DateTime> distinctDateTime = (from n in ensembleRasters.Values
                                                       from f in n.Keys
                                                       select f).Distinct().ToList();

                    for (int i = 0; i < distinctDateTime.Count; i++)
                    {
                        //find all ensembles with current dateTime
                        DateTime current = distinctDateTime[i];
                        FileInfo temp;

                        List<FileInfo> files = (from n in ensembleRasters.Values
                                                where n.TryGetValue(current, out temp)
                                                select n[current]).ToList();

                        temp = new FileInfo(localWorkspace + "\\" + name + "_PF_" + current.ToString("yyyyMMddTHHmmZ") + ".tif");

                        CalculateProbabilityOfInundationRaster(files, temp);

                        probability.Add(current, temp);
                    }


                    DeleteNetCDFs(downloads);

                    previousForeCastDate = dt;
                    filesUsedInPreviousForecast = downloads.Select(p => p.FullName).ToList();
                    Save();

                    //Export 
                    UploadForecastsToGeoServer(probability);

                    Console.WriteLine("Finished forecast  for DateTime Starting " + dt.ToString("yyyyMMddTHHmmZ"));
                }

            }
            else
            {
                Console.WriteLine("No new forecast is available :( ....\n");
            }
        }
        else
        {
            Console.WriteLine("No new forecast is available :( ....\n");
        }
    }

    private void RunLocalNetCDFs()
    {
        if (localNetCDFFilesToRun.Count > 0)
        {
            List<DateTime> uniqueForecastDates = (from n in localNetCDFFilesToRun
                                                  select n.ForecastDate).Distinct().ToList();

            foreach (var dt in uniqueForecastDates)
            {
                List<FileInfo> downloads = (from n in localNetCDFFilesToRun
                                            where n.ForecastDate == dt &&
                                            File.Exists(n.Path)
                                            select new FileInfo(n.Path)).ToList();

                if (downloads.Count > 0)
                {
                    Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>> timeSeriesForCOMID = new Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>>();

                    Console.WriteLine("Reading flows from  forecasts...\n");

                    //Changed to read all timeseries at once so that probabilities can be calculated per timesetp
                    for (int i = 0; i < downloads.Count; i++)
                    {
                        FileInfo currentEnsemble = downloads[i];

                        DateTime forecastDate = dt;
                        String ensemble = GetiRODSDataObjectEnsembleID(currentEnsemble.FullName).Value;

                        RetriveTimeSeriesForCOMIDs(forecastDate, ensemble, currentEnsemble, ref timeSeriesForCOMID);
                    }

                    Console.WriteLine("Finished reading flows from  forecasts\n");

                    List<DateTime> dateTimes = timeSeriesForCOMID.Keys.ToList();


                    //Dictionary<DateTime, Dictionary<string, FileInfo>> ensembleRasters = new Dictionary<DateTime, Dictionary<string, FileInfo>>();
                    Dictionary<DateTime, FileInfo> probability = new Dictionary<DateTime, FileInfo>();

                    Console.WriteLine("Running forecasts...\n");


                    for (int i = 0; i < dateTimes.Count; i++)
                    {
                        DateTime dateTime = dateTimes[i];
                        Dictionary<string, Dictionary<int, double>> valuesForEnsemble = timeSeriesForCOMID[dateTime];
                        List<string> ensembles = valuesForEnsemble.Keys.ToList();

                        List<FileInfo> filesForProbability = new List<FileInfo>();

                        Console.WriteLine("Calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");


                        for (int j = 0; j < ensembles.Count; j++)
                        {
                            string ensemble = ensembles[j];

                            string fname = localWorkspace + "\\" + name + "_" + ensemble + "_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif";
                            FileInfo outputRasterFile = new FileInfo(fname);

                            Console.WriteLine("\tCalculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                            Dictionary<int, double> flowsByCOMID = valuesForEnsemble[ensemble];


                            List<River> rivers = model.Rivers.Values.ToList();

                            List<WaterSurfacePolygon> inundationPolygons = new List<WaterSurfacePolygon>();

                            for (int k = 0; k < rivers.Count; k++)
                            {
                                River river = rivers[k];

                                List<Reach> reaches = river.Reaches.Values.ToList();

                                for (int m = 0; m < reaches.Count; m++)
                                {

                                    Reach reach = reaches[m];

                                    List<string> xsections = (from n in reach.XSections.Values
                                                              select n.StationName).ToList();

                                    Dictionary<string, double> dflows = new Dictionary<string, double>();

                                    for (int l = 0; l < xsections.Count; l++)
                                    {
                                        string xsection = xsections[l];
                                        int comid = riverReachXSectionCOMIDMapping[river.Name][reach.Name][xsection];
                                        double factor = riverReachXSectionFlowFactorsMapping[river.Name][reach.Name][xsection];

#if DEBUG
                                    double xflow = factor * flowsByCOMID[comid] * flowConversionFactor;
                                    Debug.WriteLine(xflow);
#else
                                        double xflow = factor * flowsByCOMID[comid] * flowConversionFactor;
#endif

                                        dflows.Add(xsection, xflow);
                                    }

                                    reach.setWaterDepthsFromFlow(ref dflows);
                                    inundationPolygons.AddRange(reach.WaterSurfaces);
                                }
                            }

                            CalculateInundationDepthRaster(ref inundationPolygons, outputRasterFile);
                            filesForProbability.Add(outputRasterFile);
                            Console.WriteLine("\tFinished calculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                        }

                        Console.WriteLine("Finished calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");

                        Console.WriteLine("Calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");

                        List<FileInfo> files = probability.Values.ToList();

                        FileInfo probabilityFile = new FileInfo(localWorkspace + "\\" + name + "_PF_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif");

                        CalculateProbabilityOfInundationRaster(filesForProbability, probabilityFile);

                        probability.Add(dateTime, probabilityFile);

                        Console.WriteLine("Finished calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "\n");
                    }

                    Console.WriteLine("Finished running forecasts\n");
                }
                else
                {
                    Console.WriteLine("Please include valid local netcdf files to run");
                }
            }
        }
        else
        {
            Console.WriteLine("Please include valid local netcdf files to run");
        }
    }

    private void RunLocalNetCSV()
    {
        Console.WriteLine("Running from local timeseries file => " + timeSeriesCSVFile + "...\n");

        if (File.Exists(timeSeriesCSVFile))
        {
            //DateTime, Ensemble, COMID, Value
            Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>> valueByCOMIDByEnsembleByDateTime =
                new Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>>();

            string[] delim = new string[] { ",", "\t" };
            string line;

            Console.WriteLine("Reading flows from  timeseries file...\n");

            using (TextReader reader = new StreamReader(timeSeriesCSVFile))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] cols = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                    if (cols.Length == 4)
                    {
                        DateTime dt;
                        int ensemble;
                        int Comid;
                        double value;

                        if (
                            DateTime.TryParse(cols[0], out dt) &&
                            int.TryParse(cols[1], out ensemble) &&
                            int.TryParse(cols[2], out Comid) &&
                            double.TryParse(cols[3], out value)
                            )
                        {
                            Dictionary<string, Dictionary<int, double>> valueByCOMIDByEnsemble;

                            if (valueByCOMIDByEnsembleByDateTime.ContainsKey(dt))
                            {
                                valueByCOMIDByEnsemble = valueByCOMIDByEnsembleByDateTime[dt];
                            }
                            else
                            {
                                valueByCOMIDByEnsemble = new Dictionary<string, Dictionary<int, double>>();
                                valueByCOMIDByEnsembleByDateTime.Add(dt, valueByCOMIDByEnsemble);
                            }

                            Dictionary<int, double> valueByCOMID;

                            if (valueByCOMIDByEnsemble.ContainsKey(ensemble.ToString()))
                            {
                                valueByCOMID = valueByCOMIDByEnsemble[ensemble.ToString()];
                            }
                            else
                            {
                                valueByCOMID = new Dictionary<int, double>();
                                valueByCOMIDByEnsemble.Add(ensemble.ToString(), valueByCOMID);
                            }

                            if (!valueByCOMID.ContainsKey(Comid))
                            {
                                valueByCOMID.Add(Comid, value);
                            }
                        }
                    }
                }
            }


            Console.WriteLine("Finished reading flows from  timeseries file!\n");


            List<DateTime> dateTimes = valueByCOMIDByEnsembleByDateTime.Keys.ToList();


            //Dictionary<DateTime, Dictionary<string, FileInfo>> ensembleRasters = new Dictionary<DateTime, Dictionary<string, FileInfo>>();
            Dictionary<DateTime, FileInfo> probability = new Dictionary<DateTime, FileInfo>();

            Console.WriteLine("Running forecasts...\n");


            for (int i = 0; i < dateTimes.Count; i++)
            {
                DateTime dateTime = dateTimes[i];
                Dictionary<string, Dictionary<int, double>> valuesForEnsemble = valueByCOMIDByEnsembleByDateTime[dateTime];
                List<string> ensembles = valuesForEnsemble.Keys.ToList();

                List<FileInfo> filesForProbability = new List<FileInfo>();

                Console.WriteLine("Calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");


                for (int j = 0; j < ensembles.Count; j++)
                {
                    string ensemble = ensembles[j];

                    string fname = localWorkspace + "\\" + name + "_" + ensemble + "_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif";
                    FileInfo outputRasterFile = new FileInfo(fname);

                    Console.WriteLine("\tCalculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                    Dictionary<int, double> flowsByCOMID = valuesForEnsemble[ensemble];

                    List<River> rivers = model.Rivers.Values.ToList();

                    List<WaterSurfacePolygon> inundationPolygons = new List<WaterSurfacePolygon>();

                    for (int k = 0; k < rivers.Count; k++)
                    {
                        River river = rivers[k];

                        List<Reach> reaches = river.Reaches.Values.ToList();

                        for (int m = 0; m < reaches.Count; m++)
                        {
                            Reach reach = reaches[m];

                            List<string> xsections = (from n in reach.XSections.Values
                                                      select n.StationName).ToList();

                            Dictionary<string, double> dflows = new Dictionary<string, double>();

                            for (int l = 0; l < xsections.Count; l++)
                            {
                                string xsection = xsections[l];
                                int comid = riverReachXSectionCOMIDMapping[river.Name][reach.Name][xsection];
                                double factor = riverReachXSectionFlowFactorsMapping[river.Name][reach.Name][xsection];

                                double xflow = factor * flowsByCOMID[comid] * flowConversionFactor;

                                dflows.Add(xsection, xflow);
                            }

                            reach.setWaterDepthsFromFlow(ref dflows);
                            inundationPolygons.AddRange(reach.WaterSurfaces);
                        }
                    }

                    CalculateInundationDepthRaster(ref inundationPolygons, outputRasterFile);
                    filesForProbability.Add(outputRasterFile);
                    Console.WriteLine("\tFinished calculating inundation raster [" + fname + "] for ensemble " + ensemble + "...\n");

                }

                Console.WriteLine("Finished calculating inundation rasters for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");

                Console.WriteLine("Calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "...\n");

                List<FileInfo> files = probability.Values.ToList();

                FileInfo probabilityFile = new FileInfo(localWorkspace + "\\" + name + "_PF_" + dateTime.ToString("yyyyMMddTHHmmZ") + ".tif");

                CalculateProbabilityOfInundationRaster(filesForProbability, probabilityFile);

                probability.Add(dateTime, probabilityFile);

                Console.WriteLine("Finished calculating probabilistic inundation for " + dateTime.ToString("yyyyMMddTHHmmZ") + "\n");
            }

            UploadForecastsToGeoServer(probability);

        }
        else
        {
            Console.WriteLine("Timeseries file => " + timeSeriesCSVFile + " was not found !\n");

            throw new FileNotFoundException("Timeseries file was not found", timeSeriesCSVFile);
        }
        //Export 


        //Save myself
        Save();

        Console.WriteLine("Finished running from local timeseries file => " + timeSeriesCSVFile + "!\n");
    }

    private void RetriveTimeSeriesForCOMIDs(DateTime forecastDate, string ensemble, FileInfo netcdFile, ref Dictionary<DateTime, Dictionary<string, Dictionary<int, double>>> timeseries)
    {
        Console.WriteLine("Reading flows from  => " + netcdFile.FullName + " \n");

        Debug.WriteLine("Values for Ensemble " + ensemble);

        using (Microsoft.Research.Science.Data.DataSet dt = Microsoft.Research.Science.Data.DataSet.Open(netcdFile.FullName, ResourceOpenMode.ReadOnly))
        {
            List<int> uniqueCOMIDs = (from n in riverReachXSectionCOMIDMapping.Values.AsParallel()
                                   from m in n.Values.AsParallel()
                                   from k in m.Values.AsParallel()
                                   select k).Distinct().ToList();

            Variable ids = dt["COMID"];
            Variable qout = dt["Qout"];
            Array comidValues = ids.GetData();

            Microsoft.Research.Science.Data.Dimension time = dt.Dimensions["Time"];

#if DEBUG
            DateTime[] dates = new DateTime[20];

            for (int f = 0; f < 20; f++)
#else
            DateTime[] dates = new DateTime[time.Length];

            for (int f = 0; f < time.Length; f++)
#endif

            {

                DateTime currentDt = forecastDate.AddHours(f * 6);
                dates[f] = currentDt;

                if (!timeseries.ContainsKey(currentDt))
                {
                    Dictionary<string, Dictionary<int, double>> ens = new Dictionary<string, Dictionary<int, double>>();
                    ens.Add(ensemble, new Dictionary<int, double>());
                    timeseries.Add(currentDt, ens);
                }
                else
                {
                    Dictionary<string, Dictionary<int, double>> ens = timeseries[currentDt];

                    if (!ens.ContainsKey(ensemble))
                    {
                        ens.Add(ensemble, new Dictionary<int, double>());
                    }
                }
            }


            for (int i = 0; i < comidValues.Length; i++)
            {
                int id = (int)comidValues.GetValue(i);


                if (uniqueCOMIDs.Contains(id))
                {
                    uniqueCOMIDs.Remove(id);

                    Array flowTS = qout.GetData(new int[] { 0, i }, new int[] { time.Length, 1 });


#if DEBUG
                    double max = double.MinValue;
                    for (int f = 0; f < time.Length; f++)
                    {
                        if ((float)flowTS.GetValue(f, 0) > max)
                        {
                            max = (float)flowTS.GetValue(f, 0);
                        }
                    }

                    Debug.WriteLine("Max " + max);

                    for (int f = 0; f < 20; f++)
#else
                    for (int f = 0; f < time.Length; f++)
#endif
                    {

                        double value = (float)flowTS.GetValue(f, 0);
#if DEBUG

                        Debug.WriteLine(value);
#endif

                        timeseries[dates[f]][ensemble].Add(id, value);
                    }
                }

                if (uniqueCOMIDs.Count == 0)
                {
                    break;
                }
            }


            // dt.Dispose();
        }

        Console.WriteLine("Finished reading flows from  => " + netcdFile.FullName + " \n");
    }

    private Dictionary<DateTime, FileInfo> RunForeCast(FileInfo netcdf)
    {
        Console.WriteLine("Delineating inundation raster for  => " + netcdf.FullName + " \n");

        DateTime forecastDate = GetLocalFileDate(netcdf);
        String ensemble = GetLocalEnsembleID(netcdf.FullName);

        Dictionary<DateTime, FileInfo> rasters = new Dictionary<DateTime, FileInfo>();

        //get unique comids
        Dictionary<int, bool> uniqueCOMIDs = (from n in riverReachXSectionCOMIDMapping.Values.AsParallel()
                                           from m in n.Values.AsParallel()
                                           from k in m.Values.AsParallel()
                                           select k).Distinct().ToDictionary(v => v, v => false);

        Console.WriteLine("Reading flows from  => " + netcdf.FullName + " \n");

        Microsoft.Research.Science.Data.DataSet dt = Microsoft.Research.Science.Data.DataSet.Open(netcdf.FullName, ResourceOpenMode.ReadOnly);

        Variable ids = dt["COMID"];
        Variable qout = dt["Qout"];
        
        Array comids = ids.GetData();
        Microsoft.Research.Science.Data.Dimension time = dt.Dimensions["Time"];

        //get timeseries for comid
        Dictionary<int, Dictionary<DateTime, double>> downloadCIDData = new Dictionary<int, Dictionary<DateTime, double>>();

        for (int i = 0; i < comids.Length; i++)
        {
            //check if all time series has been set probably inefficient will optimize later
            int id = (int)comids.GetValue(i);

            if (uniqueCOMIDs.ContainsKey(id))
            {

                uniqueCOMIDs.Remove(id);


                Dictionary<DateTime, double> flowTimeSeries = new Dictionary<DateTime, double>();

                Array flowTS = qout.GetData(new int[] { 0, i }, new int[] { time.Length, 1 });

                for (int f = 0; f < time.Length; f++)
                {
                    DateTime currentDt = forecastDate.AddHours(f * 6);
                    double value = (float)flowTS.GetValue(f, 0);
                    flowTimeSeries.Add(currentDt, value);
                }

                downloadCIDData.Add(id, flowTimeSeries);
            }

            if (uniqueCOMIDs.Count == 0)
            {
                break;
            }
        }




        Console.WriteLine("Finished reading flows from  => " + netcdf.FullName + " \n");

        //Apply data for each Date
        List<DateTime> dateTimes = (from n in downloadCIDData.Values
                                    from f in n.Keys
                                    select f).Distinct().ToList();

        //Parallel.For(0, dateTimes.Count, i =>
        for (int i = 0; i < dateTimes.Count; i++)
        {

#if DEBUG
            //remove
            if (i == 3)
            {
                break;
            }
#endif
            DateTime date = dateTimes[i];
            string fname = localWorkspace + "\\" + name + "_" + ensemble + "_" + date.ToString("yyyyMMddTHHmmZ") + ".tif";

            Dictionary<int, double> flows = (from n in downloadCIDData
                                             from f in n.Value
                                             where f.Key == date
                                             select new { Key = n.Key, Value = f.Value }).ToDictionary(v => v.Key, v => v.Value);

            List<River> rivers = model.Rivers.Values.ToList();

            List<WaterSurfacePolygon> inundationPolygons = new List<WaterSurfacePolygon>();

            for (int j = 0; j < rivers.Count; j++)
            {
                River river = rivers[j];

                List<Reach> reaches = river.Reaches.Values.ToList();

                for (int m = 0; m < reaches.Count; m++)
                {
                    Reach reach = reaches[m];

                    List<string> xsections = (from n in reach.XSections.Values
                                              select n.StationName).ToList();

                    Dictionary<string, double> dflows = new Dictionary<string, double>();

                    for (int k = 0; k < xsections.Count; k++)
                    {
                        string xsection = xsections[k];
                        int comid = riverReachXSectionCOMIDMapping[river.Name][reach.Name][xsection];
                        double factor = riverReachXSectionFlowFactorsMapping[river.Name][reach.Name][xsection];
#if DEBUG
                    double xflow = factor * flows[comid] * flowConversionFactor;
#else

                        double xflow = factor * flows[comid] * flowConversionFactor;
#endif
                        dflows.Add(xsection, xflow);
                    }
                    reach.setWaterDepthsFromFlow(ref dflows);
                    inundationPolygons.AddRange(reach.WaterSurfaces);
                }
            }

            //create raster
            Console.WriteLine("Creating inundation raster   => " + fname + " \n");
            FileInfo outputRasterFile = new FileInfo(fname);

            CalculateInundationDepthRaster(ref inundationPolygons, outputRasterFile);

            rasters.Add(date, outputRasterFile);

            Console.WriteLine("Finished creating inundation raster   => " + fname + " \n");
        }
        //);

        dt.Dispose();
        dt = null;

        Console.WriteLine("Finished delineating inundation raster for  => " + netcdf.FullName + " \n");

        return rasters;
    }

    public void SaveAs(FileInfo file)
    {
        using (TextWriter writer = new StreamWriter(file.FullName))
        {
            XmlSerializer sr = new XmlSerializer(typeof(ForeCastConfiguration));
            this.forecastFile = file.FullName;
            sr.Serialize(writer, this);
        }
    }

    public void ArchiveFiles()
    {
        if (archiveResultsAfterForecast)
        {
            Console.WriteLine("Archiving files...\n");

            if (Directory.Exists(localWorkspace) && Directory.Exists(archiveWorkspace))
            {
                DirectoryInfo workspace = new DirectoryInfo(localWorkspace);
                DirectoryInfo archive = new DirectoryInfo(archiveWorkspace);
                FileInfo[] filestoMove = workspace.GetFiles();

                string folder = archive.FullName + "\\" + previousDate.ToString("yyyyMMddTHHmmZ");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                for (int i = 0; i < filestoMove.Length; i++)
                {
                    try
                    {
                        File.Move(filestoMove[i].FullName, folder + "\\" + filestoMove[i].Name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message + "\n");
                    }
                }
            }

            Console.WriteLine("Finished archiving files\n");
        }
    }

    public void UploadForecastsToGeoServer(Dictionary<DateTime, FileInfo> probabilities)
    {
        if (uploadResultsToGeoserver)
        {
            StartPythonSession();

            Console.WriteLine("Uploading results to the geoserver...\n");

            PyObject upload_files_to_geoserver = uploadToGeoserver.GetAttr("upload_files_to_geoserver");

            PyList files = new PyList();
            List<string> zippedFiles = new List<string>();

            foreach (var s in probabilities.Values)
            {

                //write projection file

                FileInfo prj = new FileInfo(s.FullName.Replace(s.Extension, ".prj"));
                FileInfo tfw = new FileInfo(s.FullName.Replace(s.Extension, ".tfw"));

                File.WriteAllText(prj.FullName, projection.ToUpper());
                string zippedFile = s.FullName.Replace(s.Extension, ".zip");
                zippedFiles.Add(zippedFile);


                using (FileStream fstream = File.Create(zippedFile))
                {

                    using (IWriter writer = WriterFactory.Open(fstream, SharpCompress.Common.ArchiveType.Zip, SharpCompress.Common.CompressionType.None))
                    {
                        writer.Write(s.Name, s.FullName);
                        writer.Write(prj.Name, prj.FullName);
                        writer.Write(tfw.Name, tfw.FullName);
                        writer.Dispose();
                    }

                    fstream.Close();
                    fstream.Dispose();
                }



                files.Append(new PyString(zippedFile));

                //files.Append(new PyString(s.FullName));
            }

            PyObject[] args = new PyObject[] 
                {
                    files,
                    new PyString(geoServerURI),
                    new PyString(geoServerRestServiceEndpoint),
                    new PyString(geoServerWorkSpace),
                    new PyString(geoServerUserName),
                    new PyString(GeoServerPassword)
                };

            upload_files_to_geoserver.Invoke(args);
            files.Dispose();
            files = null;


            for (int i = 0; i < args.Length; i++)
            {
                args[i].Dispose();
                args[i] = null;
            }

            foreach (var zipped in zippedFiles)
            {
                if (File.Exists(zipped))
                {
                    try
                    {
                        File.Delete(zipped);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }


            Console.WriteLine("\nFinished uploading results to the geoserver!");


            EndPythonSession();
        }
    }

    public void DeleteNetCDFs(List<FileInfo> files)
    {
        if (deleteNetCDFAfterForecast)
        {
            try
            {                 //set new date and save after successful forecast

                foreach (var f in files)
                    File.Delete(f.FullName);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public void SetCOMIDAndFlowFactor(string river, string reach, string XSection, int COMID, double factor)
    {
        if (riverReachXSectionCOMIDMapping.ContainsKey(river))
        {
            SerializableDictionary<string, SerializableDictionary<string, int>> reachXSectionCOMIDMapping = riverReachXSectionCOMIDMapping[river];

            if (reachXSectionCOMIDMapping.ContainsKey(reach))
            {
                SerializableDictionary<string, int> xSectionCOMIDMap = reachXSectionCOMIDMapping[reach];

                if (xSectionCOMIDMap.ContainsKey(XSection))
                {
                    xSectionCOMIDMap[XSection] = COMID;
                }
            }
        }


        if (riverReachXSectionFlowFactorsMapping.ContainsKey(river))
        {

            SerializableDictionary<string, SerializableDictionary<string,double>> reachXSectionFlowFactorsMap = riverReachXSectionFlowFactorsMapping[river];

            if (reachXSectionFlowFactorsMap.ContainsKey(reach))
            {
                SerializableDictionary<string, double> xSectionFlowFactorsMap = reachXSectionFlowFactorsMap[reach];

                if (xSectionFlowFactorsMap.ContainsKey(XSection))
                {
                    xSectionFlowFactorsMap[XSection] = factor;
                }
            }
        }
    }

    private Dictionary<DateTime, List<string>> GetAvailableForecastList()
    {
        Dictionary<DateTime, List<string>> forecasts = new Dictionary<DateTime, List<string>>();
        PyObject create_session = iRODSClientModule.GetAttr("create_session");

        PyObject[] sessionArgs = new PyObject[]
                                {
                                    new PyString(iRODShost),
                                    new PyString(iRODSuserName),
                                    new PyString(iRODSpassword),
                                    new PyString(iRODSzone),
                                    new PyInt(iRODSport)
                                };

        PyObject session = create_session.Invoke(sessionArgs);

        PyObject get_collection = iRODSClientModule.GetAttr("get_collection");
        PyObject get_all_data_objects_recursively = iRODSClientModule.GetAttr("get_all_data_objects_recursively");

        PyObject[] args = new PyObject[]
                                {
                                    session,
                                    new PyString(iRODScollection),
                                };

        PyObject headNodeCollection = get_collection.Invoke(args);

        for (int m = 0; m < args.Length; m++)
        {
            PyObject obj = args[m];
            obj.Dispose();
            obj = null;
        }

        args = new PyObject[]
                                {
                                 headNodeCollection
                                };

        PyObject allFiles = get_all_data_objects_recursively.Invoke(args);

        PyList dataObjects = new PyList(allFiles);

        int value = dataObjects.Length();

        for (int i = 0; i < value; i++)
        {
            PyObject dataObject = dataObjects.GetItem(i);
            string path = dataObject.ToString();

            KeyValuePair<bool, DateTime> dateTime = GetiRODSDataObjectDate(path);

            if (dateTime.Key)
            {
                KeyValuePair<bool, string> ensembleid = GetiRODSDataObjectEnsembleID(path);

                if (ensembleid.Key)
                {
                    if (forecasts.ContainsKey(dateTime.Value))
                    {
                        forecasts[dateTime.Value].Add(path);
                    }
                    else
                    {
                        List<string> newstring = new List<string>();
                        newstring.Add(path);
                        forecasts.Add(dateTime.Value, newstring);
                    }

                }
                else
                {
                    dataObject.ToString();
                }
            }
            else
            {
                dataObject.ToString();
            }

            dataObject.Dispose();
            dataObject = null;
        }

        session.Dispose();
        session = null;
        create_session.Dispose();
        create_session = null;

        for (int m = 0; m < sessionArgs.Length; m++)
        {
            PyObject obj = sessionArgs[m];
            obj.Dispose();
            obj = null;
        }

        get_collection.Dispose();
        get_collection = null;

        get_all_data_objects_recursively.Dispose();
        get_all_data_objects_recursively = null;

        for (int m = 0; m < args.Length; m++)
        {
            PyObject obj = args[m];
            obj.Dispose();
            obj = null;
        }

        headNodeCollection.Dispose();
        headNodeCollection = null;

        allFiles.Dispose();
        allFiles = null;

        dataObjects.Dispose(); dataObjects = null;

        return forecasts;
    }

    private KeyValuePair<DateTime, List<string>> GetLatestForeCast(ref Dictionary<DateTime, List<string>> availabledataobject)
    {
        if (availabledataobject.Count > 0)
        {
            KeyValuePair<DateTime, List<string>> latest = (from n in availabledataobject
                                                           orderby n.Key descending
                                                           select new KeyValuePair<DateTime, List<string>>(n.Key, n.Value)).FirstOrDefault();
            return latest;

        }
        else
        {
            return new KeyValuePair<DateTime, List<string>>(DateTime.MinValue, new List<string>());
        }

    }

    private List<FileInfo> DownloadAndUnzipForecastsLocally(List<string> files)
    {
        //Remember to remove
        List<FileInfo> downloadedFiles = new List<FileInfo>();
        PyList server_paths = new PyList();
        PyList local_paths = new PyList();

        List<string> local_paths_ = new List<string>();

        for (int i = 0; i < files.Count; i++)
        {
#if DEBUG
            if (i > 1)
                break;
#endif
            string ipath = files[i];
            DateTime dateTime = GetiRODSDataObjectDate(ipath).Value;

            string saveAs = name + "_" + GetiRODSDataObjectEnsembleID(ipath).Value +
                 "_" + dateTime.Year.ToString("0000") + dateTime.Month.ToString("00") +
                 dateTime.Day.ToString("00") + "T" + dateTime.Hour.ToString("00") +
                 dateTime.Minute.ToString("00") + "Z" + "." + GetFileExtension(ipath);

            FileInfo saveAsFile = new FileInfo(localWorkspace + "\\" + saveAs);

            local_paths_.Add(saveAsFile.FullName);

            server_paths.Append(new PyString(ipath));
            local_paths.Append(new PyString(saveAsFile.FullName));

        }

        PyObject create_session = iRODSClientModule.GetAttr("create_session");

        PyObject[] sessionArgs = new PyObject[]
                                {
                                    new PyString(iRODShost),
                                    new PyString(iRODSuserName),
                                    new PyString(iRODSpassword),
                                    new PyString(iRODSzone),
                                    new PyInt(iRODSport)
                                };

        PyObject session = create_session.Invoke(sessionArgs);

        PyObject save_data_objects_locally = iRODSClientModule.GetAttr("save_data_objects_locally");

        PyObject[] args = new PyObject[] 
                {
                    session, 
                    server_paths,
                    local_paths
                };

        PyObject result = save_data_objects_locally.Invoke(args);
        result.Dispose();
        result = null;


        for (int m = 1; m < args.Length; m++)
        {
            PyObject obj = args[m];
            obj.Dispose();
            obj = null;
        }

        save_data_objects_locally.Dispose();
        save_data_objects_locally = null;

        session.Dispose();
        session = null;

        for (int m = 0; m < sessionArgs.Length; m++)
        {
            PyObject obj = sessionArgs[m];
            obj.Dispose();
            obj = null;
        }

        create_session.Dispose();
        create_session = null;

        foreach (var f in local_paths_)
        {
            if (File.Exists(f))
            {
                FileInfo saveAsFile = new FileInfo(f);

                Console.WriteLine("Decompressing  [" + f + "]\n");

                using (FileStream downloadedStream = new FileStream(saveAsFile.FullName, FileMode.Open))
                {
                    using (IReader reader = ReaderFactory.Open(downloadedStream))
                    {
                        FileInfo file = new FileInfo(saveAsFile.FullName.Replace(saveAsFile.Extension, ".nc"));

                        while (reader.MoveToNextEntry())
                        {
                            if (!reader.Entry.IsDirectory && reader.Entry.FilePath.ToLower().Contains(".nc"))
                            {
                                try
                                {
                                    reader.WriteEntryToFile(file.FullName);

                                    using (Microsoft.Research.Science.Data.DataSet dt = Microsoft.Research.Science.Data.DataSet.Open(file.FullName, ResourceOpenMode.ReadOnly))
                                    {

                                    }

                                    downloadedFiles.Add(file);
                                }
                                catch (Exception ex)
                                {

                                    Console.WriteLine(ex.Message);

                                }
                                break;
                            }
                        }

                        reader.Dispose();

                        Console.WriteLine("Finished Decompressing " + GetLocalFileDate(file).ToString("s") + " [" + file.FullName + "]\n");
                    }

                    downloadedStream.Close();
                    downloadedStream.Dispose();
                }

                try
                {
                    File.Delete(saveAsFile.FullName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n" + ex.Message);
                }
            }
        }


        return downloadedFiles;
    }

    private void CalculateInundationDepthRaster(ref List<WaterSurfacePolygon> waterSurfaces, FileInfo outputRaster)
    {
        OSGeo.GDAL.Driver driver = Gdal.GetDriverByName(rasterDriver);
        Dataset newRaster = driver.Create(outputRaster.FullName, xSize, ySize, 1, dataType, new string[] { "TFW=YES", "COMPRESS=LZW" });
        newRaster.GetRasterBand(1).SetNoDataValue(noData);
        newRaster.SetGeoTransform(geoTransformation);
        newRaster.SetProjection(projection);

        double a = geoTransformation[0];
        double b = geoTransformation[1];
        double c = geoTransformation[2];
        double d = geoTransformation[3];
        double e = geoTransformation[4];
        double f = geoTransformation[5];

        Band newRasterBand = newRaster.GetRasterBand(1);

        //max 536870912
        float[] depthValues = new float[xSize * ySize];

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                depthValues[j * xSize + i] = noData;
            }
        }

        double xlocation, ylocation;

        List<WaterSurfacePolygon> wsurfaces = waterSurfaces;

        int fcount = 0;

        //Parallel.For(0, xSize, i =>
        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                float elevation = elevationData[i][j];

                if (elevation != noData)
                {
                    int m = rasterWaterSurfaceMapping[i][j];

                    if (m >= 0)
                    {
                        int location = rasterTriangleMapping[i][j];

                        if (location >= 0)
                        {
                            xlocation = a + i * b + j * c;
                            ylocation = d + i * e + j * f;

                            WaterSurfacePolygon surface = wsurfaces[m];

                            //lock (surface)
                            //{
                            double wselev = surface.GetZ(location, xlocation, ylocation);

                            double depth = wselev - elevation;

                            if (depth < 0)
                            {
                                depth = noData;
                            }
                            else
                            {
                                Interlocked.Increment(ref fcount);
                            }

                            //lock (depthValues)
                            //{
                            depthValues[j * xSize + i] = (float)depth;
                            //}
                            //}
                        }
                    }
                }
            }
        }
        //);

        //write to raster
        newRasterBand.WriteRaster(0, 0, xSize, ySize, depthValues, xSize, ySize, 0, 0);

        if (fcount > 2)
        {
            double min, max, mean, stdev;
            newRasterBand.GetStatistics(0, 1, out min, out max, out mean, out stdev);

            double temp = fcount * 100.0 / (xSize * ySize * 1.0);
            Console.WriteLine("\t" + temp.ToString("###.0") + " %  of pixels were inundated ! \n");
        }

        newRasterBand.FlushCache();
        newRaster.FlushCache();

        newRaster.Dispose();
        newRaster = null;

        driver.Dispose();
        driver = null;

        # region unnecessary
#if DEBUG
        string ensemble = GetLocalEnsembleID(outputRaster.FullName);
        string dateTime = GetLocalFileDate(outputRaster).ToString("yyyy-MM-dd hh:mm:ss");

        using (IFeatureSet fs = new FeatureSet(DotSpatial.Topology.FeatureType.Point))
        {
            fs.DataTable.Columns.AddRange(new DataColumn[]
                    {
                      new DataColumn("X" , typeof(double)),
                      new DataColumn("Y", typeof(double)),
                      new DataColumn("Z", typeof(double)),
                      new DataColumn("Ensemble" , typeof(string)),
                      new DataColumn("DateTime" , typeof(string)),

                    });


            for (int k = 0; k < waterSurfaces.Count; k++)
            {
                WaterSurfacePolygon surface = waterSurfaces[k];

                foreach (TriangleNet.Data.Triangle pgon in surface.Triangles)
                {
                    TriangleNet.Data.Triangle ts = pgon;

                    TriangleNet.Data.Vertex v0 = ts.GetVertex(0);
                    TriangleNet.Data.Vertex v1 = ts.GetVertex(1);
                    TriangleNet.Data.Vertex v2 = ts.GetVertex(2);

                    Point p0 = surface.Points[ts.P0]; ;
                    Point p1 = surface.Points[ts.P1];
                    Point p2 = surface.Points[ts.P2];


                    //add attribute fields to attribute table
                    fs.Features.Add(new Coordinate(p0.X, p0.Y, p0.Z));
                    IFeature fset = fs.Features[fs.Features.Count - 1];

                    fset.DataRow.BeginEdit();

                    fset.DataRow["X"] = p0.X;
                    fset.DataRow["Y"] = p0.Y;
                    fset.DataRow["Z"] = p0.Z;
                    fset.DataRow["Ensemble"] = ensemble;
                    fset.DataRow["DateTime"] = dateTime;

                    fset.DataRow.EndEdit();

                    fs.Features.Add(new Coordinate(p1.X, p1.Y, p1.Z));
                    fset = fs.Features[fs.Features.Count - 1];

                    fset.DataRow.BeginEdit();
                    fset.DataRow["X"] = p1.X;
                    fset.DataRow["Y"] = p1.Y;
                    fset.DataRow["Z"] = p1.Z;
                    fset.DataRow["Ensemble"] = ensemble;
                    fset.DataRow["DateTime"] = dateTime;

                    fset.DataRow.EndEdit();

                    fs.Features.Add(new Coordinate(p2.X, p2.Y, p2.Z));
                    fset = fs.Features[fs.Features.Count - 1];

                    fset.DataRow.BeginEdit();
                    fset.DataRow["X"] = p2.X;
                    fset.DataRow["Y"] = p2.Y;
                    fset.DataRow["Z"] = p2.Z;
                    fset.DataRow["Ensemble"] = ensemble;
                    fset.DataRow["DateTime"] = dateTime;

                    fset.DataRow.EndEdit();
                }
            }

            fs.SaveAs(outputRaster.FullName.Replace(outputRaster.Extension, "_points.shp"), true);
            fs.Close();
            fs.Dispose();
        }

        using (IFeatureSet fs = new FeatureSet(DotSpatial.Topology.FeatureType.Polygon))
        {
            fs.DataTable.Columns.AddRange(new DataColumn[]
                    {
                      new DataColumn("MaxElev" , typeof(double)),
                      new DataColumn("MinElev" , typeof(double)),
                      new DataColumn("Ensemble" , typeof(string)),
                      new DataColumn("DateTime" , typeof(string)),

                    });

            for (int k = 0; k < waterSurfaces.Count; k++)
            {
                WaterSurfacePolygon surface = waterSurfaces[k];

                foreach (TriangleNet.Data.Triangle pgon in surface.Triangles)
                {
                    TriangleNet.Data.Triangle ts = pgon;
                    List<Coordinate> vertices = new List<Coordinate>();

                    Point p0 = surface.Points[ts.P0];
                    Point p1 = surface.Points[ts.P1];
                    Point p2 = surface.Points[ts.P2];

                    Coordinate c1 = new Coordinate(p0.X, p0.Y, p0.Z);
                    Coordinate c2 = new Coordinate(p1.X, p1.Y, p1.Z);
                    Coordinate c3 = new Coordinate(p2.X, p2.Y, p2.Z);

                    vertices.Add(c1);
                    vertices.Add(c2);
                    vertices.Add(c3);

                    Polygon polygon = new Polygon(vertices);

                    IFeature fset = fs.AddFeature(polygon);

                    fset.DataRow.BeginEdit();

                    fset.DataRow["MaxElev"] = Math.Max(Math.Max(p0.Z , p1.Z), p2.Z);
                    fset.DataRow["MinElev"] = Math.Min(Math.Min(p0.Z, p1.Z), p2.Z);
                    fset.DataRow["Ensemble"] = ensemble;
                    fset.DataRow["DateTime"] = dateTime;


                    fset.DataRow.EndEdit();
                }
            }

            fs.SaveAs(outputRaster.FullName.Replace(outputRaster.Extension, "_polygon.shp"), true);
            fs.Close();
            fs.Dispose();
        }

#endif

        #endregion

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void CalculateProbabilityOfInundationRaster(List<FileInfo> inputRasters, FileInfo outputRaster)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();

        OSGeo.GDAL.Driver driver = Gdal.GetDriverByName(rasterDriver);

        Dataset newRaster = driver.Create(outputRaster.FullName, xSize, ySize, 1, dataType, new string[] { "TFW=YES", "COMPRESS=LZW" });
        newRaster.GetRasterBand(1).SetNoDataValue(noData);
        newRaster.SetGeoTransform(geoTransformation);
        newRaster.SetProjection(projection);

        Band newRasterBand = newRaster.GetRasterBand(1);

        int stepSize = (int)Math.Floor(xSize * ySize * inputRasters.Count * 0.01);
        int progressMax = xSize * ySize * inputRasters.Count;

        float[] values = new float[xSize * ySize]; //Enumerable.Repeat((float)nodata, xSize * ySize).ToArray();

        for (int i = 0; i < xSize; i++)
        {

            for (int j = 0; j < ySize; j++)
            {
                values[j * xSize + i] = noData;
            }
        }

        float[] depthValues = new float[xSize * ySize];

        int fcount = 0;

        for (int k = 0; k < inputRasters.Count; k++)
        {
            Dataset templateRaster = Gdal.Open(inputRasters[k].FullName, Access.GA_ReadOnly);
            Band band = templateRaster.GetRasterBand(1);
            band.ReadRaster(0, 0, xSize, ySize, depthValues, xSize, ySize, 0, 0);

            Parallel.For(0, xSize, i =>
            //for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    Interlocked.Increment(ref fcount);

                    if (fcount % stepSize == 0)
                    {
                        double temp = fcount * 50.0 / (progressMax * 1.0);

                        lock (Console.Out)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop);
                            Console.Write("Progress => " + temp.ToString("###") + " % ");
                        }

                    }

                    double depth = depthValues[j * xSize + i];


                    if (depth != noData && depth > 0)
                    {
                        float runningAdd = values[j * xSize + i];

                        if (runningAdd == noData)
                        {
                            lock (values)
                            {
                                values[j * xSize + i] = 1.0f;
                            }
                        }
                        else
                        {
                            lock (values)
                            {
                                values[j * xSize + i] = runningAdd + 1.0f;
                            }
                        }
                    }
                }
            }
            );

            templateRaster.Dispose();
            templateRaster = null;
            GC.Collect();
        }

        fcount = 0;
        stepSize = (int)Math.Floor(xSize * ySize * 0.01);
        int count = 0;

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                float value = values[j * xSize + i];

                if (value != noData)
                {
                    float temp = (float)(value / (1.0 * inputRasters.Count));
                    values[j * xSize + i] = temp;
                    fcount++;
                }

                count++;

                if (count % stepSize == 0)
                {
                    double temp = 50.0 + (count * 50.0 / (xSize * ySize * 1.0));
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("Progress => " + temp.ToString("###") + " % ");
                }
            }
        }

        Console.WriteLine("\n");

        newRasterBand.WriteRaster(0, 0, xSize, ySize, values, xSize, ySize, 0, 0);


        if (fcount > 2)
        {
            double min, max, mean, stdev;
            newRasterBand.GetStatistics(0, 1, out min, out max, out mean, out stdev);

            double temp = fcount * 100.0 / (xSize * ySize * 1.0);
            Console.WriteLine("\t" + temp.ToString("###.00") + " %  of pixels were inundated ! \n");
        }

        newRasterBand.FlushCache();
        newRaster.FlushCache();

        newRaster.Dispose();
        newRaster = null;

        driver.Dispose();
        driver = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private static KeyValuePair<bool, DateTime> GetiRODSDataObjectDate(string path)
    {
        DateTime dateTime = DateTime.MaxValue;
        try
        {
            string[] cols = path.Split(iPathDel, StringSplitOptions.None);
            cols = cols[cols.Length - 1].Split(itDel, StringSplitOptions.None);
            cols = cols[cols.Length - 2].Split(ifDel, StringSplitOptions.None);
            string dt = cols[0];
            int hours = int.Parse(cols[cols.Length - 1]);
            int year = int.Parse(dt.Substring(0, 4));
            int month = int.Parse(dt.Substring(4, 2));
            int day = int.Parse(dt.Substring(6, 2));

            dateTime = new DateTime(year, month, day);
            dateTime = dateTime.AddHours(hours);
        }
        catch (Exception ex)
        {
            return new KeyValuePair<bool, DateTime>(false, dateTime);
        }

        return new KeyValuePair<bool, DateTime>(true, dateTime);
    }

    private static KeyValuePair<bool, string> GetiRODSDataObjectEnsembleID(string path)
    {

        try
        {
            string[] cols = path.Split(iPathDel, StringSplitOptions.None);
            cols = cols[cols.Length - 1].Split(ifDel, StringSplitOptions.None);

            return new KeyValuePair<bool, string>(true, cols[cols.Length - 2]);
        }
        catch (Exception ex)
        {
            ex.ToString();
        }

        return new KeyValuePair<bool, string>(false, "");
    }

    private static string GetLocalEnsembleID(string path)
    {
        //path = path.Replace("warning_points_", "");

        string[] cols = path.Split(lPathDel, StringSplitOptions.None);
        cols = cols[cols.Length - 1].Split(ifDel, StringSplitOptions.None);

        return cols[cols.Length - 3];
    }

    private static DateTime GetLocalFileDate(FileInfo file)
    {
        string[] dateparsec = file.FullName.Split(ifDel, StringSplitOptions.RemoveEmptyEntries);
        string toparse = dateparsec[dateparsec.Length - 2].ToLower().Replace("z", "").Replace("t", "");
        DateTime dt = DateTime.ParseExact(toparse, "yyyyMMddHHmm", CultureInfo.InvariantCulture);
        return dt;
    }

    private string GetFileExtension(string path)
    {
        //path = path.Replace("warning_points_", "");
        string[] cols = path.Split(iPathDel, StringSplitOptions.None);
        cols = cols[cols.Length - 1].Split(ifDel, StringSplitOptions.None);
        return cols[cols.Length - 1];
    }

    private void StartPythonSession()
    {
        PythonEngine.Initialize();
        enginePtr = PythonEngine.AcquireLock();

        iRODSClientModule = PythonEngine.ImportModule("iRODSClient");
        uploadToGeoserver = PythonEngine.ImportModule("UploadToGeoserver");
    }

    private void EndPythonSession()
    {
        if (iRODSClientModule != null)
        {
            iRODSClientModule.Dispose();
            iRODSClientModule = null;
        }

        if (uploadToGeoserver != null)
        {
            uploadToGeoserver.Dispose();
            uploadToGeoserver = null;
        }

        PythonEngine.ReleaseLock(enginePtr);
        PythonEngine.Shutdown();
    }

    #endregion functions
}

public class EnsembleForecastFile
{
    string path;
    DateTime forecastDate;

    [XmlAttribute]
    public DateTime ForecastDate
    {
        get { return forecastDate; }
        set { forecastDate = value; }
    }

    public string Path
    {
        get { return path; }
        set { path = value; }
    }


}
