using Autodesk.AutoCAD.Geometry;
using System.Drawing.Drawing2D;



namespace SmartValveMatcherEngine.Models;

public class ValvePattern
{
    public string ValveType { get; set; } = string.Empty;

    public List<LineData> Lines { get; set; } = new();
    public List<CircleData> Circles { get; set; } = new();
    public List<ArcData> Arcs { get; set; } = new();
    public List<PolylineData> Polylines { get; set; } = new();
    public List<SolidData> Solids { get; set; } = new(); //
    public List<HatchData> Hatches { get; set; } = new();   //
    public List<LeaderData> Leaders { get; set; } = new();

}

public class LineData
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
}

public class CircleData
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
}

public class ArcData
{
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double Radius { get; set; }
    public double StartAngle { get; set; }
    public double EndAngle { get; set; }
}

public class PolylineData
{
}

public class SolidData
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double X3 { get; set; }
    public double Y3 { get; set; }
}

public class HatchData
{
    public string PatternName { get; set; } = "";
    public double CenterX { get; set; }
    public double CenterY { get; set; }
}

public class LeaderData
{
    
}

