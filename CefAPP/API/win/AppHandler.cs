using AppHandleMessage;
using CefAPP.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CefAPP.API.win
{
    public class AppHandler
    {
        public static AppHandler Intance = new AppHandler();
        public string getDBUser()
        {
            return "{\"username\":\"zhangsan\", \"pwd\":\"123456\"}";
        }
        public void closeApp()
        {
            new CloseAppMessage().Send();
        }
        public void startDebug()
        {
            AppCommon.LoginFrm._browser.ShowDevTools();
        }
    }
}
