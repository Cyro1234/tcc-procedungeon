using UnityEngine;

// Pega as informacoes de posicao da camera no frame para que os debuffs complexos possam alterar esses valores

// Posicao nao entra aqui, quem controla isso e o CinemachineFollow!!!!!!!!! Se quiser criar um modificador ou algo assim que altera dinamicamente na posicao da camera, faca isso no CinemachineFollow!!!!!!!!!
public struct CameraModifierContext
{
    public Quaternion rotation;
    public float orthographicSize;
}

public interface ICameraModifier
{
    void Modify(ref CameraModifierContext ctx, float dt);
}
