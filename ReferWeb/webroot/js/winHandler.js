winApi = {
    getUser : function () {
        return appHandler.getDBUser();
    },
    closeLoginFrm : function () {
        appHandler.closeApp();
    },
    startDebug: function () {
        appHandler.startDebug();
    }
}