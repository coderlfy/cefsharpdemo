/*
 方法：接收FORM消息处理
 参数： key         消息键值
        content     消息内容
返回值：无
*/
function ReciveFormMsgHandler(key, content) {
    if (typeof ReciveFormMsg == "function") {
        ReciveFormMsg(key, content);
    }
}
/*
 方法：发送FORM消息处理
 参数： key         消息键值
        content     消息内容
返回值：无
*/
function SendFormMsg(key, content) {
    winformObj.sendMsg(key, content);
}
/*
方法：调用Form方法
参数： funcName     方法名称，待定义
       funcParam    方法参数，待定义 
返回值：待定义
*/
function CallFormFunction(funcName, funcParam) {
    return winformObj.callFunction(funcName, funcParam);
} 

