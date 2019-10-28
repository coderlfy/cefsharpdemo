using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class Connection : MarshalByRefObject
	{
		private Server _server;
		private Socket _socket;
		private static string _defaultLoalhostIP;
		private static string _localServerIP;
		internal bool Connected
		{
			get
			{
				return this._socket.Connected;
			}
		}
		internal bool IsLocal
		{
			get
			{
				string remoteIP = this.RemoteIP;
				return !string.IsNullOrEmpty(remoteIP) && (remoteIP.Equals("127.0.0.1") || remoteIP.Equals("::1") || remoteIP.Equals("::ffff:127.0.0.1") || Connection.LocalServerIP.Equals(remoteIP));
			}
		}
		private static string LocalServerIP
		{
			get
			{
				if (Connection._localServerIP == null)
				{
					IPHostEntry hostEntry = Dns.GetHostEntry(Environment.MachineName);
					IPAddress iPAddress = hostEntry.AddressList[0];
					Connection._localServerIP = iPAddress.ToString();
				}
				return Connection._localServerIP;
			}
		}
		private string DefaultLocalHostIP
		{
			get
			{
				if (string.IsNullOrEmpty(Connection._defaultLoalhostIP))
				{
					bool flag = !Socket.OSSupportsIPv4 && Socket.OSSupportsIPv6;
					if (flag)
					{
						Connection._defaultLoalhostIP = "::1";
					}
					else
					{
						Connection._defaultLoalhostIP = "127.0.0.1";
					}
				}
				return Connection._defaultLoalhostIP;
			}
		}
		internal string LocalIP
		{
			get
			{
				IPEndPoint iPEndPoint = (IPEndPoint)this._socket.LocalEndPoint;
				if (iPEndPoint != null && iPEndPoint.Address != null)
				{
					return iPEndPoint.Address.ToString();
				}
				return this.DefaultLocalHostIP;
			}
		}
		internal string RemoteIP
		{
			get
			{
				IPEndPoint iPEndPoint = (IPEndPoint)this._socket.RemoteEndPoint;
				if (iPEndPoint != null && iPEndPoint.Address != null)
				{
					return iPEndPoint.Address.ToString();
				}
				return "";
			}
		}
		internal Connection(Server server, Socket socket)
		{
			this._server = server;
			this._socket = socket;
		}
		public override object InitializeLifetimeService()
		{
			return null;
		}
		internal void Close()
		{
			try
			{
				this._socket.Shutdown(SocketShutdown.Both);
				this._socket.Close();
			}
			catch
			{
			}
			finally
			{
				this._socket = null;
			}
		}
		private static string MakeResponseHeaders(int statusCode, string moreHeaders, int contentLength, bool keepAlive)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(string.Concat(new object[]
			{
				"HTTP/1.1 ", 
				statusCode, 
				" ", 
				HttpWorkerRequest.GetStatusDescription(statusCode), 
				"\r\n"
			}));
            stringBuilder.Append("Expires:Tue, 03 Jul 3001 06:00:00 GMT \r\n");
			stringBuilder.Append("Server: ASP.NET Development Server/" + Messages.VersionString + "\r\n");
			stringBuilder.Append("Date: " + DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo) + "\r\n");
			if (contentLength >= 0)
			{
				stringBuilder.Append("Content-Length: " + contentLength + "\r\n");
			}
			if (moreHeaders != null)
			{
				stringBuilder.Append(moreHeaders);
			}
			if (!keepAlive)
			{
                //stringBuilder.Append("Connection: Close\r\n");
				stringBuilder.Append("Connection: keep-alive\r\n");
			}
			stringBuilder.Append("\r\n");
			return stringBuilder.ToString();
		}
		private static string MakeContentTypeHeader(string fileName)
		{
			string text = null;
			FileInfo fileInfo = new FileInfo(fileName);
			string text2 = fileInfo.Extension.ToLowerInvariant();
			string key;
			switch (key = text2)
			{
				case ".bmp":
				{
					text = "image/bmp";
					break;
				}
				case ".css":
				{
					text = "text/css";
					break;
				}
				case ".gif":
				{
					text = "image/gif";
					break;
				}
				case ".ico":
				{
					text = "image/x-icon";
					break;
				}
				case ".htm":
				case ".html":
				{
					text = "text/html";
					break;
				}
				case ".jpe":
				case ".jpeg":
				case ".jpg":
				{
					text = "image/jpeg";
					break;
				}
				case ".js":
				{
					text = "application/x-javascript";
					break;
				}
			}
			if (text == null)
			{
				return null;
			}
			return "Content-Type: " + text + "\r\n";
		}
		private string GetErrorResponseBody(int statusCode, string message)
		{
			string text = Messages.FormatErrorMessageBody(statusCode, this._server.VirtualPath);
			if (message != null && message.Length > 0)
			{
				text = text + "\r\n<!--\r\n" + message + "\r\n-->";
			}
			return text;
		}
		internal byte[] ReadRequestBytes(int maxBytes)
		{
			byte[] result;
			try
			{
				if (this.WaitForRequestBytes() == 0)
				{
					result = null;
				}
				else
				{
					int num = this._socket.Available;
					if (num > maxBytes)
					{
						num = maxBytes;
					}
					int num2 = 0;
					byte[] array = new byte[num];
					if (num > 0)
					{
						num2 = this._socket.Receive(array, 0, num, SocketFlags.None);
					}
					if (num2 < num)
					{
						byte[] array2 = new byte[num2];
						if (num2 > 0)
						{
							Buffer.BlockCopy(array, 0, array2, 0, num2);
						}
						array = array2;
					}
					result = array;
				}
			}
			catch
			{
				result = null;
			}
			return result;
		}
		internal void Write100Continue()
		{
			this.WriteEntireResponseFromString(100, null, null, true);
		}
		internal void WriteBody(byte[] data, int offset, int length)
		{
			try
			{
				this._socket.Send(data, offset, length, SocketFlags.None);
			}
			catch (SocketException)
			{
			}
		}
		internal void WriteEntireResponseFromString(int statusCode, string extraHeaders, string body, bool keepAlive)
		{
			try
			{
				int contentLength = (body != null) ? Encoding.UTF8.GetByteCount(body) : 0;
				string str = Connection.MakeResponseHeaders(statusCode, extraHeaders, contentLength, keepAlive);
				this._socket.Send(Encoding.UTF8.GetBytes(str + body));
			}
			catch (SocketException)
			{
			}
			finally
			{
				if (!keepAlive)
				{
					this.Close();
				}
			}
		}
		internal void WriteEntireResponseFromFile(string fileName, bool keepAlive)
		{
			if (!File.Exists(fileName))
			{
				this.WriteErrorAndClose(404);
				return;
			}
			string text = Connection.MakeContentTypeHeader(fileName);
			if (text == null)
			{
				this.WriteErrorAndClose(403);
				return;
			}
			bool flag = false;
			FileStream fileStream = null;
			try
			{
				fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				int num = (int)fileStream.Length;
				byte[] buffer = new byte[num];
				int num2 = fileStream.Read(buffer, 0, num);
				string s = Connection.MakeResponseHeaders(200, text, num2, keepAlive);
				this._socket.Send(Encoding.UTF8.GetBytes(s));
				this._socket.Send(buffer, 0, num2, SocketFlags.None);
				flag = true;
			}
			catch (SocketException)
			{
			}
			finally
			{
				if (!keepAlive || !flag)
				{
					this.Close();
				}
				if (fileStream != null)
				{
					fileStream.Close();
				}
			}
		}
		internal void WriteErrorAndClose(int statusCode, string message)
		{
			this.WriteEntireResponseFromString(statusCode, "Content-type:text/html;charset=utf-8\r\n", this.GetErrorResponseBody(statusCode, message), false);
		}
		internal void WriteErrorAndClose(int statusCode)
		{
			this.WriteErrorAndClose(statusCode, null);
		}
		internal void WriteErrorWithExtraHeadersAndKeepAlive(int statusCode, string extraHeaders)
		{
			this.WriteEntireResponseFromString(statusCode, extraHeaders, this.GetErrorResponseBody(statusCode, null), true);
		}
		internal int WaitForRequestBytes()
		{
			int result = 0;
			try
			{
				if (this._socket.Available == 0)
				{
					this._socket.Poll(100000, SelectMode.SelectRead);
					if (this._socket.Available == 0 && this._socket.Connected)
					{
						this._socket.Poll(30000000, SelectMode.SelectRead);
					}
				}
				result = this._socket.Available;
			}
			catch
			{
			}
			return result;
		}
		internal void WriteHeaders(int statusCode, string extraHeaders)
		{
			string s = Connection.MakeResponseHeaders(statusCode, extraHeaders, -1, false);
			try
			{
				this._socket.Send(Encoding.UTF8.GetBytes(s));
			}
			catch (SocketException)
			{
			}
		}
	}
}
