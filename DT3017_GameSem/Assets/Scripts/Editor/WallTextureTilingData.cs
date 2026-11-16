using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public sealed class WallTextureTilingData : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string objectId;
        public Mesh sourceMesh;
        public Mesh generatedMesh;
        public bool proBuilder;
        public Vector2[] originalUV;
        public FaceUV[] originalFaces;
    }
    [Serializable]
    public sealed class FaceUV
    {
        public bool manual;
        public int textureGroup;
        public AutoUnwrapSettings unwrap;
    }
    public List<Entry> entries = new List<Entry>();
}