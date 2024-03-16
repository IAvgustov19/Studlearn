var WorksLikesViewModel = function (rating, isPositive, IsUserVoted, authorId, currentUserId, workId, callBackUrl) {
    var viewModel = {
        isUserVoted: ko.observable(isPositive),
        isPositive: ko.observable(IsUserVoted),
        rating: ko.observable(rating),
        negative: function (userId, comment) {

            if (currentUserId == "") {
                SwCore.showError("Для оценивания работ необходимо <a style='text-decoration:underline;color: white;' href='" + callBackUrl + "'>авторизироваться</a>");
                return;
            }

            if (authorId == currentUserId) {
                SwCore.showError("Вы не можетет оценить вашу работу");
                return;
            }
            if (this.isUserVoted()) {
                if (this.isPositive()) {
                    this.isUserVoted(true);
                    this.isPositive(false);
                    this.rating(this.rating() - 2);
                }
                else {
                    this.isUserVoted(false);
                    this.isPositive(false);
                    this.rating(this.rating() + 1);
                }
            }
            else {
                this.isUserVoted(true);
                this.isPositive(false);
                this.rating(this.rating() - 1);
            }

            var url = "/api/internal/works/" + workId + "/" + this.isUserVoted() + "/" + this.isPositive();
            SwCore.getServerData(url, "workrating", function (data) {});
        },
        positive: function (userId, comment) {

            if (currentUserId == "") {
                SwCore.showError("Для оценивания работ необходимо <a style='text-decoration:underline;color: white;' href='" + callBackUrl + "'>авторизироваться</a>");
                return;
            }

            if (authorId == currentUserId) {
                SwCore.showError("Вы не можетет оценить вашу работу");
                return;
            }

            if (this.isUserVoted()) {
                if (this.isPositive()) {
                    this.isUserVoted(false);
                    this.isPositive(false);
                    this.rating(this.rating() - 1);
                }
                else {
                    this.isUserVoted(true);
                    this.isPositive(true);
                    this.rating(this.rating() + 2);
                }
            }
            else {
                this.isUserVoted(true);
                this.isPositive(true);
                this.rating(this.rating() + 1);
            }

            var url = "/api/internal/works/" + workId + "/" + this.isUserVoted() + "/" + this.isPositive();
            SwCore.getServerData(url, "workrating", function (data) {});
        }
    }
    return viewModel;
};