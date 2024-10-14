mergeInto(LibraryManager.library, {
    $instance: null,
    $getInstance: function() {
        if (instance != null) return instance;
        if (typeof(gameInstance) != 'undefined') {
            return gameInstance;
        } else if (typeof(unityInstance) != 'undefined') {
            return (unityInstance);
        } else if (typeof(myGameInstance) != 'undefined') {
            return myGameInstance;
        }
        return null;
    },
    $identifierCallback: function(id) {
        instance = getInstance();
        if (instance == null) {
            if (typeof(SendMessage) == 'undefined') {
                console.log('[DevToDev] Error: Unable to find any of unityInstance. Modify your WebGL template to create unityInstance variable.');
            } else {
                SendMessage('[devtodev_AsyncOperationDispatcher]', 'OnIdentifierReceived', id.toString());
            }
            return;
        }

        instance.SendMessage('[devtodev_AsyncOperationDispatcher]', 'OnIdentifierReceived', id.toString());
    },
    $loggerCallback: function(logLevel, message) {
        var logMessage = logLevel + '|' + logLevel.toUpperCase() + ' ' + message;
        instance = getInstance();
        if (instance == null) {
            if (typeof(SendMessage) == 'undefined') {
                console.log('[DevToDev] Error: Unable to find any of unityInstance. Modify your WebGL template to create unityInstance variable.');
            } else {
                SendMessage('[devtodev_AsyncOperationDispatcher]', 'OnLogReceived', logMessage);
            }

            return;
        }

        instance.SendMessage('[devtodev_AsyncOperationDispatcher]', 'OnLogReceived', logMessage);
    },
    setIdentifierCallback__deps: ['$instance', '$getInstance', '$identifierCallback'],
    setIdentifierCallback: function() {
        window.devtodev.setIdentifiersListener(identifierCallback);
    },
    setLoggerCallback__deps: ['$instance', '$getInstance', '$loggerCallback'],
    setLoggerCallback: function() {
        window.devtodev.onDebugMessage(loggerCallback);
    }
});