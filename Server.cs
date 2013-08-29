using System;
using System.Net.Sockets;
using System.Threading;

namespace TCPRedirect
{
	/// <summary>
	/// Summary description for Server.
	/// </summary>
	public class Server
	{

		private int _LocalPort;
		private string _Host;
		private int _DestinationPort;
		private string _LogFile;
		public bool Running = false;
		private string _LastError = "";
		private bool _Throttle = false;

		#region Property Acessors

		public bool Throttle
		{
			get { return _Throttle; } 
			set { _Throttle = value; } 	
		}

		public int LocalPort
		{
			get { return _LocalPort; } 
			set { _LocalPort = value; } 	
		}

		public string Host
		{
			get { return _Host; } 
			set { _Host = value; } 	
		}

		public int DestinationPort
		{
			get { return _DestinationPort; } 
			set { _DestinationPort = value; } 	
		}

		public string LogFile
		{
			get { return _LogFile; } 
			set { _LogFile = value; } 	
		}

		public string LastError
		{
			get { return _LastError; }
			set { _LastError = value; }
		}

		#endregion

		public event Client.DataEventHandler DataIn;
		public event Client.DataEventHandler DataOut;

		public Server(int LocalPort, string Host, int DestPort, string LogFile, bool Throttle)
		{
			_LocalPort = LocalPort;
			_Host = Host;
			_DestinationPort = DestPort;
			_LogFile = LogFile;
			_Throttle = Throttle;
		}

		public void Stop()
		{
			Running = false;
		}

		public void Start()
		{
			try 
			{
				Running = true;
				TcpListener Listener = new TcpListener(_LocalPort);
				Listener.Start();
				Console.WriteLine("Forwarding traffic on port " + _LocalPort.ToString() + 
					" to " + _Host + ":" + _DestinationPort);
				int ConnectionCount = 0;
				while (Running)
				{
					TcpClient tcp = Listener.AcceptTcpClient();
					ConnectionCount ++;
					Client client = new Client(tcp, _Host, _DestinationPort, _LogFile, _Throttle);
					client.DataIn += DataIn;
					client.DataOut += DataOut;
					ThreadStart s = new ThreadStart(client.Start);
					Thread t = new Thread(s);
					t.Name = "Connection";
					t.Start();
					Console.WriteLine("A Connection on port " + _LocalPort + " is being forwarded");
					Thread.Sleep(50);
				}
			} 
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				_LastError = e.Message;
				Running = false;
			}
		}

		private void client_DataIn(string Data)
		{
			if (DataIn != null)
			{
				DataIn(Data);
			}
		}

		private void client_DataOut(string Data)
		{
			if (DataOut != null)
			{
				DataOut(Data);
			}
		}
	}
}
