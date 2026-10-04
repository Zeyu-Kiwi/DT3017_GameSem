using UnityEngine;

// Marks runtime visuals so interaction highlights only duplicate the original item mesh.
[AddComponentMenu("")]
public sealed class ItemShineVisual : MonoBehaviour
{
    public ItemShineEffect Owner { get; set; }
}