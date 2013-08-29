using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TCPRedirect
{

	public class Client
	{

		private const int UNTHROTTLED_BUFFER = 20480;
		private const int THROTTLED_BUFFER = 712;
		private const int REPORT_INTERVAL = 102400;
		private const int THROTTLE = 20;

		private TcpClient _Connection;
		private string _Destination;
		private int _Port;
		private bool _Log = false;
		private string _LogPath;
		private bool _ShowDirectionTags = true;
		private bool _Throttle = false;
		private int BufferSize = 0;

		bool Clean = false;
		private bool logLastIn = false;
		private TcpClient Forwarder;
		NetworkStream sin;
		NetworkStream sout;
		Thread tIn;
		Thread tOut;

		#region Property Acessors

		public bool Throttle
		{
			get { return _Throttle; } 
			set 
			{ 
				_Throttle = value; 
			} 	
		}

		public TcpClient Connection
		{
			get { return _Connection; } 
			set { _Connection = value; } 	
		}

		public string Destination
		{
			get { return _Destination; } 
			set { _Destination = value; } 	
		}

		public int Port
		{
			get { return _Port; } 
			set { _Port = value; } 	
		}

		public bool Log
		{
			get { return _Log; } 
			set { _Log = value; } 	
		}

		public string LogPath
		{
			get { return _LogPath; } 
			set { _LogPath = value; } 	
		}

		public bool ShowDirectionTags
		{
			get { return _ShowDirectionTags; } 
			set { _ShowDirectionTags = value; } 	
		}

		#endregion

		public delegate void DataEventHandler(string Data);
		public event DataEventHandler DataIn;
		public event DataEventHandler DataOut;

		public Client(TcpClient connection, string dest, int port, string logPath, bool throttle)
		{
			_Connection = connection;
			_Destination = dest;
			_Port = port;
			_Throttle = throttle;
			if (logPath != "")
			{
				_Log = true;
				_LogPath = logPath;
			}
			BufferSize = _Throttle ? THROTTLED_BUFFER : UNTHROTTLED_BUFFER;
		}

		public void Start()
		{
			try
			{
				Forwarder = new TcpClient();
				Forwarder.Connect(IPAddress.Parse(_Destination), _Port);
				while (true)
				{
					sin = _Connection.GetStream();
					sout = Forwarder.GetStream();
					tIn = new Thread(new ThreadStart(InStream));
					tOut = new Thread(new ThreadStart(OutStream));
					tIn.Start();
					tOut.Start();
					while(tIn.IsAlive || tOut.IsAlive){ Thread.Sleep(1); }
					CleanUp();
					Console.WriteLine("Connection Finished"); 
					return;
				}
			} 
			catch (Exception e)
			{
				Console.WriteLine(e.Message + " - " + "Connection Finished");
				CleanUp();
			}
		}

		private void InStream()
		{
			long LastReported = 0;
			long Bytes = 0;
			byte[] buffer = new byte[BufferSize];
			try
			{
				int Read = 0;
				do
				{
					if (_Throttle) { Thread.Sleep(THROTTLE); }
					Read = sin.Read(buffer,0,buffer.Length);
					if (Read != 0)
					{
						sout.Write(buffer,0,Read);
						if(_Log){ WriteLog(buffer, Read, _LogPath, true); }
						Bytes += Read;
						if ((Bytes - LastReported) > REPORT_INTERVAL)
						{
							Console.WriteLine((Bytes/1024).ToString() + " Kb In");
							LastReported = Bytes;
						}
						OnDataIn(buffer);
					}
				} 
				while(Read != 0);
			}
			catch (Exception e) { }
			Console.WriteLine("Client dropped"); 
		}

		private void OutStream()
		{
			long LastReported = 0;
			long Bytes = 0;
			byte[] buffer = new byte[BufferSize];
			try 
			{
				int Read =0;
				do
				{
					if (_Throttle) { Thread.Sleep(THROTTLE); }
					Read = sout.Read(buffer,0,buffer.Length);
					sin.Write(buffer,0,Read);
					if(_Log){ WriteLog(buffer, Read, _LogPath, false); }
					Bytes += Read;
					if ((Bytes - LastReported) > REPORT_INTERVAL)
					{
						Console.WriteLine((Bytes/1024).ToString() + " Kb Out");
						LastReported = Bytes;
					}
					OnDataOut(buffer);
				}	
				while(Read != 0);
			}
			catch{}
			Console.WriteLine("Server dropped");
		}

		public virtual void OnDataIn(byte[] data)
		{
			if (DataIn != null)
			{
				string Data = System.Text.Encoding.ASCII.GetString(data);
				DataIn(Data);
			}
		}

		public virtual void OnDataOut(byte[] data)
		{
			if (DataOut != null)
			{
				string Data = System.Text.Encoding.ASCII.GetString(data);
				DataOut(Data);
			}
		}

		private void WriteLog(byte[] Data, int Length, string Path, bool In)
		{
			try 
			{
				FileStream s = new FileStream(Path, FileMode.Append);
				if (In != logLastIn && _ShowDirectionTags)
				{
					string Tag = In ? "\r\n<<< IN >>>\r\n" : "\r\n<<< OUT >>>\r\n";
					s.Write(System.Text.Encoding.ASCII.GetBytes(Tag),0,Tag.Length);
					logLastIn = In;
				}
				s.Write(Data, 0, Length);
				s.Flush();
				s.Close();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private void CleanUp()
		{
			if (!Clean)
			{
				tIn.Abort();
				tOut.Abort();
				sin.Close();
				sout.Close();
				Forwarder.Close();
				_Connection.Close();
				Clean = true;
			}
		}

	}
}
