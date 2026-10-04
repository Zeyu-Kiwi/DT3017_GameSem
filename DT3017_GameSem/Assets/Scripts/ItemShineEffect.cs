using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class ItemShineEffect : MonoBehaviour
{
    private static ItemShineSettings Settings => ItemShineSettings.Global;
    private bool constantScreenSize => Settings.constantScreenSize;
    private bool sheenEnabled => Settings.sheenEnabled;
    private Color sheenColor => Settings.sheenColor;
    private float sheenIntensity => Settings.sheenIntensity;
    private float sheenWidth => Settings.sheenWidth;
    private float sheenDuration => Settings.sheenDuration;
    private float sheenPause => Settings.sheenPause;
    private bool sparklesEnabled => Settings.sparklesEnabled;
    private Color sparkleColor => Settings.sparkleColor;
    private float sparkleIntensity => Settings.sparkleIntensity;
    private int sparkleCount => Settings.sparkleCount;
    private float sparkleWorldSize => Settings.sparkleWorldSize;
    private float sparkleSizePixels => Settings.sparkleSizePixels;
    private float sparkleLifetime => Settings.sparkleLifetime;
    private float sparkleRespawnDelay => Settings.sparkleRespawnDelay;
    private float sparklePadding => Settings.sparklePadding;
    private float pixelSize => Settings.pixelSize;

    private sealed class Copy
    {
        public Renderer source;
        public Renderer overlay;
        public MaterialPropertyBlock[] properties;
    }

    private readonly List<Copy> copies = new List<Copy>();
    private Material sheenMaterial, sparkleMaterial;
    private Mesh sparkleMesh;
    private MeshRenderer sparkleRenderer;
    private Vector3[] vertices, origins;
    private Vector2[] sparkleData;
    private float[] ages, lifetimes;
    private Bounds worldBounds;
    private float phaseOffset;
    private int builtCount;
    private float builtLifetime;

    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        Build();
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void Build()
    {
        var sheenTemplate = Resources.Load<Material>("ItemSheen");
        var sparkleTemplate = Resources.Load<Material>("ItemSparkles");
        if (sheenTemplate == null || sparkleTemplate == null)
        {
            Debug.LogWarning("Item shine materials are missing from Resources.", this);
            return;
        }
        sheenMaterial = new Material(sheenTemplate) { hideFlags = HideFlags.DontSave };
        sparkleMaterial = new Material(sparkleTemplate) { hideFlags = HideFlags.DontSave };
        phaseOffset = Random.value * (sheenDuration + sheenPause);

        // Collect the original renderers before creating any visual copies.
        foreach (var source in GetComponentsInChildren<Renderer>(true))
        {
            if (source.GetComponent<ItemShineVisual>() != null ||
                source.gameObject.name == "Interaction Outline") continue;
            Mesh mesh = null;
            if (source is SkinnedMeshRenderer skin) mesh = skin.sharedMesh;
            else if (source is MeshRenderer && source.TryGetComponent<MeshFilter>(out var filter))
                mesh = filter.sharedMesh;
            if (mesh == null || mesh.subMeshCount == 0) continue;

            var host = new GameObject("Item Sheen");
            host.hideFlags = HideFlags.DontSave;
            host.layer = source.gameObject.layer;
            host.transform.SetParent(source.transform, false);
            host.AddComponent<ItemShineVisual>().Owner = this;
            Renderer overlay;
            if (source is SkinnedMeshRenderer originalSkin)
            {
                var duplicate = host.AddComponent<SkinnedMeshRenderer>();
                duplicate.sharedMesh = mesh;
                duplicate.bones = originalSkin.bones;
                duplicate.rootBone = originalSkin.rootBone;
                duplicate.localBounds = originalSkin.localBounds;
                duplicate.quality = originalSkin.quality;
                duplicate.updateWhenOffscreen = originalSkin.updateWhenOffscreen;
                overlay = duplicate;
            }
            else
            {
                host.AddComponent<MeshFilter>().sharedMesh = mesh;
                overlay = host.AddComponent<MeshRenderer>();
            }
            var materials = new Material[mesh.subMeshCount];
            var properties = new MaterialPropertyBlock[mesh.subMeshCount];
            var originals = source.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = sheenMaterial;
                var block = properties[i] = new MaterialPropertyBlock();
                Material original = originals.Length > 0 ? originals[Mathf.Min(i, originals.Length - 1)] : null;
                string textureProperty = original == null ? null :
                    original.HasProperty("_BaseMap") ? "_BaseMap" :
                    original.HasProperty("_Albedo") ? "_Albedo" :
                    original.HasProperty("_MainTex") ? "_MainTex" : null;
                Texture texture = textureProperty != null ? original.GetTexture(textureProperty) : null;
                block.SetTexture("_BaseMap", texture != null ? texture : Texture2D.whiteTexture);
                Vector2 scale = textureProperty != null ? original.GetTextureScale(textureProperty) : Vector2.one;
                Vector2 offset = textureProperty != null ? original.GetTextureOffset(textureProperty) : Vector2.zero;
                block.SetVector("_BaseMap_STCopy", new Vector4(scale.x, scale.y, offset.x, offset.y));
                float cutoff = original != null && original.HasProperty("_AlphaClip") &&
                    original.GetFloat("_AlphaClip") > .5f && original.HasProperty("_Cutoff")
                    ? original.GetFloat("_Cutoff") : 0f;
                block.SetFloat("_Cutoff", cutoff);
            }
            overlay.sharedMaterials = materials;
            ConfigureRenderer(overlay);
            overlay.renderingLayerMask = source.renderingLayerMask;
            copies.Add(new Copy { source = source, overlay = overlay, properties = properties });
        }

        var sparkles = new GameObject("Item Sparkles", typeof(MeshFilter), typeof(MeshRenderer));
        sparkles.hideFlags = HideFlags.DontSave;
        sparkles.layer = gameObject.layer;
        sparkles.transform.SetParent(transform, false);
        sparkles.AddComponent<ItemShineVisual>().Owner = this;
        sparkleRenderer = sparkles.GetComponent<MeshRenderer>();
        sparkleRenderer.sharedMaterial = sparkleMaterial;
        ConfigureRenderer(sparkleRenderer);
        sparkleMesh = new Mesh { name = "Item Sparkle Quads", hideFlags = HideFlags.DontSave };
        sparkleMesh.MarkDynamic();
        sparkles.GetComponent<MeshFilter>().sharedMesh = sparkleMesh;
        UpdateBounds();
        BuildSparkles();
    }

    private static void ConfigureRenderer(Renderer renderer)
    {
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
    }

    private bool UpdateBounds()
    {
        bool found = false;
        foreach (var copy in copies)
        {
            if (copy.source == null || !copy.source.enabled || !copy.source.gameObject.activeInHierarchy) continue;
            if (!found) { worldBounds = copy.source.bounds; found = true; }
            else worldBounds.Encapsulate(copy.source.bounds);
        }
        return found;
    }

    private void BuildSparkles()
    {
        builtCount = Mathf.Clamp(sparkleCount, 0, 64);
        builtLifetime = Mathf.Max(.1f, sparkleLifetime);
        vertices = new Vector3[builtCount * 4];
        origins = new Vector3[builtCount];
        ages = new float[builtCount];
        lifetimes = new float[builtCount];
        sparkleData = new Vector2[builtCount * 4];
        var uv = new Vector2[builtCount * 4];
        var triangles = new int[builtCount * 6];
        for (int i = 0; i < builtCount; i++)
        {
            SpawnSparkle(i);
            ages[i] = Random.value * (lifetimes[i] + Mathf.Max(0f, sparkleRespawnDelay));
            int v = i * 4, t = i * 6;
            uv[v] = Vector2.zero; uv[v + 1] = Vector2.right;
            uv[v + 2] = Vector2.one; uv[v + 3] = Vector2.up;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        sparkleMesh.Clear();
        sparkleMesh.vertices = vertices;
        sparkleMesh.uv = uv;
        sparkleMesh.triangles = triangles;
    }

    private void SpawnSparkle(int i)
    {
        // Store a normalized position so sparkles follow moving and rotating items.
        origins[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        // Place the center on the outside of the bounds, rather than inside the mesh.
        float edge = Mathf.Max(Mathf.Abs(origins[i].x), Mathf.Abs(origins[i].y), Mathf.Abs(origins[i].z));
        origins[i] = edge > .0001f ? origins[i] / edge : Vector3.up;
        ages[i] = 0f;
        lifetimes[i] = Mathf.Max(.1f, sparkleLifetime) * Random.Range(.7f, 1.3f);
    }

    private void LateUpdate()
    {
        if (sheenMaterial == null) return;
        bool visible = UpdateBounds();
        foreach (var copy in copies)
        {
            if (copy.overlay == null) continue;
            copy.overlay.enabled = Settings.effectsEnabled && sheenEnabled && copy.source != null && copy.source.enabled &&
                copy.source.gameObject.activeInHierarchy;
            if (copy.source is SkinnedMeshRenderer source && copy.overlay is SkinnedMeshRenderer overlay)
                for (int i = 0; i < source.sharedMesh.blendShapeCount; i++)
                    overlay.SetBlendShapeWeight(i, source.GetBlendShapeWeight(i));
        }
        sparkleRenderer.enabled = Settings.effectsEnabled && sparklesEnabled && visible && sparkleCount > 0;
        if (builtCount != Mathf.Clamp(sparkleCount, 0, 64) ||
            !Mathf.Approximately(builtLifetime, Mathf.Max(.1f, sparkleLifetime))) BuildSparkles();
        if (sparklesEnabled && visible)
        {
            for (int i = 0; i < builtCount; i++)
            {
                ages[i] += Time.deltaTime;
                if (ages[i] >= lifetimes[i] + Mathf.Max(0f, sparkleRespawnDelay)) SpawnSparkle(i);
                float life = Mathf.Clamp01(ages[i] / lifetimes[i]);
                float pulse = Mathf.Max(0f, Mathf.Sin(life * Mathf.PI));
                Vector3 position = worldBounds.center + Vector3.Scale(origins[i],
                    worldBounds.extents + Vector3.one * sparklePadding);
                position.y += life * sparklePadding;
                position = transform.InverseTransformPoint(position);
                for (int j = 0; j < 4; j++)
                {
                    vertices[i * 4 + j] = position;
                    sparkleData[i * 4 + j] = new Vector2(pulse, sparkleSizePixels * pulse);
                }
            }
            sparkleMesh.vertices = vertices;
            sparkleMesh.uv2 = sparkleData;
            sparkleMesh.RecalculateBounds();
            // Pixel-sized stars can extend beyond their center points, especially at a distance.
            Bounds bounds = sparkleMesh.bounds; bounds.Expand(1f); sparkleMesh.bounds = bounds;
        }
        sheenMaterial.SetColor("_EffectColor", sheenColor);
        sheenMaterial.SetFloat("_Intensity", sheenIntensity);
        sheenMaterial.SetFloat("_Width", sheenWidth);
        sheenMaterial.SetFloat("_PixelSize", pixelSize);
        float duration = Mathf.Max(.1f, sheenDuration);
        float phase = Mathf.Repeat(Time.time + phaseOffset, duration + Mathf.Max(0f, sheenPause));
        sheenMaterial.SetFloat("_Progress", phase < duration ? Mathf.Lerp(-.3f, 2.3f, phase / duration) : -1f);
        sparkleMaterial.SetColor("_EffectColor", sparkleColor);
        sparkleMaterial.SetFloat("_Intensity", sparkleIntensity);
        sparkleMaterial.SetFloat("_PixelSize", pixelSize);
        sparkleMaterial.SetFloat("_ConstantScreenSize", constantScreenSize ? 1f : 0f);
        sparkleMaterial.SetFloat("_WorldSize", Mathf.Max(.001f, sparkleWorldSize));
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (isActiveAndEnabled && sheenMaterial != null) PrepareForCamera(camera);
    }

    public void PrepareForCamera(Camera camera)
    {
        if (camera == null || copies.Count == 0) return;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = worldBounds.center + Vector3.Scale(worldBounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 screen = camera.WorldToViewportPoint(corner);
            if (screen.z <= 0f) continue;
            min = Vector2.Min(min, screen); max = Vector2.Max(max, screen);
        }
        if (min.x == float.MaxValue) return;
        Vector4 bounds = new Vector4(min.x, min.y, Mathf.Max(.0001f, max.x - min.x), Mathf.Max(.0001f, max.y - min.y));
        foreach (var copy in copies)
        {
            if (copy.overlay == null) continue;
            for (int i = 0; i < copy.properties.Length; i++)
            {
                copy.properties[i].SetVector("_ScreenBounds", bounds);
                copy.overlay.SetPropertyBlock(copy.properties[i], i);
            }
        }
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        foreach (var copy in copies)
            if (copy.overlay != null) { copy.overlay.gameObject.SetActive(false); Destroy(copy.overlay.gameObject); }
        copies.Clear();
        if (sparkleRenderer != null) { sparkleRenderer.gameObject.SetActive(false); Destroy(sparkleRenderer.gameObject); }
        if (sparkleMesh != null) Destroy(sparkleMesh);
        if (sheenMaterial != null) Destroy(sheenMaterial);
        if (sparkleMaterial != null) Destroy(sparkleMaterial);
    }
}
