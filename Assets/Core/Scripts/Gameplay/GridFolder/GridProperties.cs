using System;
using UnityEngine;

namespace Core.Scripts.Gameplay.GridFolder
{
    [Serializable]
    public struct GridProperties
    {
        public int width;
        public int height;
        public int tileSize;
        public float scale;
        public float spacing;
        public bool randomRotation;
        public Vector3Int startPoint;
    }
}