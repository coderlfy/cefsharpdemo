using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppHandleMessage
{
    public enum AppModule
    {
        MyDesktop,
        InstalledApps,
        AppStoreCenter,
        Uninstall,
        UpdateApp,
        DownManage,
        Login,
        DownloadSet,
        AppDetail,
        MyDesktopStyle,
        LoginName,
        CustomerVip
    }
    public class AppModuleSetting
    {
        public static AppModule GetAppModuleFromString(string appModule)
        {
            #region
            return (AppModule)Enum.Parse(typeof(AppModule), appModule.Replace("\"", ""), true);
            #endregion
        }
    }
}
