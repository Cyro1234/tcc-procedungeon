using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedTable<T> 
{
    public List<WeightedItem<T>> items = new List<WeightedItem<T>>();

    public bool TryValidate(out string error)
    {
        return TryGetTotalWeight(out _, out error);
    }

    private bool TryGetTotalWeight(out int totalWeight, out string error)
    {
        totalWeight = 0;
        if (items == null || items.Count == 0)
        {
            error = "A tabela deve conter pelo menos uma entrada.";
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            if (entry == null)
            {
                error = $"A entrada {i} esta nula.";
                return false;
            }
            if (entry.weight < 0)
            {
                error = $"A entrada {i} tem peso negativo. Use zero para desativar uma entrada.";
                return false;
            }
            if (entry.weight == 0) continue;
            if (entry.item is null || (entry.item is Object obj && obj == null))
            {
                error = $"A entrada {i} tem peso positivo, mas nenhum item atribuido.";
                return false;
            }
            if (entry.weight > int.MaxValue - totalWeight)
            {
                error = $"A soma dos pesos excede {int.MaxValue}. Reduza os pesos mantendo suas proporcoes.";
                return false;
            }
            totalWeight += entry.weight;
        }

        if (totalWeight == 0)
        {
            error = "A tabela deve conter pelo menos uma entrada com peso maior que zero.";
            return false;
        }

        error = null;
        return true;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void ValidateInEditor(WeightedTable<T> table, Object context, string fieldName)
    {
        string error = "A tabela nao foi atribuida.";
        if (table == null || !table.TryValidate(out error))
        {
            Debug.LogError($"{context.name}.{fieldName}: {error}", context);
        }
    }

    public T getRandom(System.Random rng)
    {
        if (rng == null) throw new System.ArgumentNullException(nameof(rng));
        if (!TryGetTotalWeight(out int totalWeight, out string error))
            throw new System.InvalidOperationException($"WeightedTable<{typeof(T).Name}>: {error}");

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

        throw new System.InvalidOperationException("A tabela foi alterada durante o sorteio.");
    }
}
