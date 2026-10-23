using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Usa os assets originais do personagem, sem duplicar arte ou depender dos sprites antigos.
public static class TutorialCharacterSetup
{
    [MenuItem("Tools/Tutorial/Atualizar personagem do tutorial")]
    public static void Atualizar()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Pare o Play antes de atualizar o tutorial.");
        const string caminho = "Assets/Scenes/Tutorial.unity";
        Scene cena = SceneManager.GetSceneByPath(caminho);
        bool abriu = !cena.isLoaded;
        if (abriu) cena = EditorSceneManager.OpenScene(caminho, OpenSceneMode.Additive);
        try
        {
            Tutorial tutorial = cena.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Tutorial>(true)).Single();
            var dados = new SerializedObject(tutorial);
            Sprite[] frente = Quadros("FrenteMC");
            dados.FindProperty("alex").objectReferenceValue = frente[0];
            Definir(dados, "paradoFrente", frente);
            Definir(dados, "paradoLateral", Quadros("Lateral"));
            Definir(dados, "caminhada", Quadros("LateralWalk"));
            Definir(dados, "caminhadaEsquerda", Quadros("LateralWalkL"));
            dados.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena);
            Debug.Log("Tutorial atualizado com FrenteMC, Lateral, LateralWalk e LateralWalkL.");
        }
        finally { if (abriu) EditorSceneManager.CloseScene(cena, true); }
    }

    private static Sprite[] Quadros(string nome)
    {
        Sprite[] quadros = AssetDatabase.LoadAllAssetsAtPath("Assets/Animation/Player/" + nome + ".ase")
            .OfType<Sprite>().OrderBy(s => Numero(s.name)).ThenBy(s => s.name, StringComparer.Ordinal).ToArray();
        if (quadros.Length == 0) throw new InvalidOperationException("Animacao sem sprites: " + nome);
        return quadros;
    }

    private static int Numero(string nome)
    {
        Match numero = Regex.Match(nome, @"(\d+)$");
        return numero.Success ? int.Parse(numero.Value) : 0;
    }

    private static void Definir(SerializedObject objeto, string campo, Sprite[] quadros)
    {
        SerializedProperty array = objeto.FindProperty(campo);
        array.arraySize = quadros.Length;
        for (int i = 0; i < quadros.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = quadros[i];
    }
}
