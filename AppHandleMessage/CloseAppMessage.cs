using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppHandleMessage
{
    public class CloseAppMessage : Message
    {
        public DlReslove CloseApp { get; set; }
        public new void Send()
        {
            #region

            this.ToControls = new List<IntPtr>();
            this.ToControls.Add(ControlSet.LoginFormHandle);

            base.Send();
            #endregion
        }

        public override object GetData()
        {
            return "";
        }

        public override void Receive(string json)
        {
            if (this.CloseApp != null)
                this.CloseApp();
        }
    }
}
