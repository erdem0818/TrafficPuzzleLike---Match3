using System;
using UnityEngine;

namespace Core.Scripts.Gameplay.GridFolder
{
    [Serializable]
    public struct Coord2D : IEquatable<Coord2D>
    {
        public int x, y;

        public Coord2D(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        
        public bool Equals(Coord2D other)
        {
            return x == other.x && y == other.y;
        }
        
        public static implicit operator Coord2D(Vector2Int vector2)
        {
            return new Coord2D{ x = vector2.x, y = vector2.y };
        }
        
        public override string ToString()
        {
            return $"x:{x} - y:{y}";
        }
    }
}