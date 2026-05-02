using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DebuffTester : MonoBehaviour
{
    [SerializeField] private PlayerStatsHandler playerStats;

    // Lista para visualizar no Inspetor quais debuffs foram encontrados
    private List<IDebuff> availableDebuffs = new List<IDebuff>();

    private void Start()
    {
        // Pega todos os scripts de debuff anexados a ESTE mesmo GameObject
        // Se os scripts estiverem em outros objetos, você precisará de outra lógica
        availableDebuffs = GetComponents<IDebuff>().ToList();

        Debug.Log($"[DebuffTester] {availableDebuffs.Count} debuffs carregados!");
    }

    private void Update()
    {
        // Verifica se alguma tecla foi pressionada neste frame
        if (!Input.anyKeyDown || string.IsNullOrEmpty(Input.inputString)) return;

        // Pega o primeiro caractere digitado (ex: "1", "2")
        char c = Input.inputString[0];

        // Se o caractere for um dígito de 1 a 9
        if (char.IsDigit(c) && c != '0')
        {
            // Converte o caractere para int (ex: '1' vira 1) e subtrai 1 para o índice da lista
            int index = (int)char.GetNumericValue(c) - 1;

            ApplyDebuffByIndex(index);
        }
    }

    private void ApplyDebuffByIndex(int index)
    {
        if (index < availableDebuffs.Count)
        {
            availableDebuffs[index].Apply(playerStats);
            Debug.Log($"Aplicando: {availableDebuffs[index].GetType().Name}");
        }
    }
}