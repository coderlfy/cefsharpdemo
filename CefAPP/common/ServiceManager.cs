using Microsoft.VisualStudio.WebHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CefAPP.common
{
    public class ServiceManager
    {
        private static Server _server;
        /// <summary>
        /// 开启web站点
        /// </summary>
        /// <param name="port"></param>
        /// <param name="root"></param>
        public static void StartService(
            int port, 
            string root)
        {
            #region
            if (_server == null)
                _server = new Server(port, "/", root);
            
            _server.Start();
            #endregion
        }

        public static void StopService()
        {
            #region
            _server.Stop();
            #endregion
        }
    }
}
