using UnityEngine;
namespace Traitor_God
{
    public static class GOUtil
    {
        public static GameObject FindGameObjectInChildren(this GameObject parent,string name)
        {
            foreach(var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if(child.name == name)
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}
