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



namespace Robot
{ 
    //анапа
    internal class RobotControl
	{
		private static readonly ClientSettings Settings = new ClientSettings
		{
			Side = Side.Left, //Переключив это поле, можно отладить алгоритм для левой или правой стороны, а также для произвольной стороны, назначенной сервером
			LevelName = LevelName.Level1, //Задается уровень, в котором вы хотите принять участие
            MapNumber = 1 //Задавая различные значения этого поля, вы можете сгенерировать различные случайные карты
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

	
		        //Console.WriteLine(element);
            var robot = new RobotMove(server, helloPackageAns.SensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1]); // создание класса робот

		    foreach (var bomj in helloPackageAns.SensorsData.BuildMap().Details) // координаты деталей(юзал для проверки)
		        Console.WriteLine(bomj.Type);

            var map = helloPackageAns.SensorsData.BuildMap();

            foreach (var element in map.Walls)
                Console.WriteLine(element.Type);

            var details = new HashSet<string> { "GreenDetail", "BlueDetail", "RedDetail" }; // список деталей
		    Point target = null; // 1 деталь
		    foreach (var element in map.Details) // поиск ближайшей детали
		        if (details.Contains(element.Type) && (target == null ||
		                                               PointExtension.VectorLength(robot.RobotCoordinate, target) >
		                                               PointExtension.VectorLength(robot.RobotCoordinate,element.AbsoluteCoordinate)))
		            target = new Point(element.AbsoluteCoordinate.X, element.AbsoluteCoordinate.Y);

		    var path = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
		        map.GetDiscretePosition(new PositionData(new Frame3D(target.X, target.Y, 0)))); // поиск пути к ближайшей детали
		    foreach (var element in path)
		        Console.WriteLine(element);
            sensorsData = robot.RobotMoveTo(path.Take(path.Length - 1).ToArray());
            sensorsData = robot.RobotTake(path[path.Length - 1],target);

            
		     map.Update(sensorsData);
             var walls = new HashSet<string> { "VerticalRedSocket", "HorizontalRedSocket" };
		    target = null;
             foreach (var element in map.Walls) // поиск ближайшей детали
		        if (walls.Contains(element.Type) && (target == null ||
		                                               PointExtension.VectorLength(robot.RobotCoordinate, target) >
		                                               PointExtension.VectorLength(robot.RobotCoordinate,element.AbsoluteCoordinate)))
		            target = new Point(element.AbsoluteCoordinate.X, element.AbsoluteCoordinate.Y);
             path = PathSearcher.FindPath(map, map.GetDiscretePosition(map.CurrentPosition),
                 map.GetDiscretePosition(new PositionData(new Frame3D(target.X, target.Y, 0)))); // поиск пути к ближайшей детали
             foreach (var element in path)
                 Console.WriteLine(element);
             sensorsData = robot.RobotMoveTo(path);
             sensorsData = server.SendCommand(new Command { Action = CommandAction.Release, Time = 1 });
            // sensorsData = robot.RobotTake(path[path.Length - 1]);


            //PathSearcher.FindPath(helloPackageAns.SensorsData.BuildMap(), robot.RobotCoordinate, )//new Point(3, 1));

			//Так вы можете отправлять различные команды. По результатам выполнения каждой команды, вы получите sensorsData, 
			//который содержит информацию о происходящем на поле
			//sensorsData = server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(-90), Time = 1 });
		    //var a = helloPackageAns.SensorsData.MapSensor;
            //RepairTheStarship.Sensors.MapSensorData 
            //var mapSensor = sensorsData.MapSensor;
		    //var f = sensorsData.Position.PositionsData[0];
            //Console.WriteLine(sensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1].X + " " + sensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1].Y);
            //Console.WriteLine("------------------");
            //var a = helloPackageAns.SensorsData.BuildMap();
            //foreach (var element in a.Details)
            //    Console.WriteLine(element);
            //Console.WriteLine(helloPackageAns.SensorsData.MapSensor.MapItems.GetLength(0));
            //foreach (var element in sensorsData.MapSensor.MapItems)
            //    Console.WriteLine(element);
            //var d = PathSearcher.FindPath(a, a.GetDiscretePosition(a.CurrentPosition), a.Details[0].DiscreteCoordinate);
            //foreach (var element in d)
                //Console.WriteLine(element);
            //new Command({})
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
