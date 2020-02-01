using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ComponentEditor(typeof(ICalculateLandmarks))]
    public class CalculateLandmarksInspector : ComponentInspector
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            LandmarkControllerEditor.DrawAddLandmarkButton((ICalculateLandmarks)target);
        }

        public override bool HasDisplayProperties()
        {
            return true;
        }
    }
}
