using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class GFeature
{
    List<Point> points;
    Extent extent;

    public GFeature()
    {
        points = new List<Point>();
        extent = new Extent();
    }

    public Extent Extent
    {
        get { return extent; }
        set { extent = value; }
    }

    public virtual bool Contains(ref GFeature feature)
    {
        if (extent.ContainsExtent(ref feature))
            return true;
        return false;
    }
}


class Extent
{

    double minX = 0.0, maxX = 0.0, minY = 0.0, maxY = 0.0, minZ = 0, maxZ = 0;


    public Extent()
    {

    }

    public Extent(double minX = 0.0, double maxX = 0.0, double minY = 0.0, double maxY = 0.0)
    {
        this.minX = minX; this.maxX = maxX; this.minY = minY; this.maxY = maxY;
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
        get { return minX; }
        set { minX = value; }
    }

    public double MaxY
    {
        get { return maxX; }
        set { maxX = value; }
    }

    public double MaxZ
    {
        get { return maxZ; }
        set { maxZ = value; }
    }

    public double MinZ
    {
        get { return minZ; }
        set { minZ = value; }
    }

    public bool ContainsExtent(ref GFeature feature)
    {
        if (minX <= feature.Extent.MinX &&
           maxX >= feature.Extent.MaxX &&
           minY <= feature.Extent.MinY &&
           maxY >= feature.Extent.MaxY && 
           minZ <= feature.Extent.MinZ &&
           maxZ >= feature.Extent.MaxZ)
        {
            return true;
        }

        return false;
    }

    public bool ContainsExtent(ref Extent extent)
    {
        if (minX <= extent.MinX &&
           maxX >= extent.MaxX &&
           minY <= extent.MinY &&
           maxY >= extent.MaxY &&
           minZ <= extent.MinZ &&
           maxZ >= extent.MaxZ)
        {
            return true;
        }

        return false;
    }
}


class QuadTreeNode
{
    protected int maxObjects = 5;
    protected int level;
    private List<GFeature> associatedGFeatures;
    private QuadTreeNode[] nodes;
    private Extent extent;
    int maxDepth;
   
    public QuadTreeNode(Extent extent)
    {
        associatedGFeatures = new List<GFeature>();
        nodes = new QuadTreeNode[4];
    }

    public int Level
    {
        get { return level; }
    }

    public QuadTreeNode[] Nodes
    {
        get { return nodes; }
    }

    public List<GFeature> AssociatedGFeatures
    {
        get { return associatedGFeatures; }
    }

    public Extent Extent
    {
        get { return extent; }
    }

    public void ClearAssociatedGFeatures()
    {
        associatedGFeatures.Clear();
    }

    private void Split()
    {
        if (level < maxDepth)
        {
            double midX = (extent.MaxX + extent.MinX) / 2.0;
            double midY = (extent.MaxY + extent.MinY) / 2.0;

            nodes[0] = new QuadTreeNode(new Extent(extent.MinX, midX, midY, extent.MaxY))
            {
                maxObjects = maxObjects,
                maxDepth = this.maxDepth,
                level = level + 1
            };

            nodes[1] = new QuadTreeNode(new Extent(midX, extent.MaxX, midY, extent.MaxY))
            {
                maxObjects = maxObjects,
                maxDepth = this.maxDepth,
                level = level + 1
            };

            nodes[2] = new QuadTreeNode(new Extent(extent.MinX, midX, extent.MinY, midY))
            {
                maxObjects = maxObjects,
                maxDepth = this.maxDepth,
                level = level + 1
            };

            nodes[3] = new QuadTreeNode(new Extent(midX, extent.MaxX, extent.MinY, midY))
            {
                maxObjects = maxObjects,
                maxDepth = this.maxDepth,
                level = level + 1
            };
        }
    }

    public bool InsertFeature(ref GFeature feature)
    {


        return false;
    }

    public List<GFeature> FindFeatures()
    {
        List<GFeature> features = new List<GFeature>();


        return features;
    }
}


class QuadTree : QuadTreeNode
{
    int maxDepth;

    public QuadTree(Extent extent, int maxDepth = 10, int maxObjects = 5)
        :base(extent)
    {
        this.maxObjects = maxObjects;
        this.maxDepth = maxDepth;
        this.level = 1;
    }

    public int MaxDepth
    {
        get { return maxDepth; }
    }
}


