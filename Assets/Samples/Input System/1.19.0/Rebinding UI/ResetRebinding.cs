using UnityEngine;
using UnityEngine.InputSystem;

public class ResetAllButtons : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;
    public void ResetAllBindings()
    {
        foreach (InputActionMap map in _inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }
    }
}
