using ChatWebSocket.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatWebSocket.Middlewares
{
    public class WebsocketHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public WebsocketHandlerMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory
            )
        {
            _next = next;
            _logger = loggerFactory.
                CreateLogger<WebsocketHandlerMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    string clientId = Guid.NewGuid().ToString(); ;
                    var wsClient = new WebsocketClient
                    {
                        Id = clientId,
                        WebSocket = webSocket
                    };
                    try
                    {
                        await Handle(wsClient);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Echo websocket client {0} err .", clientId);
                        await context.Response.WriteAsync("closed");
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task Handle(WebsocketClient webSocket)
        {
            WebsocketClientCollection.Add(webSocket);
            _logger.LogInformation($"Websocket client added.");

            WebSocketReceiveResult result = null;
            do
            {
                var buffer = new byte[1024 * 1];
                result = await webSocket.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text && !result.CloseStatus.HasValue)
                {
                    var msgString = Encoding.UTF8.GetString(buffer);
                    _logger.LogInformation($"Websocket client ReceiveAsync message {msgString}.");
                    var message = JsonConvert.DeserializeObject<Message>(msgString);
                    message.SendClientId = webSocket.Id;
                    MessageRoute(message);
                }
            }
            while (!result.CloseStatus.HasValue);
            WebsocketClientCollection.Remove(webSocket);
            _logger.LogInformation($"Websocket client closed.");
        }

        private void MessageRoute(Message message)
        {
            var client = WebsocketClientCollection.Get(message.SendClientId);
            switch (message.action)
            {
                //Entrando na sala
                case "join":

                    //Valida Se Nick já está na sala
                    var clientJoin = WebsocketClientCollection.GetClientNick(message.nick);
                    if (clientJoin != null)
                    {
                        client.SendMessageAsync("Nick já existente na sala, favor escolher outro.");
                        break;
                    }
                    client.RoomNo = message.msg;
                    client.Nick = message.nick;

                    //Notifica todos da sala a conexão de um usuário novo
                    var clientsEnter = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    clientsEnter.ForEach(c =>
                    {
                        c.SendMessageAsync($"{message.nick} entrou na sala {client.RoomNo} com sucesso .");
                    });
                    _logger.LogInformation($"Websocket client {message.SendClientId} entrou na sala {client.RoomNo}.");

                    //Notifica pro usuário logado as instruções de Uso
                    client.SendMessageAsync("Instruções para envio de mensagens:\n - Para enviar mensagens privadas, digite: @Nick=Mensagem" +
                        "\n - Para enviar mensagens públicas na sala, basta escrever a mensagem");
                    break;

                //Envio de mensagem
                case "send_to_room":
                    if (string.IsNullOrEmpty(client.RoomNo))
                    {
                        client.SendMessageAsync("Entre na sala para enviar a mensagem!");
                        break;
                    }
                    //Mensagem privada para um usuário da sala
                    if (message.msg.First() == '@')
                    {
                        string[] estruturaMensagem = message.msg.Split("=");
                        var mensagem = estruturaMensagem[1];
                        var nick = estruturaMensagem[0].Substring(1, estruturaMensagem[0].Length - 1);
                        var clientDestinatario = WebsocketClientCollection.GetClientNick(nick);
                        var clientRemetente = WebsocketClientCollection.GetClientNick(message.nick);
                        if (clientDestinatario != null)
                        {
                            clientDestinatario.SendMessageAsync($"{message.nick} diz para {nick}: {mensagem}");
                            clientRemetente.SendMessageAsync($"{message.nick} diz para {nick}: {mensagem}");
                        }
                        else
                            client.SendMessageAsync($"O usuário {nick} não está na sala!");

                        break;
                    }

                    //Mensagem publica para usuários da sala
                    var clients = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    clients.ForEach(c =>
                    {
                        c.SendMessageAsync(message.nick + " : " + message.msg);
                    });
                    _logger.LogInformation($"Websocket client {message.SendClientId} enviou uma mensagem {message.msg} para a sala {client.RoomNo}");

                    break;
                //Saindo da sala
                case "leave":
                    var clientsLeave = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    clientsLeave.ForEach(c =>
                    {
                        c.SendMessageAsync($"{message.nick} saiu da sala {client.RoomNo} com sucesso .");
                    });
                    client.RoomNo = "";
                    client.Nick = "";
                    var clientsSala = WebsocketClientCollection.GetRoomClients(client.RoomNo);
                    _logger.LogInformation($"Websocket client {message.SendClientId} saiu da sala {client.RoomNo}");
                    break;
                default:
                    break;
            }
        }
    }
}
