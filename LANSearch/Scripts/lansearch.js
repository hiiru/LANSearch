$(document).ready(function () {
    $('[data-toggle=offcanvas]').click(function () {
        if ($('.sidebar-offcanvas').css('background-color') == 'rgb(255, 255, 255)') {
            $('.list-group-item').attr('tabindex', '-1');
        } else {
            $('.list-group-item').attr('tabindex', '');
        }
        $('.row-offcanvas').toggleClass('active');
    });

    $(".server-login-toggle").on('change', function (e) {
        var checkbox = $(e.target);
        var serverlogin = checkbox.parents(".server-login");
        serverlogin.find(".server-login-item").toggleClass("open", checkbox.is(':checked'));
    });
    $(".delete-button").on('click', function() {
        return confirm("Are you sure you want to delete this server?");
    });
});