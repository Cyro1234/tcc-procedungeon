using UnityEngine;
using Unity.Cinemachine;

// O jogo usa Cinemachine (CinemachineBrain na Main Camera + um CinemachineCamera
// que segue o player), entao debuffs de camera nao devem mexer na Transform/Camera
// diretamente - isso seria sobrescrito todo frame pelo Brain (mexer direto no
// orthographicSize da Camera tambem brigava com o confiner de sala). Em vez disso
// este componente fica no mesmo objeto do CinemachineCamera e escreve nos campos
// da Lens (o que o Brain de fato le): OrthographicSize pra zoom e Dutch (roll)
// pra rotacao continua, tipo labirintite - nenhum dos dois toca a posicao que o
// Cinemachine controla.
[RequireComponent(typeof(CinemachineCamera))]
public class CameraEffectsController : MonoBehaviour
{
    public static CameraEffectsController Instance { get; private set; }

    public readonly ModifierPipeline<ICameraModifier> CameraModifiers = new ModifierPipeline<ICameraModifier>();

    private CinemachineCamera cineCam;
    private float baseOrthographicSize;
    private float baseDutch;

    private void Awake()
    {
        Instance = this;
        cineCam = GetComponent<CinemachineCamera>();
    }

    private void Start()
    {        
        baseDutch = cineCam.Lens.Dutch;
    }
    
    private void Update()
    {
        baseOrthographicSize = cineCam.Lens.OrthographicSize;

        var ctx = new CameraModifierContext
        {
            rotation = Quaternion.Euler(0f, 0f, baseDutch),
            orthographicSize = baseOrthographicSize
        };

        foreach (var modifier in CameraModifiers.Modifiers)
        {
            modifier.Modify(ref ctx, Time.deltaTime);
        }

        cineCam.Lens.OrthographicSize = ctx.orthographicSize;
        cineCam.Lens.Dutch = ctx.rotation.eulerAngles.z;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
