using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

[System.Serializable]
public class WeightedTable<T> 
{
    public List<WeightedItem<T>> items;

    public T getRandom(System.Random rng)
    {
        // Calcula peso total
        int totalWeight = 0;

        foreach (WeightedItem<T> item in items)
        {
            totalWeight += item.weight;
        }

        // Calcula o item a ser pego
        int roll = rng.Next(0, totalWeight);

        foreach(WeightedItem<T> item in items)
        {
            if (roll < item.weight)
            {
                return item.item;
            }
            roll -= item.weight;
        }

        return items[0].item; // por seguranca
    }
}
