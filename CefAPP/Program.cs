using CefAPP.common;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CefAPP
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            //获取分配的web服务端口
            Common.ConstVar.WebPort = AppCommon.GetEnablePort(Common.ConstVar.WebPort);

            //启动web服务
            ServiceManager.StartService(
                Common.ConstVar.WebPort, 
                Path.Combine(Directory.GetCurrentDirectory(), "webroot"));

            //cef配置初始化
            CefSettings cefsetting = new CefSettings();
            cefsetting.Locale = "zh_CN";
            cefsetting.IgnoreCertificateErrors = true;
            cefsetting.LogSeverity = LogSeverity.Disable;
            cefsetting.CefCommandLineArgs.Add("Enable JavaScript source maps", "false");
            cefsetting.CefCommandLineArgs.Add("no-proxy-server", "1");
            Cef.Initialize(cefsetting);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AppCommon.LoginFrm = new LoginFrm();
            Application.Run(AppCommon.LoginFrm);
        }

    }
}
