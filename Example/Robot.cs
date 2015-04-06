using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using AIRLab.Mathematics;
using ClientBase;
using CVARC.Basic.Controllers;
using CVARC.Basic.Sensors;
using FarseerPhysics;
using MapHelper;
using RepairTheStarship.Sensors;

namespace Robot
{
    enum DetailType
    {
        Red, Green, Blue
    }
    class Robot
    {

        public Robot(Server<PositionSensorsData> server, PositionData info)
        {
            Info = info;
            Server = server;
        }
        private ClientBase.Server<PositionSensorsData> Server { get; set; }
        public PositionData Info { get; private set; }

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
            get { return new Point((int)Info.X, (int)Info.Y); }
        }

        public void TurnTo(double angle)
        {
            Console.WriteLine("Поворачиваю");
            var resAngle = GetNormalAngle(angle - Angle);
            PositionSensorsData sensorsData = null;
            if (resAngle < -180) resAngle += 360;
            if (resAngle > 180) resAngle -= 360;           
            Console.WriteLine("На угол "+angle + " с этого " + Angle);
            if (Math.Abs(resAngle) < 0.5) return;
            sensorsData =
                Server.SendCommand(new Command
                {
                    AngularVelocity = AIRLab.Mathematics.Angle.FromGrad(90 * Math.Sign(resAngle)),
                    Time = Math.Abs(resAngle) / 90
                });
            Info = sensorsData.Position.PositionsData[Id];           
        }

        public double GetNormalAngle(double angle)
        {
            return angle % 360;
        }
        
        public PositionSensorsData MoveTo(Direction[] directions,MapHelper.Map map)
        {
            var robotDiscretePosition = map.GetDiscretePosition(map.CurrentPosition);
            var directionCommands = new Dictionary<Direction, Func<Point>>
            {
                {Direction.Up, () => new Point(robotDiscretePosition.X, robotDiscretePosition.Y - 1)},
                {Direction.Right, () => new Point(robotDiscretePosition.X + 1, robotDiscretePosition.Y)},
                {Direction.Down, () => new Point(robotDiscretePosition.X, robotDiscretePosition.Y + 1)},
                {Direction.Left, () => new Point(robotDiscretePosition.X - 1, robotDiscretePosition.Y)}
            };
            Console.WriteLine("Еду");
            PositionSensorsData sensorsData = null;
            for (int i = 0; i < directions.Length; i++)
            {
                Console.WriteLine(robotDiscretePosition + "  = " + map.GetDiscretePosition(map.CurrentPosition));
                if (robotDiscretePosition.X != map.GetDiscretePosition(map.CurrentPosition).X && robotDiscretePosition.Y != map.GetDiscretePosition(map.CurrentPosition).Y)
                {
                   TurnTo(directions[i].ToAngle());
                   sensorsData = Server.SendCommand(new Command { Action = CommandAction.Release, Time = 1 });   
                   return sensorsData;                   
                }
                //if (i != 0 && directions[i] != directions[i-1])
                //{
                //    TurnTo(Math.Sign(directions[i].ToAngle()) * 155);
                //    sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 0.7 });
                //    //TurnTo(Math.Sign(directions[i].ToAngle()) * 125);
                //    //sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 0.3 });
                //    MoveToMiddle(map);
                //}
                //else 
                    TurnTo(directions[i].ToAngle());
                //if(i < directions.Length - 1 && directions[i+1] != directions[i])
                //    sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 0.6 });
                //else
                    sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
                robotDiscretePosition = directionCommands[directions[i]]();
                Info = sensorsData.Position.PositionsData[Id];
                map.Update(sensorsData);
            }
            if (sensorsData == null) sensorsData = Server.SendCommand(new Command { LinearVelocity = 0, Time = 0.2 });
            return sensorsData;
        }

        public PositionSensorsData Take(MapHelper.Map map,Point target)
        {
            Console.WriteLine("Пытаюсь взять"); 
            PositionSensorsData sensorsData = null;
            //TurnTo(direction.ToAngle());            
            var angle = Math.Atan2(target.Y - Coordinate.Y, target.X - Coordinate.X) * 180 / Math.PI;
            TurnTo(angle);
            var r = new Point((int)map.CurrentPosition.X, (int)map.CurrentPosition.Y);
            if (PointExtension.VectorLength(target, r) > 17)          
                Server.SendCommand(new Command { LinearVelocity = 50, Time = (PointExtension.VectorLength(target, r)-17) / 50 });
          
            //var distance = PointExtension.VectorLength(Coordinate, target) - 20;
            //sensorsData = Server.SendCommand(new Command { LinearVelocity = distance, Time = 1 });
            sensorsData=Server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
            if (PointExtension.VectorLength(target, r) > 17)            
                sensorsData = Server.SendCommand(new Command { LinearVelocity = -50, Time = (PointExtension.VectorLength(target, r) -17) / 50 });
            map.Update(sensorsData);
            //sensorsData = Server.SendCommand(new Command { LinearVelocity = -distance, Time = 1 });           
            Info = sensorsData.Position.PositionsData[Id];
            
            return sensorsData;
        }

        public PositionSensorsData TakeClosestDetail(MapHelper.Map map, HashSet<string> detailsType, out DetailType detailType)
        {
            Console.WriteLine("Еду к ближайшей детали");
            var typeDictionary = new Dictionary<string, DetailType>
            {
                {"GreenDetail", DetailType.Green},
                {"RedDetail", DetailType.Red},
                {"BlueDetail", DetailType.Blue}
            };
            //Point target = null;// ближайшая деталь
            detailType = DetailType.Red;
            PositionSensorsData sensorsData = null;
            var pathTuple = map
                .Details.Select(detail => Tuple.Create(detail,PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                detail.DiscreteCoordinate)))
                .OrderBy(tuple =>tuple.Item2.Length)
                .FirstOrDefault();
            
            if (pathTuple == null)
                Server.Exit();
            var path = pathTuple.Item2;
            var target = pathTuple.Item1.AbsoluteCoordinate;                        

            detailType = typeDictionary[pathTuple.Item1.Type];
             // поиск пути к ближайшей детали
            sensorsData = MoveTo(path.Take(path.Length - 1).ToArray(),map);
            //if (path.Length == 0) sensorsData = Server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
            //else
            map.Update(sensorsData);
            sensorsData = Take(map, target);
            Info = sensorsData.Position.PositionsData[Id];
            return sensorsData;
        }

        public PositionSensorsData MoveToClosestWall(MapHelper.Map map, DetailType detailType)
        { 
            Console.WriteLine("Еду к нужной стене");
            PositionSensorsData sensorsData = null;

            var dictionaryOfWalls = new Dictionary<DetailType, HashSet<string>>
            {
                {DetailType.Red, new HashSet<string> {"VerticalRedSocket", "HorizontalRedSocket"}},
                {DetailType.Green, new HashSet<string> {"VerticalGreenSocket", "HorizontalGreenSocket"}},
                {DetailType.Blue, new HashSet<string> {"VerticalBlueSocket", "HorizontalBlueSocket"}}
            };

            Tuple<Point, Direction[]> pathTuple = map.Walls
                .Where(wall => dictionaryOfWalls[detailType].Contains(wall.Type))
                .Select(wall => Tuple.Create(wall.AbsoluteCoordinate, PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                 wall.DiscreteCoordinate)))
                .OrderBy(tuple => tuple.Item2.Length)
                .FirstOrDefault();

            var path = pathTuple.Item2;
            var target = pathTuple.Item1;
            sensorsData = MoveTo(path,map);
            sensorsData = Server.SendCommand(new Command { Action = CommandAction.Release, Time = 1 });
            return sensorsData;
        }
        public PositionSensorsData MoveToMiddle(MapHelper.Map map)
        {
            PositionSensorsData sensorsData = null;
            var robotPosition = map.GetDiscretePosition(Coordinate.X,Coordinate.Y);            
            var r = new Point((int)map.CurrentPosition.X, (int)map.CurrentPosition.Y);
            var target = new Point(robotPosition.X*50 - 25 - 150, robotPosition.Y*-50 - 25 + 150);

            //Console.WriteLine(robotPosition + " || "+ target + " - " + r + " - " + PointExtension.VectorLength(target, r));
            //foreach (var e in map.Walls)
            //{
            //    Console.WriteLine(e.AbsoluteCoordinate);
            //}
            if (Math.Abs(PointExtension.VectorLength(target, r)) > 1e-1)
            {
                var angle = Math.Atan2(target.Y - Coordinate.Y, target.X - Coordinate.X)*180/Math.PI;
                TurnTo(angle);
                sensorsData =
                    Server.SendCommand(new Command
                    {
                        LinearVelocity = 50,
                        Time = (PointExtension.VectorLength(target, r))/50
                    });
            }
            else sensorsData = Server.SendCommand(new Command {LinearVelocity = 0, Time = 0.2});
                        
             Console.WriteLine("Двигаюсь в центр"); 
            //var angle = Math.Atan((rocket.Location.Y - target.Y) / (target.X - rocket.Location.X));
            //if (rocket.Location.X > target.X) angle = Math.PI + angle;      
            return sensorsData;
        }
    }
}
