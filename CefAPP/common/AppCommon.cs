using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;

namespace CefAPP.common
{
    public class AppCommon
    {
        public static LoginFrm LoginFrm = null;
        /// <summary>
        /// 获取可用端口
        /// </summary>
        /// <param name="startPort"></param>
        /// <returns></returns>
        public static int GetEnablePort(
            int startPort)
        {
            #region
            while (true)
            {
                if (!portInUse(startPort))
                    break;
                startPort++;
            }
            return startPort;
            #endregion
        }
        /// <summary>
        /// 查询端口port是否被用
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        private static bool portInUse(
            int port)
        {
            #region
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }

            return inUse;
            #endregion
        }

    }
}
