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
    public int portNum = 0;
    public int curCount = 0;
    public int maxCount = 4;
    public string roomName = "";

    public RoomData()
    {

    }

    public RoomData(IPAddress _ip, int _portNum, string _roomName)
    {
        roomServerIP = _ip;
        portNum = _portNum;
        roomName = _roomName;
    }

    public RoomData(byte[] _recvRoomData)
    {
        //방 데이터 만들어놓고, 방 참가할 수 있도록 요청한 애 한테 정보 전달
        /*
         * [0] 이 방 정보의 총 길이
         * [1] 룸의 현재 인원 - 0이면 자신이 방장
         * [2] 룸포트 길이 2
         * [3] 방 이름 길이 
         * [4] 3번부터 2번 만큼 길이를 ushort로 변환한거 - 포트번호
         * [4+[2]] 부터 [3] 길이 만큼이 방이름
         */
        curCount = _recvRoomData[1];
        byte[] portByte = new byte[2];
        Buffer.BlockCopy(_recvRoomData, 4, portByte, 0, 2);
        portNum = (ushort)EndianChanger.NetToHost(portByte);
        byte[] nameByte = new byte[_recvRoomData[3]];
        Buffer.BlockCopy(_recvRoomData, 4+2, nameByte, 0, _recvRoomData[3]);
        roomName = Encoding.Unicode.GetString(nameByte);

    }

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

    public byte[] GetRoomDataPacket()
    {
        //방 데이터 만들어놓고, 방 참가할 수 있도록 요청한 애 한테 정보 전달
        /*
         * [0] 이 방 정보의 총 길이
         * [1] 룸의 현재 인원 - 0이면 자신이 방장
         * [2] 룸포트 길이 2
         * [3] 방 이름 길이 
         * [4] 3번부터 2번 만큼 길이를 ushort로 변환한거 - 포트번호
         * [4+[2]] 부터 [3] 길이 만큼이 방이름
         */

        ushort roomPort = (ushort)portNum;
        byte[] roomPortByte = EndianChanger.HostToNet(roomPort);
        byte[] roomNameByte = Encoding.Unicode.GetBytes(roomName);
        //한자릿수 라서 엔디안 신경 쓸필요가 없음.
        byte roomPortLength = (byte)2;
        byte roomNameLength = (byte)roomNameByte.Length;
        byte[] roomInfoPacket = new byte[1+1 + 1 + 1 + roomPortByte.Length + roomNameLength];
        int offSet = 0;
        roomInfoPacket[offSet++] = (byte)roomInfoPacket.Length;
        roomInfoPacket[offSet++] = (byte)curCount;
        roomInfoPacket[offSet++] = roomPortLength;
        roomInfoPacket[offSet++] = roomNameLength;
        Buffer.BlockCopy(roomPortByte, 0, roomInfoPacket, offSet, roomPortByte.Length);
        offSet += roomPortByte.Length;
        Buffer.BlockCopy(roomNameByte, 0, roomInfoPacket, offSet, roomNameByte.Length);
        return roomInfoPacket;
    }
}

