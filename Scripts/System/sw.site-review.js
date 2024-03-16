$("#button-post-review").click(function (e) {
    var $points = $("#post-review input[name=points]:checked");
    var $comment = $("#post-review textarea[name=comment]");

    var comment = $comment.val();
    var points = $points.val();
    if (points < 3 && comment == '') {
        $comment.addClass("invalid");
        e.preventDefault();
        return false;
    } else {
        $comment.removeClass("invalid");
    }

    var emotion = $points.closest(".item").attr('title');
    var data = { 'comment': comment, 'points': points, 'emotion': emotion };
    $.ajax({
        url: "/api/sitereview/postreview",
        type: "POST",
        data: JSON.stringify(data),
        contentType: 'application/json;charset=utf-8',
        success: function (response) {
            console.log(response);
            $("#post-review .smiles-panel").hide();
            $("#post-review #thank-you-panel").show();
            $comment.val('');
            $points.val('');
            $("#post-review .smiles .item img").removeClass("disabled");
            $("#post-review .comment-panel").hide();
        },
        error: function (response) {
            toastr.error(response);
        }
    });
});

$('#site-review-modal').on('hidden.bs.modal', function (e) {
    $("#post-review #thank-you-panel").hide();
    $("#post-review .smiles-panel").show();
})

$("#post-review .smiles .item").click(function () {
    $("#post-review .smiles .item img").addClass("disabled");
    $(this).find("img").removeClass("disabled");
    $("#post-review .comment-panel").show();

    var points = $(this).data("points");
    var text = "Посоветуйте, какую информацию нам добавить?";
    if (points < 3) {
        text = "Вы чем-то расстроены? Пожалуйста, поделитесь этим с нами!";
        $("#post-review textarea").attr("required", "required");
        $("#post-review .comment-panel .info-text .star").show();
    } else {
        $("#post-review textarea").removeAttr("required");
        $("#post-review .comment-panel .info-text .star").hide();
        $("#post-review textarea").removeClass("invalid");
    }
    $("#post-review .comment-panel .info-text .question").text(text);
});