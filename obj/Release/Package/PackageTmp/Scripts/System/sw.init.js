var SwInit = function () {
    return {
        initMessages: function () {
            toastr.options = {
                "closeButton": true,
                "debug": false,
                "positionClass": "toast-top-right",
                "onclick": null,
                "showDuration": "1000",
                "hideDuration": "1000",
                "timeOut": "5000",
                "extendedTimeOut": "1000",
                "showEasing": "swing",
                "hideEasing": "linear",
                "showMethod": "fadeIn",
                "hideMethod": "fadeOut"
            }
        },
        initValidation: function () {
            ko.validation.rules.pattern.message = 'Invalid.';

            /*
            https://github.com/Knockout-Contrib/Knockout-Validation/wiki/Configuration
            */
            ko.validation.init({
                registerExtenders: true,
                insertMessages: false,
                decorateInputElement: false,
                errorMessageClass: '',
                errorElementClass: 'has-error',
                errorsAsTitle: false,
                errorClass: null,
                parseInputAttributes: false,
                messagesOnModified: false,
                decorateElementOnModified: false,
                messageTemplate: null,
                grouping: {
                    deep: true,
                    observable: true,
                    live: true
                }
            }, true);

            /*
            https://github.com/Knockout-Contrib/Knockout-Validation/tree/master/Localization
            */
            ko.validation.localize({
                required: 'Пожалуйста, заполните это поле.',
                min: 'Пожалуйста, введите число большее или равное {0}.',
                max: 'Пожалуйста, введите число меньшее или равное {0}.',
                minLength: 'Пожалуйста, введите по крайней мере {0} символов.',
                maxLength: 'Пожалуйста, введите не больше чем {0} символов.',
                pattern: 'Пожалуйста, проверьте это поле.',
                step: 'Значение должно быть кратным {0}',
                email: 'Пожалуйста, укажите здесь правильный адрес электронной почты',
                date: 'Пожалуйста, введите правильную дату',
                dateISO: 'Пожалуйста, введите правильную дату в формате ISO',
                number: 'Пожалуйста, введите число',
                digit: 'Пожалуйста, введите цифры',
                phoneUS: 'Пожалуйста, укажите правильный телефонный номер',
                equal: 'Значения должны быть равны',
                notEqual: 'Пожалуйста, выберите другое значение.',
                unique: 'Пожалуйста, укажите уникальное значение.'
            });
        },
        initFileSizes: function () {
            $.map($(".sw-file-size"), function (node, i) {
                var value = $(node).text();
                if (jQuery.isNumeric(value)) {
                    $(node).text(
                        SwHelpers.getReadableFileSizeString(value)
                    );
                }
            });
        },
        initDateTimeFromNow: function () {
            $.map($(".sw-datetime-from-now:not(.calculated)"), function (node, i) {
                var value = $(node).text();
                $(node).text(
                    SwHelpers.getDateTimeMomentString(value)
                );
                $(node).addClass("calculated");
            });
        },
        initDateTime: function () {
            $.map($(".sw-datetime:not(.calculated)"), function (node, i) {
                var value = $(node).text();
                if (/^[-]?$/.test(value)) {
                    $(node).text('-');
                }
                else {
                    var format = $(node).data('format');
                    $(node).text(SwHelpers.getDateTimeStringCustom(value, format));
                }
                $(node).addClass("calculated");
            });
        },
        initLazyImages: function (delay) {
            delay = delay || 100;
            if (typeof ($.fn.lazyload) === "undefined") {
                console.log("http://www.appelsiini.net/projects/lazyload plugin missed")
            }
            else {
                $("img.lazy").lazyload({
                    event: "load.delayed.image",
                    effect: "fadeIn",
                    effectTime: 250
                });
                setTimeout(function () {

                    $("img.lazy").trigger("load.delayed.image")
                }, delay);
            }
        },
        initTips: function () {
            $('[data-toggle="tooltip"]').tooltip({
                'selector': '',
                'placement': 'top',
                'container': false
            });
        }
    };
}();