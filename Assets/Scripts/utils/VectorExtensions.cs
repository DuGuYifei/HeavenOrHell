using UnityEngine;

namespace utils
{
    public static class VectorExtensions
    {
        public static Vector2 XY(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.y);
        }
        
        public static Vector2 YZ(this Vector3 vec)
        {
            return new Vector2(vec.y, vec.z);
        }
        
        public static Vector2 XZ(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }

        public static Vector2Int ToVector2Int(this Vector3 vec)
        {
            return new Vector2Int(Mathf.RoundToInt(vec.x), Mathf.RoundToInt(vec.y));
        }
        
        public static Vector2Int FloorVector2Int(this Vector3 vec)
        {
            return new Vector2Int(Mathf.FloorToInt(vec.x), Mathf.FloorToInt(vec.y));
        }
    }
}