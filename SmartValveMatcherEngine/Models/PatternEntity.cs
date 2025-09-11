using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartValveMatcherEngine.Models;

public class PatternEntity
{
    public string Type { get; set; } = "";
    public double Length { get; set; } = 0;
    public double Radius { get; set; } = 0;
    public double StartAngle { get; set; } = 0;
    public double EndAngle { get; set; } = 0;
    public int VertexCount { get; set; } = 0;
    public Point2D Position { get; set; } = new Point2D(); // Add position for geometric comparison
}

public class Point2D
{
    public double X { get; set; }
    public double Y { get; set; }
}

