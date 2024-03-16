var CommentsViewModel = function (master, isActive, id, callBackUrl) {
    var m = master;
    var self = this;

    var cont = $('#SwCommentsWrapper');
    var list = $('.chats', cont);
    var form = $('.chat-form', cont);
    var input = $('input', form);
    var btn = $('.btn', form);

    var getLastPostPos = function () {
        var height = 0;
        cont.find("li.out, li.in").each(function () {
            height = height + $(this).outerHeight();
        });

        return height;
    }

    var count = 10;
    var increment = 10;

    this.isActive = ko.observable(isActive || false);
    this.comments = ko.observableArray();
    this.model = ko.validatedObservable();
    this.showMoreAvailable = ko.observable(true)

    var validationMapping = {
        'message': {
            create: function (options) {
                return ko.observable(options.data)
                    .extend({ required: true })
                    .extend({ minLength: 1 })
                    .extend({ maxLength: 256 });
            }
        }
    };

    this.refresh = function (id, _count) {
        if (_count != undefined)
            count = _count;
        var url = "/api/internal/comments/" + id + "/" + count + "/0";
        // get document
        SwCore.getServerData(url, "SwCommentsWrapper", function (items) {
            self.comments.fastFill(items);
            SwInit.initDateTime();

            if (items.length < count)
                self.showMoreAvailable(false);

            //Metronic.initSlimScroll('.scroller');
            //cont = $('#SwCommentsWrapper');
            //cont.find('.scroller').slimScroll({
            //    scrollTo: getLastPostPos()
            //});
        });

    };

    this.post = function () {
        //var message = self.model().message();
        //self.model().message(message.replace(/^@[^:]+:\s/, ''));
        var errors = ko.validation.group(self.model());
        if (errors().length == 0) {
            var input = ko.mapping.toJSON(self.model());
            var url = "/api/internal/comments/" + self.model().documentId();
            SwCore.setServerData(url, "POST", "SwCommentsWrapper", input, function () {
                self.refresh(self.model().documentId());

                //clear input message
                var _newObject = { message: null, documentId: self.model().documentId(), repliedTo: { id: null } };
                self.model(ko.mapping.fromJS(_newObject, validationMapping));
            });
        }
        else {
            SwCore.showWarning("Мы обнаружили ошибочки :)", "Убедитесь что входные данные составлены корректно и попробуйте снова");
            self.model().message(message);
        }
    };

    this.click = function () {
        var name = this.author.username; // get clicked user's full name
        var message = $('.new-message').val() || '';
        $('.new-message').val(name + ', ' + message.replace(/^[^\s]+,\s/, ''));// set it into the input field
        Metronic.scrollTo($('.new-message'));
    }

    this.negative = function (userId, comment) {

        if (userId == "") {
            SwCore.showError("Для оценивания комментариев необходимо <a style='text-decoration:underline;color: white;' href='" + callBackUrl + "'>авторизироваться</a>");
            return;
        }

        if (comment.author.id == userId) {
            SwCore.showError("Ошибка", "Вы не можетет оценить ваш комментарий");
            return;
        }

        var newComment = JSON.parse(JSON.stringify(comment));
        if (newComment.isUserVoted) {
            if (newComment.isPositive) {
                newComment.isUserVoted = true;
                newComment.isPositive = false;
                newComment.rating = newComment.rating - 2;
            }
            else {
                newComment.isUserVoted = false;
                newComment.isPositive = false;
                newComment.rating = newComment.rating + 1;
            }
        }
        else {
            newComment.isUserVoted = true;
            newComment.isPositive = false;
            newComment.rating = newComment.rating - 1;
        }

        self.comments.smartRefresh("commentId", newComment, false);
        SwInit.initDateTime();

        var url = "/api/internal/comments/vote/" + newComment.commentId + "/" + newComment.isUserVoted + "/" + newComment.isPositive;
        SwCore.getServerData(url, "SwCommentsWrapper", function (data) {

        });
    };

    this.positive = function (userId, comment) {

        if (userId == "") {
            SwCore.showError("Для оценивания комментариев необходимо <a style='text-decoration:underline;color: white;' href='" + callBackUrl + "'>авторизироваться</a>");
            return;
        }

        if (comment.author.id == userId) {
            SwCore.showError("Ошибка", "Вы не можетет оценить ваш комментарий");
            return;
        }

        var newComment = JSON.parse(JSON.stringify(comment));

        if (newComment.isUserVoted) {
            if (newComment.isPositive) {
                newComment.isUserVoted = false;
                newComment.isPositive = false;
                newComment.rating = newComment.rating - 1;
            }
            else {
                newComment.isUserVoted = true;
                newComment.isPositive = true;
                newComment.rating = newComment.rating + 2;
            }
        }
        else {
            newComment.isUserVoted = true;
            newComment.isPositive = true;
            newComment.rating = newComment.rating + 1;
        }

        self.comments.smartRefresh("commentId", newComment, false);
        SwInit.initDateTime();

        var url = "/api/internal/comments/" + newComment.commentId + "/" + newComment.isUserVoted + "/" + newComment.isPositive;
        SwCore.getServerData(url, "SwCommentsWrapper", function (data) {
        });
    };

    this.showMore = function () {
        var newCount = count + increment;
        self.refresh(id, newCount);
    }

    this.startEdit = function (e) {
        var newComment = JSON.parse(JSON.stringify(e));
        newComment.onEdit = true;
        self.comments.smartRefresh("commentId", newComment, false);
        SwInit.initDateTime();
    };

    this.stopEdit = function (e) {
        if (e.message === "") {
            SwCore.showWarning("Мы обнаружили ошибочки :)", "Убедитесь что входные данные составлены корректно и попробуйте снова");
            return;
        }

        var input = ko.mapping.toJSON(e);
        var url = "/api/internal/comment/" + e.commentId + "/edit";
        SwCore.setServerData(url, "POST", "SwCommentsWrapper", input, function () {
            self.refresh(self.model().documentId());
        });
    };

    this.delete = function (e) {
        var url = "/api/internal/comment/" + e.commentId + "/delete";
        SwCore.getServerData(url, "SwCommentsWrapper",  function () {
            self.refresh(self.model().documentId());
        });
    };


    // subscriptions
    //master.shouter.subscribe(function (id) {


    //    var _newObject = { message: null, documentId: id, repliedTo: { id: null } };
    //    self.model(ko.mapping.fromJS(_newObject, validationMapping));
    //}, this, master.messages.createOrEdit);

    self.refresh(id);
    self.isActive(true);
    var _newObject = { message: null, documentId: id, repliedTo: { id: null } };
    self.model(ko.mapping.fromJS(_newObject, validationMapping));

    $('body').on('click', '.message .name', function (e) {
        e.preventDefault(); // prevent click event
        var name = $(this).text(); // get clicked user's full name
        var message = self.model().message() || '';
        self.model().message(name + ', ' + message.replace(/^[^\s]+,\s/, ''));// set it into the input field
        //self.model().repliedTo.id($(this).attr("user"));

        //cont = $('#SwCommentsWrapper');
        //form = $('.chat-form', cont);
        //input = $('input', form);
        //Metronic.scrollTo(input); // scroll to input if needed
    });
};