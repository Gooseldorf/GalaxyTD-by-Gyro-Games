window.createDictionaryFromJson = function(json, name) {
    var data = JSON.parse(json);
    var field = data[name];
    var dictionary = {};
    for (var i in field) {
        for (var j in field[i]) {
            console.log(j);
            console.log(field[i][j])
            dictionary[j] = field[i][j];
        }
    }

    return dictionary;
};