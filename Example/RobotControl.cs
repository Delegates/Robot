using System;
using System.Collections.Generic;
using System.Threading;
using AIRLab.Mathematics;
using ClientBase;
using CommonTypes;
using CVARC.Basic.Controllers;
using CVARC.Basic.Sensors;
using CVARC.Network;
using MapHelper;
using RepairTheStarship.Sensors;
using System.Linq;
using SlimDX.X3DAudio;


namespace Robot
{ 
    
    
    internal class RobotControl
	{ 
        //static Random rnd = new Random(DateTime.Now.Millisecond);
		private static readonly ClientSettings Settings = new ClientSettings
		{           
			Side = Side.Right, //Переключив это поле, можно отладить алгоритм для левой или правой стороны, а также для произвольной стороны, назначенной сервером
			LevelName = LevelName.Level1, //Задается уровень, в котором вы хотите принять участие            
            MapNumber = 198//Задавая различные значения этого поля, вы можете сгенерировать различные случайные карты
		};
        
		private static void Main(string[] args)
		{
			var server = new CvarcClient(args, Settings).GetServer<PositionSensorsData>();
			var helloPackageAns = server.Run();
            
			//Здесь вы можете узнать сторону, назначенную вам сервером в случае, если запросили Side.Random. 
			//ВАЖНО!
			//Side и MapNumber влияют на сервер только на этапе отладки. В боевом режиме и то, и другое будет назначено сервером
			//вне зависимости от того, что вы указали в Settings! Поэтому ваш итоговый алгоритм должен использовать helloPackageAns.RealSide
			Console.WriteLine("Your Side: {0}", helloPackageAns.RealSide); 

			PositionSensorsData sensorsData = null;
		   
            var robot = new Robot(server, helloPackageAns.SensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1]); // создание класса робот

		    foreach (var bomj in helloPackageAns.SensorsData.BuildMap().Details) // координаты деталей(юзал для проверки)
		        Console.WriteLine(bomj.Type);

            var map = helloPackageAns.SensorsData.BuildMap();
	
            var details = new HashSet<string> { "GreenDetail", "BlueDetail", "RedDetail" }; // список деталей

            while (true)
            {
                Console.WriteLine("зашел");
              sensorsData = robot.MoveToMiddle(map);                
              map.Update(sensorsData);
              Point target = null; // 1 деталь
              DetailType detail;

              sensorsData = robot.TakeClosestDetail(map, details, out detail);
              map.Update(sensorsData);
              sensorsData = robot.MoveToClosestWall(map, detail);
              map.Update(sensorsData);
            }
		    

			//Так вы можете отправлять различные команды. По результатам выполнения каждой команды, вы получите sensorsData, 
			//который содержит информацию о происходящем на поле
			//sensorsData = server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(-90), Time = 1 });
		    
            
            //sensorsData = server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
            //sensorsData = server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
            //sensorsData = server.SendCommand(new Command { LinearVelocity = -50, Time = 1 });
            System.Threading.Thread.Sleep(50000);
            ////MapHelper.PathSearcher.FindPath();
            ////DirectionHelper.
            //sensorsData = server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(90), Time = 1 });
            //sensorsData = server.SendCommand(new Command {Action = CommandAction.Release, Time = 1});

			//Используйте эту команду в конце кода для того, чтобы в режиме отладки все окна быстро закрылись, когда вы откатали алгоритм.
			//Если вы забудете это сделать, сервер какое-то время будет ожидать команд от вашего отвалившегося клиента. 
			server.Exit();
		}      
	}
}
