using AppHandleMessage;
using CefAPP.API.win;
using CefBrowser;
using CefSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CefAPP
{
    public partial class LoginFrm : Form
    {
        public CefWebBrowser _browser = null;
        public LoginFrm()
        {
            InitializeComponent();

            this.bindMessage();
        }

        private void LoginFrm_Load(
            object sender, EventArgs e)
        {
            #region
            this.addBrowser();
            ControlSet.LoginFormHandle = this.Handle;
            #endregion
        }

        private void addBrowser()
        {
            #region
            BrowserSettings browserSettings = new BrowserSettings();
            browserSettings.FileAccessFromFileUrlsAllowed = true;
            browserSettings.UniversalAccessFromFileUrlsAllowed = true;
            browserSettings.TextAreaResizeDisabled = true;
            browserSettings.DefaultEncoding = "utf-8";

            //browserSettings.cef
            this._browser = new CefWebBrowser("");
            this._browser.BrowserSettings = browserSettings;
            this._browser.Dock = DockStyle.Fill;
            this.Controls.Add(this._browser);

            //将c#定义的对象映射到js中
            this._browser.RegisterJsObject("appHandler", AppHandler.Intance);
            #endregion
        }

        private void LoginFrm_FormClosing(
            object sender, FormClosingEventArgs e)
        {
            Cef.Shutdown();
        }

        /// <summary>
        /// 演示如何操作浏览器中的元素或调用js方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSetUser_Click(
            object sender, EventArgs e)
        {
            #region
            var script = string.Format("setUserinfor('{0}','{1}');", "zhangsan", "123456");
            this._browser.ExecuteScriptAsync(script);
            #endregion
        }
    }
}
