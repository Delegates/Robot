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
    class RobotMove
    {
        public RobotMove(Server<PositionSensorsData> server, PositionData robotInfo)
        {
            RobotInfo = robotInfo;
            Server = server;
        }
        private ClientBase.Server<PositionSensorsData> Server { get; set; }
        private PositionData RobotInfo { get; set; }

        private int RobotId
        {
            get { return RobotInfo.RobotNumber; }
        }

        private double RobotAngle
        {
            get { return RobotInfo.Angle; }
        }

        public Point RobotCoordinate
        {
            get { return new Point((int)RobotInfo.X, (int)RobotInfo.Y); }
        }

        public void RobotTurnTo(double angle)
        {
            PositionSensorsData sensorsData = null;
            Console.WriteLine(angle + " " + RobotAngle);
            //Console.WriteLine(angle - RobotAngle < double.Epsilon);
            if (Math.Abs(angle - RobotAngle) < 1e-2) return;
            sensorsData = Server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(90*Math.Sign(angle - RobotAngle)), Time = Math.Abs(angle - RobotAngle)/90 });
            RobotInfo = sensorsData.Position.PositionsData[RobotId];
        }
        public void RobotMoveTo(Direction[] directions)
        {
            PositionSensorsData sensorsData = null;
            for (int i = 0; i < directions.Length; i++)
            {
                RobotTurnTo(directions[i].ToAngle());
                sensorsData = Server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
                RobotInfo = sensorsData.Position.PositionsData[RobotId];
            }
        }
    }
}
