// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.


var server = 'ws://localhost:5000'; 

var WEB_SOCKET = new WebSocket(server + '/ws');

WEB_SOCKET.onopen = function (evt) {
    console.log('Abrindo conexão ...');
};

WEB_SOCKET.onmessage = function (evt) {
    console.log('Received Message: ' + evt.data);
    if (evt.data) {
        var content = $('#msgList').val();
        content = content + '\r\n' + evt.data;

        $('#msgList').val(content);
    }
};

WEB_SOCKET.onclose = function (evt) {
    console.log('Conexão fechada.');
};

$('#btnJoin').on('click', function () {
    var roomNo = $('#txtRoomNo').val();
    var nick = $('#txtNickName').val();
    if (nick != undefined && nick != '' && nick != null && roomNo != undefined && roomNo != '' && roomNo != null) {
        if (roomNo) {
            var msg = {
                action: 'join',
                msg: roomNo,
                nick: nick
            };
            WEB_SOCKET.send(JSON.stringify(msg));
        }
    }
    else
        alert("Informe o Nick e a sala para continuar!");
});

$('#btnSend').on('click', function () {
    var message = $('#txtMsg').val();
    var nick = $('#txtNickName').val();
    if (message) {
        WEB_SOCKET.send(JSON.stringify({
            action: 'send_to_room',
            msg: message,
            nick: nick
        }));
        $('#txtMsg').val("");
    }
});

$('#btnLeave').on('click', function () {
    var nick = $('#txtNickName').val();
    if (nick != undefined && nick != '' && nick != null) {
        var msg = {
            action: 'leave',
            msg: '',
            nick: nick
        };
        WEB_SOCKET.send(JSON.stringify(msg));
    }
    else
        alert("Informe o Nick para sair!");
});
