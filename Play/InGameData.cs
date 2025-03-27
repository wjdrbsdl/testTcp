public class InGameData
{
    public int userCount;
    public string[] userIds = new string[4]; //최대 4명

    public string curId; //현재 유저
    public string preCard; //전에 낸 카드

    public string roomName; //방 이름
    public void SetRoomName(string _name)
    {
        //플클에서 ResRoonName에서 진행
        roomName = _name;
    }

    public void SetUserCount(int _count)
    {
        //플클에서 ResRegisterClientIDToPartyID 로 응답 받아 아이디 다 기록후 마지막에 유저수 기입
        userCount = _count;
    }
}
