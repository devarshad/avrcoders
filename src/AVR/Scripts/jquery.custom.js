$(function () {
    if ($('.ContactUs')[0]) {
        var settings = $.data($('.ContactUs')[0], 'validator').settings;
        settings.submitHandler = function (form, event) {
            $('.ContactUs').hide();
            $('.ContactUsSucess').show();
            $(form)[0].reset()
            $.ajax({
                url: form.action,
                type: 'POST',
                data: $(form).serialize(),
                success: function (result) {
                    // The AJAX call succeeded and the server returned a JSON 
                    // with a property "s" => we can use this property
                    // and set the html of the target div
                    $('#ShowResultHere').html(result.s);
                }
            });
            // it is important to return false in order to 
            // cancel the default submission of the form
            // and perform the AJAX call
            return false;
        };
    }
});

$(document).ready(function () {
    $('.chat-window-title').click(function () {
        $('.chat-window-content').toggle();
        $(this).find('.fa.fa-times').toggle();
        $('.ContactUs').show();
        $('.ContactUsSucess').hide();
    });
})
/*
 * Custom jquery.validation defaults
 */
$.validator.setDefaults({
    highlight: function (element, errorClass, validClass) {
        if (element.type === 'radio') {
            this.findByName(element.name).addClass(errorClass).removeClass(validClass);
        } else {
            $(element).addClass(errorClass).removeClass(validClass);
            $(element).closest('.form-group').removeClass('has-success').addClass('has-error');
        }
    },
    unhighlight: function (element, errorClass, validClass) {
        if (element.type === 'radio') {
            this.findByName(element.name).removeClass(errorClass).addClass(validClass);
        } else {
            $(element).removeClass(errorClass).addClass(validClass);
            $(element).closest('.form-group').removeClass('has-error').addClass('has-success');
        }
    },
    showErrors: function (errorMap, errorList) {
        this.defaultShowErrors();
        // If an element is valid, it doesn't need a tooltip
        $("." + this.settings.validClass).tooltip("destroy");

        // Add tooltips
        for (var i = 0; i < errorList.length; i++) {
            var error = errorList[i];
            //var id = '#' + error.element.id;
            //var isInModal = $(id).parents('.modal').length > 0;
            //, container: isInModal, html: false }) // Activate the tooltip on focus
            $(error.element).tooltip({ trigger: "focus" })
				.attr('data-tooltip', 'tooltip-danger')
				.attr("data-original-title", error.message);
        }
    }
});