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

public class CardData : Comparer<CardData>
{
    public static CardClass minClass = CardClass.Spade;
    public static int minNum = 3;
    public CardClass cardClass = CardClass.Spade;
    public int num;


    public CardData(CardClass _cardClass, int _num)
    {
        cardClass = _cardClass;
        num = _num;
    }

    public int Compare(CardClass _class, int _num)
    {
        if (num == _num)
        {
            //무늬가 강한것부터 0으로 시작되어있어서, 뺏을 때의 값에 역순을 곱해서 크기 표현
            return ((int)cardClass - (int)_class) * -1;
        }

        return num - _num;
    }

    public int Compare(CardData _card)
    {
        if (num == _card.num)
        {
            //무늬가 강한것부터 0으로 시작되어있어서, 뺏을 때의 값에 역순을 곱해서 크기 표현
            return ((int)cardClass - (int)_card.cardClass) * -1;
        }

        return num - _card.num;
    }

    public override int Compare(CardData? x, CardData? y)
    {
        if(x.num == y.num)
        {
            //무늬가 강한것부터 0으로 시작되어있어서, 뺏을 때의 값에 역순을 곱해서 크기 표현
            return ((int)x.cardClass - (int)y.cardClass) * -1;
        }

        return x.num - y.num;
    }
}

