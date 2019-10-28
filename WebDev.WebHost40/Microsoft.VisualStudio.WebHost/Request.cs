using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Web;
using System.Web.Hosting;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class Request : SimpleWorkerRequest
	{
		private const int MaxChunkLength = 65536;
		private const int maxHeaderBytes = 32768;
		private static char[] badPathChars = new char[]
		{
			'%', 
			'>', 
			'<', 
			':', 
			'\\'
		};
		private static string[] defaultFileNames = new string[]
		{
			"default.aspx", 
			"default.htm", 
			"default.html"
		};
		private static string[] restrictedDirs = new string[]
		{
			"/bin", 
			"/app_browsers", 
			"/app_code", 
			"/app_data", 
			"/app_localresources", 
			"/app_globalresources", 
			"/app_webreferences"
		};
		private Host _host;
		private Connection _connection;
		private IStackWalk _connectionPermission = new PermissionSet(PermissionState.Unrestricted);
		private byte[] _headerBytes;
		private int _startHeadersOffset;
		private int _endHeadersOffset;
		private ArrayList _headerByteStrings;
		private bool _isClientScriptPath;
		private string _verb;
		private string _url;
		private string _prot;
		private string _path;
		private string _filePath;
		private string _pathInfo;
		private string _pathTranslated;
		private string _queryString;
		private byte[] _queryStringBytes;
		private int _contentLength;
		private int _preloadedContentLength;
		private byte[] _preloadedContent;
		private string _allRawHeaders;
		private string[][] _unknownRequestHeaders;
		private string[] _knownRequestHeaders;
		private bool _specialCaseStaticFileHeaders;
		private bool _headersSent;
		private int _responseStatus;
		private StringBuilder _responseHeadersBuilder;
		private ArrayList _responseBodyBytes;
		private static char[] IntToHex = new char[]
		{
			'0', 
			'1', 
			'2', 
			'3', 
			'4', 
			'5', 
			'6', 
			'7', 
			'8', 
			'9', 
			'a', 
			'b', 
			'c', 
			'd', 
			'e', 
			'f'
		};
		public Request(Host host, Connection connection) : base(string.Empty, string.Empty, null)
		{
			this._host = host;
			this._connection = connection;
		}
		[AspNetHostingPermission(SecurityAction.Assert, Level = AspNetHostingPermissionLevel.Medium)]
		public void Process()
		{
			if (!this.TryParseRequest())
			{
				return;
			}
			if (this._verb == "POST" && this._contentLength > 0 && this._preloadedContentLength < this._contentLength)
			{
				this._connection.Write100Continue();
			}
			if (this._host.RequireAuthentication && !this.TryNtlmAuthenticate())
			{
				return;
			}
			if (this._isClientScriptPath)
			{
				this._connection.WriteEntireResponseFromFile(this._host.PhysicalClientScriptPath + this._path.Substring(this._host.NormalizedClientScriptPath.Length), false);
				return;
			}
			if (this.IsRequestForRestrictedDirectory())
			{
				this._connection.WriteErrorAndClose(403);
				return;
			}
			if (this.ProcessDefaultDocumentRequest())
			{
				return;
			}
			this.PrepareResponse();
			HttpRuntime.ProcessRequest(this);
		}
		private void Reset()
		{
			this._headerBytes = null;
			this._startHeadersOffset = 0;
			this._endHeadersOffset = 0;
			this._headerByteStrings = null;
			this._isClientScriptPath = false;
			this._verb = null;
			this._url = null;
			this._prot = null;
			this._path = null;
			this._filePath = null;
			this._pathInfo = null;
			this._pathTranslated = null;
			this._queryString = null;
			this._queryStringBytes = null;
			this._contentLength = 0;
			this._preloadedContentLength = 0;
			this._preloadedContent = null;
			this._allRawHeaders = null;
			this._unknownRequestHeaders = null;
			this._knownRequestHeaders = null;
			this._specialCaseStaticFileHeaders = false;
		}
		private bool TryParseRequest()
		{
			this.Reset();
			this.ReadAllHeaders();
            //if (!this._connection.IsLocal)//wyz
            //{
            //    this._connection.WriteErrorAndClose(403);
            //    return false;
            //}
			if (this._headerBytes == null || this._endHeadersOffset < 0 || this._headerByteStrings == null || this._headerByteStrings.Count == 0)
			{
				this._connection.WriteErrorAndClose(400);
				return false;
			}
			this.ParseRequestLine();
			if (this.IsBadPath())
			{
				this._connection.WriteErrorAndClose(400);
				return false;
			}
			if (!this._host.IsVirtualPathInApp(this._path, out this._isClientScriptPath))
			{
				this._connection.WriteErrorAndClose(404);
				return false;
			}
			this.ParseHeaders();
			this.ParsePostedContent();
			return true;
		}
		[SecurityPermission(SecurityAction.Assert, UnmanagedCode = true), SecurityPermission(SecurityAction.Assert, ControlPrincipal = true)]
		private bool TryNtlmAuthenticate()
		{
			try
			{
				using (NtlmAuth ntlmAuth = new NtlmAuth())
				{
					while (true)
					{
						string text = null;
						string text2 = this._knownRequestHeaders[24];
						if (text2 != null && text2.StartsWith("NTLM ", StringComparison.Ordinal))
						{
							text = text2.Substring(5);
						}
						if (text != null)
						{
							if (!ntlmAuth.Authenticate(text))
							{
								break;
							}
							if (ntlmAuth.Completed)
							{
								goto IL_9A;
							}
							text2 = "WWW-Authenticate: NTLM " + ntlmAuth.Blob + "\r\n";
						}
						else
						{
							text2 = "WWW-Authenticate: NTLM\r\n";
						}
						this.SkipAllPostedContent();
						this._connection.WriteErrorWithExtraHeadersAndKeepAlive(401, text2);
						if (!this.TryParseRequest())
						{
							goto Block_8;
						}
					}
					this._connection.WriteErrorAndClose(403);
					bool result = false;
					return result;
					Block_8:
					result = false;
					return result;
					IL_9A:
					if (this._host.GetProcessSID() != ntlmAuth.SID)
					{
						this._connection.WriteErrorAndClose(403);
						result = false;
						return result;
					}
				}
			}
			catch
			{
				try
				{
					this._connection.WriteErrorAndClose(500);
				}
				catch
				{
				}
				bool result = false;
				return result;
			}
			return true;
		}
		private bool TryReadAllHeaders()
		{
			byte[] array = this._connection.ReadRequestBytes(32768);
			if (array == null || array.Length == 0)
			{
				return false;
			}
			if (this._headerBytes != null)
			{
				int num = array.Length + this._headerBytes.Length;
				if (num > 32768)
				{
					return false;
				}
				byte[] array2 = new byte[num];
				Buffer.BlockCopy(this._headerBytes, 0, array2, 0, this._headerBytes.Length);
				Buffer.BlockCopy(array, 0, array2, this._headerBytes.Length, array.Length);
				this._headerBytes = array2;
			}
			else
			{
				this._headerBytes = array;
			}
			this._startHeadersOffset = -1;
			this._endHeadersOffset = -1;
			this._headerByteStrings = new ArrayList();
			ByteParser byteParser = new ByteParser(this._headerBytes);
			while (true)
			{
				ByteString byteString = byteParser.ReadLine();
				if (byteString == null)
				{
					break;
				}
				if (this._startHeadersOffset < 0)
				{
					this._startHeadersOffset = byteParser.CurrentOffset;
				}
				if (byteString.IsEmpty)
				{
					goto Block_6;
				}
				this._headerByteStrings.Add(byteString);
			}
			return true;
			Block_6:
			this._endHeadersOffset = byteParser.CurrentOffset;
			return true;
		}
		private void ReadAllHeaders()
		{
			this._headerBytes = null;
			while (this.TryReadAllHeaders())
			{
				if (this._endHeadersOffset >= 0)
				{
					return;
				}
			}
		}
		private void ParseRequestLine()
		{
			ByteString byteString = (ByteString)this._headerByteStrings[0];
			ByteString[] array = byteString.Split(' ');
			if (array == null || array.Length < 2 || array.Length > 3)
			{
				this._connection.WriteErrorAndClose(400);
				return;
			}
			this._verb = array[0].GetString();
			ByteString byteString2 = array[1];
			this._url = byteString2.GetString();
			if (this._url.IndexOf('�') >= 0)
			{
				this._url = byteString2.GetString(Encoding.Default);
			}
			if (array.Length == 3)
			{
				this._prot = array[2].GetString();
			}
			else
			{
				this._prot = "HTTP/1.0";
			}
			int num = byteString2.IndexOf('?');
			if (num > 0)
			{
				this._queryStringBytes = byteString2.Substring(num + 1).GetBytes();
			}
			else
			{
				this._queryStringBytes = new byte[0];
			}
			num = this._url.IndexOf('?');
			if (num > 0)
			{
				this._path = this._url.Substring(0, num);
				this._queryString = this._url.Substring(num + 1);
			}
			else
			{
				this._path = this._url;
				this._queryString = string.Empty;
			}
			if (this._path.IndexOf('%') >= 0)
			{
				this._path = HttpUtility.UrlDecode(this._path, Encoding.UTF8);
				num = this._url.IndexOf('?');
				if (num >= 0)
				{
					this._url = this._path + this._url.Substring(num);
				}
				else
				{
					this._url = this._path;
				}
			}
			int num2 = this._path.LastIndexOf('.');
			int num3 = this._path.LastIndexOf('/');
			if (num2 >= 0 && num3 >= 0 && num2 < num3)
			{
				int num4 = this._path.IndexOf('/', num2);
				this._filePath = this._path.Substring(0, num4);
				this._pathInfo = this._path.Substring(num4);
			}
			else
			{
				this._filePath = this._path;
				this._pathInfo = string.Empty;
			}
			this._pathTranslated = this.MapPath(this._filePath);
		}
		private bool IsBadPath()
		{
			return this._path.IndexOfAny(Request.badPathChars) >= 0 || CultureInfo.InvariantCulture.CompareInfo.IndexOf(this._path, "..", CompareOptions.Ordinal) >= 0 || CultureInfo.InvariantCulture.CompareInfo.IndexOf(this._path, "//", CompareOptions.Ordinal) >= 0;
		}
		private void ParseHeaders()
		{
			this._knownRequestHeaders = new string[40];
			ArrayList arrayList = new ArrayList();
			for (int i = 1; i < this._headerByteStrings.Count; i++)
			{
				string @string = ((ByteString)this._headerByteStrings[i]).GetString();
				int num = @string.IndexOf(':');
				if (num >= 0)
				{
					string text = @string.Substring(0, num).Trim();
					string text2 = @string.Substring(num + 1).Trim();
					int knownRequestHeaderIndex = HttpWorkerRequest.GetKnownRequestHeaderIndex(text);
					if (knownRequestHeaderIndex >= 0)
					{
						this._knownRequestHeaders[knownRequestHeaderIndex] = text2;
					}
					else
					{
						arrayList.Add(text);
						arrayList.Add(text2);
					}
				}
			}
			int num2 = arrayList.Count / 2;
			this._unknownRequestHeaders = new string[num2][];
			int num3 = 0;
			for (int j = 0; j < num2; j++)
			{
				this._unknownRequestHeaders[j] = new string[2];
				this._unknownRequestHeaders[j][0] = (string)arrayList[num3++];
				this._unknownRequestHeaders[j][1] = (string)arrayList[num3++];
			}
			if (this._headerByteStrings.Count > 1)
			{
				this._allRawHeaders = Encoding.UTF8.GetString(this._headerBytes, this._startHeadersOffset, this._endHeadersOffset - this._startHeadersOffset);
				return;
			}
			this._allRawHeaders = string.Empty;
		}
		private void ParsePostedContent()
		{
			this._contentLength = 0;
			this._preloadedContentLength = 0;
			string text = this._knownRequestHeaders[11];
			if (text != null)
			{
				try
				{
					this._contentLength = int.Parse(text, CultureInfo.InvariantCulture);
				}
				catch
				{
				}
			}
			if (this._headerBytes.Length > this._endHeadersOffset)
			{
				this._preloadedContentLength = this._headerBytes.Length - this._endHeadersOffset;
				if (this._preloadedContentLength > this._contentLength)
				{
					this._preloadedContentLength = this._contentLength;
				}
				if (this._preloadedContentLength > 0)
				{
					this._preloadedContent = new byte[this._preloadedContentLength];
					Buffer.BlockCopy(this._headerBytes, this._endHeadersOffset, this._preloadedContent, 0, this._preloadedContentLength);
				}
			}
		}
		private void SkipAllPostedContent()
		{
			if (this._contentLength > 0 && this._preloadedContentLength < this._contentLength)
			{
				byte[] array;
				for (int i = this._contentLength - this._preloadedContentLength; i > 0; i -= array.Length)
				{
					array = this._connection.ReadRequestBytes(i);
					if (array == null || array.Length == 0)
					{
						return;
					}
				}
			}
		}
		private bool IsRequestForRestrictedDirectory()
		{
			string text = CultureInfo.InvariantCulture.TextInfo.ToLower(this._path);
			if (this._host.VirtualPath != "/")
			{
				text = text.Substring(this._host.VirtualPath.Length);
			}
			string[] array = Request.restrictedDirs;
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i];
				if (text.StartsWith(text2, StringComparison.Ordinal) && (text.Length == text2.Length || text[text2.Length] == '/'))
				{
					return true;
				}
			}
			return false;
		}
		private bool ProcessDefaultDocumentRequest()
		{
			if (this._verb != "GET")
			{
				return false;
			}
			string text = this._pathTranslated;
			if (this._pathInfo.Length > 0)
			{
				text = this.MapPath(this._path);
			}
			if (!Directory.Exists(text))
			{
				return false;
			}
			if (!this._path.EndsWith("/", StringComparison.Ordinal))
			{
				string text2 = this._path + "/";
				string extraHeaders = "Location: " + Request.UrlEncodeRedirect(text2) + "\r\n";
				string body = "<html><head><title>Object moved</title></head><body>\r\n<h2>Object moved to <a href='" + text2 + "'>here</a>.</h2>\r\n</body></html>\r\n";
				this._connection.WriteEntireResponseFromString(302, extraHeaders, body, false);
				return true;
			}
			string[] array = Request.defaultFileNames;
			for (int i = 0; i < array.Length; i++)
			{
				string text3 = array[i];
				string text4 = text + "\\" + text3;
				if (File.Exists(text4))
				{
					this._path += text3;
					this._filePath = this._path;
					this._url = ((this._queryString != null) ? (this._path + "?" + this._queryString) : this._path);
					this._pathTranslated = text4;
					return false;
				}
			}
			return false;
		}
		private bool ProcessDirectoryListingRequest()
		{
			if (this._verb != "GET")
			{
				return false;
			}
			string path = this._pathTranslated;
			if (this._pathInfo.Length > 0)
			{
				path = this.MapPath(this._path);
			}
			if (!Directory.Exists(path))
			{
				return false;
			}
			if (this._host.DisableDirectoryListing)
			{
				return false;
			}
			FileSystemInfo[] elements = null;
			try
			{
				elements = new DirectoryInfo(path).GetFileSystemInfos();
			}
			catch
			{
			}
			string text = null;
			if (this._path.Length > 1)
			{
				int num = this._path.LastIndexOf('/', this._path.Length - 2);
				text = ((num > 0) ? this._path.Substring(0, num) : "/");
				if (!this._host.IsVirtualPathInApp(text))
				{
					text = null;
				}
			}
			this._connection.WriteEntireResponseFromString(200, "Content-type: text/html; charset=utf-8\r\n", Messages.FormatDirectoryListing(this._path, text, elements), false);
			return true;
		}
		private static string UrlEncodeRedirect(string path)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(path);
			int num = bytes.Length;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				if ((bytes[i] & 128) != 0)
				{
					num2++;
				}
			}
			if (num2 > 0)
			{
				byte[] array = new byte[num + num2 * 2];
				int num3 = 0;
				for (int j = 0; j < num; j++)
				{
					byte b = bytes[j];
					if ((b & 128) == 0)
					{
						array[num3++] = b;
					}
					else
					{
						array[num3++] = 37;
						array[num3++] = (byte)Request.IntToHex[b >> 4 & 15];
						array[num3++] = (byte)Request.IntToHex[(int)(b & 15)];
					}
				}
				path = Encoding.ASCII.GetString(array);
			}
			if (path.IndexOf(' ') >= 0)
			{
				path = path.Replace(" ", "%20");
			}
			return path;
		}
		private void PrepareResponse()
		{
			this._headersSent = false;
			this._responseStatus = 200;
			this._responseHeadersBuilder = new StringBuilder();
			this._responseBodyBytes = new ArrayList();
		}
		public override string GetUriPath()
		{
			return this._path;
		}
		public override string GetQueryString()
		{
			return this._queryString;
		}
		public override byte[] GetQueryStringRawBytes()
		{
			return this._queryStringBytes;
		}
		public override string GetRawUrl()
		{
			return this._url;
		}
		public override string GetHttpVerbName()
		{
			return this._verb;
		}
		public override string GetHttpVersion()
		{
			return this._prot;
		}
		public override string GetRemoteAddress()
		{
			this._connectionPermission.Assert();
			return this._connection.RemoteIP;
		}
		public override int GetRemotePort()
		{
			return 0;
		}
		public override string GetLocalAddress()
		{
			this._connectionPermission.Assert();
			return this._connection.LocalIP;
		}
		public override string GetServerName()
		{
			string localAddress = this.GetLocalAddress();
			if (localAddress.Equals("127.0.0.1") || localAddress.Equals("::1") || localAddress.Equals("::ffff:127.0.0.1"))
			{
				return "localhost";
			}
			return localAddress;
		}
		public override int GetLocalPort()
		{
			return this._host.Port;
		}
		public override string GetFilePath()
		{
			return this._filePath;
		}
		public override string GetFilePathTranslated()
		{
			return this._pathTranslated;
		}
		public override string GetPathInfo()
		{
			return this._pathInfo;
		}
		public override string GetAppPath()
		{
			return this._host.VirtualPath;
		}
		public override string GetAppPathTranslated()
		{
			return this._host.PhysicalPath;
		}
		public override byte[] GetPreloadedEntityBody()
		{
			return this._preloadedContent;
		}
		public override bool IsEntireEntityBodyIsPreloaded()
		{
			return this._contentLength == this._preloadedContentLength;
		}
		public override int ReadEntityBody(byte[] buffer, int size)
		{
			int num = 0;
			this._connectionPermission.Assert();
			byte[] array = this._connection.ReadRequestBytes(size);
			if (array != null && array.Length > 0)
			{
				num = array.Length;
				Buffer.BlockCopy(array, 0, buffer, 0, num);
			}
			return num;
		}
		public override string GetKnownRequestHeader(int index)
		{
			return this._knownRequestHeaders[index];
		}
		public override string GetUnknownRequestHeader(string name)
		{
			int num = this._unknownRequestHeaders.Length;
			for (int i = 0; i < num; i++)
			{
				if (string.Compare(name, this._unknownRequestHeaders[i][0], StringComparison.OrdinalIgnoreCase) == 0)
				{
					return this._unknownRequestHeaders[i][1];
				}
			}
			return null;
		}
		public override string[][] GetUnknownRequestHeaders()
		{
			return this._unknownRequestHeaders;
		}
		public override string GetServerVariable(string name)
		{
			string result = string.Empty;
			if (name != null)
			{
				if (!(name == "ALL_RAW"))
				{
					if (!(name == "SERVER_PROTOCOL"))
					{
						if (!(name == "LOGON_USER"))
						{
							if (name == "AUTH_TYPE")
							{
								if (this.GetUserToken() != IntPtr.Zero)
								{
									result = "NTLM";
								}
							}
						}
						else
						{
							if (this.GetUserToken() != IntPtr.Zero)
							{
								result = this._host.GetProcessUser();
							}
						}
					}
					else
					{
						result = this._prot;
					}
				}
				else
				{
					result = this._allRawHeaders;
				}
			}
			return result;
		}
		public override IntPtr GetUserToken()
		{
			return this._host.GetProcessToken();
		}
		public override string MapPath(string path)
		{
			string text = string.Empty;
			bool flag = false;
			if (path == null || path.Length == 0 || path.Equals("/"))
			{
				if (this._host.VirtualPath == "/")
				{
					text = this._host.PhysicalPath;
				}
				else
				{
					text = Environment.SystemDirectory;
				}
			}
			else
			{
				if (this._host.IsVirtualPathAppPath(path))
				{
					text = this._host.PhysicalPath;
				}
				else
				{
					if (this._host.IsVirtualPathInApp(path, out flag))
					{
						if (flag)
						{
							text = this._host.PhysicalClientScriptPath + path.Substring(this._host.NormalizedClientScriptPath.Length);
						}
						else
						{
							text = this._host.PhysicalPath + path.Substring(this._host.NormalizedVirtualPath.Length);
						}
					}
					else
					{
						if (path.StartsWith("/", StringComparison.Ordinal))
						{
							text = this._host.PhysicalPath + path.Substring(1);
						}
						else
						{
							text = this._host.PhysicalPath + path;
						}
					}
				}
			}
			text = text.Replace('/', '\\');
			if (text.EndsWith("\\", StringComparison.Ordinal) && !text.EndsWith(":\\", StringComparison.Ordinal))
			{
				text = text.Substring(0, text.Length - 1);
			}
			return text;
		}
		public override void SendStatus(int statusCode, string statusDescription)
		{
			this._responseStatus = statusCode;
		}
		public override void SendKnownResponseHeader(int index, string value)
		{
			if (this._headersSent)
			{
				return;
			}
			switch (index)
			{
				case 1:
				case 2:
				{
					break;
				}
				default:
				{
					switch (index)
					{
						case 18:
						case 19:
						{
							if (this._specialCaseStaticFileHeaders)
							{
								return;
							}
							break;
						}
						case 20:
						{
							if (value == "bytes")
							{
								this._specialCaseStaticFileHeaders = true;
								return;
							}
							break;
						}
						default:
						{
							if (index == 26)
							{
								return;
							}
							break;
						}
					}
					this._responseHeadersBuilder.Append(HttpWorkerRequest.GetKnownResponseHeaderName(index));
					this._responseHeadersBuilder.Append(": ");
					this._responseHeadersBuilder.Append(value);
					this._responseHeadersBuilder.Append("\r\n");
					return;
				}
			}
		}
		public override void SendUnknownResponseHeader(string name, string value)
		{
			if (this._headersSent)
			{
				return;
			}
			this._responseHeadersBuilder.Append(name);
			this._responseHeadersBuilder.Append(": ");
			this._responseHeadersBuilder.Append(value);
			this._responseHeadersBuilder.Append("\r\n");
		}
		public override void SendCalculatedContentLength(int contentLength)
		{
			if (!this._headersSent)
			{
				this._responseHeadersBuilder.Append("Content-Length: ");
				this._responseHeadersBuilder.Append(contentLength.ToString(CultureInfo.InvariantCulture));
				this._responseHeadersBuilder.Append("\r\n");
			}
		}
		public override bool HeadersSent()
		{
			return this._headersSent;
		}
		public override bool IsClientConnected()
		{
			this._connectionPermission.Assert();
			return this._connection.Connected;
		}
		public override void CloseConnection()
		{
			this._connectionPermission.Assert();
			this._connection.Close();
		}
		public override void SendResponseFromMemory(byte[] data, int length)
		{
			if (length > 0)
			{
				byte[] array = new byte[length];
				Buffer.BlockCopy(data, 0, array, 0, length);
				this._responseBodyBytes.Add(array);
			}
		}
		public override void SendResponseFromFile(string filename, long offset, long length)
		{
			if (length == 0L)
			{
				return;
			}
			FileStream fileStream = null;
			try
			{
				fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
				this.SendResponseFromFileStream(fileStream, offset, length);
			}
			finally
			{
				if (fileStream != null)
				{
					fileStream.Close();
				}
			}
		}
		public override void SendResponseFromFile(IntPtr handle, long offset, long length)
		{
			if (length == 0L)
			{
				return;
			}
			FileStream fileStream = null;
			try
			{
				SafeFileHandle handle2 = new SafeFileHandle(handle, false);
				fileStream = new FileStream(handle2, FileAccess.Read);
				this.SendResponseFromFileStream(fileStream, offset, length);
			}
			finally
			{
				if (fileStream != null)
				{
					fileStream.Close();
					fileStream = null;
				}
			}
		}
		private void SendResponseFromFileStream(FileStream f, long offset, long length)
		{
			long length2 = f.Length;
			if (length == -1L)
			{
				length = length2 - offset;
			}
			if (length == 0L || offset < 0L || length > length2 - offset)
			{
				return;
			}
			if (offset > 0L)
			{
				f.Seek(offset, SeekOrigin.Begin);
			}
			if (length <= 65536L)
			{
				byte[] array = new byte[(int)length];
				int length3 = f.Read(array, 0, (int)length);
				this.SendResponseFromMemory(array, length3);
				return;
			}
			byte[] array2 = new byte[65536];
			int i = (int)length;
			while (i > 0)
			{
				int count = (i < 65536) ? i : 65536;
				int num = f.Read(array2, 0, count);
				this.SendResponseFromMemory(array2, num);
				i -= num;
				if (i > 0 && num > 0)
				{
					this.FlushResponse(false);
				}
			}
		}
		public override void FlushResponse(bool finalFlush)
		{
			if (this._responseStatus == 404 && !this._headersSent && finalFlush && this._verb == "GET" && this.ProcessDirectoryListingRequest())
			{
				return;
			}
			this._connectionPermission.Assert();
			if (!this._headersSent)
			{
				this._connection.WriteHeaders(this._responseStatus, this._responseHeadersBuilder.ToString());
				this._headersSent = true;
			}
			for (int i = 0; i < this._responseBodyBytes.Count; i++)
			{
				byte[] array = (byte[])this._responseBodyBytes[i];
				this._connection.WriteBody(array, 0, array.Length);
			}
			this._responseBodyBytes = new ArrayList();
			if (finalFlush)
			{
				this._connection.Close();
			}
		}
		public override void EndOfRequest()
		{
		}
	}
}
