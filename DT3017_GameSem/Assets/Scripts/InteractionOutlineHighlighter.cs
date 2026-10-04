using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class InteractionOutlineHighlighter : MonoBehaviour
{
    [SerializeField] private bool outlineEnabled = true;
    [SerializeField, ColorUsage(true, true)] private Color outlineColor = new Color(1f, .75f, .15f, 1f);
    [SerializeField, Range(0f, 12f)] private float outlineWidthPixels = 3f;
    [Tooltip("Extra HDR brightness. Zero preserves the normal outline; higher values can glow when camera HDR and Bloom are enabled.")]
    [SerializeField, Min(0f)] private float outlineEmissionIntensity = 3f;
    [Tooltip("Optional replacement for the default Resources/InteractionOutline material.")]
    [SerializeField] private Material outlineMaterial;

    private sealed class Copy
    {
        public Renderer source;
        public Renderer outline;
    }

    private readonly List<Copy> copies = new List<Copy>();
    private readonly Dictionary<Mesh, Mesh> meshes = new Dictionary<Mesh, Mesh>();
    private Component target;
    private Material runtimeMaterial;

    public Component Target => target;

    public void SetTarget(Component nextTarget)
    {
        if (!isActiveAndEnabled) { Clear(); return; }
        if (target == nextTarget) return;
        Clear();
        target = nextTarget;
        if (target == null) return;
        if (runtimeMaterial == null)
        {
            var template = outlineMaterial != null ? outlineMaterial : Resources.Load<Material>("InteractionOutline");
            if (template == null)
            {
                Debug.LogWarning("Interaction outline material is missing.", this);
                return;
            }
            runtimeMaterial = new Material(template) { hideFlags = HideFlags.DontSave };
        }

        foreach (var source in target.GetComponentsInChildren<Renderer>())
        {
            Mesh mesh = null;
            if (source is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
            else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter)) mesh = filter.sharedMesh;
            if (mesh == null) continue;
            Mesh outlineMesh = GetOutlineMesh(mesh);
            var host = new GameObject("Interaction Outline", typeof(MeshFilter));
            host.hideFlags = HideFlags.DontSave;
            host.layer = source.gameObject.layer;
            host.transform.SetParent(source.transform, false);
            Renderer outline;
            if (source is SkinnedMeshRenderer originalSkin)
            {
                var copySkin = host.AddComponent<SkinnedMeshRenderer>();
                copySkin.sharedMesh = outlineMesh;
                copySkin.bones = originalSkin.bones;
                copySkin.rootBone = originalSkin.rootBone;
                copySkin.localBounds = originalSkin.localBounds;
                copySkin.quality = originalSkin.quality;
                copySkin.updateWhenOffscreen = originalSkin.updateWhenOffscreen;
                outline = copySkin;
            }
            else
            {
                host.GetComponent<MeshFilter>().sharedMesh = outlineMesh;
                outline = host.AddComponent<MeshRenderer>();
            }
            var materials = new Material[outlineMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = runtimeMaterial;
            outline.sharedMaterials = materials;
            outline.shadowCastingMode = ShadowCastingMode.Off;
            outline.receiveShadows = false;
            outline.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outline.renderingLayerMask = source.renderingLayerMask;
            copies.Add(new Copy { source = source, outline = outline });
        }
        UpdateCopies();
    }

    private Mesh GetOutlineMesh(Mesh source)
    {
        // Imported meshes without Read/Write still render using their original normals.
        if (!source.isReadable) return source;
        if (meshes.TryGetValue(source, out var cached)) return cached;
        var mesh = Instantiate(source);
        mesh.name = source.name + " (Interaction Outline)";
        mesh.hideFlags = HideFlags.DontSave;
        if (mesh.normals.Length != mesh.vertexCount) mesh.RecalculateNormals();
        var vertices = mesh.vertices;
        var normals = mesh.normals;
        var sums = new Dictionary<Vector3, Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        {
            sums.TryGetValue(vertices[i], out var sum);
            sums[vertices[i]] = sum + normals[i];
        }
        var smooth = new List<Vector3>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++) smooth.Add(sums[vertices[i]].normalized);
        // Store smoothed normals only on the duplicate mesh; keep the original model untouched.
        mesh.normals = smooth.ToArray();
        meshes.Add(source, mesh);
        return mesh;
    }

    private void LateUpdate()
    {
        if (target == null && copies.Count > 0) Clear();
        UpdateCopies();
    }

    private void UpdateCopies()
    {
        if (runtimeMaterial != null)
        {
            Color color = target is ItemPickup pickup && pickup.UseCustomOutlineColor
                ? pickup.OutlineColor
                : outlineColor;
            float emission = target is ItemPickup emissionPickup && emissionPickup.UseCustomOutlineEmission
                ? emissionPickup.OutlineEmissionIntensity
                : Mathf.Max(0f, outlineEmissionIntensity);
            runtimeMaterial.SetFloat("_OutlineEmissionIntensity", emission);
            runtimeMaterial.SetColor("_OutlineColor", color);
            runtimeMaterial.SetFloat("_OutlineWidth", outlineWidthPixels);
        }
        foreach (var copy in copies)
        {
            if (copy.outline == null) continue;
            copy.outline.enabled = outlineEnabled && copy.source != null && copy.source.enabled && copy.source.gameObject.activeInHierarchy;
            if (copy.source is SkinnedMeshRenderer sourceSkin && copy.outline is SkinnedMeshRenderer outlineSkin)
            {
                for (int i = 0; i < sourceSkin.sharedMesh.blendShapeCount; i++)
                    outlineSkin.SetBlendShapeWeight(i, sourceSkin.GetBlendShapeWeight(i));
            }
        }
    }

    public void Clear()
    {
        target = null;
        foreach (var copy in copies)
        {
            if (copy.outline == null) continue;
            copy.outline.gameObject.SetActive(false);
            Destroy(copy.outline.gameObject);
        }
        copies.Clear();
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        Clear();
        foreach (var mesh in meshes.Values) if (mesh != null) Destroy(mesh);
        if (runtimeMaterial != null) Destroy(runtimeMaterial);
    }
}
