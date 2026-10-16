using System;
using UnityEngine;

[Serializable]
public class WeightedItem<T>
{
    public T item;
    [Min(0), Tooltip("Peso relativo no sorteio. Zero desativa esta entrada.")]
    public int weight;
}
