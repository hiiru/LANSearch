$(document).ready(function () {
    //#region General
    function htmlEncode(value) {
        return $('<div />').text(value).html();
    }

    $(".confirm-button").on('click', function () {
        var msg = $(this).data("confirm");
        if (msg.length)
            return confirm(msg);
        else
            return confirm("Are you sure?");
    });
    //#endregion

    //#region Search Page
    var pageSearch = $(".page-search");
    if (pageSearch.length) {
        $('[data-toggle=offcanvas]').click(function () {
            if ($('.sidebar-offcanvas').css('background-color') == 'rgb(255, 255, 255)') {
                $('.list-group-item').attr('tabindex', '-1');
            } else {
                $('.list-group-item').attr('tabindex', '');
            }
            $('.row-offcanvas').toggleClass('active');
        });
        pageSearch.children(".result-list").children(".disabled").on('click', function () { return false; });
    }
    //#endregion

    //#region Server Page
    var pageServer = $(".page-server");
    if (pageServer.length) {
        $(".server-login-toggle").on('change', function (e) {
            var checkbox = $(e.target);
            var serverlogin = checkbox.parents(".server-login");
            serverlogin.find(".server-login-item").toggleClass("open", checkbox.is(':checked'));
        });
    }
    //#endregion

    //#region Notification Page
    var pageNotification = $(".page-notification");
    if (pageNotification.length) {
        var lblCheck = $("#lblCheck");
        var btnPermission = $("#btnPermission");
        var btnTest = $("#btnTest");
        function UpdatePermissionLabel() {
            if (!("Notification" in window)) {
                lblCheck.text("Not Supported by Browser");
                lblCheck.addClass("label-danger");
            } else {
                lblCheck.removeClass("label-success label-danger label-warning");
                switch (Notification.permission) {
                    case "granted":
                        lblCheck.text("Allowed");
                        lblCheck.addClass("label-success");
                        btnPermission.hide();
                        btnTest.addClass("btn-primary");
                        break;
                    case "denied":
                        lblCheck.text("Denied");
                        lblCheck.addClass("label-danger");
                        btnPermission.hide();
                        break;
                    default:
                        lblCheck.text("Not Enabled");
                        lblCheck.addClass("label-warning");
                        break;
                }
            }
        }
        UpdatePermissionLabel();
        btnPermission.on('click', function () {
            if (!("Notification" in window)) {
                BootstrapDialog.alert("Your Browser does not support HTML5 Notifications.");
                return;
            }
            Notification.requestPermission(function (permission) {
                if (!('permission' in Notification)) {
                    Notification.permission = permission;
                }
                UpdatePermissionLabel();
                if (permission === "granted") {
                    var notification = new Notification("LANSearch: Notifications are enabled.",
                    {
                        dir: "ltr",
                        lang: "en",
                        body: "You can now receive HTML 5 Notifications from LANSearch.",
                        tag: "notification-eanbled"
                    });
                }
            });
        });
        btnTest.on('click', function () {
            if (!("Notification" in window)) {
                BootstrapDialog.alert("Your Browser does not support HTML5 Notifications, you will only get in-browser dialogs like this.");
                return;
            }
            if (Notification.permission === "granted") {
                var notification = new Notification("LANSearch: Notifications are enabled.",
                {
                    dir: "ltr",
                    lang: "en",
                    body: "You can receive HTML 5 Notifications from LANSearch.",
                    tag: "notification-eanbled"
                });
            } else {
                BootstrapDialog.alert("HTML5 Notifications aren't allowed yet, please click the \"Grant Permission\" Button, or if it's denied, reset it in your browser's configuration (often it's located behind the icon in your address bar, click on it and you should get a dialog to change the permissions)");
            }
        });
    }
    //#endregion

    //#region SignalR Notification stuff

    var disableNotification = false;
    function browserNotification(name, message, tag) {
        if (disableNotification) {
            return;
        }
        if (!("Notification" in window)) {
            disableNotification = true;
            if (console.log) {
                console.log("Notifications are not supported by this browser.");
            }
            return;
        }
        if (Notification.permission === "granted") {
            var notification = new Notification("LANSearch: New results for " + name,
            {
                dir: "ltr",
                lang: "en",
                body: message,
                tag: tag,
                onclick: function (a, b, c) {
                    console.log("clicked");
                }
            });
            notification.onclick = function () {
                $.each(BootstrapDialog.dialogs, function (i, obj) {
                    if (obj.getId() == tag) {
                        obj.getModal().css('z-index', BootstrapDialog.ZINDEX_MODAL + 500);
                        return false;
                    }
                });
            }
            return;
        } else {
            disableNotification = true;
            if (console.log) {
                console.log("Notifications are not allowed.");
            }
        }
    }

    var connectionReady = false;
    var connection = $.hubConnection("/sr", { useDefaultPath: false });
    var notifyHub = connection.createHubProxy('notificationHub');
    notifyHub.on('notify', function (id, name, url, items) {
        if (items.length == 0) return;
        var notId = "notify-" + id;

        var shortMessage = "";
        var htmlMessage = "";
        $.each(items, function (i, obj) {
            if (shortMessage.length > 0) {
                shortMessage += "\n";
            }
            shortMessage += obj.FileName + " (" + obj.FileSize + ")";
            htmlMessage += '<a class="list-group-item filelist" target="_blank" href="' + obj.FileUrl.replace('"', '') + '">' +
                '<h4 class="media-heading">' + htmlEncode(obj.FileName) + '</h4>' +
                '<div class="row">' +
                '<div class="col-xs-6"><span class="glyphicon glyphicon-hdd"></span> ' + htmlEncode(obj.ServerName) + '</div>' +
                '<div class="col-xs-6"><span class="glyphicon glyphicon-file"></span> ' + htmlEncode(obj.FileSize) + '</div>' +
                '</div></a>';
        });
        var dialog;
        $.each(BootstrapDialog.dialogs, function (i, obj) {
            if (obj.getId() == id) {
                dialog = obj;
                return false;
            }
        });
        if (!dialog) {
            dialog = new BootstrapDialog({
                id: notId,
                title: "Search Notification for " + name,
                message: htmlMessage,
                draggable: true,
                closable: false,
                buttons: [
                    {
                        label: 'Close',
                        action: function (dialogRef) {
                            dialogRef.close();
                        }
                    }
                ]
            });
        } else {
            dialog.setMessage(htmlMessage);
        }
        dialog.open();
        browserNotification(name, shortMessage, notId);
    });

    notifyHub.on('announcement', function (title, message, type) {
        var dType;
        switch (type) {
            case "danger":
                dType = BootstrapDialog.TYPE_DANGER;
                break;
            case "warning":
                dType = BootstrapDialog.TYPE_WARNING;
                break;
            case "success":
                dType = BootstrapDialog.TYPE_SUCCESS;
                break;
            default:
                dType = BootstrapDialog.TYPE_INFO;
                break;
        }

        var dialog = new BootstrapDialog({
            title: title,
            message: message,
            type: dType,
            draggable: true,
            closable: false,
            buttons: [
                {
                    label: 'Close',
                    action: function (dialogRef) {
                        dialogRef.close();
                    }
                }
            ]
        });
        dialog.open();
    });

    notifyHub.on('administratorMessage', function (message) {
        var dialog = new BootstrapDialog({
            title: "Message from LANSearch Administrator",
            message: message,
            type: BootstrapDialog.TYPE_WARNING,
            draggable: true,
            closable: false,
            buttons: [
                {
                    label: 'Close',
                    action: function (dialogRef) {
                        dialogRef.close();
                    }
                }
            ]
        });
        dialog.open();
    });
    
    //#region Admin methods
    var pageAdminAnnouncement = $(".page-admin-announcement");
    if (pageAdminAnnouncement.length) {
        notifyHub.on('cmdConfirm', function (message) {
            var dialog = new BootstrapDialog({
                title: "Server Response",
                message: message,
                type: BootstrapDialog.TYPE_DEFAULT,
                draggable: true
            });
            dialog.open();
        });

        var annTitle = $("#annTitle");
        var annMsg = $("#annMsg");
        var annType = $("#annType");
        $("#annSend").on('click', function () {
            if (!connectionReady) {
                BootstrapDialog.alert("SignalR connection not ready yet, please wait a few seconds.");
                return;
            }
            var title = annTitle.val();
            var msg = annMsg.val();
            var type = annType.val();

            if (title.length == 0 || msg.length == 0 || type.length == 0) {
                BootstrapDialog.alert("The data is incomplete, please fill out title, message and select a type.");
                return;
            }

            if (notifyHub)
                notifyHub.invoke("SendAnnouncementMessage", title, msg, type);
            else
                BootstrapDialog.alert("Error: SignalR connection not setup correctly, can't sent message!");
        });

        var msgUser = $("#msgUser");
        var msgMsg = $("#msgMsg");
        var msgType = $("#msgType");
        $("#msgSend").on('click', function() {
            if (!connectionReady) {
                BootstrapDialog.alert("SignalR connection not ready yet, please wait a few seconds.");
                return;
            }
            var user = msgUser.val();
            var msg = msgMsg.val();
            var type = msgType.val();

            if (user.length == 0 || msg.length == 0 || type.length == 0) {
                BootstrapDialog.alert("The data is incomplete, please fill out title, message and select a type.");
                return;
            }

            if (notifyHub)
                notifyHub.invoke("SendAdminMessage", user, msg, type);
            else
                BootstrapDialog.alert("Error: SignalR connection not setup correctly, can't sent message!");
        });
    }
    //#endregion

    connection.start().done(function () {
        connectionReady = true;
    });

    //#endregion
});
