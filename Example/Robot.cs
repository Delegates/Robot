using System;
using System.Collections.Generic;
using System.Linq;
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
            var resAngle = angle - Angle;
            PositionSensorsData sensorsData = null;
            if (resAngle < -180) resAngle += 360;
            if (resAngle > 180) resAngle -= 360;
            Console.WriteLine(angle + " " + Angle);
            if (Math.Abs(resAngle) < 0.5) return;
            sensorsData =
                Server.SendCommand(new Command
                {
                    AngularVelocity = AIRLab.Mathematics.Angle.FromGrad(90 * Math.Sign(resAngle)),
                    Time = Math.Abs(resAngle) / 90
                });
            Info = sensorsData.Position.PositionsData[Id];           
        }
        public PositionSensorsData MoveTo(Direction[] directions)
        {
            PositionSensorsData sensorsData = null;
            for (int i = 0; i < directions.Length; i++)
            {
                TurnTo(directions[i].ToAngle());
                sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
                Info = sensorsData.Position.PositionsData[Id];
            }
            return sensorsData;
        }

        public PositionSensorsData Take(Direction direction,Point target)
        {
            PositionSensorsData sensorsData = null;
            TurnTo(direction.ToAngle());
            var distance = PointExtension.VectorLength(Coordinate, target)-20;
            sensorsData = Server.SendCommand(new Command { LinearVelocity = distance, Time = 1 });            
            sensorsData = Server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
            sensorsData = Server.SendCommand(new Command { LinearVelocity = -distance, Time = 1 });
            Info = sensorsData.Position.PositionsData[Id];
            return sensorsData;
        }

        public PositionSensorsData TakeClosestDetail(MapHelper.Map map, HashSet<string> detailsType, out DetailType detailType)
        {
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
            sensorsData = MoveTo(path.Take(path.Length - 1).ToArray());
            if (path.Length == 0) sensorsData = Server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
            else
            sensorsData = Take(path[path.Length - 1], target);
            Info = sensorsData.Position.PositionsData[Id];
            return sensorsData;
        }

        public PositionSensorsData MoveToClosestWall(MapHelper.Map map, DetailType detailType)
        {
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
            sensorsData = MoveTo(path);
            sensorsData = Server.SendCommand(new Command { Action = CommandAction.Release, Time = 1 });
            return sensorsData;
        }
    }
}
