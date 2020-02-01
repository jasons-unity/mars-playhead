#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    static class QueryObjectMapping
    {
        internal static readonly Dictionary<long, GameObject> Map = new Dictionary<long, GameObject>();

        internal static readonly Dictionary<long, GameObject> Sets = new Dictionary<long, GameObject>();
    }
}
#endif
