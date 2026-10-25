#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public static class DevItemSpawner
{
    public static bool Spawn(DevItemCatalog.Entry entry, bool onGround, FirstPersonController player, out string message)
    {
        if (entry == null || entry.item == null) { message = "No item selected."; return false; }
        var inventory = InventoryManager.Instance;
        if (inventory != null && !inventory.RegisterDevelopmentItem(entry.item))
        { message = "Another item already uses this item's ID."; return false; }
        if (!onGround)
        {
            if (inventory == null) { message = "No inventory in this scene."; return false; }
            int added = inventory.AddItem(entry.item, 1);
            message = added > 0 ? "Added 1 " + entry.item.DisplayName + " to inventory." : entry.item.DisplayName + " is full.";
            return added > 0;
        }
        if (player == null) { message = "No player in this scene."; return false; }
        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 origin = player.transform.position + Vector3.up * 0.7f;
        float distance = 1.5f;
        foreach (var hit in Physics.SphereCastAll(origin, 0.2f, forward, distance, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(player.transform) && hit.normal.y < 0.6f)
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.25f));
        Vector3 target = player.transform.position + forward * distance;
        var hits = Physics.RaycastAll(target + Vector3.up * 2f, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        bool found = false;
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(player.transform) || hit.normal.y < 0.5f || hit.collider.GetComponentInParent<ItemPickup>() != null) continue;
            target = hit.point;
            found = true;
            break;
        }
        if (!found) { message = "No ground found in front of the player."; return false; }
        var root = new GameObject("Dev Pickup - " + entry.item.DisplayName);
        root.SetActive(false);
        var visuals = new GameObject("Visuals");
        visuals.transform.SetParent(root.transform, false);
        bool copied = false;
        if (entry.worldModel != null)
        {
            foreach (var source in entry.worldModel.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = source.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var mesh = new GameObject(source.name, typeof(MeshFilter), typeof(MeshRenderer));
                mesh.transform.SetParent(visuals.transform, false);
                Matrix4x4 matrix = entry.worldModel.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                mesh.transform.localPosition = matrix.GetColumn(3);
                mesh.transform.localRotation = matrix.rotation;
                mesh.transform.localScale = matrix.lossyScale;
                mesh.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                mesh.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
                copied = true;
            }
        }
        if (!copied)
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = entry.item.DisplayName;
            fallback.transform.SetParent(visuals.transform, false);
            var collider = fallback.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
        }
        // Normalize imported models without copying crafting logic or prefab colliders.
        var renderers = visuals.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float longest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        float scale = longest > 0.000001f ? 0.3f / longest : 1f;
        visuals.transform.localScale *= scale;
        visuals.transform.localPosition = -bounds.center * scale;
        bounds = renderers[0].bounds;
        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        var box = root.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = new Vector3(Mathf.Max(bounds.size.x,0.04f),Mathf.Max(bounds.size.y,0.04f),Mathf.Max(bounds.size.z,0.04f));
        root.transform.position = target + Vector3.up * (box.size.y * 0.5f + 0.04f);
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer < 0) layer = 3;
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        var body = root.AddComponent<Rigidbody>();
        body.mass = 0.2f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        root.AddComponent<ItemPickup>().Configure(entry.item, 1);
        root.SetActive(true);
        message = "Spawned 1 " + entry.item.DisplayName + " on the ground.";
        return true;
    }
}
#endif