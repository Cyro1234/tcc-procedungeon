using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider healthSlider;
    public Slider easeHealthSlider;
    private float lerpSpeed = 0.05f;

    private void Awake()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>();
        easeHealthSlider = sliders[0];
        healthSlider = sliders[1];
    }

    public void SetMaxHealth(float maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        easeHealthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
        easeHealthSlider.value = maxHealth;
    }

    public void SetHealth(float currentHealth)
    {
        healthSlider.value = currentHealth;
    }

    private void FixedUpdate()
    {
        if (healthSlider.value != easeHealthSlider.value)
        {
            easeHealthSlider.value = Mathf.Lerp(easeHealthSlider.value, healthSlider.value, lerpSpeed);
        }
    }
}