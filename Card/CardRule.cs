using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum EMixtureType
{
    None, Pass, OneCard, OnePair, Triple, Straight, Flush, FullHouse, FourCard, StraightFlush, RoyalStriaghtFlush
}

public class CardRule
{
    public bool IsVarid(List<CardData> _list, out TMixture _mixtureValue)
    {
        EMixtureType mixture = CheckValidRule(_list, out _mixtureValue);
        return mixture != EMixtureType.None;
    }

    public EMixtureType CheckValidRule(List<CardData> _list, out TMixture _mixtureValue)
    {
        int cardCount = _list.Count;
        TMixture mixtureValue = new TMixture();
        if(cardCount == 0)
        {
            mixtureValue.mixture = EMixtureType.Pass;
            _mixtureValue = mixtureValue;
            return EMixtureType.Pass;
        }
        if (cardCount == 1)
        {
            mixtureValue.mixture = EMixtureType.OneCard;
            mixtureValue.mainRealValue = _list[0].realValue;
            mixtureValue.mainCardClass = _list[0].cardClass;
            mixtureValue.cardCount = 1;
            _mixtureValue = mixtureValue;
            return EMixtureType.OneCard;
        }
        else if (cardCount == 2)
        {
            if (IsTwoPair(_list, out _mixtureValue))
            {
                return EMixtureType.OnePair;
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
            mixtureValue.mixture = EMixtureType.OnePair;
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
            mixtureValue.cardCount = 2;
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
            mixtureValue.cardCount = 3;
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
        tmixtureValue.cardCount = 5;
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
            tmixtureValue.mixture = EMixtureType.Flush;
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
            tmixtureValue.mixture = EMixtureType.FullHouse;
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

    public bool TryCompare(TMixture _oneValue, TMixture _twoValue, out int _compareValue)
    {
        //사용된 카드 수가 다르면 - 조합 수치가 다르면 비교 불가
        if (_oneValue.cardCount != _twoValue.cardCount)
        {
            _compareValue = 0;
            return false;
        }

        _compareValue = TMixture.Compare(_oneValue, _twoValue);
        return true;
    }

    public static int CompareCardClass(CardClass _first, CardClass _second)
    {
        return ((int)_first - (int)_second) * -1;
    }

}

public struct TMixture
{
    public EMixtureType mixture; //조합
    public CardClass mainCardClass; //기준 무늬
    public int mainRealValue; //기준 가치
    public int cardCount; //사용에 쓰인 카드 수 

    public TMixture()
    {
        mixture = EMixtureType.None;
        mainCardClass = CardClass.Spade;
        mainRealValue = 0;
        cardCount = 0;
    }

    public static int Compare(TMixture _one, TMixture _two)
    {
        //비교 가능한 것들만 들어올것

        EMixtureType oneType = _one.mixture;
        EMixtureType twoType = _two.mixture;

        if (oneType != twoType)
        {
            //두타입이 다르면 5종 끼리의 조합
            //enum에 작은것부터 명시 해놔서 enum값 뺀거 반환하면됨.
            return (int)_one.mixture - (int)_two.mixture;
        }

        int compareValue = 0;
        //여기부턴 같은거끼리의 비교
        switch (oneType)
        {
            case EMixtureType.OneCard:
                //가치 비교
                compareValue = _one.mainRealValue - _two.mainRealValue;
                if (compareValue == 0)
                {
                    //무늬 비교
                    compareValue = CardRule.CompareCardClass(_one.mainCardClass, _two.mainCardClass);
                }
                return compareValue;
            case EMixtureType.OnePair:
                //가치 비교
                compareValue = _one.mainRealValue - _two.mainRealValue;
                if (compareValue == 0)
                {
                    //무늬 비교
                    compareValue = CardRule.CompareCardClass(_one.mainCardClass, _two.mainCardClass);
                }
                return compareValue;
            case EMixtureType.Triple:
                //가치 비교, 값이같은경우는 있을수 없음. 
                return _one.mainRealValue - _two.mainRealValue;
            case EMixtureType.Straight:
                //가치 비교
                compareValue = _one.mainRealValue - _two.mainRealValue;
                if (compareValue == 0)
                {
                    //숫자가 같으면 큰 무늬
                    compareValue = CardRule.CompareCardClass(_one.mainCardClass, _two.mainCardClass);
                }
                return compareValue;
            case EMixtureType.Flush:
                //무늬 비교
                compareValue = CardRule.CompareCardClass(_one.mainCardClass, _two.mainCardClass);
                if (compareValue == 0)
                {
                    //무늬 같으면 숫자 비교
                    compareValue = _one.mainRealValue - _two.mainRealValue;
                }
                return compareValue;
            case EMixtureType.FullHouse:
                //트리플의 가치라서 값만 비교
                compareValue = _one.mainRealValue - _two.mainRealValue;
                return compareValue;
            case EMixtureType.FourCard:
                //네장의 가치만 비교
                compareValue = _one.mainRealValue - _two.mainRealValue;
                return compareValue;
            case EMixtureType.StraightFlush:
                //무늬 비교
                compareValue = CardRule.CompareCardClass(_one.mainCardClass, _two.mainCardClass);
                if (compareValue == 0)
                {
                    //무늬 같으면 숫자 비교
                    compareValue = _one.mainRealValue - _two.mainRealValue;
                }
                return compareValue;
        }
        return 0;
    }
}