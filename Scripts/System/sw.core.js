var SwCore = function () {
    return {
        /*
		Ajax helpers
		*/
        getServerData: function (url, wrapperId, onSuccess, onError) {
            $.ajax({
                url: url,
                type: "get",
                beforeSend: function () {
                    SwCore.block(wrapperId);
                },
                success: function (data, textStatus, jqXHR) {
                    onSuccess(data);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    var e = jqXHR.responseJSON;
                    var header = "Ошибка на сервере";
                    var message = "Неизвестная ошибка произошла на сервере";
                    if (typeof (e) !== "undefined") {
                        var message = e.ExceptionMessage || e.Message || e || '-';
                    }
                    if (typeof (onError) === 'undefined')
                        SwCore.showError(header, message);
                    else
                        onError(errorThrown, jqXHR);
                },
                complete: function (jqXHR, textStatus) {
                    SwCore.unblock(wrapperId);
                }
            });
        },

        setServerData: function (url, method, wrapperId, input, onSuccess) {
            $.ajax({
                url: url,
                type: method,
                data: input,
                contentType: "application/json",
                beforeSend: function () {
                    if (typeof (wrapperId) == "object") {
                        wrapperId.message = wrapperId.message || 'Идет загрузка';
                        SwCore.blockPage(wrapperId.message);
                    }
                    else {
                        SwCore.block(wrapperId);
                    }
                },
                success: function (data, textStatus, jqXHR) {
                    if (typeof (onSuccess) === 'undefined')
                    {
                        SwCore.showSuccess('Данные успешно сохранены');
                    }
                    else {
                        onSuccess(data)
                    }
                    
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    var e = jqXHR.responseJSON;
                    var header = "Ошибка на сервере";
                    var message = "Неизвестная ошибка произошла на сервере";
                    if (typeof (e) !== "undefined") {
                        var message = e.ExceptionMessage || e.Message || e || '-';
                    }
                    if (typeof (onError) === 'undefined')
                        SwCore.showError(header, message);
                    else
                        onError(errorThrown, jqXHR);
                },
                complete: function (jqXHR, textStatus) {
                    if (typeof (wrapperId) == "object") {
                        SwCore.unblockPage();
                    }
                    else {
                        SwCore.unblock(wrapperId);
                    }
                }
            });
        },

        uploadServerImage: function(url, method, wrapperId, formData, onSuccess){
            $.ajax({
                url: url,
                type: method,
                cache: false,
                contentType: false,
                processData: false,
                data: formData,  
                beforeSend: function () {
                    SwCore.block(wrapperId);
                },
                success: function (data, textStatus) {
                    if (typeof (onSuccess) === 'undefined')
                    {
                        SwCore.showSuccess("Обновление изображения", "изображение успешно обновлено");
                    }
                    else {
                        onSuccess(data)
                    }
                    
                },
                error: function (jqXHR, textStatus, errmsg) {
                    var e = jqXHR.responseJSON;
                    var header = "Ошибка на сервере";
                    var message = "Неизвестная ошибка произошла на сервере";
                    if (typeof (e) !== "undefined") {
                        var message = e.ExceptionMessage || e.Message || e || '-';                        
                    }
                    SwCore.showError(header, message);
                },
                complete: function (jqXHR, textStatus) {
                    SwCore.unblock(wrapperId);
                }
            })
        },

        /*
		Messages & loaders
		*/
        showError: function (header, message, onShownCallback) {
            toastr.options.onShown = typeof (onShownCallback) === "undefined" ? function () { } : onShownCallback;
            toastr.error(message, header);
        },
        showSuccess: function (header, message, onShownCallback) {
            toastr.options.onShown = typeof (onShownCallback) === "undefined" ? null : onShownCallback;
            toastr.success(message, header);
        },
        showWarning: function (heaeder, message, onShownCallback) {
            toastr.options.onShown = typeof (onShownCallback) === "undefined" ? function () { } : onShownCallback;
            toastr.warning(message, heaeder);
        },
        showInfo: function (header, message, onShownCallback) {
            toastr.options.onShown = typeof (onShownCallback) === "undefined" ? function () { } : onShownCallback;
            toastr.info(message, header);
        },
        block: function (id) {
            if (typeof (Metronic) !== "undefined") 
                Metronic.blockUI({ target: '#' + id, animate: true });           
        },
        unblock: function (id) {
            if (typeof (Metronic) !== "undefined")
                Metronic.unblockUI('#' + id);
        },
        blockPage: function(m){
            m = m || "Пожалуйста, подождите ....";
            Metronic.startPageLoading({ message: m });

        },
        unblockPage: function(){
            Metronic.stopPageLoading();
        },
        /*
        Alerts
        */
        alert: function (t, m) {
            bootbox.dialog({
                message: m,
                title: t,
                buttons: {
                    success: {
                        label: "Ok",
                        className: "blue",
                    }
                }
            });
        },
        prompt: function (t, callback, v) {
            var box = bootbox.prompt({
                title: t,
                value: v,
                callback: function (result) {
                    if (result !== null) {
                        callback(result);
                    }
                }
            });
            box.bind('shown.bs.modal', function () {
                box.find("input").focus();
            });
        },
        confirm: function (t, m, callback) {
            bootbox.dialog({
                message: m,
                title: t,
                buttons: {
                    success: {
                        label: "Ok",
                        className: "blue",
                        callback: function () {
                            if (typeof (callback) !== "undefined")
                                callback();
                        }
                    },
                    danger: {
                        label: "Cancel",
                        className: "grey",
                        callback: function () {
                            // do nothing
                        }
                    },
                }
            });
        }
    };
}();
