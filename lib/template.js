module.exports = (function (locals, filters, escape, rethrow/**/) {
    var __stack = { lineno: 1, input: "{\r\n'models': [\r\n<% for (var modelName in models) {\r\n     var meta = models[modelName];\r\n     debugger;\r\n     console.log(meta);\r\n     modelName = modelName[0].toUpperCase() + modelName.slice(1);\r\n-%>\r\n{\r\n  'name' : '<%-: modelName  %>',\r\n  'plural' : '<%-: meta.pluralName  %>',\r\n  'base' :  '<%-:meta.baseModel %>',\r\n  'idInjection' : <% if(meta.isGenerated){%>true<% }else { %>false<% } %>,\r\n  'properties' : {\r\n<% for(var param in meta.params){ -%>\r\n        '<%-: param   %>': { <% var name = meta.params[param].type.name%> <%if(name==null) name = meta.params[param].type[0].name%>\r\n            'type' : '<%-: name %>',\r\n            'required' : <%: if(!meta.params[param].required){%>false<% }else {%> true<% } %>,\r\n            'isId' : <%: if(!meta.params[param ].id){%>false<% }else {%> true<% } %>,\r\n            'isArray' : <%: if(!meta.params[param].type.prototype){%>true <% }else {%> false<% } %>\r\n        }<% if(!(meta.params[param] === meta.params[Object.keys(meta.params)[Object.keys(meta.params).length-1]])){ %>,<% } %>\r\n<% }; -%>\r\n    },\r\n    'validations': [],\r\n    'relations': {\r\n    <% for(var rel in meta.relations){ -%>\r\n    '<%-: rel %>': { 'foreignKey': '<%-: meta.relations[rel].foreignKey %>', 'model': '<%-: meta.relations[rel].model %>', 'type': '<%-: meta.relations[rel].type %>'} <% if(!(meta.relations[rel] === meta.relations[Object.keys(meta.relations)[Object.keys(meta.relations).length-1]])){ %>,<% } %>\r\n    <% } -%>\r\n    },\r\n    'acls': [],\r\n    'methods':\r\n    {\r\n<% for(var action in meta.methods) {\r\n     var methodName = meta.methods[action].name.split('.').join('$');\r\n-%>\r\n        '<%-: methodName  %>': {\r\n          'url': ' <%-: meta.methods[action].getFullPath()  %>',\r\n          'method': '<%-: meta.methods[action].getHttpMethod()  %>',\r\n          'params': {\r\n<%  for(var acc in meta.methods[action].accepts) { -%>\r\n<% if(typeof(meta.methods[action].accepts[acc].http) === \"function\") continue -%>\r\n<% var paramPlacement = \"query\" -%>\r\n<% if(meta.methods[action].getFullPath().indexOf(\":\" + meta.methods[action].accepts[acc].arg) >= 0) paramPlacement = \"path\" -%>\r\n<% if(typeof(meta.methods[action].accepts[acc].http) != \"undefined\") paramPlacement = meta.methods[action].accepts[acc].http.source -%>\r\n             '<%- meta.methods[action].accepts[acc].arg %>' : {'type' : '<%-: meta.methods[action].accepts[acc].type %>', 'placement' : '<%-: paramPlacement %>'}<% if(!(meta.methods[action].accepts[acc] === meta.methods[action].accepts[Object.keys(meta.methods[action].accepts)[Object.keys(meta.methods[action].accepts).length-1]])){ %>,<% } %>\r\n<% }; -%>\r\n          },\r\n          'returns': <%if(meta.methods[action].returns.length <= 1) {%>{\r\n<%  for(var ret in meta.methods[action].returns) { -%> <%if(meta.methods[action].returns[ret].arg==null) break;%>\r\n          '<%- meta.methods[action].returns[ret].arg %>' : '<%-: meta.methods[action].returns[ret].type %>'<% if(!(meta.methods[action].returns[ret] === meta.methods[action].returns[Object.keys(meta.methods[action].returns)[Object.keys(meta.methods[action].returns).length-1]])){ %>,<% } %>,\r\n            'isArray': <% if (meta.methods[action].isReturningArray()) { %>true<%}else {%>false<% } %>,\r\n            'root': <% if (meta.methods[action].returns[ret].root){%>true<%} else { %>false<%}};%>\r\n          }<%} else {%>{'not_supported_yet': 'more_than_one_return_arg', 'isArray': 'false', 'root': 'false'}<%}%>,\r\n          'description': '<%- meta.methods[action].description !== undefined ? meta.methods[action].description.replace(\"'\", \"-\") : \"No description given.\" %>'\r\n       }<% if(!(meta.methods[action] === meta.methods[Object.keys(meta.methods)[Object.keys(meta.methods).length-1]])){ %>,<% } %>\r\n<%  };  %>\r\n    }\r\n}<% if(!(meta === models[Object.keys(models)[Object.keys(models).length-1]])){ %>,<% } %>\r\n<% }  -%>\r\n]\r\n}", filename: undefined };
    function rethrow(err, str, filename, lineno) {
        var lines = str.split('\n')
            , start = Math.max(lineno - 3, 0)
            , end = Math.min(lines.length, lineno + 3);

        // Error context
        var context = lines.slice(start, end).map(function (line, i) {
            var curr = i + start + 1;
            return (curr == lineno ? ' >> ' : '    ')
                + curr
                + '| '
                + line;
        }).join('\n');

        // Alter exception message
        err.path = filename;
        err.message = (filename || 'ejs') + ':'
            + lineno + '\n'
            + context + '\n\n'
            + err.message;

        throw err;
    }
    try {
        var buf = [];
        var models = filters.models;
        with (locals || {}) {
            (function () {
                buf.push('{\n\'models\': [\n');
                __stack.lineno = 3;
                for (var modelName in models) {
                    var meta = models[modelName];
       
                    console.info(meta);
                    modelName = modelName[0].toUpperCase() + modelName.slice(1);
                    buf.push(
                        '{\n  \'name\' : \'',
                        (__stack.lineno = 8, modelName),
                        '\',\n  \'plural\' : \'',
                        (__stack.lineno = 9, meta.pluralName),
                        '\',\n  \'base\' :  \'',
                        (__stack.lineno = 10, meta.baseModel),
                        '\',\n  \'idInjection\' : '
                    );
                    __stack.lineno = 11;
                    if (meta.isGenerated) {
                        ;
                        buf.push('true');
                        __stack.lineno = 11;
                    } else {
                        ;
                        buf.push('false');
                        __stack.lineno = 11;
                    };
                    buf.push(',\n  \'properties\' : {\n');
                    __stack.lineno = 13;
                    for (var param in meta.params) {
                        ;
                        buf.push(
                            '        \'',
                            (__stack.lineno = 13, param),
                            '\': { '
                        );
                        __stack.lineno = 13;
                        var name = meta.params[param].type.name;
                        buf.push(' ');
                        __stack.lineno = 13;
                        if (name == null) name = meta.params[param].type[0].name;
                        buf.push(
                            '\n            \'type\' : \'',
                            (__stack.lineno = 14, name),
                            '\',\n            \'required\' : '
                        );
                        __stack.lineno = 15;
                        if (!meta.params[param].required) {
                            ;
                            buf.push('false');
                            __stack.lineno = 15;
                        } else {
                            ;
                            buf.push(' true');
                            __stack.lineno = 15;
                        };
                        buf.push(',\n            \'isId\' : ');
                        __stack.lineno = 16;
                        if (!meta.params[param].id) {
                            ;
                            buf.push('false');
                            __stack.lineno = 16;

                        } else {
                            ;
                            buf.push(' true');
                            __stack.lineno = 16;
                        };
                        buf.push(',\n            \'isArray\' : ');
                        __stack.lineno = 17;
                        if (!meta.params[param].type.prototype) {
                            ;
                            buf.push('true ');
                            __stack.lineno = 17;
                        } else {
                            ;
                            buf.push(' false');
                            __stack.lineno = 17;
                        };
                        buf.push('\n        }');
                        __stack.lineno = 18;
                        if (!(meta.params[param] === meta.params[Object.keys(meta.params)[Object.keys(meta.params).length - 1]])) {
                            ;
                            buf.push(',');
                            __stack.lineno = 18;
                        };
                        buf.push('\n');
                        __stack.lineno = 19;

                    };;

                    buf.push('    },\n    \'validations\': [],\n    \'relations\': {\n    ');
                    __stack.lineno = 22;
                    for (var rel in meta.relations) {
                        ;
                        buf.push('    \'', (__stack.lineno = 22, rel), '\': { \'foreignKey\': \'', (__stack.lineno = 22, meta.relations[rel].foreignKey), '\', \'model\': \'', (__stack.lineno = 22, meta.relations[rel].model), '\', \'type\': \'', (__stack.lineno = 22, meta.relations[rel].type), '\'} ');
                        __stack.lineno = 22;
                        if (!(meta.relations[rel] === meta.relations[Object.keys(meta.relations)[Object.keys(meta.relations).length - 1]])) {
                            ;
                            buf.push(',');
                            __stack.lineno = 22;
                        };
                        buf.push('\n    ');
                        __stack.lineno = 23;
                    };
                    buf.push('    },\n    \'acls\': [],\n    \'methods\':\n    {\n');
                    __stack.lineno = 27;
                    for (var action in meta.methods) {
                        var methodName = meta.methods[action].name.split('.').join('$');

                        if (methodName) {
                            ;
                            buf.push(
                                '        \'',
                                (__stack.lineno = 28, methodName),
                                '\': {\n          \'url\': \' ',
                                (__stack.lineno = 29, meta.methods[action].getFullPath())
                                , '\',\n          \'method\': \'',
                                (__stack.lineno = 30, meta.methods[action].getHttpMethod()),
                                '\',\n          \'params\': {\n'
                            );

                            __stack.lineno = 32;
                            for (var acc in meta.methods[action].accepts) {
                                ;
                                buf.push('');
                                __stack.lineno = 32;
                                if (typeof (meta.methods[action].accepts[acc].http) === "function") continue;
                                buf.push('');
                                __stack.lineno = 32;
                                var paramPlacement = "query";
                                buf.push('');
                                __stack.lineno = 32;
                                if (meta.methods[action].getFullPath().indexOf(":" + meta.methods[action].accepts[acc].arg) >= 0) paramPlacement = "path";
                                buf.push('');
                                __stack.lineno = 32;
                                if (typeof (meta.methods[action].accepts[acc].http) != "undefined") paramPlacement = meta.methods[action].accepts[acc].http.source;
                                buf.push('             \'', (__stack.lineno = 32, meta.methods[action].accepts[acc].arg), '\' : {\'type\' : \'', (__stack.lineno = 32, meta.methods[action].accepts[acc].type), '\', \'placement\' : \'', (__stack.lineno = 32, paramPlacement), '\'}');
                                __stack.lineno = 32;
                                if (!(meta.methods[action].accepts[acc] === meta.methods[action].accepts[Object.keys(meta.methods[action].accepts)[Object.keys(meta.methods[action].accepts).length - 1]])) {
                                    ;
                                    buf.push(',');
                                    __stack.lineno = 32;
                                };
                                buf.push('\n');

                                __stack.lineno = 33;
                            };;

                            buf.push('          },\n          \'returns\': ');
                            __stack.lineno = 34;
                            if (meta.methods[action].returns.length <= 1) {
                                ;
                                buf.push('{\n');
                                __stack.lineno = 35;
                                for (var ret in meta.methods[action].returns) {
                                    ;
                                    buf.push(' ');
                                    __stack.lineno = 35;
                                    if (meta.methods[action].returns[ret].arg == null) break;;
                                    buf.push('          \'', (__stack.lineno = 35, meta.methods[action].returns[ret].arg), '\' : \'', (__stack.lineno = 35, meta.methods[action].returns[ret].type), '\'');
                                    __stack.lineno = 35;
                                    if (!(meta.methods[action].returns[ret] === meta.methods[action].returns[Object.keys(meta.methods[action].returns)[Object.keys(meta.methods[action].returns).length - 1]])) {
                                        ;
                                        buf.push(',');
                                        __stack.lineno = 35;
                                    };
                                    buf.push(',\n            \'isArray\': ');
                                    __stack.lineno = 36;
                                    if (meta.methods[action].isReturningArray()) {
                                        ;
                                        buf.push('true');
                                        __stack.lineno = 36;
                                    } else {
                                        ;
                                        buf.push('false');
                                        __stack.lineno = 36;
                                    };
                                    buf.push(',\n            \'root\': ');
                                    __stack.lineno = 37;
                                    if (meta.methods[action].returns[ret].root) {
                                        ;
                                        buf.push('true');
                                        __stack.lineno = 37;
                                    } else {
                                        ;
                                        buf.push('false');
                                        __stack.lineno = 37;
                                    }
                                };;
                                buf.push('\n          }');
                                __stack.lineno = 38;

                            } else {
                                ;
                                buf.push('{\'not_supported_yet\': \'more_than_one_return_arg\', \'isArray\': \'false\', \'root\': \'false\'}');
                                __stack.lineno = 38;
                            };
                            buf.push(',\n          \'description\': \'', (__stack.lineno = 39, meta.methods[action].description !== undefined ? meta.methods[action].description.replace("'", "-") : "No description given."), '\'\n       }');
                            __stack.lineno = 40;
                            if (!(meta.methods[action] === meta.methods[Object.keys(meta.methods)[Object.keys(meta.methods).length - 1]])) {
                                ;
                                buf.push(',');
                                __stack.lineno = 40;
                            };
                            buf.push('\n');
                            __stack.lineno = 41;
                        }
                    };;

                    buf.push('\n    }\n}');
                    __stack.lineno = 43;
                    if (!(meta === models[Object.keys(models)[Object.keys(models).length - 1]])) {
                        ;
                        buf.push(',');
                        __stack.lineno = 43;
                    };
                    buf.push('\n');
                    __stack.lineno = 44;

                };
                buf.push(']\n}');

            })();

        }
        return buf.join('');

    } catch (err) {
        rethrow(err, __stack.input, __stack.filename, __stack.lineno);

    }
})