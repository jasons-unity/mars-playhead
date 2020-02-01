using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS.Query
{
    class RatingMetaEnumerator :
        MetaEnumerator<KeyValuePair<int, float>, Dictionary<int, float>.Enumerator, Dictionary<int, float>>
    {
        public RatingMetaEnumerator(Dictionary<int, float>[] dictionaries) : base(dictionaries) { }
    }
}
