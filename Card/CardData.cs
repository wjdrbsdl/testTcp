using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public enum CardClass
{
    Spade, Dia, Heart, Clover
}

public class CardData : IComparable<CardData>
{
    public static CardClass minClass = CardClass.Spade;
    public static int minRealValue = 3;
    public CardClass cardClass = CardClass.Spade;
    public int num;
    public int realValue;


    public CardData(CardClass _cardClass, int _num)
    {
        cardClass = _cardClass;
        num = _num;
        realValue = num;
        if(num == 1 || num == 2)
        {
            realValue += 13;
        }
    }

    public int Compare(CardClass _class, int _realValue)
    {
        if (realValue == _realValue)
        {
            //무늬가 강한것부터 0으로 시작되어있어서, 뺏을 때의 값에 역순을 곱해서 크기 표현
            return ((int)cardClass - (int)_class) * -1;
        }

        return realValue - _realValue;
    }

    public int CompareTo(CardData _card)
    {
        if (_card == null)
            return 0;

        if (realValue == _card.realValue)
        {
            //무늬가 강한것부터 0으로 시작되어있어서, 뺏을 때의 값에 역순을 곱해서 크기 표현
            return ((int)cardClass - (int)_card.cardClass) * -1;
        }

        return realValue - _card.realValue;
    }

  }

