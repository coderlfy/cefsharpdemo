using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VisualStudio.WebHost
{
    internal class DateTimeHelper
    {

        //放在公共区域
        [DllImport("Kernel32.dll")]
        public static extern bool SetSystemTime(ref SystemTime sysTime);

        [DllImport("Kernel32.dll")]
        public static extern void GetSystemTime(ref SystemTime sysTime);

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMiliseconds;
        }

        /// <summary> 
        /// 设置系统时间 
        /// </summary> 
        public static Boolean SyncTime(DateTime currentTime)
        {
            Boolean flag = false;
            try
            {
                SystemTime sysTime = new SystemTime();
                sysTime.wYear = Convert.ToUInt16(currentTime.Year);
                sysTime.wMonth = Convert.ToUInt16(currentTime.Month);
                sysTime.wDay = Convert.ToUInt16(currentTime.Day);
                sysTime.wDayOfWeek = Convert.ToUInt16(currentTime.DayOfWeek);
                sysTime.wMinute = Convert.ToUInt16(currentTime.Minute);
                sysTime.wSecond = Convert.ToUInt16(currentTime.Second);
                sysTime.wMiliseconds = Convert.ToUInt16(currentTime.Millisecond);

                //处理北京时间 
                int nBeijingHour = currentTime.Hour - 8;
                if (nBeijingHour <= 0)
                {
                    nBeijingHour = 24;
                    sysTime.wDay = Convert.ToUInt16(currentTime.Day - 1);
                    //sysTime.wDayOfWeek = Convert.ToUInt16(current.DayOfWeek - 1); 
                }
                else
                {
                    sysTime.wDay = Convert.ToUInt16(currentTime.Day);
                    sysTime.wDayOfWeek = Convert.ToUInt16(currentTime.DayOfWeek);
                }
                sysTime.wHour = Convert.ToUInt16(nBeijingHour);

                SetSystemTime(ref sysTime);//设置本机时间
                flag = true;
            }
            catch (Exception)
            {

                flag = false;
            }
            return flag;
        }
        private static bool IsSyncTime = false;
        /// 获取网络日期时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNetDateTime()
        {
            if (IsSyncTime)
                return DateTime.Now;
            //IsSyncTime = true;

            WebRequest request = null;
            WebResponse response = null;
            WebHeaderCollection headerCollection = null;
            string datetime = string.Empty;
            try
            {
                request = WebRequest.Create("https://www.baidu.com");
                request.Timeout = 3000;
                request.Credentials = CredentialCache.DefaultCredentials;
                response = (WebResponse)request.GetResponse();
                headerCollection = response.Headers;
                foreach (var h in headerCollection.AllKeys)
                { if (h == "Date") { datetime = headerCollection[h]; } }
                DateTime time = DateTime.Parse(datetime);
                TimeSpan ts = time - DateTime.Now;
                if (System.Math.Abs(ts.Days) > 0 || System.Math.Abs(ts.Hours) > 0 || System.Math.Abs(ts.Minutes) > 0)
                    IsSyncTime = SyncTime(time);
                else
                    IsSyncTime = true;
                return time;//DateTime.Parse(datetime);
            }
            catch (Exception) { IsSyncTime = true; return DateTime.Now; }
            finally
            {
                if (request != null)
                { request.Abort(); }
                if (response != null)
                { response.Close(); }
                if (headerCollection != null)
                { headerCollection.Clear(); }
            }
        }
    }
}








