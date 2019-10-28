using CefSharp;
using CefSharp.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WDCloud.Service.DAL;

namespace CefBrowser
{
    /// <summary>
    /// HTML页面，JS嵌入对象
    /// </summary>
    public class JavaScriptInteractionObj
    {

        [JavascriptIgnore]
        public ChromiumWebBrowserEx m_chromeBrowser { get; set; }
        /// <summary>
        /// HTML页面，JS嵌入对象
        /// </summary>
        /// <param name="MsgEvent">外部发送消息委托</param>
        public JavaScriptInteractionObj(ModuleBase.SendMsgEvent MsgEvent)
        {
            sendMsgEvent = MsgEvent;
        }

        [JavascriptIgnore]
        public void SetChromeBrowser(ChromiumWebBrowserEx b)
        {
            m_chromeBrowser = b;
        }

        /// <summary>
        /// 外部发送消息委托
        /// </summary>
        private ModuleBase.SendMsgEvent sendMsgEvent;
        /// <summary>
        /// JS消息发送，转发至主框架
        /// 注意：js脚本调用时，函数首字母为小写，应为 winformObj.sendMsg('abc','cccc');
        /// </summary>
        /// <param name="key">消息KEY,用来识别消息</param>
        /// <param name="content">消息内容，用来定义消息内容</param>
        public void SendMsg(string key,string content)
        {
            if (sendMsgEvent != null)
            {//调用委托事件处理
                ModuleMsg msgobj = new ModuleMsg();
                msgobj.MsgType = ModuleMsgType.All;
                msgobj.TargetKey = key;
                msgobj.Content = content;
                sendMsgEvent(msgobj);
            }
        }

        public string getWebApiPort()
        {
            return this.m_chromeBrowser.WebapiPort.ToString();
        }

        public string getWebPort()
        {
            return this.m_chromeBrowser.WebPort.ToString();
        }
        /// <summary>
        /// 主框架接收消息，转发至当前页面JS
        /// 注意：当前页面必须实现JS方法 ReciveMsgHandler(string key,string content);
        /// </summary>
        /// <param name="key">消息KEY,用来识别消息</param>
        /// <param name="content">消息内容，用来定义消息内容</param>
        public void ReciveMsg(string key, string content)
        {
            var js = string.Format("ReciveFormMsgHandler('{0}','{1}');", key, content);
            //m_chromeBrowser.ExecuteScriptAsync(js);
        }
        /// <summary>
        /// 框架执行当前页面包含的JS方法
        /// </summary>
        /// <param name="script">JS方法名，如"alert(123);"</param>
        public void ExecJSFromWinForms(string script)
        {
            m_chromeBrowser.ExecuteScriptAsync(script);
        }
        /// <summary>
        /// 动态调用方法
        /// </summary>
        /// <param name="FuncName"></param>
        /// <param name="FuncParam"></param>
        /// <returns></returns>
        public string CallFunction(string FuncName,string FuncParam)
        {
            switch(FuncName)
            {
                case "OpenFileDialog":
                    return Function_OpenFileDialog(FuncParam);
                case "GetWorkAreaHeight":
                    return (m_chromeBrowser.Height - 70).ToString();                 
                default:
                    break;

            }
            return "";
        }
        public string getcurzh()
        {
            return LoginDAL.curTeacher.zh;
        }
        #region 动态调用方法定义

        public delegate string delegate_OpenFileDialog(string gs);
        public delegate_OpenFileDialog delegateOpenFileDialog;
        /// <summary>
        /// 打开文件查找对话框
        /// </summary>
        /// <param name="gs">
        /// 一种文件多种格式 标签|*.jpg;*.png;*.gif  
        /// 多种文件多种格式 标签|*.jpg|标签|*.text</param>
        /// <returns></returns>
        private string Function_OpenFileDialog(string gs)
        {
            return delegateOpenFileDialog(gs);
        }
        #endregion
    }
}
