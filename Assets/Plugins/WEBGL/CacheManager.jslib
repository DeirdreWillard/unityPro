mergeInto(LibraryManager.library, {
  ClearWebGLCaches: function () {
    try {
      localStorage.clear();
      console.log("localStorage 清理完成");

      // 通知主页面执行异步操作
      var event = new Event("ClearIndexedDB");
      window.dispatchEvent(event);
    } catch (e) {
      console.error("清理缓存出错: " + e);
    }
  },
  GetLocationFromBrowser: function() {
    console.log("WEBGL 开始获取定位");
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            function(position) {
                var gpsData = position.coords.latitude + "," + position.coords.longitude;
                console.log("getCurrentPosition定位数据：" + gpsData);
                // 使用GameInstance而非直接访问unityInstance
                var gameInstance = window.unityInstance || window.gameInstance || SendMessage.gameInstance;
                if (gameInstance) {
                    gameInstance.SendMessage('BuiltinViews', 'OnLocationReceived', gpsData);
                } else {
                    console.error("Unity实例未找到，无法发送消息");
                }
            },
            function(error) {
                console.log("定位失败：" + error.message);
                var gameInstance = window.unityInstance || window.gameInstance || SendMessage.gameInstance;
                if (gameInstance) {
                    gameInstance.SendMessage('BuiltinViews', 'OnLocationError', error.message);
                } else {
                    console.error("Unity实例未找到，无法发送错误信息");
                }
            },
            { 
                enableHighAccuracy: true, 
                timeout: 5000, 
                maximumAge: 0 
            }
        );
    } else {
        console.log("浏览器不支持地理定位");
        var gameInstance = window.unityInstance || window.gameInstance || SendMessage.gameInstance;
        if (gameInstance) {
            gameInstance.SendMessage('BuiltinViews', 'OnLocationError', "浏览器不支持地理定位");
        } else {
            console.error("Unity实例未找到，无法发送错误信息");
        }
    }
}
});