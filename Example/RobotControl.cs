using System;
using System.Threading;
using AIRLab.Mathematics;
using ClientBase;
using CommonTypes;
using CVARC.Basic.Controllers;
using CVARC.Network;
using MapHelper;
using RepairTheStarship.Sensors;
<<<<<<< HEAD



=======
// Я НЕ НАВИЖУ ГИТХАБ
>>>>>>> parent of ffae464... Revert "123"
namespace Robot
{ 
    //ненавижу уродов ,которые придумали задачи
    //я тоже
    internal class RobotControl
	{
		private static readonly ClientSettings Settings = new ClientSettings
		{
			Side = Side.Left, //Переключив это поле, можно отладить алгоритм для левой или правой стороны, а также для произвольной стороны, назначенной сервером
			LevelName = LevelName.Level1, //Задается уровень, в котором вы хотите принять участие
            MapNumber = -1 //Задавая различные значения этого поля, вы можете сгенерировать различные случайные карты
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

			//Так вы можете отправлять различные команды. По результатам выполнения каждой команды, вы получите sensorsData, 
			//который содержит информацию о происходящем на поле
			sensorsData = server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(-90), Time = 1 });
		    //var a = helloPackageAns.SensorsData.MapSensor;
            //RepairTheStarship.Sensors.MapSensorData 
            //var mapSensor = sensorsData.MapSensor;
            //Console.WriteLine(sensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1].X + " " + sensorsData.Position.PositionsData[helloPackageAns.RealSide == Side.Left ? 0 : 1].Y);
            //Console.WriteLine("------------------");
		    var a = helloPackageAns.SensorsData.BuildMap();
            foreach (var element in a.Details)
                Console.WriteLine(element);
		    Console.WriteLine(helloPackageAns.SensorsData.MapSensor.MapItems.GetLength(0));
            foreach (var element in sensorsData.MapSensor.MapItems)
                Console.WriteLine(element);

			sensorsData = server.SendCommand(new Command { LinearVelocity = 50, Time = 1 });
			sensorsData = server.SendCommand(new Command { Action = CommandAction.Grip, Time = 1 });
			sensorsData = server.SendCommand(new Command { LinearVelocity = -50, Time = 1 });
            System.Threading.Thread.Sleep(50000);
		    //MapHelper.PathSearcher.FindPath();
            //DirectionHelper.
			sensorsData = server.SendCommand(new Command { AngularVelocity = Angle.FromGrad(90), Time = 1 });
		    sensorsData = server.SendCommand(new Command {Action = CommandAction.Release, Time = 1});

			//Используйте эту команду в конце кода для того, чтобы в режиме отладки все окна быстро закрылись, когда вы откатали алгоритм.
			//Если вы забудете это сделать, сервер какое-то время будет ожидать команд от вашего отвалившегося клиента. 
			server.Exit();
		}
	}
}
