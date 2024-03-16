/*
General
*/
(function ($) {
    String.prototype.addQuery = function (key, value) {
        if (typeof value === undefined || value === '')
            return this;
        var prefix = this.indexOf('?') === -1 ? '?' : this.indexOf('?') == this.length - 1 ? '' : '&';
        return this + prefix + key + '=' + value;
    };
    $.fn.ensureTemplates = function (module, templates, onSuccess) {
        var callback = onSuccess || function() { };
        var loadedTemplates = [];
        ko.utils.arrayForEach(templates, function (t) {
            var name = t.title || t;
            var type = t.type || "text/html";
            $.get("/scripts/Modules/" + module + "/Templates/" + name + ".html", function (template) {
                $("body").append("<script id=\"" + name + "\" type=\"" + type + "\">" + template + "<\/script>");
                loadedTemplates.push(name);
                if (templates.length === loadedTemplates.length) {
                    callback();
                }
            });
        });
    }
})(jQuery);

/*
Knockout
*/
(function ($, ko) {
    // ENTER key pressed
    ko.bindingHandlers.executeOnEnter = {
        init: function (element, valueAccessor, allBindingsAccessor, viewModel) {
            var allBindings = allBindingsAccessor();
            $(element).keypress(function (event) {
                var keyCode = (event.which ? event.which : event.keyCode);
                if (keyCode === 13) {
                    allBindings.executeOnEnter.call(viewModel);
                    return false;
                }
                return true;
            });
        }
    };
    // empty value = '' instead of undefined
    ko.bindingHandlers.valueEmpty = {
        init: function (element, valueAccessor, allBindingsAccessor) {
            var underlyingObservable = valueAccessor();
            var interceptor = ko.dependentObservable({
                read: underlyingObservable,
                write: function (value) {
                    if ((value != null && value.trim() == '') || typeof value === "undefined")
                        underlyingObservable('');
                    else
                        underlyingObservable(value);
                }
            });
            ko.bindingHandlers.value.init(element, function () { return interceptor }, allBindingsAccessor);
        },
        update: ko.bindingHandlers.value.update
    };
    // set css background image style
    ko.bindingHandlers.backgroundImage = {
        update: function (element, valueAccessor) {
            ko.bindingHandlers.style.update(element,
              function () { return { backgroundImage: "url('" + valueAccessor() + "')" } });
        }
    };
    // better click
    ko.bindingHandlers.singleClick = {
        init: function (element, valueAccessor) {
            var handler = valueAccessor(),
                delay = 400,
                clickTimeout = false;

            $(element).click(function () {
                if (clickTimeout !== false) {
                    clearTimeout(clickTimeout);
                    clickTimeout = false;
                } else {
                    clickTimeout = setTimeout(function () {
                        clickTimeout = false;
                        handler();
                    }, delay);
                }
            });
        }
    };
    // stop binding: EXTRA USEFULL!!!
    ko.bindingHandlers.stopBinding = {
        init: function () {
            return { controlsDescendantBindings: true };
        }
    };
    ko.virtualElements.allowedBindings.stopBinding = true;
    // href
    ko.bindingHandlers.href = {
        update: function (element, valueAccessor) {
            ko.bindingHandlers.attr.update(element, function () {
                return { href: valueAccessor() }
            });
        }
    };
    // src
    ko.bindingHandlers.src = {
        update: function (element, valueAccessor) {
            ko.bindingHandlers.attr.update(element, function () {
                return { src: valueAccessor() }
            });
        }
    };
    // src no cache
    ko.bindingHandlers.srcNoCache = {
        update: function (element, valueAccessor) {
            ko.bindingHandlers.attr.update(element, function () {
                return { src: ko.unwrap( valueAccessor()).addQuery("t", new Date().getTime()) }
            });
        }
    };
    // currency
    ko.bindingHandlers.currency = {
        symbol: ko.observable('$'),
        update: function (element, valueAccessor, allBindingsAccessor) {
            return ko.bindingHandlers.text.update(element, function () {
                var value = +(ko.utils.unwrapObservable(valueAccessor()) || 0),
                    symbol = ko.utils.unwrapObservable(allBindingsAccessor().symbol !== undefined
                                ? allBindingsAccessor().symbol
                                : ko.bindingHandlers.currency.symbol);
                return symbol + value.toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, "$1,");
            });
        }
    };  
    /*
    array extensions
    */
    ko.observableArray.fn.fastFill = function (newItems) {
        this([]);
        var underlyingArray = this();
        for (var i = 0; i < newItems.length; i++) {
            underlyingArray.push(newItems[i]);
        }
        this.valueHasMutated();
    }
    ko.observableArray.fn.smartRefresh = function (identifierName, itemToUpdate, isDelete) {
        if (itemToUpdate == null || typeof (itemToUpdate) === "undefined")
            return;
        // delete
        isDelete = isDelete || false;
        var id = itemToUpdate[identifierName];
        if (isDelete) {
            var item;
            ko.utils.arrayForEach(this(), function (i) {
                if (i[identifierName] == id) { item = i; }
            });
            ko.utils.arrayRemoveItem(this(), item);
            this.valueHasMutated();
            return;
        }
        // insert or update
        var isNew = true;
        for (var i = 0, j = this().length; i < j; i++) {
            if (this()[i][identifierName] === id) {
                // delete
                // update
                this()[i] = itemToUpdate;   // update model
                this.valueHasMutated();     // update UI
                isNew = false;              // item was modified (not new)
                break;                      // exit from the loop
            }
        }
        // insert
        if (isNew)
            this.push(itemToUpdate);
    }
    /*
    UI extensions
    */
    ko.bindingHandlers.fadeInText = {
        'update': function (element, valueAccessor) {
            $(element).hide();
            ko.bindingHandlers.text.update(element, valueAccessor);
            $(element).fadeIn('slow');
        }
    };

    ko.bindingHandlers.fadeVisible = {
        init: function (element, valueAccessor) {
            var shouldDisplay = valueAccessor();
            $(element).toggle(shouldDisplay);
        },
        update: function (element, valueAccessor) {
            // On update, fade in/out
            var shouldDisplay = valueAccessor();
            shouldDisplay ? $(element).fadeIn('slow') : $(element).hide();
        }
    };

    ko.bindingHandlers.select2 = {
        init: function (el, valueAccessor, allBindingsAccessor, viewModel) {
            ko.utils.domNodeDisposal.addDisposeCallback(el, function () {
                $(el).select2('destroy');
            });

            var allBindings = allBindingsAccessor(),
                select2 = ko.utils.unwrapObservable(allBindings.select2);

            $(el).select2(select2);
        },
        update: function (el, valueAccessor, allBindingsAccessor, viewModel) {
            var allBindings = allBindingsAccessor();

            if ("value" in allBindings) {
                $(el).select2("val", allBindings.value());
            } else if ("selectedOptions" in allBindings) {
                var converted = [];
                var textAccessor = function (value) { return value; };
                if ("optionsText" in allBindings) {
                    textAccessor = function (value) {
                        var valueAccessor = function (item) { return item; }
                        if ("optionsValue" in allBindings) {
                            valueAccessor = function (item) { return item[allBindings.optionsValue]; }
                        }
                        var items = $.grep(allBindings.options(), function (e) { return valueAccessor(e) == value });
                        if (items.length == 0 || items.length > 1) {
                            return "UNKNOWN";
                        }
                        return items[0][allBindings.optionsText];
                    }
                }
                $.each(allBindings.selectedOptions(), function (key, value) {
                    converted.push({ id: value, text: textAccessor(value) });
                });
                $(el).select2("data", converted);
            }
        }
    };
})(jQuery, ko);