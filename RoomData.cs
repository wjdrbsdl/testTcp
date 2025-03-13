using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using testTcp;

public enum RoomState
{
    Ready, Play
}

public class RoomData
{
    public RoomState roomState = RoomState.Ready;
    public IPAddress roomServerIP;
    public int curCount = 0;
    public int maxCount = 4;

    public void ChangeState(RoomState _state)
    {
        Console.WriteLine("방 상태 변경 "+_state);
        roomState = _state;
    }

    public void Enter()
    {
        curCount++;
    }

    public void Out()
    {
        curCount--;
    }
}

