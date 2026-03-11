using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGVManagement.MapPaint
{
    public class AutomaticClass
    {
    }
}

public class Node
{
    public double[] Axis { get; set; }
    public int LineOrArc { get; set; }
    public double[] NextAxis { get; set; }
    public int IsDirChange { get; set; }
    public double[] ArcCentreAxis { get; set; }
    public double ArcRadius { get; set; }
    public Node Next { get; set; }
}

public class Node1
{
    public int AxisR { get; set; }
    public int AxisC { get; set; }
    public double Cost { get; set; }
    public Node1 Parent { get; set; }
    public Node1 Next { get; set; }
}

public class KDTree
{
    public Node1 LeftLeaf { get; set; }
    public int LeftSize { get; set; }
    public int LeftRCentre { get; set; }
    public Node1 RightLeaf { get; set; }
    public int RightSize { get; set; }
    public int RightRCentre { get; set; }
    public KDTree LeftTree { get; set; }
    public KDTree RightTree { get; set; }
    public int Dim { get; set; }
    public double Border { get; set; }
}

public class Node2
{
    public Node1 Vertex { get; set; }
    public Node2 Next { get; set; }
}

public class PathPoint
{
    public int Row { get; set; }
    public int Colume { get; set; }
    public int Time { get; set; }
    public PathPoint Next { get; set; }
}

public class Node5
{
    public int R { get; set; }
    public int C { get; set; }
    public int Flag { get; set; }
    public int T { get; set; }
    public Node5 Next { get; set; }
}

public class Axis
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class Node6
{
    public Axis OriginalPoint { get; set; }
    public Axis TangentPoint1 { get; set; }
    public Axis TangentPoint2 { get; set; }
    public Axis Center { get; set; }
    public double Radius { get; set; }
    public double IsArcFree { get; set; }
    public Node6 Next { get; set; }

}

public class Node7
{
    public int[] Axis { get; set; }
    public int LineOrArc { get; set; }
    public int[] ArcCentreAxis { get; set; }
    public int ArcRadius { get; set; }
    public Node7 Next { get; set; }
}

public class Program
{
    public static Node6 CreateNode(Axis originalPoint, Axis tangentPoint1, Axis tangentPoint2, Axis center, double radius, double isArcFree)
    {
        Node6 newNode = new Node6
        {
            OriginalPoint = originalPoint,
            TangentPoint1 = tangentPoint1,
            TangentPoint2 = tangentPoint2,
            Center = center,
            Radius = radius,
            IsArcFree = isArcFree,
            Next = null,
        };

        return newNode;
    }

    public static void PrintList(Node6 head)
    {
        Node6 current = head;
        int nodeIndex = 1;

        while (current != null)
        {
            Console.WriteLine($"Node6 {nodeIndex}:");
            Console.WriteLine($"Original axis: ({current.OriginalPoint.X}, {current.OriginalPoint.Y})");
            Console.WriteLine($"Tangent axis 1: ({current.TangentPoint1.X}, {current.TangentPoint1.Y})");
            Console.WriteLine($"Tangent axis 2: ({current.TangentPoint2.X}, {current.TangentPoint2.Y})");
            Console.WriteLine($"Center: ({current.Center.X}, {current.Center.Y})");
            Console.WriteLine($"Radius: {current.Radius}\n");
            Console.WriteLine($"IsArcFree: {current.IsArcFree}\n");

            current = current.Next;
            nodeIndex++;
        }
    }

    //public static void FreeNodeList(NodeList L)
    //{
    //    NodeList L1;
    //    while (L != null)
    //    {
    //        L1 = L.Next;
    //        // Assuming L is not a real Node instance as it was in the original C code.
    //        // In C#, we typically use 'new Node()' to create instances.
    //        // So, you might need to adjust this part based on the actual structure.
    //        // Also, C# has automatic garbage collection, so manual freeing of memory is not necessary.
    //        // The memory will be automatically reclaimed when there are no more references to the object.
    //        // Example: 'L = null;' would be sufficient in most cases.
    //        L = L1;
    //    }
    //}


}
