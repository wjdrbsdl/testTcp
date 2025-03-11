using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace testTcp
{
    public class RoomServer
    {
        public List<Socket> roomUser = new List<Socket>();
        public Socket linkSocket;
        //방장이 호스트가 되어 해당 방에있던 유저들의 입력을 받고 처리 

        public void Connect()
        {
            linkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 5001);
            linkSocket.BeginAccept(AcceptCallBack, linkSocket);
        }

        private void AcceptCallBack(IAsyncResult _result)
        {

        }
       
    }

}
