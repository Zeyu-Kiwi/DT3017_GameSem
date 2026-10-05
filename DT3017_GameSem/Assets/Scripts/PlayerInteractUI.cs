using TMPro;
using UnityEngine;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject containerGameObject;
    [Tooltip("Text that displays the currently selected interaction key.")]
    [SerializeField] private TMP_Text interactionKeyText;

    private void Awake()
    {
        FindKeyTextIfNeeded();
        Hide();
    }

    public void SetInteractionKey(KeyCode key)
    {
        FindKeyTextIfNeeded();

        if (interactionKeyText != null)
        {
            interactionKeyText.text = GetKeyDisplayName(key);
        }
    }

    public void Show()
    {
        containerGameObject.SetActive(true);
    }

    public void Hide()
    {
        containerGameObject.SetActive(false);
    }

    private void FindKeyTextIfNeeded()
    {
        if (interactionKeyText == null && containerGameObject != null)
        {
            interactionKeyText =
                containerGameObject.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private string GetKeyDisplayName(KeyCode key)
    {
        string keyName = key.ToString();

        if (keyName.StartsWith("Alpha"))
        {
            return keyName.Substring("Alpha".Length);
        }

        switch (key)
        {
            case KeyCode.Return:
                return "Enter";

            case KeyCode.KeypadEnter:
                return "Numpad Enter";

            case KeyCode.Space:
                return "Space";

            default:
                return keyName;
        }
    }
}
