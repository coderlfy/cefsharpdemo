using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CefSharp.WinForms;
using BZXT.ClientFramework.ClientEntity;
using BZXT.ClientFramework.ClientInterface;
using CefSharp;

namespace CefBrowser
{
    public partial class KSRWBrowers :  ModuleBase, IModule
    {
        private ChromiumWebBrowserEx m_chromeBrowser = null;
        private JavaScriptInteractionObj m_jsInteractionObj = null;
        public KSRWBrowers()
        {
            InitializeComponent();
        }
         private delegate void MyInvoke(ModuleMsg msg);
        private void RecvieMsg_Handler(ModuleMsg msg)
        {
            switch (msg.MsgType)
            {
                case ModuleMsgType.All:
                    {
                        MyInvoke myinvoke = new MyInvoke(EventHandler_AllMsg);
                        BeginInvoke(myinvoke, new object[] { msg });
                        break;
                    }
                case ModuleMsgType.Single:
                    {
                        //MyInvoke myinvoke = new MyInvoke(EventHandler_SingleMsg);
                        //BeginInvoke(myinvoke, new object[] { msg });
                        break;
                    }
                default:
                    break;
            }

        }
        private void EventHandler_AllMsg(ModuleMsg msg)
        {
            switch (msg.TargetKey)
            {
                case "InitKSRWWeb":
                    var script = string.Format("getKsRwData({0});", msg.Content); 
                    m_chromeBrowser.ExecuteScriptAsync(script);
                    break;
                case "ShowTools":
                    {
                        m_chromeBrowser.ShowDevTools();
                        break;
                    }
                default:
                    break;
            }
        }
        public void Init(string CnOrEn = "cn", string webports = "9001,9002,9003,9004,9005")
        {
            if (m_chromeBrowser == null)
            {
                string[] webportsarr = webports.Split(',');
                int webport = int.Parse(webportsarr[4]);

                string page = string.Format("http://127.0.0.1:{0}/WDk12web/app/jsp/extend/ksrw.html", webport);
                m_chromeBrowser = new ChromiumWebBrowserEx(page);
                m_chromeBrowser.WebPort = webport;
                m_chromeBrowser.WebapiPort = int.Parse(webportsarr[2]);
                m_chromeBrowser.WebsocketPort = int.Parse(webportsarr[1]);
                BrowserSettings browserSettings = new BrowserSettings();

                browserSettings.FileAccessFromFileUrlsAllowed = true;//以file://协议访问文件允许权限
                browserSettings.UniversalAccessFromFileUrlsAllowed = true;
                browserSettings.TextAreaResizeDisabled = true;
                //browserSettings.WebSecurityDisabled = true;
                //browserSettings.JavascriptDisabled = false;
                m_chromeBrowser.BrowserSettings = browserSettings;
                Controls.Add(m_chromeBrowser);


                m_jsInteractionObj = new JavaScriptInteractionObj(sendMsgEvent);
                m_jsInteractionObj.SetChromeBrowser(m_chromeBrowser);
                m_chromeBrowser.RegisterJsObject("winformObj", m_jsInteractionObj); 
                m_chromeBrowser.MenuHandler = new MenuHandler();               

            }
            else
            {

            }
        }
        private void WebBrowers_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F1)
            {
                m_chromeBrowser.ShowDevTools();
                //button1.PerformClick();// 执行按钮“1”的操作
                //e.Handled = true;
            }
        }
        private void KSRWBrowers_Load_1(object sender, EventArgs e)
        {
            receiveMsgEvent += new ReciveMsgEvent(RecvieMsg_Handler);
        }
    }

}
