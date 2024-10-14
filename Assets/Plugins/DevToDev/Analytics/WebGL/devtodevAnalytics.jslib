var DevToDev = {
    migrateByUser: function(appKey, migrationData) {
        try {
            window.devtodev.migrationUser(Pointer_stringify(appKey), Pointer_stringify(migrationData));
        } catch (e) {
            _logger.error('In the migration method error has occurred: ' + e);
            _logger.error('Execution of the migration method was canceled!');
        }
    },
    migrateByDevice: function(appKey, migrationData) {
        try {
            window.devtodev.migrationDevice (Pointer_stringify(appKey), Pointer_stringify(migrationData));
        } catch (e) {
            _logger.error('In the migration method error has occurred: ' + e);
            _logger.error('Execution of the migration method was canceled!');
        }
    },
    initialize: function(appKey) {
        try {
            window.devtodev.initialize(Pointer_stringify(appKey), {});
        } catch (e) {
            _logger.error('In the initialize method error has occurred: ' + e);
            _logger.error('Execution of the initialize method was canceled!');
        }
    },
    initializeWithConfig: function(appKey, userId, currentLevel, trackingAvailability, logLevel, applicationVersion) {
        try {
            var args = {};
            args['userId'] = Pointer_stringify(userId);
            args['logLevel'] = Pointer_stringify(logLevel);
            args['applicationVersion'] = Pointer_stringify(applicationVersion);
            if (Pointer_stringify(currentLevel) != 'null') {
                args['currentLevel'] = parseInt(Pointer_stringify(currentLevel));
            }
            if (Pointer_stringify(trackingAvailability) != 'Unknown') {
                if (Pointer_stringify(trackingAvailability) == 'Enable') {
                    args['trackingAvailability'] = true;
                } else {
                    args['trackingAvailability'] = false;
                }
            }

            window.devtodev.initialize(Pointer_stringify(appKey), args);
        } catch (e) {
            _logger.error('In the initialize method error has occurred: ' + e);
            _logger.error('Execution of the initialize method was canceled!');
        }
    },
    adImpression: function(network, revenue, placement, unit) {
        try {
            window.devtodev.adImpression(Pointer_stringify(network), revenue, Pointer_stringify(placement), Pointer_stringify(unit));
        } catch (e) {
            _logger.error('In the adImpression method error has occurred: ' + e);
            _logger.error('Execution of the adImpression method was canceled!');
        }
    },
    setSdkCodeVersion: function(version) {
        try {
            window.devtodev.setSDKVersionCode(version);
        } catch (e) {
            _logger.error('In the setSdkCodeVersion method error has occurred: ' + e);
            _logger.error('Execution of the setSdkCodeVersion method was canceled!');
        }
    },
    setSdkVersion: function(version) {
        try {
            window.devtodev.setSDKVersion(Pointer_stringify(version));
        } catch (e) {
            _logger.error('In the setSdkVersion method error has occurred: ' + e);
            _logger.error('Execution of the setSdkVersion method was canceled!');
        }
    },
    getSdkVersion: function() {
        try {
            var result = window.devtodev.getSDKVersion();
            if(typeof(result) === 'undefined') return null;
            var buffer = _malloc(lengthBytesUTF8(result) + 1);
            var bufferSize = lengthBytesUTF8(result) + 1;
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {
            _logger.error('In the getSdkVersion method error has occurred: ' + e);
            _logger.error('Execution of the getSdkVersion method was canceled!');
            return null;
        }
    },
    getAppVersion: function() {
        try {
            var result = window.devtodev.getAppVersion();
            if(typeof(result) === 'undefined') return null;
            var buffer = _malloc(lengthBytesUTF8(result) + 1);
            var bufferSize = lengthBytesUTF8(result) + 1;
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {
            _logger.error('In the getAppVersion method error has occurred: ' + e);
            _logger.error('Execution of the getAppVersion method was canceled!');
            return null;
        }
    },
    setAppVersion: function(version) {
        try {
            window.devtodev.setAppVersion(Pointer_stringify(version));
        } catch (e) {
            _logger.error('In the setAppVersion method error has occurred: ' + e);
            _logger.error('Execution of the setAppVersion method was canceled!');
        }
    },
    getUserId: function() {
        try {
            var result = window.devtodev.getUserId();
            if(typeof(result) === 'undefined') return null;
            var buffer = _malloc(lengthBytesUTF8(result) + 1);
            var bufferSize = lengthBytesUTF8(result) + 1;
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {
            _logger.error('In the getUserId method error has occurred: ' + e);
            _logger.error('Execution of the getUserId method was canceled!');
            return null;
        }
    },
    setUserId: function(userId) {
        try {
            window.devtodev.setUserId(Pointer_stringify(userId));
        } catch (e) {
            _logger.error('In the setUserId method error has occurred: ' + e);
            _logger.error('Execution of the setUserId method was canceled!');
        }
    },
    getCurrentLevel: function() {
        try {
            var result = window.devtodev.getCurrentLevel();
            if(typeof(result) === 'undefined') return null;
            var buffer = _malloc(lengthBytesUTF8(result) + 1);
            var bufferSize = lengthBytesUTF8(result) + 1;
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {
            _logger.error('In the getCurrentLevel method error has occurred: ' + e);
            _logger.error('Execution of the getCurrentLevel method was canceled!');
            return 0;
        }
    },
    setCurrentLevel: function(level) {
        try {
            window.devtodev.setCurrentLevel(level);
        } catch (e) {
            _logger.error('In the setCurrentLevel method error has occurred: ' + e);
            _logger.error('Execution of the setCurrentLevel method was canceled!');
        }
    },
    getLogLevel: function() {
        try {
            var result = window.devtodev.logLevel;
            if(typeof(result) === 'undefined') return null;
            var buffer = _malloc(lengthBytesUTF8(result) + 1);
            var bufferSize = lengthBytesUTF8(result) + 1;
            stringToUTF8(result, buffer, bufferSize);
            return buffer;
        } catch (e) {
            _logger.error('In the getLogLevel method error has occurred: ' + e);
            _logger.error('Execution of the getLogLevel method was canceled!');
            return null;
        }
    },
    setLogLevel: function(logLevel) {
        try {
            window.devtodev.logLevel = Pointer_stringify(logLevel);
        } catch (e) {
            _logger.error('In the setLogLevel method error has occurred: ' + e);
            _logger.error('Execution of the setLogLevel method was canceled!');
        }
    },
    getTrackingAvailability: function() {
        try {
            return window.devtodev.getTrackingAvailability();
        } catch (e) {
            _logger.error('In the getTrackingAvailability method error has occurred: ' + e);
            _logger.error('Execution of the getTrackingAvailability method was canceled!');
            return true;
        }

    },
    setTrackingAvailability: function(value) {
        try {
            window.devtodev.setTrackingAvailability(!!value);
        } catch (e) {
            _logger.error('In the setTrackingAvailability method error has occurred: ' + e);
            _logger.error('Execution of the setTrackingAvailability method was canceled!');
        }
    },
    referrer: function(value) {
        try {
            if (value == null || Pointer_stringify(value).length == 0) {
                window.devtodev.referrer({});
            }
            var utm = JSON.parse(Pointer_stringify(value));
            window.devtodev.referrer(utm);
        } catch (e) {
            _logger.error('In the referrer method error has occurred: ' + e);
            _logger.error('Execution of the referrer method was canceled!');
        }
    },
    virtualCurrencyPayment: function(purchaseId, purchaseType, purchaseAmount, args) {
        try {
            var purchaseId = Pointer_stringify(purchaseId);
            var purchaseType = Pointer_stringify(purchaseType);
            var purchaseAmount = purchaseAmount;
            var data = {};
            if (args != null || Pointer_stringify(args).length != 0) {
                data = JSON.parse(Pointer_stringify(args));
            }
            window.devtodev.virtualCurrencyPayment(purchaseId, purchaseType, purchaseAmount, data);
        } catch (e) {
            _logger.error('In the virtualCurrencyPayment method error has occurred: ' + e);
            _logger.error('Execution of the virtualCurrencyPayment method was canceled!');
        }
    },
    currencyAccrual: function(currencyName, currencyAmount, source, accrualType) {
        try { window.devtodev.currencyAccrual(Pointer_stringify(currencyName), currencyAmount, Pointer_stringify(source), accrualType); } catch (e) {
            _logger.error('In the currencyAccrual method error has occurred: ' + e);
            _logger.error('Execution of the currencyAccrual method was canceled!');
        }
    },
    realCurrencyPayment: function(orderId, price, productId, currencyCode) {
        try { window.devtodev.realCurrencyPayment(Pointer_stringify(orderId), price, Pointer_stringify(productId), Pointer_stringify(currencyCode)); } catch (e) {
            _logger.error('In the realCurrencyPayment method error has occurred: ' + e);
            _logger.error('Execution of the realCurrencyPayment method was canceled!');
        }
    },
    tutorial: function(step) {
        window.devtodev.tutorial(step);
    },
    socialNetworkConnect: function(name) {
        try { window.devtodev.socialNetworkConnect(Pointer_stringify(name)); } catch (e) {
            _logger.error('In the socialNetworkConnect method error has occurred: ' + e);
            _logger.error('Execution of the socialNetworkConnect method was canceled!');
        }
    },
    socialNetworkPost: function(name, reason) {
        try { window.devtodev.socialNetworkPost(Pointer_stringify(name), Pointer_stringify(reason)); } catch (e) {
            _logger.error('In the socialNetworkPost method error has occurred: ' + e);
            _logger.error('Execution of the socialNetworkPost method was canceled!');
        }
    },
    sendBufferedEvents: function() {
        window.devtodev.sendBufferedEvents();
    },
    startProgressionEvent: function(val, valJSON) {
        try {
            if (valJSON == null || Pointer_stringify(valJSON).length == 0) {
                window.devtodev.startProgressionEvent(Pointer_stringify(val), {});
                return;
            }

            var json = JSON.parse(Pointer_stringify(valJSON));
            var validatedObject = {};
            if (json['source'] != null && json['source'].length > 0) validatedObject['source'] = json['source'];
            if (json['difficulty'] != null) validatedObject['difficulty'] = json['difficulty'];
            window.devtodev.startProgressionEvent(Pointer_stringify(val), validatedObject);
        } catch (e) {
            _logger.error('In the startProgressionEvent method error has occurred: ' + e);
            _logger.error('Execution of the startProgressionEvent method was canceled!');
        }
    },
    finishProgressionEvent: function(val, valJSON) {
        try {
            if (valJSON == null || Pointer_stringify(valJSON).length == 0) {
                window.devtodev.finishProgressionEvent(Pointer_stringify(val), {});
                return;
            }

            var json = JSON.parse(Pointer_stringify(valJSON));
            console.log(Pointer_stringify(valJSON));
            var validatedObject = {};
            validatedObject['successfulCompletion'] = json['successfulCompletion'];
            if (json['duration'] != null && json['duration'] > 0) validatedObject['duration'] = json['duration'];
            if (json['spent'] != null && Object.keys(json['spent']).length > 0) validatedObject['spent'] = json['spent'];
            if (json['earned'] != null && Object.keys(json['earned']).length > 0) validatedObject['earned'] = json['earned'];
            window.devtodev.finishProgressionEvent(Pointer_stringify(val), validatedObject);
        } catch (e) {
            _logger.error('In the finishProgressionEvent method error has occurred: ' + e);
            _logger.error('Execution of the finishProgressionEvent method was canceled!');
        }
    },
    customEvent: function(name, params) {
        try {
            if (params == null || Pointer_stringify(params).length == 0) {
                window.devtodev.customEvent(Pointer_stringify(name));
                return;
            }
            var customParams = JSON.parse(Pointer_stringify(params));
            window.devtodev.customEvent(Pointer_stringify(name), customParams);
        } catch (e) {
            _logger.error('In the customEvent method error has occurred: for key ' + Pointer_stringify(name) + ":" + e);
            _logger.error('Execution of the customEvent method was canceled!');
        }
    },
    replaceUserId: function(previousUserId, userId) {
        try {
            window.devtodev.replaceUserId(Pointer_stringify(previousUserId), Pointer_stringify(userId));
        } catch (e) {
            _logger.error('In the replaceUserId method error has occurred: ' + e);
            _logger.error('Execution of the replaceUserId method was canceled!');
        }
    },
    levelUp: function(level){
        try {
            window.devtodev.levelUp(level);
        } catch (e) {
            _logger.error('In the levelUp method error has occurred: ' + e);
            _logger.error('Execution of the levelUp method was canceled!');
        }
    },
    levelUpWithResources: function(
        level, 
        jsonBalance, 
        jsonSpent,
        jsonEarned,
        jsonBought) {
        try {
            var balance = JSON.parse(Pointer_stringify(jsonBalance));
            var spent = JSON.parse(Pointer_stringify(jsonSpent));
            var earned = JSON.parse(Pointer_stringify(jsonEarned));
            var bought = JSON.parse(Pointer_stringify(jsonBought));
            window.devtodev.levelUp(level, balance, spent, earned, bought);
        } catch (e) {
            _logger.error('In the levelUp method error has occurred: ' + e);
            _logger.error('Execution of the levelUp method was canceled!');
        }
    },
    currentBalance: function(jsonString) {
        try {
            if (jsonString == null || Pointer_stringify(jsonString).length == 0) {
                window.devtodev.currentBalance(null);
                return;
            }
            var convertedJson = Pointer_stringify(jsonString);
            var balance = JSON.parse(convertedJson);
            window.devtodev.currentBalance(balance);
        } catch (e) {
            _logger.error('In the currentBalance method error has occurred: ' + e);
            _logger.error('Execution of the currentBalance method was canceled!');
        }
    },
    setTestProxyUrl: function(url) {
        window.devtodev.testProxyUrl = Pointer_stringify(url);
    },
    setTestCustomUrl: function(url) {
        window.devtodev.testCustomUrl = Pointer_stringify(url);
    },
    getTestProxyUrl: function(url) {
        var result = window.devtodev.testProxyUrl;
        if(typeof(result) === 'undefined') return null;
        var buffer = _malloc(lengthBytesUTF8(result) + 1);
        var bufferSize = lengthBytesUTF8(result) + 1;
        stringToUTF8(result, buffer, bufferSize);
        return buffer;

    },
    getTestCustomUrl: function(url) {
        var result = window.devtodev.testCustomUrl;
        if(typeof(result) === 'undefined') return null;
        var buffer = _malloc(lengthBytesUTF8(result) + 1);
        var bufferSize = lengthBytesUTF8(result) + 1;
        stringToUTF8(result, buffer, bufferSize);
        return buffer;
    },
    testLogs: function() {
        window.devtodev.testLogs();
    }
};
mergeInto(LibraryManager.library, DevToDev);