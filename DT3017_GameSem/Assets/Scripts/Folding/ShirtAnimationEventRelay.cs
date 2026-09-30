using UnityEngine;

[DisallowMultipleComponent]
public class ShirtAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private ShirtFoldingController foldingController;

    private void Awake()
    {
        if (foldingController == null)
        {
            foldingController = GetComponentInParent<ShirtFoldingController>();
        }
    }

    // Select this function in the Animation Event on each fold clip.
    public void OnFoldAnimationFinished()
    {
        if (foldingController != null)
        {
            foldingController.OnFoldAnimationFinished();
        }
    }
}
