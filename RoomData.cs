using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<Server.AsyncObject> clientList;

    public RoomData()
    {
        clientList = new List<Server.AsyncObject>();
    }

    public void AddParty(Server.AsyncObject _obj)
    {
        clientList.Add(_obj);
    }

    public List<Server.AsyncObject> GetParty()
    {
        return clientList;
    }

    public void ChangeState(RoomState _state)
    {
        Console.WriteLine("방 상태 변경 "+_state);
        roomState = _state;
    }
}

