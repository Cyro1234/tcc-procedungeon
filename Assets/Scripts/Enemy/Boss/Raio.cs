using UnityEngine;

public class Raio : MonoBehaviour
{

    [SerializeField] private float speed = 10f;

    private bool direcao = true;

    public void DefinirDirecao(bool novaDirecao)
    {
        direcao = novaDirecao;
    }

    private void FixedUpdate()
    {
        if (direcao)
        {
            transform.Rotate(0, 0, speed * Time.deltaTime);
        }
        else
        {
            transform.Rotate(0, 0, -speed * Time.deltaTime);
        }
    }
        


}
