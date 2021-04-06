using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatWebSocket.Models
{
    public class WebsocketClientCollection
    {
        private static List<WebsocketClient> _clients = new List<WebsocketClient>();

        //Incluindo um SocketClient
        public static void Add(WebsocketClient client)
        {
            _clients.Add(client);
        }

        //Removendo um SocketClient
        public static void Remove(WebsocketClient client)
        {
            _clients.Remove(client);
        }

        //Buscando um Socket por Id
        public static WebsocketClient Get(string clientId)
        {
            var client = _clients.FirstOrDefault(c=>c.Id == clientId);

            return client;
        }

        //Buscando um Socket por Nick
        public static WebsocketClient GetClientNick(string nick)
        {
            var client = _clients.FirstOrDefault(c => c.Nick == nick);

            return client;
        }

        //Buscando todos os Sockets Conectados em uma sala
        public static List<WebsocketClient> GetRoomClients(string roomNo)
        {
            var client = _clients.Where(c => c.RoomNo == roomNo);
            return client.ToList();
        }
    }
}
