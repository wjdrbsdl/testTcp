using System.Net.Sockets;


public class ClientInfo
{
    private static int InfoNumber = 1;
    public int socketNumber = 0;
    public byte[] buffer;
    public Socket workingSocket;
    public int PID = 0; //0이면 아이디를 전달받지 못한 상태
    public string NickID = ""; 
    public int HaveCard = 0;
    public int BadPoint = 0;
    public bool IsReady = false;
    public bool IsOut = false; //정상적으로 나가길 요청했는지

    public ClientInfo(int _bufferSize, Socket _claSocket)
    {
        buffer = new byte[_bufferSize];
        workingSocket = _claSocket;
        socketNumber = InfoNumber;
        InfoNumber++;
    }

    public void ResetScore()
    {
        HaveCard = 0;
        BadPoint = 0;
    }

    public void Dispose()
    {
        workingSocket?.Shutdown(SocketShutdown.Both);
        workingSocket?.Close();
        workingSocket?.Dispose();
    }

    public void CopyValue(ClientInfo info)
    {
        PID = info.PID;
        NickID = info.NickID;
        HaveCard = info.HaveCard;
        BadPoint = info.BadPoint;
        IsReady = info.IsReady;
        IsOut = info.IsOut;
    }
}

