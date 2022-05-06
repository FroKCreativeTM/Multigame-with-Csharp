using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using Google.Protobuf.WellKnownTypes;
using Server.Data;
using Server.DB;
using Server.Game;
using ServerCore;

namespace Server
{
	class Program
	{
		static Listener _listener = new Listener();

		// 게임의 로직들을 담당한다.
		static void GameLogicTask()
		{
			while(true)
            {
				GameLogic.Instance.Update();
				Thread.Sleep(0);
            }
		}

		// 게임의 Db 접근을 담당한다.
		static void GameDbTask()
		{
			while(true)
            {
				DbTransaction.Instance.Flush();
				Thread.Sleep(0);
            }
		}

		// BroadCast 시 Client Session에 들어있는 Packet list의 flush를 담당한다.
		static void NetworkTask()
        {
			while(true)
            {
				List<ClientSession> sessions = SessionManager.Instance.GetClientSessions();
				foreach(ClientSession session in sessions)
                {
					session.FlushSend();
                }
				Thread.Sleep(0);
            }
        }


		static void Main(string[] args)
		{
			ConfigManager.LoadConfig();
			DataManager.LoadData();

			GameLogic.Instance.Push(() => { GameRoom room = GameLogic.Instance.Add(1); });

			// DNS (Domain Name System)
			string host = Dns.GetHostName();
			IPHostEntry ipHost = Dns.GetHostEntry("127.0.0.1");
			IPAddress ipAddr = ipHost.AddressList[0];
			IPEndPoint endPoint = new IPEndPoint(ipAddr, 7777);

			_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });
			Console.WriteLine("Listening...");

			{
				// 하나의 스레드를 생성한 뒤 이를 던져둔다.
				Thread dbLogicThread = new Thread(GameDbTask);
				dbLogicThread.Name = "DB";
				dbLogicThread.Start();
			}
			{
				// 하나의 스레드를 생성한 뒤 이를 던져둔다.
				Thread networkLogicThread = new Thread(NetworkTask);
				networkLogicThread.Name = "Network Send";
				networkLogicThread.Start();
			}
			Thread.CurrentThread.Name = "Game Logic";
			GameLogicTask();
		}
	}
}
