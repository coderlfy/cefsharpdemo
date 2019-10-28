function login()
{
    var user = $('#username').val()
    var pwd = $('#pwd').val();
    var userinfordata = winApi.getUser();

    var userinfor = JSON.parse(userinfordata);
    if (userinfor.username == user &&
        userinfor.pwd == pwd) {
        alert('验证通过！');
    }
    else {
        alert('用户名密码错误！');
    }
}
function startDebug()
{
    winApi.startDebug();
}
function setUserinfor(user, pwd)
{
    $('#username').val(user);
    $('#pwd').val(pwd);
}