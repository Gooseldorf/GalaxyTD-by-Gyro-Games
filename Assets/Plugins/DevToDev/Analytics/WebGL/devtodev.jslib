var devtodevPlugin = {
    /**
     * Get IndexedDB availability
     * @return bool
     */
    isIndexedDBAvailable: function() {
        var isAvailable = false;
        window.indexedDB = window.indexedDB ||
            window.mozIndexedDB ||
            window.webkitIndexedDB ||
            window.msIndexedDB;

        if (window.indexedDB) isAvailable = true;

        return isAvailable;
    },

    /**
     * Get storage availability
     * @return bool
     */
    isStorageAvailable: function() {
        try {
            const key = "__example_key__";
            window.localStorage.setItem(key, key);
            window.localStorage.removeItem(key);
            return true;
        } catch (e) {
            return false;
        }
    },

    /**
     * @param key string
     * @return void
     */
    removeItem: function(key) {
        try {
            window.localStorage.removeItem(Pointer_stringify(key));
        } catch (e) {}

    },

    /**
     * @param key string
     * @param value string
     * @return void
     */
    setItem: function(key, value) {
        try {
            window.localStorage.setItem(Pointer_stringify(key), Pointer_stringify(value));
        } catch (e) {}
    },

    /**
     * @param key string
     * @return object || null
     */
    getItem: function(key) {
        try {
            var result = window.localStorage.getItem(Pointer_stringify(key));
            result = result === 'undefined' ? null : result;
            if (result != null) {
                var buffer = _malloc(lengthBytesUTF8(result) + 1);
                writeStringToMemory(result, buffer);
                return buffer;
            }
        } catch (e) {}

        return null;
    },

    /**
     * @param key string
     * @return bool
     */
    isExistItem: function(key) {
        try {
            var result = window.localStorage.getItem(key);
            result = result === 'undefined' ? false : true;
            return result;
        } catch (e) {}

        return false;
    }
};
mergeInto(LibraryManager.library, devtodevPlugin);