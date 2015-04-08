using System;
using System.Collections.Generic;
using MapHelper;

namespace Robot
{
    public static class RobotHelper
    {
        private const int halfFieldSize = 150;
        private const int squadSize = 50;

        public static readonly Dictionary<Direction, Point> squareEdges = new Dictionary<Direction, Point>
        {
            {Direction.Up, new Point(-squadSize/2, 0)},
            {Direction.Down, new Point(-squadSize/2, -squadSize)},
            {Direction.Left, new Point(-squadSize, -squadSize/2)},
            {Direction.Right, new Point(0, -squadSize/2)},
            {Direction.No, new Point(-squadSize/2, -squadSize/2)}
        };

        public static double GetNormalAngle(double angle)
        {
            angle = angle%360;
            if (angle < -180) angle += 360;
            if (angle > 180) angle -= 360;
            return angle;
        }

        public static Point GetAbsoluteCoordinateEdgeInSquard(Point point, Direction direction)
        {
            return new Point(point.X * squadSize + squareEdges[direction].X - halfFieldSize,
                point.Y * -squadSize + squareEdges[direction].Y + halfFieldSize);
        }

        public static double VectorLength(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
