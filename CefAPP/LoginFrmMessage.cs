using AppHandleMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CefAPP
{
    public partial class LoginFrm
    {
        //private ForceLogoutMessage _forceLogoutMessage = null;
        private MessageManager _messageManager = null;
        private CloseAppMessage _closeAppMessage = null;
        private void bindMessage()
        {
            #region
            _messageManager = new MessageManager();
            _messageManager.AddReceiveMessage(this.createCloseAppMessage());
            //_messageManager.AddReceiveMessage(this.createTransferMessage());
            //_messageManager.AddReceiveMessage(this.createCurrentChatUserMessage());
            //_messageManager.AddReceiveMessage(this.createCloseMainFromMessage());

            #endregion
        }

        protected override void DefWndProc(
    ref System.Windows.Forms.Message m)
        {
            #region

            _messageManager.Receive(base.DefWndProc, ref m);
            #endregion
        }


        private AppHandleMessage.Message createCloseAppMessage()
        {
            #region
            _closeAppMessage = new CloseAppMessage();
            _closeAppMessage.CloseApp = new Message.DlReslove(() =>
            {
                if (this.InvokeRequired)
                    this.Close();
                else
                    this.BeginInvoke(new Action(() => {
                        this.Close();
                    }));
            });
            return _closeAppMessage;
            #endregion
        }
    }
}
