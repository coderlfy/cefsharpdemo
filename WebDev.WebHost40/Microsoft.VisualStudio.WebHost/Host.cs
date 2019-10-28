using System;
using System.Globalization;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Hosting;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class Host : MarshalByRefObject, IRegisteredObject
	{
		private Server _server;
		private int _port;
		private int _pendingCallsCount;
		private string _virtualPath;
		private string _lowerCasedVirtualPath;
		private string _lowerCasedVirtualPathWithTrailingSlash;
		private string _physicalPath;
		private string _installPath;
		private string _physicalClientScriptPath;
		private string _lowerCasedClientScriptPathWithTrailingSlash;
		private bool _requireAuthentication;
		private bool _disableDirectoryListing;
		public string InstallPath
		{
			get
			{
				return this._installPath;
			}
		}
		public string NormalizedClientScriptPath
		{
			get
			{
				return this._lowerCasedClientScriptPathWithTrailingSlash;
			}
		}
		public string NormalizedVirtualPath
		{
			get
			{
				return this._lowerCasedVirtualPathWithTrailingSlash;
			}
		}
		public string PhysicalClientScriptPath
		{
			get
			{
				return this._physicalClientScriptPath;
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
		public string VirtualPath
		{
			get
			{
				return this._virtualPath;
			}
		}
		public bool RequireAuthentication
		{
			get
			{
				return this._requireAuthentication;
			}
		}
		public bool DisableDirectoryListing
		{
			get
			{
				return this._disableDirectoryListing;
			}
		}
		public override object InitializeLifetimeService()
		{
			return null;
		}
		public Host()
		{
			HostingEnvironment.RegisterObject(this);
		}
		public void Configure(Server server, int port, string virtualPath, string physicalPath, bool requireAuthentication)
		{
			this.Configure(server, port, virtualPath, physicalPath, requireAuthentication, false);
		}
		public void Configure(Server server, int port, string virtualPath, string physicalPath, bool requireAuthentication, bool disableDirectoryListing)
		{
			this._server = server;
			this._port = port;
			this._installPath = null;
			this._virtualPath = virtualPath;
			this._requireAuthentication = requireAuthentication;
			this._disableDirectoryListing = disableDirectoryListing;
			this._lowerCasedVirtualPath = CultureInfo.InvariantCulture.TextInfo.ToLower(this._virtualPath);
			this._lowerCasedVirtualPathWithTrailingSlash = (virtualPath.EndsWith("/", StringComparison.Ordinal) ? virtualPath : (virtualPath + "/"));
			this._lowerCasedVirtualPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(this._lowerCasedVirtualPathWithTrailingSlash);
			this._physicalPath = physicalPath;
			this._physicalClientScriptPath = HttpRuntime.AspClientScriptPhysicalPath + "\\";
			this._lowerCasedClientScriptPathWithTrailingSlash = CultureInfo.InvariantCulture.TextInfo.ToLower(HttpRuntime.AspClientScriptVirtualPath + "/");
		}
		public void ProcessRequest(Connection conn)
		{
			this.AddPendingCall();
			try
			{
				Request request = new Request(this, conn);
				request.Process();
			}
			finally
			{
				this.RemovePendingCall();
			}
		}
		private void WaitForPendingCallsToFinish()
		{
			while (this._pendingCallsCount > 0)
			{
				Thread.Sleep(250);
			}
		}
		private void AddPendingCall()
		{
			Interlocked.Increment(ref this._pendingCallsCount);
		}
		private void RemovePendingCall()
		{
			Interlocked.Decrement(ref this._pendingCallsCount);
		}
		[SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
		public void Shutdown()
		{
			HostingEnvironment.InitiateShutdown();
		}
		void IRegisteredObject.Stop(bool immediate)
		{
			if (this._server != null)
			{
				this._server.HostStopped();
			}
			this.WaitForPendingCallsToFinish();
			HostingEnvironment.UnregisterObject(this);
		}
		public bool IsVirtualPathInApp(string path)
		{
			bool flag;
			return this.IsVirtualPathInApp(path, out flag);
		}
		public bool IsVirtualPathInApp(string path, out bool isClientScriptPath)
		{
			isClientScriptPath = false;
			if (path == null)
			{
				return false;
			}
			path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);
			if (this._virtualPath == "/" && path.StartsWith("/", StringComparison.Ordinal))
			{
				if (path.StartsWith(this._lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal))
				{
					isClientScriptPath = true;
				}
				return true;
			}
			if (path.StartsWith(this._lowerCasedVirtualPathWithTrailingSlash, StringComparison.Ordinal))
			{
				return true;
			}
			if (path == this._lowerCasedVirtualPath)
			{
				return true;
			}
			if (path.StartsWith(this._lowerCasedClientScriptPathWithTrailingSlash, StringComparison.Ordinal))
			{
				isClientScriptPath = true;
				return true;
			}
			return false;
		}
		public bool IsVirtualPathAppPath(string path)
		{
			if (path == null)
			{
				return false;
			}
			path = CultureInfo.InvariantCulture.TextInfo.ToLower(path);
			return path == this._lowerCasedVirtualPath || path == this._lowerCasedVirtualPathWithTrailingSlash;
		}
		public IntPtr GetProcessToken()
		{
			new SecurityPermission(PermissionState.Unrestricted).Assert();
			return this._server.GetProcessToken();
		}
		public string GetProcessUser()
		{
			return this._server.GetProcessUser();
		}
		public SecurityIdentifier GetProcessSID()
		{
			SecurityIdentifier result = null;
			using (WindowsIdentity windowsIdentity = new WindowsIdentity(this._server.GetProcessToken()))
			{
				result = windowsIdentity.User;
			}
			return result;
		}
	}
}
