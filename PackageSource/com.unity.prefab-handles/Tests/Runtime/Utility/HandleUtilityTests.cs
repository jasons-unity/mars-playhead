using NUnit.Framework;

namespace UnityEngine.PrefabHandles.Tests
{
    sealed class HandleUtilityTests
    {

        [Test]
        public void PixelsPerPoint_IsExpectedValue()
        {
#if UNITY_EDITOR
            Assert.That(PrefabHandles.HandleUtility.pixelsPerPoint, Is.EqualTo(UnityEditor.EditorGUIUtility.pixelsPerPoint));
#else
            Assert.That(PrefabHandles.HandleUtility.pixelsPerPoint, Is.EqualTo(Screen.dpi / 96f));
#endif
        }
    }
}