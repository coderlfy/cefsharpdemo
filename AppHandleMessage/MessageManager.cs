using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using WDY.Tools;
//using WDY.Utils;

namespace AppHandleMessage
{
    public class MessageManager
    {
        private List<Message> _messages = null;
        public List<Message> Messages
        {
            get
            {
                if (_messages == null)
                {
                    _messages = new List<Message>();
                }
                return this._messages;
            }
            set
            {
                this._messages = value;
            }
        }

        public void AddReceiveMessage(
            Message message)
        {
            this.Messages.Add(message);
            message.Manager = this;
        }

        public delegate void DLDefWndProc(ref System.Windows.Forms.Message m);

        public void Receive(
            DLDefWndProc proc, 
            ref System.Windows.Forms.Message m)
        {
            #region
            proc(ref m);
            foreach (Message msg in this.Messages)
            {
                if (msg.MessageCode == m.Msg)
                {
                    TrafficMsg.COPYDATASTRUCT mystr = new TrafficMsg.COPYDATASTRUCT();
                    Type mytype = mystr.GetType();
                    try
                    {
                        mystr = (TrafficMsg.COPYDATASTRUCT)m.GetLParam(mytype);

                    }
                    catch(Exception e)
                    {
                        System.Console.WriteLine(e);
                        //Logger.ErrorWriteLog(e.ToString());
                    }

                    msg.Receive(mystr.lpData);
                    return;
                }
            }
            #endregion
        }
        
    }
}
