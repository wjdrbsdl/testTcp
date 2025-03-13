using System.Net;
using System.Net.Sockets;

namespace testTcp
{
    public class ServerTest
    {
        Socket mainSocket; //서버입장에서 소켓은 뭘까 - 플레이어로 부터 접속을 요청받을 곳
        List<ClientSockect> clientList; //요청 수락후 플레이어로 자료 전달을 위한 소켓들
        public int m_portNum = 5000;
        public int m_numbering = 1; 
        public void Start()
        {
            //서버에서 할일들
            //1. 자기 소켓 오픈 - 주소 지정체계? / 연결지향, 비연결지향 / 프로토콜 방식
            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //아이피 주소와, 포트넘버로 엔드포인트 설정 - 서버의 경우엔 대상 주소를 any로
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, m_portNum);
            //2. 바인딩
            mainSocket.Bind(serverEP); // 모든 주소를 ep로 하기 때문에 특정 주소 지정을 위해 바인딩 
            //3. 몇개 받을지
            mainSocket.Listen(10); //
            //4. 연결 받을 상태로 진행 - 연결 요청 들어왔을때 어떻게할지 콜백함수 필요
            mainSocket.BeginAccept(AcceptCallBack, null);
        }

        private void AcceptCallBack(IAsyncResult _ar)
        {
            //요청 들어온 ip로 클라 소켓 구성하고
            //해당 소켓을 자료를 받을 상태로 요청
            try
            {
                Console.WriteLine("서버에서 요청 수락");
                Socket client = mainSocket.EndAccept(_ar); //EndAccept 으로 요청 들어온 Sockect을 생성
                ClientSockect cSocket = new ClientSockect(client); //입맛에 맞게 클래스로 포장
                cSocket.Numbering = m_numbering;
                m_numbering++;
                clientList.Add(cSocket); //연결된 리스트에 추가
                //client.BeginReceive(); //해당 소켓 수신 상태로 호출

                mainSocket.BeginAccept(AcceptCallBack, null); //서버 소켓은 다시 연결수락용으로 진행
            }
            catch(Exception e)
            {

            }
        }

        private void ReceiveDataCallBack(IAsyncResult _ar)
        {
            //들어온 자료가 이상할 수 있어서
            try
            {
                //자료를 받은 경우 
                //해당 자료가 진짜인지 체크
                //자료를 가지고 처리
                //다시 자료 받을 상태로 요청
            }
            catch (Exception e)
            {

            }

        }

        public void Close()
        {
            //나의 소켓 닫기
        }
    }

    public class ClientSockect
    {
        //연결된 소켓에 최초로 정보를 입력해서 계속 돌리기 위한 클래스
        public Socket WorkSockect;
        public byte[] Buffer;
        public int Numbering = -1;
        
        public ClientSockect(Socket _socket)
        {
            WorkSockect = _socket;
        }
    }
   
}

