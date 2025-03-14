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
    public bool IsVarid(List<CardData> _list, out TMixture _mixtureValue)
    {
        EMixtureType mixture = CheckValidRule(_list, out _mixtureValue);
        Console.WriteLine($"{_mixtureValue.mixture} : {_mixtureValue.mainCardClass} : {_mixtureValue.mainRealValue}");
        return mixture != EMixtureType.None;
    }

    public EMixtureType CheckValidRule(List<CardData> _list, out TMixture _mixtureValue)
    {
        int cardCount = _list.Count;
        TMixture mixtureValue = new TMixture(EMixtureType.None, CardClass.Spade, 0);
        if (cardCount == 1)
        {
            mixtureValue.mixture = EMixtureType.OnePair;
            mixtureValue.mainRealValue = _list[0].realValue;
            mixtureValue.mainCardClass = _list[0].cardClass;
            _mixtureValue = mixtureValue;
            return EMixtureType.OnePair;
        }
        else if (cardCount == 2)
        {
            if (IsTwoPair(_list, out _mixtureValue))
            {
                return EMixtureType.TwoPair;
            }
        }
        else if (cardCount == 3)
        {
            if (IsTriple(_list, out _mixtureValue))
            {
                return EMixtureType.Triple;
            }
        }
        else if (cardCount == 5)
        {
            return CheckFiveMixture(_list, out _mixtureValue);
        }

        _mixtureValue = mixtureValue;
        return EMixtureType.None;
    }

    public bool IsTwoPair(List<CardData> _list, out TMixture _mixtureValue)
    {
        TMixture mixtureValue = new TMixture();
        if (_list[0].num == _list[1].num)
        {
            mixtureValue.mixture = EMixtureType.TwoPair;
            mixtureValue.mainRealValue = _list[0].realValue;
            //두개중에 큰 무늬를 메인으로
            if (CompareCardClass(_list[0].cardClass, _list[1].cardClass) > 0)
            {
                mixtureValue.mainCardClass = _list[0].cardClass;
            }
            else
            {
                mixtureValue.mainCardClass = _list[1].cardClass;
            }
            _mixtureValue = mixtureValue;
            return true;
        }
        _mixtureValue = mixtureValue;
        return false;
    }

    public bool IsTriple(List<CardData> _list, out TMixture _mixtureValue)
    {
        TMixture mixtureValue = new TMixture();
        if (_list[0].num == _list[1].num && _list[1].num == _list[2].num)
        {
            mixtureValue.mixture = EMixtureType.Triple;
            mixtureValue.mainCardClass = CardClass.Dia;//상관없음
            mixtureValue.mainRealValue = _list[0].realValue;
            _mixtureValue = mixtureValue;
            return true;
        }
        _mixtureValue = mixtureValue;
        return false;
    }

    public EMixtureType CheckFiveMixture(List<CardData> _list, out TMixture _mixtureValue)
    {
        TMixture tmixtureValue = new TMixture();
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
        if (isStraight && isFlush)
        {
            //로티플은 j q k 1 2 에 무늬가 스페이드
            if (_list[0].realValue == 11 && _list[0].cardClass == CardClass.Spade)
            {
                tmixtureValue.mixture = EMixtureType.RoyalStriaghtFlush;
                tmixtureValue.mainCardClass = CardClass.Spade;
                tmixtureValue.mainRealValue = 15;
                _mixtureValue = tmixtureValue;
                return EMixtureType.RoyalStriaghtFlush;
            }
            //그게 아니면 스티플 끼리는 무늬부터, 그리고 가장 큰 숫자 
            tmixtureValue.mixture = EMixtureType.StraightFlush;
            tmixtureValue.mainCardClass = _list[0].cardClass;
            tmixtureValue.mainRealValue = _list[4].realValue; //
            _mixtureValue = tmixtureValue;
            return EMixtureType.StraightFlush;
        }
        if (isStraight)
        {
            //가장 큰 숫자의 무늬와 실제벨류가 기준
            tmixtureValue.mixture = EMixtureType.Straight;
            tmixtureValue.mainCardClass = _list[4].cardClass;
            tmixtureValue.mainRealValue = _list[4].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.Straight;
        }
        if (isFlush)
        {
            //가장 큰 숫자의 무늬와 실제벨류
            tmixtureValue.mixture = EMixtureType.Straight;
            tmixtureValue.mainCardClass = _list[4].cardClass;
            tmixtureValue.mainRealValue = _list[4].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.Flush;
        }

        //풀하우스 따지기
        //앞에 3, 뒤2 이 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[1].realValue == _list[2].realValue &&
            _list[3].realValue == _list[4].realValue)
        {
            //같은거 3개의 숫자크기
            tmixtureValue.mixture = EMixtureType.FullHouse;
            tmixtureValue.mainCardClass = _list[0].cardClass; //상관없음
            tmixtureValue.mainRealValue = _list[0].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.FullHouse;
        }
        //앞에2, 뒤3이 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[2].realValue == _list[3].realValue &&
            _list[3].realValue == _list[4].realValue)
        {
            //같은거 3개의 숫자크기
            tmixtureValue.mixture = EMixtureType.Straight;
            tmixtureValue.mainCardClass = _list[4].cardClass; //상관없음
            tmixtureValue.mainRealValue = _list[4].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.FullHouse;
        }
        //포카드 따지기
        //앞에 4개가 같거나
        if (_list[0].realValue == _list[1].realValue &&
            _list[1].realValue == _list[2].realValue &&
            _list[2].realValue == _list[3].realValue)
        {
            tmixtureValue.mixture = EMixtureType.FourCard;
            tmixtureValue.mainCardClass = _list[0].cardClass; //상관없음
            tmixtureValue.mainRealValue = _list[0].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.FourCard;
        }
        //뒤에 4개가 같거나
        if (_list[4].realValue == _list[1].realValue &&
           _list[1].realValue == _list[2].realValue &&
           _list[2].realValue == _list[3].realValue)
        {
            tmixtureValue.mixture = EMixtureType.FourCard;
            tmixtureValue.mainCardClass = _list[4].cardClass; //상관없음
            tmixtureValue.mainRealValue = _list[4].realValue;
            _mixtureValue = tmixtureValue;
            return EMixtureType.FourCard;
        }

        _mixtureValue = tmixtureValue;
        return EMixtureType.None;
    }

    int CompareCardClass(CardClass _first, CardClass _second)
    {
        return ((int)_first - (int)_second) * -1;
    }

}

public struct TMixture
{
    public EMixtureType mixture; //조합
    public CardClass mainCardClass; //기준 무늬
    public int mainRealValue; //기준 가치

    public TMixture()
    {
        mixture = EMixtureType.None;
        mainCardClass = CardClass.Spade;
        mainRealValue = 0;
    }

    public TMixture(EMixtureType _mixtureType, CardClass _class, int _realValue)
    {
        mixture = _mixtureType;
        mainCardClass = _class;
        mainRealValue = _realValue;
    }
}