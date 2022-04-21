using UnityEngine;
namespace Traitor_God
{
    public static class MiscUtil
    {
        public static Vector3 SetY(this Vector3 v,float value)
        {
            v[1] = value;
            return v;
        }
    }
}
