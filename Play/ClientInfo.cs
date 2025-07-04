using System.Net.Sockets;

public class ClientInfo
{
    public byte[] buffer;
    public Socket workingSocket;
    public int PID = 0; //0이면 아이디를 전달받지 못한 상태
    public string NickID = ""; 
    public int HaveCard = 0;
    public int BadPoint = 0;
    public bool IsReady = false;

    public ClientInfo(int _bufferSize, Socket _claSocket)
    {
        buffer = new byte[_bufferSize];
        workingSocket = _claSocket;

    }

    public void ResetScore()
    {
        HaveCard = 0;
        BadPoint = 0;
    }

    public void SetSocket(Socket newSock)
    {
        workingSocket?.Shutdown(SocketShutdown.Both);
        workingSocket?.Close();
        workingSocket?.Dispose();

        workingSocket = newSock;
    }
}

