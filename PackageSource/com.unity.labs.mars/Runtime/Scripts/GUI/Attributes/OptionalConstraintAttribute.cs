using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class OptionalConstraintAttribute : Attribute
    {
        public readonly string BoolPropertyName;

        public OptionalConstraintAttribute(string boolPropertyName) { BoolPropertyName = boolPropertyName; }
    }
}
