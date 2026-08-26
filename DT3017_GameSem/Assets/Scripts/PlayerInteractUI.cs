using UnityEngine;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject containerGameObject;

    private bool isInteractable;

    private void Awake()
    {
        SetInteractable(false);
    }

    public void SetInteractable(bool value)
    {
        isInteractable = value;
        containerGameObject.SetActive(isInteractable);
    }
}
