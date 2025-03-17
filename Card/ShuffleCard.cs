using System;
using System.Collections.Generic;


public class ShuffleCard
{
    public CardData[] Shuffle(CardData[] _cards)
    {
        int mixCount = 50;
        Random ran = new Random();
        for (int i = 0; i < _cards.Length; i++)
        {
           int ranNum = ran.Next() % _cards.Length;
            CardData ori = _cards[i];
            _cards[i] = _cards[ranNum];
            _cards[ranNum] = ori;
        }
        return _cards;
    }
}

