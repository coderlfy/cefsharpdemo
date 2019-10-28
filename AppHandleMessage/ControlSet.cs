using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AppHandleMessage
{
    public class ControlSet
    {
        public static IntPtr WebBrowser { get; set; }
        public static IntPtr WebBrowserChatRecv { get; set; }
        public static IntPtr MainFormHandle { get; set; }
        public static IntPtr IMChatHandle { get; set; }
        public static IntPtr OptionFormHandle { get; set; }
        public static IntPtr LoginNameManageHandle { get; set; }
        public static Form MainForm { get; set; }
        public static IntPtr LoginFormHandle { get; set; }

        /// <summary>
        /// 接入会话窗口
        /// </summary>
        public static IntPtr UCAccessHandle { get; set; }

        /// <summary>
        /// 激活回话的人名显示控件句柄
        /// </summary>
        public static IntPtr UCActiveNameHandle { get; set; }

        /// <summary>
        /// 我的团队控件句柄
        /// </summary>
        public static IntPtr UCMyGroupHandle { get; set; }

        /// <summary>
        /// 用户信息句柄
        /// </summary>
        public static IntPtr UCUserInfoHandle { get; set; }

        /// <summary>
        /// 用户状态设置显示
        /// </summary>
        public static IntPtr UCSetStatusHandle { get; set; }

        /// <summary>
        /// 历史消息
        /// </summary>
        public static IntPtr UCAdviceRecordHandle { get; set; }

        /// <summary>
        /// 转移回话
        /// </summary>
        public static IntPtr UCTransferHandle { get; set; }

        /// <summary>
        /// 转移回话分组
        /// </summary>
        public static IntPtr TransferFrmHandle { get; set; }

          /// <summary>
        /// 导航信息
        /// </summary>
        public static IntPtr UCNavigationHandle { get; set; }
        
    }
}
