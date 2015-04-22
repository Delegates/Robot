using System;
using System.Collections.Generic;
using System.Linq;
using AIRLab.Mathematics;
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

        public static readonly Dictionary<DetailType, List<Point>> knownWalls = new Dictionary<DetailType, List<Point>>
        {
            {DetailType.Red, new List<Point>()},
            {DetailType.Green, new List<Point>()},
            {DetailType.Blue,  new List<Point>()}
        };

        public static readonly List<Point> knownDetails = new List<Point>();
     //   {
      //      {DetailType.Red, new List<Point>()},
     //       {DetailType.Green, new List<Point>()},
     //       {DetailType.Blue,  new List<Point>()}
      //  };

        private static readonly Dictionary<string, DetailType> detailTypeDictionary = new Dictionary<string, DetailType>
        {
            {"GreenDetail", DetailType.Green},
            {"RedDetail", DetailType.Red},
            {"BlueDetail", DetailType.Blue}
        };

        private static readonly Dictionary<string, DetailType> wallTypeDictionary = new Dictionary<string, DetailType>
        {    
            {"VerticalRedSocket", DetailType.Red},
            {"HorizontalRedSocket", DetailType.Red},
            {"VerticalBlueSocket", DetailType.Blue},
            {"HorizontalBlueSocket", DetailType.Blue},
            {"VerticalGreenSocket", DetailType.Green},
            {"HorizontalGreenSocket", DetailType.Green}
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

        public Map map { get; set; }

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

        private bool Danger = false;

        public Point Coordinate
        {
            get { return new Point((int) Info.X, (int) Info.Y); }
        }

        public void MapUpdate(PositionSensorsData sensorData)
        {
            map.Update(sensorData);            
            foreach (var e in map.Walls)
            {
                if(!wallTypeDictionary.ContainsKey(e.Type))
                    continue;
                var color = wallTypeDictionary[e.Type];
                if (!knownWalls[color].Any(p => p.X == e.DiscreteCoordinate.X && p.Y == e.DiscreteCoordinate.Y))
                {
                    knownWalls[color].Add(e.DiscreteCoordinate);
                }
            }
            foreach (var e in map.Details)
            {
                if (!knownDetails.Any(p => p.X == e.DiscreteCoordinate.X && p.Y == e.DiscreteCoordinate.Y))
                    knownDetails.Add(e.DiscreteCoordinate);
            }
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
            MapUpdate(sensorsData);
        }

        public void InDanger()
        {
            if (Danger) return;
            
            PositionSensorsData sensorsData = null;
            double time = 0;
            var path = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition), map.GetDiscretePosition(map.OpponentPosition));
            while (path.Length <= 1)
            {
                Danger = true;
                if (time > 3)
                {
                    var robotDiscret = map.GetDiscretePosition(Coordinate.X, Coordinate.Y);
                    var direction = map.AvailableDirectionsByCoordinates[robotDiscret.X, robotDiscret.Y];
                    var directions = (RobotHelper.squareEdges.ContainsKey(direction)) ? new List<Direction> { direction } : ((direction.ToString() == "All") ? new[] { "Rigth", "Left", "Up", "Down" } : direction.ToString().Split(','))
                        .Select(str =>
                        {                            
                            Direction y;
                            Enum.TryParse(str, out y);
                            return y;
                        })
                        .ToList();
                    Console.WriteLine(direction.ToString());
                    Console.WriteLine(RobotHelper.squareEdges.ContainsKey(direction));
                    double distance = RobotHelper.VectorLength(map.CurrentPosition, map.OpponentPosition);
                    //foreach (var dir in directions)
                    //{
                    //    if (dir == Direction.No) continue;
                    //    var futurePosition = new Point((int)(map.CurrentPosition.X + 15 * Math.Cos(dir.ToAngle() * Math.PI / 180)),(int)(map.CurrentPosition.Y + 15 * Math.Sin(dir.ToAngle() * Math.PI / 180)));
                    //    Console.WriteLine("угол " + Direction.Down.ToAngle());
                    //    Console.WriteLine(RobotHelper.VectorLength(futurePosition, new Point((int)map.OpponentPosition.X, (int)map.OpponentPosition.Y)) - RobotHelper.VectorLength(map.CurrentPosition, map.OpponentPosition));
                    //if (RobotHelper.VectorLength(futurePosition, new Point((int)map.OpponentPosition.X, (int)map.OpponentPosition.Y)) > RobotHelper.VectorLength(map.CurrentPosition, map.OpponentPosition))
                    //{
                    //    MoveTo(new Direction[] { dir });
                    //    Danger = false;
                    //    return;
                    //}
                    Console.WriteLine("до");
                    var tuple = directions
                                    .Where(dir => dir != Direction.No)
                                    .Select(dir => Tuple.Create(RobotHelper.VectorLength(new Point((int)(map.CurrentPosition.X + 15 * Math.Cos(dir.ToAngle() * Math.PI / 180)), (int)(map.CurrentPosition.Y + 15 * Math.Sin(dir.ToAngle() * Math.PI / 180))), new Point((int)map.OpponentPosition.X, (int)map.OpponentPosition.Y)) - distance, dir))
                                    .Max();
                    Console.WriteLine("после");
                    if (tuple.Item1>0)
                    {
                        //MoveTo(new Direction[] { tuple.Item2 });
                        TurnTo(tuple.Item2.ToAngle());
                        sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
                        Danger = false;
                        return;
                    }
                        
                    //}
                   
                        //MoveTo(new Direction[] 
                        //    {  
                        //        directions
                        //            .Where(dir => dir != Direction.No)
                        //            .Select(dir => Tuple.Create(RobotHelper.VectorLength(new Point((int)(map.CurrentPosition.X + 15 * Math.Cos(dir.ToAngle() * Math.PI / 180)), (int)(map.CurrentPosition.Y + 15 * Math.Sin(dir.ToAngle() * Math.PI / 180))), new Point((int)map.OpponentPosition.X, (int)map.OpponentPosition.Y)) - RobotHelper.VectorLength(map.CurrentPosition, map.OpponentPosition), dir))
                        //            .Max()
                        //            .Item2 
                        //    }
                        //);
                        Console.WriteLine(directions
                                        .Where(dir => dir != Direction.No)
                                        .Select(dir => Tuple.Create(RobotHelper.VectorLength(new Point((int)(map.CurrentPosition.X + 15 * Math.Cos(dir.ToAngle() * Math.PI / 180)), (int)(map.CurrentPosition.Y + 15 * Math.Sin(dir.ToAngle() * Math.PI / 180))), new Point((int)map.OpponentPosition.X, (int)map.OpponentPosition.Y)) - RobotHelper.VectorLength(map.CurrentPosition, map.OpponentPosition), dir))
                                        .Max()
                                        .Item2);
                    time = 0;                    
                }
                sensorsData = Server.SendCommand(new Command { LinearVelocity = 0, Time = 0.2 });
                time += 0.2;
                MapUpdate(sensorsData);
                path = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition), map.GetDiscretePosition(map.OpponentPosition));
            }
            Danger = false;

        }
        public PositionSensorsData MoveTo(Point target)
        {
            Console.WriteLine("уже еду ");
            PositionSensorsData sensorsData = null;
            if (target.X == map.GetDiscretePosition(map.CurrentPosition).X &&
                target.Y == map.GetDiscretePosition(map.CurrentPosition).Y)
            {
                Console.WriteLine("стипан пидр ");
                sensorsData = Server.SendCommand(new Command {LinearVelocity = 0, Time = 0.1});
                MapUpdate(sensorsData);
                return sensorsData;
            }

            var directions = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition), target)[0];
        //    Console.WriteLine(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition), target).Count());
         //   foreach (var e in PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition), target))
        //    {
        //        Console.WriteLine(e);
        //    }
         //   foreach (var e in map.Walls)
         //   {
         //       Console.WriteLine("стена " + e);
                
         //   }
           // for (var i = 0; i < directions.Length; i++)
          //  {
                if (!RobotHelper.squareEdges.ContainsKey(directions))
                    Enum.TryParse(directions.ToString().Split(',')[0], out directions); // Отрезаем 1-ую часть скреплённых enum
                MoveToEdge(directions);
                var directionVelocity = 1;
                if (Math.Abs(RobotHelper.GetNormalAngle(directions.ToAngle() - Info.Angle)) > 115)
                {
                    directionVelocity = -1;
                    TurnTo(directions.ToAngle() + 180);
                }
                else
                    TurnTo(directions.ToAngle());
                InDanger();
                sensorsData = Server.SendCommand(new Command {LinearVelocity = maxSpeed*directionVelocity, Time = 0.3});
                MapUpdate(sensorsData);
           // }
           // if (sensorsData == null) sensorsData = Server.SendCommand(new Command {LinearVelocity = 0, Time = 0.1});
         //   MapUpdate(sensorsData);
         //   if (target.X != map.GetDiscretePosition(map.CurrentPosition).X &&
        //        target.Y != map.GetDiscretePosition(map.CurrentPosition).Y)
        //    {
         //       sensorsData = MoveTo(target);
             //   MapUpdate(sensorsData);
         //   }
                return MoveTo(target);
        }

        public PositionSensorsData Take(Point target)
        {
            PositionSensorsData sensorsData = null;
            var angle = Math.Atan2(target.Y - Coordinate.Y, target.X - Coordinate.X)*180/Math.PI;
            TurnTo(angle);
            if (RobotHelper.VectorLength(target, Coordinate) > squadSize)
            {
                InDanger();
                Server.SendCommand(new Command
                {
                    LinearVelocity = maxSpeed,
                    Time = (RobotHelper.VectorLength(target, Coordinate) - squadSize) / maxSpeed
                });
            }
            Server.SendCommand(new Command {Action = CommandAction.Grip, Time = 1});
            sensorsData = Server.SendCommand(new Command { LinearVelocity = -maxSpeed, Time = 0.2 });
            MapUpdate(sensorsData);
            return sensorsData;
        }

        public PositionSensorsData TakeClosestDetail(HashSet<string> detailsType, out DetailType detailType)
        {
            while (!map.Details.Any())
            {
                Console.WriteLine("еду хуй знает куда");
                if (knownDetails.Count != 0)
                {                  
                    MoveTo(knownDetails.First());                 
                }
                else
                {
                    var rnd = new Random(DateTime.Now.Millisecond);
                  //  Console.WriteLine("еду хуй знает куда"+);
                    MoveTo(new Point(rnd.Next(1, 6), rnd.Next(1, 4)));
                    
                }
            }
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
            //if (path.Last() != Direction.No)
             //   MoveTo(map.GetDiscretePosition(new PositionData(new Frame3D(target.X - RobotHelper.squareEdges[path.Last()].X * 2, target.Y - RobotHelper.squareEdges[path.Last()].Y * 2, 0))));
                MoveTo(map.GetDiscretePosition(new PositionData(new Frame3D(target.X, target.Y, 0))));
            sensorsData = Take(target);
         
            return sensorsData;
        }

        public PositionSensorsData MoveToClosestWall(DetailType detailType)
        {
            while (!map.Walls.Any(wall => socketWallsDictionary[detailType].Contains(wall.Type)))
            {
                if (knownWalls[detailType].Count != 0)
                {
                    MoveTo(knownWalls[detailType].First());
                    knownWalls[detailType].Remove(knownWalls[detailType].First());
                }
                else
                {
                    var rnd = new Random(DateTime.Now.Millisecond);
                    MoveTo(new Point(rnd.Next(1, 6), rnd.Next(1, 4)));
                }
            }

            Console.WriteLine("Еду к нужной стене");
            PositionSensorsData sensorsData = null;
            var path = map.Walls
                .Where(wall => socketWallsDictionary[detailType].Contains(wall.Type))
                .SelectMany(wall =>
                {
                    var walls = new List<Tuple<Direction[],Point>>();
                    walls.Add(Tuple.Create(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                        wall.DiscreteCoordinate), wall.DiscreteCoordinate));
                    if (wall.Type.StartsWith("Vertical"))
                    {
                        if (wall.DiscreteCoordinate.X > 1)
                            walls.Add(Tuple.Create(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                                new Point(wall.DiscreteCoordinate.X - 1, wall.DiscreteCoordinate.Y)), new Point(wall.DiscreteCoordinate.X-1, wall.DiscreteCoordinate.Y)));
                    }
                    else
                    {
                        if (wall.DiscreteCoordinate.Y > 1)
                            walls.Add(Tuple.Create(PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                                new Point(wall.DiscreteCoordinate.X, wall.DiscreteCoordinate.Y - 1)),  new Point(wall.DiscreteCoordinate.X, wall.DiscreteCoordinate.Y - 1)));
                    }
                    return walls;
                })
                .OrderBy(tuple => tuple.Item1.Length)
                .FirstOrDefault();
            Console.WriteLine("Сейчас поеду");

            MoveTo(path.Item2);
            MoveToEdge();
            Server.SendCommand(new Command {LinearVelocity = -maxSpeed, Time = 0.2});
            knownDetails.RemoveAt(0);
            sensorsData = Server.SendCommand(new Command {Action = CommandAction.Release, Time = 1});
            MapUpdate(sensorsData);
            return sensorsData;
        }

        public PositionSensorsData MoveToEdge(Direction direction = Direction.No)
        {
            PositionSensorsData sensorsData = null;
            var robotPosition = map.GetDiscretePosition(Coordinate.X, Coordinate.Y);
            InDanger();
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
                        LinearVelocity = maxSpeed * directionVelocity,
                        Time = (RobotHelper.VectorLength(target, Coordinate)) / maxSpeed
                    });
            
            MapUpdate(sensorsData);
            return sensorsData;
        }
    }
}
