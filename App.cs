using System;
using System.Threading;
using System.Net.Sockets;

namespace TCPRedirect
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class App
	{

		const string APP = "TCPRedirect";
		const string VERSION = "0.9";

		private static string ListenString = "";
		private static int[] Listen;
		private static string DestHost = "";
		private static int DestPort = -1;
		private static string LogPath = "";
		private static bool Throttle56k = false;

		private static bool ParseArgs(string[] args)
		{
			// Mandatory args
			if (args.Length < 3){ return false; }
			ListenString = args[0];
			DestHost = args[1];
			DestPort = int.Parse(args[2]);
			// Get PortRange
			if (ListenString.IndexOf("-") != -1)
			{
				string[] Parts = ListenString.Split('-');
				int Start = int.Parse(Parts[0]);
				int End = int.Parse(Parts[1]);
				End ++;
				Listen = new int[End-Start];
				for (int x = Start; x< End; x++)
				{
					Listen[x-Start] = x;
				}
			} 
			else 
			{
				Listen = new int[1];
				Listen[0] = int.Parse(ListenString);
			}
			// Parse optional args
			for(int x = 0; x< args.Length; x++)
			{
				if (args[x] == "-l") { LogPath = args[x+1]; }
				if (args[x] == "-t") { Throttle56k = true; }
			}
			if (LogPath != String.Empty){ Console.WriteLine("\nLogging to " + LogPath); }
			if (Throttle56k) { Console.WriteLine("Throttling throughput"); }
			return true;
		}

		private static void ShowUsage()
		{
			Console.WriteLine("\nusage: TPCRedirect \r\n\t<Listen port[or range i.e 20-30]> \r\n\t<Destination host> \r\n\t<Destination port> \r\n" +
				"\t[-l Log path] \r\n\t[-t]");
			Console.WriteLine("-t throttles thru-traffic to simulate a slow connection");
			return;
		}

		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("\r\n" + APP + " v" + VERSION + "\nBy Tristan Phillips");
			if(!ParseArgs(args)) { ShowUsage(); return;}
			try 
			{
				Console.WriteLine("\nConfiguring port(s) . . .");
				foreach (int port in Listen)
				{
					ThreadStart s = new ThreadStart(new Server(port, DestHost, DestPort, LogPath, Throttle56k).Start);
					Thread t = new Thread(s);
					t.Start();
				}
				Console.WriteLine("Port(s) configured");
			} 
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

	}
}
