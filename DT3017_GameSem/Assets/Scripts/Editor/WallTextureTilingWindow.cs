using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.ProBuilder;

public enum WallUVProjection { AutomaticBoxFaces, WorldXY, WorldZY, WorldXZ }

public sealed class WallTextureTilingWindow : EditorWindow
{
    private const string Folder = "Assets/Generated/WallTextureTiling";
    private const string DataPath = Folder + "/WallTextureTilingData.asset";
    [SerializeField] private float metresPerTile = 1f;
    [SerializeField] private Vector3 alignmentOrigin;
    [SerializeField] private WallUVProjection projection;
    [SerializeField] private bool includeChildren;
    [SerializeField] private bool liveUpdate;
    private WallTextureTilingData data;
    private readonly Dictionary<MeshFilter, Matrix4x4> watched = new Dictionary<MeshFilter, Matrix4x4>();
    private string status = "Select your wall pieces, choose a texture size, then click Apply.";
    private double nextUpdate;

    [MenuItem("Tools/Environment/Wall Texture Tiling")]
    public static void Open() => GetWindow<WallTextureTilingWindow>("Wall Texture Tiling");

    private void OnEnable()
    {
        minSize = new Vector2(350f, 340f);
        EditorApplication.update += UpdateSelected;
        Selection.selectionChanged += SelectionChanged;
        Undo.undoRedoPerformed += AfterUndoRedo;
        SceneView.duringSceneGui += DuringSceneGUI;
        EditorSceneManager.sceneSaved += SceneSaved;
        SelectionChanged();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdateSelected;
        Selection.selectionChanged -= SelectionChanged;
        Undo.undoRedoPerformed -= AfterUndoRedo;
        SceneView.duringSceneGui -= DuringSceneGUI;
        EditorSceneManager.sceneSaved -= SceneSaved;
        Flush();
    }

    private static bool CanEdit => !EditorApplication.isPlayingOrWillChangePlaymode
        && PrefabStageUtility.GetCurrentPrefabStage() == null;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Consistent textures across wall pieces", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bakes world-aligned UVs into regular meshes or existing ProBuilder walls. Materials stay shared; no components or runtime scripts are added.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!CanEdit))
        {
            EditorGUI.BeginChangeCheck();
            metresPerTile = EditorGUILayout.FloatField(new GUIContent("Metres per texture tile", "World-space size of one repeat when material Tiling is (1, 1)."), metresPerTile);
            metresPerTile = Mathf.Max(.001f, float.IsNaN(metresPerTile) || float.IsInfinity(metresPerTile) ? 1f : metresPerTile);
            projection = (WallUVProjection)EditorGUILayout.EnumPopup("Projection", projection);
            alignmentOrigin = EditorGUILayout.Vector3Field("Shared alignment origin", alignmentOrigin);
            includeChildren = EditorGUILayout.Toggle("Include selected children", includeChildren);
            if (EditorGUI.EndChangeCheck()) SelectionChanged();
            liveUpdate = EditorGUILayout.Toggle(new GUIContent("Live update while scaling", "Updates already-applied selected pieces as you move, rotate or scale them. Keep this window open."), liveUpdate);
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply to Selected Pieces", GUILayout.Height(30f))) ApplySelection();
            if (GUILayout.Button("Restore Original Meshes")) RestoreSelection();
            EditorGUILayout.LabelField("Selected mesh objects: " + SelectedMeshes().Count());
        }
        if (!CanEdit) EditorGUILayout.HelpBox("Exit Play mode and Prefab mode to edit scene wall pieces.", MessageType.Warning);
        EditorGUILayout.HelpBox("Automatic Box Faces is suitable for walls around a hole. Use the same tile size and alignment origin on every piece. Use texture Wrap Mode Repeat and material Tiling (1, 1) for the chosen tile size.", MessageType.None);
        EditorGUILayout.LabelField(status, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Apply once, then enable Live Update and use Unity's normal Scale tool. Save the scene to keep mesh assignments. UV assets are saved automatically.", EditorStyles.wordWrappedLabel);
    }

    private IEnumerable<MeshFilter> SelectedMeshes()
    {
        return Selection.gameObjects.Where(g => g != null && !EditorUtility.IsPersistent(g))
            .SelectMany(g => includeChildren ? g.GetComponentsInChildren<MeshFilter>(true) : g.GetComponents<MeshFilter>())
            .Where(f => f != null).Distinct();
    }

    private void SelectionChanged()
    {
        watched.Clear();
        foreach (var filter in SelectedMeshes()) watched[filter] = filter.transform.localToWorldMatrix;
        Repaint();
    }

    private static void RefreshProBuilderUV(ProBuilderMesh pb)
    {
        var filter=pb.GetComponent<MeshFilter>();
        // Cutting geometry can leave the rendered mesh pending a rebuild.
        if(filter.sharedMesh==null || filter.sharedMesh.vertexCount!=pb.vertexCount)
        {
            pb.ToMesh();
            pb.Refresh();
        }
        else pb.Refresh(RefreshMask.UV | RefreshMask.Tangents);
    }
    private void AfterUndoRedo()
    {
        foreach(var filter in SelectedMeshes())
        {
            var pb=filter.GetComponent<ProBuilderMesh>();
            if(pb!=null){RefreshProBuilderUV(pb);}
        }
        SelectionChanged();
    }
    private WallTextureTilingData GetData(bool create)
    {
        if (data != null) return data;
        data = AssetDatabase.LoadAssetAtPath<WallTextureTilingData>(DataPath);
        if (data == null && create)
        {
            EnsureFolder(Folder);
            data = CreateInstance<WallTextureTilingData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        return data;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    public void ApplySelection()
    {
        if (!CanEdit) return;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Wall Texture Tiling");
        int count = 0;
        var errors = new List<string>();
        foreach (var filter in SelectedMeshes())
        {
            try { ApplyOne(filter, metresPerTile, alignmentOrigin, projection); count++; }
            catch (Exception e) { errors.Add(filter.name + ": " + e.Message); }
        }
        Undo.CollapseUndoOperations(group);
        Flush(); SelectionChanged(); SceneView.RepaintAll();
        status = "Updated " + count + " piece(s)." + (errors.Count > 0 ? "\n" + string.Join("\n", errors.Take(4)) : "");
    }

    // Public so the same operation can be invoked from other editor tools.
    public void ApplyOne(MeshFilter filter, float tileSize, Vector3 origin, WallUVProjection mode)
    {
        if (!CanEdit) throw new InvalidOperationException("Use this tool in scene Edit mode.");
        if (filter == null || filter.sharedMesh == null) throw new InvalidOperationException("No mesh assigned.");
        if (string.IsNullOrEmpty(filter.gameObject.scene.path)) throw new InvalidOperationException("Save this scene first.");
        var proBuilder = filter.GetComponent<ProBuilderMesh>();
        if (proBuilder != null) { ApplyProBuilder(proBuilder, tileSize, origin, mode); return; }
        if (Mathf.Abs(filter.transform.localToWorldMatrix.determinant) < 1e-10f)
            throw new InvalidOperationException("An object or parent has zero scale.");
        if (float.IsNaN(tileSize) || float.IsInfinity(tileSize) || tileSize <= 0f)
            throw new ArgumentOutOfRangeException(nameof(tileSize));
        string id = GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString();
        var registry = GetData(true);
        var entry = registry.entries.Find(e => e.objectId == id);
        Mesh source = entry != null && filter.sharedMesh == entry.generatedMesh ? entry.sourceMesh : filter.sharedMesh;
        // A duplicated piece initially references its sibling's baked mesh: recover its original source.
        var inherited = registry.entries.Find(e => e.generatedMesh == source);
        if (inherited != null) source = inherited.sourceMesh;
        if (source == null) throw new InvalidOperationException("Original mesh is missing.");
        Mesh built = BuildMesh(source, filter.transform.localToWorldMatrix, tileSize, origin, mode);
        try
        {
            string key = Hash128.Compute(id).ToString();
            if (!EditorUtility.IsPersistent(source))
            {
                string sourcePath = Folder + "/" + key + "-Original.asset";
                var backup = AssetDatabase.LoadAssetAtPath<Mesh>(sourcePath);
                if (backup == null) { backup = Instantiate(source); AssetDatabase.CreateAsset(backup, sourcePath); }
                source = backup;
            }
            Undo.RecordObject(registry, "Store Wall Texture Tiling");
            if (entry == null) { entry = new WallTextureTilingData.Entry { objectId = id }; registry.entries.Add(entry); }
            entry.sourceMesh = source;
            if (entry.generatedMesh == null)
            {
                string meshPath = Folder + "/" + key + ".asset";
                entry.generatedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (entry.generatedMesh == null)
                {
                    entry.generatedMesh = new Mesh();
                    AssetDatabase.CreateAsset(entry.generatedMesh, meshPath);
                }
            }
            Undo.RegisterCompleteObjectUndo(entry.generatedMesh, "Update Wall Texture UVs");
            EditorUtility.CopySerialized(built, entry.generatedMesh);
            entry.generatedMesh.name = source.name + " (World Tiled)";
            EditorUtility.SetDirty(entry.generatedMesh);
            EditorUtility.SetDirty(registry);
            Undo.RecordObject(filter, "Assign Wall Texture Mesh");
            filter.sharedMesh = entry.generatedMesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            EditorSceneManager.MarkSceneDirty(filter.gameObject.scene);
        }
        finally { DestroyImmediate(built); }
    }

    private void ApplyProBuilder(ProBuilderMesh pb, float tileSize, Vector3 origin, WallUVProjection mode)
    {
        if (tileSize <= 0f || float.IsNaN(tileSize) || float.IsInfinity(tileSize)) throw new ArgumentOutOfRangeException(nameof(tileSize));
        if (Mathf.Abs(pb.transform.localToWorldMatrix.determinant)<1e-10f) throw new InvalidOperationException("An object or parent has zero scale.");
        string id=GlobalObjectId.GetGlobalObjectIdSlow(pb.GetComponent<MeshFilter>()).ToString();
        var registry=GetData(true);var entry=registry.entries.Find(e=>e.objectId==id);
        Undo.RecordObject(registry,"Store ProBuilder UV backup");
        if(entry==null)
        {
            entry=new WallTextureTilingData.Entry { objectId=id, proBuilder=true,
                originalUV=pb.textures.ToArray(), originalFaces=pb.faces.Select(f=>new WallTextureTilingData.FaceUV {
                    manual=f.manualUV, textureGroup=f.textureGroup, unwrap=f.uv }).ToArray() };
            registry.entries.Add(entry);
        }
        Undo.RegisterCompleteObjectUndo(pb,"Update ProBuilder World UVs");
        var renderedMesh=pb.GetComponent<MeshFilter>().sharedMesh;
        if(renderedMesh!=null)Undo.RegisterCompleteObjectUndo(renderedMesh,"Update ProBuilder Mesh UVs");
        var positions=pb.positions;var uv=new Vector2[pb.vertexCount];
        foreach(var face in pb.faces)
        {
            var indexes=face.indexes;
            var a=pb.transform.TransformPoint(positions[indexes[0]]);
            var b=pb.transform.TransformPoint(positions[indexes[1]]);
            var c=pb.transform.TransformPoint(positions[indexes[2]]);
            var n=Vector3.Cross(b-a,c-a).normalized;n=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
            var faceMode=mode==WallUVProjection.AutomaticBoxFaces?
                (n.y>=n.x&&n.y>=n.z?WallUVProjection.WorldXZ:n.x>=n.z?WallUVProjection.WorldZY:WallUVProjection.WorldXY):mode;
            foreach(int i in face.distinctIndexes)
            {
                var p=(pb.transform.TransformPoint(positions[i])-origin)/tileSize;
                uv[i]=faceMode==WallUVProjection.WorldXZ?new Vector2(p.x,p.z):faceMode==WallUVProjection.WorldZY?new Vector2(p.z,p.y):new Vector2(p.x,p.y);
            }
            face.manualUV=true;
            face.textureGroup=-1;
        }
        pb.textures=uv;RefreshProBuilderUV(pb);
        EditorUtility.SetDirty(pb);EditorUtility.SetDirty(registry);
        PrefabUtility.RecordPrefabInstancePropertyModifications(pb);
        EditorSceneManager.MarkSceneDirty(pb.gameObject.scene);
    }
    public void RestoreSelection()
    {
        var registry = GetData(false);
        if (!CanEdit || registry == null) return;
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore Original Wall Meshes");
        int restored = 0;
        foreach (var filter in SelectedMeshes())
        {
            string id = GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString();
            var entry = registry.entries.Find(e => e.objectId == id);
            if (entry == null) continue;
            if (entry.proBuilder)
            {
                var pb = filter.GetComponent<ProBuilderMesh>();
                if (pb == null || entry.originalUV == null || entry.originalUV.Length != pb.vertexCount
                    || entry.originalFaces == null || entry.originalFaces.Length != pb.faceCount)
                { status = filter.name + ": topology changed; use Undo or restore before editing geometry."; continue; }
                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { pb, registry }, "Restore ProBuilder UVs");
                for(int i=0;i<pb.faceCount;i++)
                {
                    pb.faces[i].manualUV=entry.originalFaces[i].manual;
                    pb.faces[i].textureGroup=entry.originalFaces[i].textureGroup;
                    pb.faces[i].uv=entry.originalFaces[i].unwrap;
                }
                pb.textures=entry.originalUV.ToArray();RefreshProBuilderUV(pb);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pb);
                EditorUtility.SetDirty(pb);EditorSceneManager.MarkSceneDirty(filter.gameObject.scene);
                registry.entries.Remove(entry);EditorUtility.SetDirty(registry);restored++;continue;
            }
            if (entry.sourceMesh == null) continue;
            Undo.RecordObjects(new UnityEngine.Object[] { filter, registry }, "Restore Wall Mesh");
            filter.sharedMesh = entry.sourceMesh;
            registry.entries.Remove(entry);
            EditorUtility.SetDirty(registry);
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            EditorSceneManager.MarkSceneDirty(filter.gameObject.scene);
            restored++;
        }
        Undo.CollapseUndoOperations(group); Flush(); SelectionChanged();
        status = "Restored " + restored + " piece(s). Saved UV copies remain available for Undo/reuse.";
    }

    private void UpdateSelected()
    {
        if (!liveUpdate || !CanEdit || EditorApplication.timeSinceStartup < nextUpdate) return;
        nextUpdate = EditorApplication.timeSinceStartup + .08;
        var registry = GetData(false); if (registry == null) return;
        int undoGroup = Undo.GetCurrentGroup();
        foreach (var filter in SelectedMeshes())
        {
            var matrix = filter.transform.localToWorldMatrix;
            if (watched.TryGetValue(filter, out var old) && !MatrixMatches(old, matrix))
            {
                string id = GlobalObjectId.GetGlobalObjectIdSlow(filter).ToString();
                if (registry.entries.Any(e => e.objectId == id))
                {
                    try { ApplyOne(filter, metresPerTile, alignmentOrigin, projection); }
                    catch (Exception e) { status = filter.name + ": " + e.Message; }
                }
            }
            watched[filter] = matrix;
        }
        Undo.CollapseUndoOperations(undoGroup);
    }

    private static bool MatrixMatches(Matrix4x4 a, Matrix4x4 b)
    {
        for (int i=0; i<16; i++) if (Mathf.Abs(a[i]-b[i]) > .00001f) return false;
        return true;
    }

    private void DuringSceneGUI(SceneView sceneView) { if (Event.current.type == EventType.MouseUp) Flush(); }
    private void SceneSaved(UnityEngine.SceneManagement.Scene scene) => Flush();
    public void Flush()
    {
        var registry = GetData(false); if (registry == null) return;
        foreach (var entry in registry.entries)
        {
            if (entry.generatedMesh != null) AssetDatabase.SaveAssetIfDirty(entry.generatedMesh);
            if (entry.sourceMesh != null && AssetDatabase.GetAssetPath(entry.sourceMesh).StartsWith(Folder + "/")) AssetDatabase.SaveAssetIfDirty(entry.sourceMesh);
        }
        AssetDatabase.SaveAssetIfDirty(registry);
    }

    public static Mesh BuildMesh(Mesh source, Matrix4x4 localToWorld, float tileSize, Vector3 origin, WallUVProjection mode)
    {
        if (source == null || source.blendShapeCount > 0) throw new InvalidOperationException("Use a static wall mesh without blend shapes.");
        var vertices = source.vertices; var normals = source.normals; var colors = source.colors32;
        var sourceUVs = new List<Vector4>[8]; var outputUVs = new List<Vector4>[8];
        for (int c=1;c<8;c++) { sourceUVs[c]=new List<Vector4>(); source.GetUVs(c, sourceUVs[c]); if(sourceUVs[c].Count==vertices.Length)outputUVs[c]=new List<Vector4>(); }
        var positions = new List<Vector3>(); var outNormals = new List<Vector3>();
        var outColors = new List<Color32>(); var uv = new List<Vector2>();
        var indices = new List<int>[source.subMeshCount];
        for (int sub=0; sub<source.subMeshCount; sub++)
        {
            if (source.GetTopology(sub)!=MeshTopology.Triangles) throw new InvalidOperationException("Only triangle meshes are supported.");
            indices[sub]=new List<int>(); var triangles=source.GetTriangles(sub);
            for (int t=0;t<triangles.Length;t+=3)
            {
                Vector3 a=localToWorld.MultiplyPoint3x4(vertices[triangles[t]]);
                Vector3 b=localToWorld.MultiplyPoint3x4(vertices[triangles[t+1]]);
                Vector3 c=localToWorld.MultiplyPoint3x4(vertices[triangles[t+2]]);
                Vector3 worldNormal=Vector3.Cross(b-a,c-a).normalized;
                var faceMode=mode;
                if (mode==WallUVProjection.AutomaticBoxFaces)
                {
                    Vector3 n=new Vector3(Mathf.Abs(worldNormal.x),Mathf.Abs(worldNormal.y),Mathf.Abs(worldNormal.z));
                    faceMode=n.y>=n.x&&n.y>=n.z?WallUVProjection.WorldXZ:n.x>=n.z?WallUVProjection.WorldZY:WallUVProjection.WorldXY;
                }
                Vector3 fallback=Vector3.Cross(vertices[triangles[t+1]]-vertices[triangles[t]],vertices[triangles[t+2]]-vertices[triangles[t]]).normalized;
                for (int k=0;k<3;k++)
                {
                    int index=triangles[t+k];indices[sub].Add(positions.Count);positions.Add(vertices[index]);
                    outNormals.Add(normals.Length==vertices.Length?normals[index]:fallback);
                    if(colors.Length==vertices.Length)outColors.Add(colors[index]);
                    Vector3 p=(localToWorld.MultiplyPoint3x4(vertices[index])-origin)/tileSize;
                    uv.Add(faceMode==WallUVProjection.WorldXZ?new Vector2(p.x,p.z):faceMode==WallUVProjection.WorldZY?new Vector2(p.z,p.y):new Vector2(p.x,p.y));
                    for(int channel=1;channel<8;channel++)if(outputUVs[channel]!=null)outputUVs[channel].Add(sourceUVs[channel][index]);
                }
            }
        }
        var mesh=new Mesh { name=source.name+" (World Tiled)", indexFormat=positions.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
        mesh.SetVertices(positions);mesh.SetNormals(outNormals);mesh.SetUVs(0,uv);
        if(outColors.Count==positions.Count)mesh.SetColors(outColors);
        for(int c=1;c<8;c++)if(outputUVs[c]!=null)mesh.SetUVs(c,outputUVs[c]);
        mesh.subMeshCount=source.subMeshCount;
        for(int sub=0;sub<indices.Length;sub++)mesh.SetTriangles(indices[sub],sub);
        mesh.RecalculateBounds();mesh.RecalculateTangents();return mesh;
    }
}