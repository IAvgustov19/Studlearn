var swSubCaptchaContainer = null;
var loadCaptcha = function () {
	var email = $('#SubscribptionEmail').val();
	if (email === "") {
		toastr.error("e-mail - обязательное поле");
		return;
	}
	$('#subscribeCaptchaModal').modal('show');
	swSubCaptchaContainer = grecaptcha.render('sw_sub_captcha_container', {
		'sitekey': '6Lc5sBETAAAAABvO2FhiWJwknAxnlhh2tpCw3r6x',
		'callback': function (response) {
			$.ajax({
				url: "/api/subscribe?g-recaptcha-response=" + response,
				type: "post",
				dataType: "json",
				contentType: "application/json; charset=utf-8",
				data: JSON.stringify(email),
				beforeSend: function () {
					$('#subscribeCaptchaModal').modal('hide');
					SwCore.blockPage();
				},
				success: function (message) {
					toastr.success(message);
				}
			})
			.fail(function (obj) {
				toastr.error(obj.responseJSON)
			})
			.always(function () {
				SwCore.unblockPage();
				$('#sw_sub_captcha_container').html('');
			});
		}
	});
};

jQuery(document).ready(function () {
	$('#Subscribe').on('click', function () {
		$.getScript("https://www.google.com/recaptcha/api.js?onload=loadCaptcha&render=explicit")
		  .fail(function (jqxhr, settings, exception) {
		  	SwCore.showError("Извини, подписка поломалась. Мы уже исправляем проблему...")
		  });
	});
});