﻿using Unity.Collections;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    public struct PointCloudData
    {
        public NativeSlice<ulong>? Identifiers;
        public NativeSlice<Vector3>? Positions;
        public NativeSlice<float>? ConfidenceValues;
    }
}
