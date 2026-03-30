using UnityEngine;

public class HeartSystem : MonoBehaviour
{
    public GameObject[] hearts;
    int life = 3;

    void Update()
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < life)
                hearts[i].SetActive(true);
            else
                hearts[i].SetActive(false);
        }
    }

    public void takeDamage(int damage) 
    { 
        life -= damage;
        life = Mathf.Max(life, 0);
        Debug.Log("TOMOU DANO! LIFE: " + life + " - CONTAINERS: " + hearts.Length);
    }
}
