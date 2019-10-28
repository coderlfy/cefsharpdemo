using System;
using System.Configuration;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;
using System.Web.Hosting;
namespace Microsoft.VisualStudio.WebHost
{
	[PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust"), PermissionSet(SecurityAction.LinkDemand, Name = "Everything")]
	public class Server : MarshalByRefObject
	{
		private const int TOKEN_ALL_ACCESS = 983551;
		private const int TOKEN_EXECUTE = 131072;
		private const int TOKEN_READ = 131080;
		private const int TOKEN_IMPERSONATE = 4;
		private const int SecurityImpersonation = 2;
		private int _port;
		private string _virtualPath;
		private string _physicalPath;
		private bool _requireAuthentication;
		private bool _disableDirectoryListing;
		private WaitCallback _onStart;
		private WaitCallback _onSocketAccept;
		private bool _shutdownInProgress;
		private ApplicationManager _appManager;
		private Socket _socketIpv4;
		private Socket _socketIpv6;
		private Host _host;
		private IntPtr _processToken;
		private string _processUser;
		private object _lockObject = new object();
		public string VirtualPath
		{
			get
			{
				return this._virtualPath;
			}
		}
		public string PhysicalPath
		{
			get
			{
				return this._physicalPath;
			}
		}
		public int Port
		{
			get
			{
				return this._port;
			}
		}
		public string RootUrl
		{
			get
			{
				if (this._port != 80)
				{
					return "http://localhost:" + this._port + this._virtualPath;
				}
				return "http://localhost" + this._virtualPath;
			}
		}
		public Server(int port, string virtualPath, string physicalPath) : this(port, virtualPath, physicalPath, false, false)
		{
		}
		public Server(int port, string virtualPath, string physicalPath, bool requireAuthentication) : this(port, virtualPath, physicalPath, requireAuthentication, false)
		{
		}
		public Server(int port, string virtualPath, string physicalPath, bool requireAuthentication, bool disableDirectoryListing)
		{
			this._port = port;
			this._virtualPath = virtualPath;
			this._physicalPath = (physicalPath.EndsWith("\\", StringComparison.Ordinal) ? physicalPath : (physicalPath + "\\"));
			this._requireAuthentication = requireAuthentication;
			this._disableDirectoryListing = disableDirectoryListing;
			this._onSocketAccept = new WaitCallback(this.OnSocketAccept);
			this._onStart = new WaitCallback(this.OnStart);
			this._appManager = ApplicationManager.GetApplicationManager();
			this.ObtainProcessToken();
		}
		public override object InitializeLifetimeService()
		{
			return null;
		}
		[DllImport("ADVAPI32.DLL", SetLastError = true)]
		private static extern bool ImpersonateSelf(int level);
		[DllImport("ADVAPI32.DLL", SetLastError = true)]
		private static extern int RevertToSelf();
		[DllImport("KERNEL32.DLL", SetLastError = true)]
		private static extern IntPtr GetCurrentThread();
		[DllImport("ADVAPI32.DLL", SetLastError = true)]
		private static extern int OpenThreadToken(IntPtr thread, int access, bool openAsSelf, ref IntPtr hToken);
		private void ObtainProcessToken()
		{
			if (Server.ImpersonateSelf(2))
			{
				Server.OpenThreadToken(Server.GetCurrentThread(), 983551, true, ref this._processToken);
				Server.RevertToSelf();
				this._processUser = WindowsIdentity.GetCurrent().Name;
			}
		}
		public IntPtr GetProcessToken()
		{
			return this._processToken;
		}
		public string GetProcessUser()
		{
			return this._processUser;
		}
		private Socket CreateSocketBindAndListen(AddressFamily family, IPAddress ipAddress, int port)
		{
			Socket socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
			socket.ExclusiveAddressUse = false;
 
			try
			{
                //socket.Bind(new IPEndPoint(IPAddress.Parse(ConfigurationSettings.AppSettings["ServerIP"]), this._port));//从配置文件读取IP
                socket.Bind(new IPEndPoint(IPAddress.Any , port));
             //socket.Bind(new IPEndPoint(IPAddress.Parse("192.168.0.100"), port));
                //socket.Bind(new IPEndPoint(0, port));
                //socket.Bind(new IPEndPoint(ipAddress, port));
			}
			catch
			{
				socket.Close();
				socket = null;
				throw;
			}
			socket.Listen(2147483647);
			return socket;
		}
		public void Start()
		{
			bool flag = false;
			flag = Socket.OSSupportsIPv4;
			if (Socket.OSSupportsIPv6)
			{
				try
				{
					this._socketIpv6 = this.CreateSocketBindAndListen(AddressFamily.InterNetworkV6, IPAddress.IPv6Loopback, this._port);
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse || !flag)
					{
						throw;
					}
				}
			}
			if (flag)
			{
				try
				{
					this._socketIpv4 = this.CreateSocketBindAndListen(AddressFamily.InterNetwork, IPAddress.Loopback, this._port);
				}
				catch (SocketException)
				{
					if (this._socketIpv6 == null)
					{
						throw;
					}
				}
			}
			if (this._socketIpv6 != null)
			{
				ThreadPool.QueueUserWorkItem(this._onStart, this._socketIpv6);
			}
			if (this._socketIpv4 != null)
			{
				ThreadPool.QueueUserWorkItem(this._onStart, this._socketIpv4);
			}
		}
		public void Stop()
		{
			this._shutdownInProgress = true;
			try
			{
				if (this._socketIpv4 != null)
				{
					this._socketIpv4.Close();
				}
				if (this._socketIpv6 != null)
				{
					this._socketIpv6.Close();
				}
			}
			catch
			{
			}
			finally
			{
				this._socketIpv4 = null;
				this._socketIpv6 = null;
			}
			try
			{
				if (this._host != null)
				{
					this._host.Shutdown();
				}
				while (this._host != null)
				{
					Thread.Sleep(100);
				}
			}
			catch
			{
			}
			finally
			{
				this._host = null;
			}
		}
		private void OnSocketAccept(object acceptedSocket)
		{
			if (!this._shutdownInProgress)
			{
				Connection connection = new Connection(this, (Socket)acceptedSocket);
				if (connection.WaitForRequestBytes() == 0)
				{
					connection.WriteErrorAndClose(400);
					return;
				}
				Host host = this.GetHost();
				if (host == null)
				{
					connection.WriteErrorAndClose(500);
					return;
				}
				host.ProcessRequest(connection);
			}
		}
		private void OnStart(object listeningSocket)
		{
			while (!this._shutdownInProgress)
			{
				try
				{
					if (listeningSocket != null)
					{
						Socket state = ((Socket)listeningSocket).Accept();
						ThreadPool.QueueUserWorkItem(this._onSocketAccept, state);
					}
				}
				catch
				{
					Thread.Sleep(100);
				}
			}
		}
		private Host GetHost()
		{
			if (this._shutdownInProgress)
			{
				return null;
			}
			Host host = this._host;
			if (host == null)
			{
				lock (this._lockObject)
				{
					host = this._host;
					if (host == null)
					{
						string text = (this._virtualPath + this._physicalPath).ToLowerInvariant();
						string appId = text.GetHashCode().ToString("x", CultureInfo.InvariantCulture);
						this._host = (Host)this._appManager.CreateObject(appId, typeof(Host), this._virtualPath, this._physicalPath, false);
						this._host.Configure(this, this._port, this._virtualPath, this._physicalPath, this._requireAuthentication, this._disableDirectoryListing);
						host = this._host;
					}
				}
			}
			return host;
		}
		internal void HostStopped()
		{
			this._host = null;
		}
	}
}
