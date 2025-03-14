using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum EMixtureType
{
    None, OnePair, TwoPair, Triple, Straight, Flush, FullHouse, FourCard, StraightFlush, RoyalStriaghtFlush
}

public class CardRule
{
    public EMixtureType CheckValidRule(List<CardData> _list)
    {
        int cardCount = _list.Count;
        
        if (cardCount == 1)
        {
            return EMixtureType.OnePair;
        }
        else if (cardCount == 2)
        {
            if (IsTwoPair(_list))
            {
                return EMixtureType.TwoPair;
            }
        }
        else if (cardCount == 3)
        {
            if (IsTriple(_list))
            {
                return EMixtureType.Triple;
            }
        }
        else if (cardCount == 5)
        {
            return CheckFiveMixture(_list);
        }

        return EMixtureType.None;
    }

    public bool IsTwoPair(List<CardData> _list)
    {
        if (_list[0].num == _list[1].num)
        {
            return true;
        }
        return false;
    }

    public bool IsTriple(List<CardData> _list)
    {
        if (_list[0].num == _list[1].num && _list[1].num == _list[2].num)
        {
            return true;
        }
        return false;
    }

    public EMixtureType CheckFiveMixture(List<CardData> _list)
    {
        _list.Sort();
        //스트레이트인가
        int straightNum = _list[0].realValue;
        bool isStraight = true;
        for (int i = 1; i < _list.Count; i++)
        {
            if (_list[i].realValue != straightNum + 1)
            {
                //다음 숫자가 이전 숫자보다 1큰게 아니면 실패
                isStraight = false;
                break;
            }
            //같으면
            straightNum++; //스트레이트 숫자 올리고
            //모두 무사히 통과되었으면 스트레이트 유지
        }

        //무늬 모두 같아야 플러쉬
        CardClass flushClass = _list[0].cardClass;
        bool isFlush = true;
        for (int i = 1; i < _list.Count; i++)
        {
            if (_list[i].cardClass != flushClass)
            {
                isFlush = false;
                break;
            }
        }

        //로티플, 스티플, 따져보기
        if(isStraight && isFlush)
        {
            //로티플 숫자는 j q k 1 2
            if(_list[0].realValue == 11)
            {
                //최소 스티플
                if (_list[0].cardClass == CardClass.Spade)
                {
                    return EMixtureType.RoyalStriaghtFlush;
                }
                return EMixtureType.StraightFlush;
            }
        }
        if (isStraight)
        {
            return EMixtureType.Straight;
        }
        if (isFlush)
        {
            return EMixtureType.Flush;
        }

        //풀하우스 따지기
        //앞에 3, 뒤2 이 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[1].realValue == _list[2].realValue &&
            _list[3].realValue == _list[4].realValue)
        {
            return EMixtureType.FullHouse;
        }
        //앞에2, 뒤3이 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[2].realValue == _list[3].realValue &&
            _list[3].realValue == _list[4].realValue)
        {
            return EMixtureType.FullHouse;
        }
        //포카드 따지기
        //앞에 4개가 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[1].realValue == _list[2].realValue &&
            _list[2].realValue == _list[3].realValue)
        {
            return EMixtureType.FourCard;
        }
        //뒤에 4개가 같거나
        if (_list[4].realValue == _list[1].realValue &&
           _list[1].realValue == _list[2].realValue &&
           _list[2].realValue == _list[3].realValue)
        {
            return EMixtureType.FourCard;
        }

        return EMixtureType.None;
    }
}
