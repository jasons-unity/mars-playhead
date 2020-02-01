using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Attribute marks a class that will not be included in the MARS Entity in the inspector and not included in the Add MARS Component menu
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExcludeInMARSEditor : Attribute { }
}
