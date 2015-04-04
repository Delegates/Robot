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
        private PositionData Info { get; set; }

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
            PositionSensorsData sensorsData = null;
            if (angle < -180) angle += 360;
            if (angle > 180) angle -= 360;
            Console.WriteLine(angle + " " + Angle);
            //Console.WriteLine(angle - Angle < double.Epsilon);
           if (Math.Abs(angle - Angle) < 1e-2) return;
            
            sensorsData = Server.SendCommand(new Command { AngularVelocity = AIRLab.Mathematics.Angle.FromGrad(90*Math.Sign(angle - Angle)), Time = Math.Abs(angle - Angle)/90 });
            //sensorsData = Server.SendCommand(new Command { LinearVelocity = 25, Time = 1 });   
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
            detailType = DetailType.Red;
            PositionSensorsData sensorsData = null;
            Point target = null; // ближайшая деталь
            foreach (var element in map.Details) // поиск ближайшей детали
                if (detailsType.Contains(element.Type) && (target == null ||
                                                           PointExtension.VectorLength(Coordinate, target) >
                                                           PointExtension.VectorLength(Coordinate,
                                                               element.AbsoluteCoordinate)))
                {
                    target = new Point(element.AbsoluteCoordinate.X, element.AbsoluteCoordinate.Y);
                    detailType = typeDictionary[element.Type];
                }
            if (target == null)
                Server.Exit();
            var path = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                map.GetDiscretePosition(new PositionData(new Frame3D(target.X, target.Y, 0)))); // поиск пути к ближайшей детали
            sensorsData = MoveTo(path.Take(path.Length - 1).ToArray());
            sensorsData = Take(path[path.Length - 1], target);
            Info = sensorsData.Position.PositionsData[Id];
            return sensorsData;
        }

    }
}
