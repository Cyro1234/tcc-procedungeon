using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private int BaixoNivel = 1;
    [SerializeField] private int MedioNivel = 2;

    private int andar = 0; // Andar que o jogador esta presente

    private List<TileMapVisualizer.Biomas> listaBiomas = null;
    private TileMapVisualizer.Biomas biomaAtual = TileMapVisualizer.Biomas.Infinito;

    public TileMapVisualizer.Biomas GetBiomaAtual() { return biomaAtual; }

    public void setup()
    {
        listaBiomas = new List<TileMapVisualizer.Biomas>();
        listaBiomas.Add(TileMapVisualizer.Biomas.Floresta);
        listaBiomas.Add(TileMapVisualizer.Biomas.Deserto);
        listaBiomas.Add(TileMapVisualizer.Biomas.Caverna);
        listaBiomas.Add(TileMapVisualizer.Biomas.Abismo);
        Shuffle(listaBiomas);
    }

    private void Shuffle<T>(IList<T> ts) // Shuffle na lista
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = Rng.DungeonRange(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public int GetAndar() { return this.andar; }
    public void CleanAndar() {  this.andar = 0; }

    public TileMapVisualizer.Biomas GetBioma()
    {
        if (listaBiomas.Count > 0)
        {
            TileMapVisualizer.Biomas biomaSelecionado = listaBiomas[0]; // Ja esta embaralhado
            listaBiomas.RemoveAt(0); // Remove pra nao repitir o bioma
            biomaAtual = biomaSelecionado;
            return biomaSelecionado;
        }
        return TileMapVisualizer.Biomas.Infinito;
    }

    public void passarAndar()
    {
        andar++;
    }


    public TileMapVisualizer.Niveis GetNivelAtual()
    {
        if (andar <= BaixoNivel)
        {
            return TileMapVisualizer.Niveis.Baixo;
        }
        else if (andar <= MedioNivel)
        {
            return TileMapVisualizer.Niveis.Medio;
        }
        else
        {
            return TileMapVisualizer.Niveis.Alto;
        }
    }
}
