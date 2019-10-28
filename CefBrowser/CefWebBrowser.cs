
using AppHandleMessage;
using CefSharp.WinForms;
using Common;
//using ChatControl.ChatRichTextBox;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
//using WDIMUI.common;
//using WDY.Internationalized;
//using WDY.Tools;
//using WDY.Utils;
//using WDY.Utils.Common;

namespace CefBrowser
{
    public class CefWebBrowser : ChromiumWebBrowser
    {
        //private WebLocationToMessage _webLocationToMessage = null;
        //private ActiveWebUITabMessage _activeWebUITabMessage = null;
        //private ViewChatRecvMessage _viewChatRecvMessage = null;
        //private CloseAppMessage _closeAppMessage = null;
        private MessageManager _messageManager = null;


        //private PopImageMessage _popImageMessage = null;
        public CefWebBrowser(string address)
            : base(address)
        {
            #region

            _messageManager = new MessageManager();
            //_messageManager.AddReceiveMessage(this.createCloseAppMessage());
            //_messageManager.AddReceiveMessage(this.createActiveWebUITabMessage());
            //_messageManager.AddReceiveMessage(this.createViewChatRecvMessage());
            //_messageManager.AddReceiveMessage(this.createAddToRecvRecordMessage());
            //_messageManager.AddReceiveMessage(this.createPopImageMessage());


            this.Load(string.Format(
                "http://localhost:{0}/login.html", ConstVar.WebPort));

            #endregion
        }
        //private AppHandleMessage.Message createCloseAppMessage()
        //{
        //    #region
        //    _closeAppMessage = new CloseAppMessage();
        //    _closeAppMessage.CloseApp = new Message.DlReslove(() =>
        //    {
        //        AppCommon.

        //    });
        //    return _closeAppMessage;
        //    #endregion
        //}
        /*
        private AppHandleMessage.Message createViewChatRecvMessage()
        {
            #region
            _viewChatRecvMessage = new ViewChatRecvMessage();
            _viewChatRecvMessage.ResloveRecvViewChat =
                new ViewChatRecvMessage.DlResloveRecvViewChat((pars) =>
                {
                    this.ExecuteScriptAsync(string.Format("chatInit({0})", JsonHelper.Get(pars)));
                });
            return _viewChatRecvMessage;
            #endregion
        }

        private AppHandleMessage.Message createAddToRecvRecordMessage()
        {
            #region
            _addToRecvRecordMessage = new AddToRecvRecordMessage();
            _addToRecvRecordMessage.ResolveAddRecordToRecv =
                new AddToRecvRecordMessage.DlResolveAddRecordToRecv((records) =>
                {
                    if (records.Count > 0)
                    {
                        this.ExecuteScriptAsync(string.Format("sendFun({0})", JsonHelper.Get(records)));
                    }
                }); 
            return _addToRecvRecordMessage;
            #endregion
        }
        */


        //private AppHandleMessage.Message createWebLocationToMessage()
        //{
        //    #region
        //    _webLocationToMessage = new WebLocationToMessage();
        //    _webLocationToMessage.LocationTo = new
        //        WebLocationToMessage.DlReslove((uiurl) =>
        //        {
        //            this.Load("http://localhost:9006/webapp/app/jsp/index.html");

        //        });
        //    return _webLocationToMessage;
        //    #endregion
        //}
        //private AppHandleMessage.Message createActiveWebUITabMessage()
        //{
        //    #region
        //    _activeWebUITabMessage = new ActiveWebUITabMessage();

        //    _activeWebUITabMessage.ActiveTab = new 
        //        ActiveWebUITabMessage.DlActiveTab((tabname) =>
        //    {
        //        string script = string.Format("hfTab('{0}');", tabname);
        //        this.ExecuteScriptAsync(script);

        //    });



        //    return _activeWebUITabMessage;
        //    #endregion
        //}


        protected override void DefWndProc(
            ref System.Windows.Forms.Message m)
        {
            _messageManager.Receive(base.DefWndProc, ref m);
        }

    }
}
