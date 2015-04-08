using System;
using System.Collections.Generic;
using System.Linq;
using BEPUphysics.Constraints.SingleEntity;
using ClientBase;
using CVARC.Basic.Controllers;
using CVARC.Basic.Sensors;
using MapHelper;
using RepairTheStarship.Sensors;
using Map = MapHelper.Map;

namespace Robot
{
    internal enum DetailType
    {
        Red,
        Green,
        Blue
    }


    internal class Robot
    {
        private const double maxSpeed = 50;
        private const double squadSize = 19;

        private static readonly Dictionary<string, DetailType> detailTypeDictionary = new Dictionary<string, DetailType>
        {
            {"GreenDetail", DetailType.Green},
            {"RedDetail", DetailType.Red},
            {"BlueDetail", DetailType.Blue}
        };

        private static readonly Dictionary<DetailType, HashSet<string>> socketWallsDictionary = new Dictionary
            <DetailType, HashSet<string>>
        {
            {DetailType.Red, new HashSet<string> {"VerticalRedSocket", "HorizontalRedSocket"}},
            {DetailType.Green, new HashSet<string> {"VerticalGreenSocket", "HorizontalGreenSocket"}},
            {DetailType.Blue, new HashSet<string> {"VerticalBlueSocket", "HorizontalBlueSocket"}}
        };

        public Robot(Server<PositionSensorsData> server, Map map)
        {
            Server = server;
            this.map = map;
        }

        private Server<PositionSensorsData> Server { get; set; }

        private Map map { get; set; }

        public PositionData Info
        {
            get { return map.CurrentPosition; }
        }

        private int Id
        {
            get { return Info.RobotNumber; }
        }

        private double Angle
        {
            get { return Info.Angle; }
        }

        public Point Coordinate
        {
            get { return new Point((int) Info.X, (int) Info.Y); }
        }

        public void TurnTo(double angle)
        {
            var resAngle = RobotHelper.GetNormalAngle(angle - Angle);
            PositionSensorsData sensorsData = null;
            if (Math.Abs(resAngle) < 0.5) return;
            sensorsData =
                Server.SendCommand(new Command
                {
                    AngularVelocity = AIRLab.Mathematics.Angle.FromGrad(90*Math.Sign(resAngle)),
                    Time = Math.Abs(resAngle)/90
                });
            map.Update(sensorsData);
        }


        public PositionSensorsData MoveTo(Direction[] directions)
        {
            PositionSensorsData sensorsData = null;
            for (var i = 0; i < directions.Length; i++)
            {
                if (!RobotHelper.squareEdges.ContainsKey(directions[i]))
                    Enum.TryParse(directions[i].ToString().Split(',')[0], out directions[i]); // Отрезаем 1-ую часть скреплённых enum
                MoveToEdge(directions[i]);
                var directionVelocity = 1;
                if (Math.Abs(RobotHelper.GetNormalAngle(directions[i].ToAngle() - Info.Angle)) > 115)
                {
                    directionVelocity = -1;
                    TurnTo(directions[i].ToAngle() + 180);
                }
                else
                    TurnTo(directions[i].ToAngle());
                sensorsData = Server.SendCommand(new Command {LinearVelocity = maxSpeed*directionVelocity, Time = 0.3});
                map.Update(sensorsData);
            }
            if (sensorsData == null) sensorsData = Server.SendCommand(new Command {LinearVelocity = 0, Time = 0.1});
            map.Update(sensorsData);
            return sensorsData;
        }

        public PositionSensorsData Take(Point target)
        {
            PositionSensorsData sensorsData = null;
            var angle = Math.Atan2(target.Y - Coordinate.Y, target.X - Coordinate.X)*180/Math.PI;
            TurnTo(angle);
            if (RobotHelper.VectorLength(target, Coordinate) > squadSize)
                Server.SendCommand(new Command
                {
                    LinearVelocity = maxSpeed,
                    Time = (RobotHelper.VectorLength(target, Coordinate) - squadSize)/maxSpeed
                });
            Server.SendCommand(new Command {Action = CommandAction.Grip, Time = 1});
            sensorsData = Server.SendCommand(new Command { LinearVelocity = -maxSpeed, Time = 0.2 });
            map.Update(sensorsData);
            return sensorsData;
        }

        public PositionSensorsData TakeClosestDetail(HashSet<string> detailsType, out DetailType detailType)
        {
            Console.WriteLine("Еду к ближайшей детали");
            detailType = DetailType.Red;
            PositionSensorsData sensorsData = null;
            var pathTuple = map
                .Details.Select(
                    detail =>
                        Tuple.Create(detail, PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                            detail.DiscreteCoordinate)))
                .OrderBy(tuple => tuple.Item2.Length)
                .FirstOrDefault();

            if (pathTuple == null)
                Server.Exit();
            if (ReferenceEquals(null, pathTuple.Item2))
                Server.Exit();
            var path = pathTuple.Item2;
            var target = pathTuple.Item1.AbsoluteCoordinate;
            detailType = detailTypeDictionary[pathTuple.Item1.Type];
            // поиск пути к ближайшей детали
            MoveTo(path.Take(path.Length - 1).ToArray());
            sensorsData = Take(target);
            return sensorsData;
        }

        public PositionSensorsData MoveToClosestWall(DetailType detailType)
        {
            Console.WriteLine("Еду к нужной стене");
            PositionSensorsData sensorsData = null;
            var path = map.Walls
                .Where(wall => socketWallsDictionary[detailType].Contains(wall.Type))
                .SelectMany(wall =>
                {
                    var walls = new List<Direction[]>();
                    walls.Add(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                        wall.DiscreteCoordinate));
                    if (wall.Type.StartsWith("Vertical"))
                    {
                        if (wall.DiscreteCoordinate.X > 1)
                            walls.Add(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                                new Point(wall.DiscreteCoordinate.X - 1, wall.DiscreteCoordinate.Y)));
                    }
                    else
                    {
                        if (wall.DiscreteCoordinate.Y > 1)
                            walls.Add(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                                new Point(wall.DiscreteCoordinate.X, wall.DiscreteCoordinate.Y - 1)));
                    }
                    return walls;
                })
                .OrderBy(tuple => tuple.Length)
                .FirstOrDefault();
            MoveTo(path);
            MoveToEdge();
            Server.SendCommand(new Command {LinearVelocity = -maxSpeed, Time = 0.2});
            sensorsData = Server.SendCommand(new Command {Action = CommandAction.Release, Time = 1});
            map.Update(sensorsData);
            return sensorsData;
        }

        public PositionSensorsData MoveToEdge(Direction direction = Direction.No)
        {
            PositionSensorsData sensorsData = null;
            var robotPosition = map.GetDiscretePosition(Coordinate.X, Coordinate.Y);
            if (!RobotHelper.squareEdges.ContainsKey(direction))
                Enum.TryParse(direction.ToString().Split(',')[0], out direction);
            var target = RobotHelper.GetAbsoluteCoordinateEdgeInSquard(robotPosition, direction);
            var directionVelocity = 1;
            var angle = Math.Atan2(target.Y - Coordinate.Y, target.X - Coordinate.X)*180/Math.PI;
            if (Math.Abs(RobotHelper.GetNormalAngle(angle - Info.Angle)) > 115)
            {
                directionVelocity = -1;
                TurnTo(angle + 180);
            }
            else
                TurnTo(angle);
            sensorsData =
                Server.SendCommand(new Command
                {
                    LinearVelocity = maxSpeed*directionVelocity,
                    Time = (RobotHelper.VectorLength(target, Coordinate))/maxSpeed
                });
            map.Update(sensorsData);
            return sensorsData;
        }
    }
}
