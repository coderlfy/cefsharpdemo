using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using WDY.Tools;
//using WDY.Utils;

namespace AppHandleMessage
{
    public class MessageData
    {
        private int _messageCode;

        public int MessageCode
        {
            get { return _messageCode; }
            set { _messageCode = value; }
        }

        private string _content;

        public string Content
        {
            get { return _content; }
            set { _content = value; }
        }
        
    }
    public abstract class Message
    {
        public delegate void DlReslove();
        public delegate void DlResloveByStr(String msg);

        private List<IntPtr> _toControls = null;

        public List<IntPtr> ToControls
        {
            get { return _toControls; }
            set { _toControls = value; }
        }

        private int _messageCode;

        public int MessageCode
        {
            get { return _messageCode; }
            set { _messageCode = value; }
        }

        private MessageManager _manager = null;

        public MessageManager Manager
        {
            get { return _manager; }
            set { _manager = value; }
        }        
        
        protected void Send()
        {
            #region
            try
            {
                TrafficMsg.COPYDATASTRUCT data = new TrafficMsg.COPYDATASTRUCT();
                data.cbData = 0;
                object orgdata = this.GetData();
                data.lpData = orgdata.GetType().Equals(typeof(string)) ? orgdata.ToString() : JsonHelper.Get(orgdata);

                //if (ControlSet.MainForm.InvokeRequired)
                //{
                //    ControlSet.MainForm.Invoke(new Action(() =>
                //    {
                //        this.sendMessage(this.ToControls, this.MessageCode, ref data);
                //    }));
                //}
                //else
                    this.sendMessage(this.ToControls, this.MessageCode, ref data);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            #endregion
        }
        private void sendMessage(
            List<IntPtr> controls, 
            int msgCode, 
            ref TrafficMsg.COPYDATASTRUCT data)
        {
            #region
            foreach (IntPtr c in controls)
                TrafficMsg.SendMessage(c, msgCode, 0, ref data);
            
            #endregion
        }
        public abstract object GetData();

        public abstract void Receive(string json);
    }
}
