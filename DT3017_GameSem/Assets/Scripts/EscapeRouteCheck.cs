using System;
using UnityEngine;

/// <summary>Marks an interactable as an escape route that can be inspected.</summary>
[DisallowMultipleComponent]
public class EscapeRouteCheck : MonoBehaviour
{
    public event Action<EscapeRouteCheck> Checked;

    // Also callable from UnityEvents for routes with custom interaction logic.
    public void MarkChecked()
    {
        Checked?.Invoke(this);
    }
}