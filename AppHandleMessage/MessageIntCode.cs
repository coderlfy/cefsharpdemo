using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppHandleMessage
{
    public class MessageIntCode
    {
        public const int MessageAppLayerBaseCode = 0x8000;
        public const int PopupFormMessage = MessageAppLayerBaseCode + 200;
        public const int ViewChatRecordMessage = MessageAppLayerBaseCode + 201;
        public const int ForceLogout = MessageAppLayerBaseCode + 203;
    }
}
