using System;
using System.Globalization;
using System.Resources;
using System.Threading;
namespace Microsoft.VisualStudio.WebHost
{
	internal sealed class SR
	{
		internal const string WEBDEV_ServerError = "WEBDEV_ServerError";
		internal const string WEBDEV_HTTPError = "WEBDEV_HTTPError";
		internal const string WEBDEV_VersionInfo = "WEBDEV_VersionInfo";
		internal const string WEBDEV_VWDName = "WEBDEV_VWDName";
		internal const string WEBDEV_DirListing = "WEBDEV_DirListing";
		internal const string WEBDEV_AssemblyVersion = "WEBDEV_AssemblyVersion";
		private static SR loader;
		private ResourceManager resources;
		private static CultureInfo Culture
		{
			get
			{
				return null;
			}
		}
		public static ResourceManager Resources
		{
			get
			{
				return SR.GetLoader().resources;
			}
		}
		internal SR()
		{
			this.resources = new ResourceManager("WebHost", base.GetType().Assembly);
		}
		private static SR GetLoader()
		{
			if (SR.loader == null)
			{
				SR value = new SR();
				Interlocked.CompareExchange<SR>(ref SR.loader, value, null);
			}
			return SR.loader;
		}
		public static string GetString(string name, params object[] args)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			string @string = sR.resources.GetString(name, SR.Culture);
			if (args != null && args.Length > 0)
			{
				for (int i = 0; i < args.Length; i++)
				{
					string text = args[i] as string;
					if (text != null && text.Length > 1024)
					{
						args[i] = text.Substring(0, 1021) + "...";
					}
				}
				return string.Format(CultureInfo.CurrentCulture, @string, args);
			}
			return @string;
		}
		public static string GetString(string name)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			return sR.resources.GetString(name, SR.Culture);
		}
		public static string GetString(string name, out bool usedFallback)
		{
			usedFallback = false;
			return SR.GetString(name);
		}
		public static object GetObject(string name)
		{
			SR sR = SR.GetLoader();
			if (sR == null)
			{
				return null;
			}
			return sR.resources.GetObject(name, SR.Culture);
		}
	}
}
