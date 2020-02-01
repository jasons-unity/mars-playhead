using NUnit.Framework;

namespace UnityEditor.PrefabHandles
{
    sealed class ReflectionTests
    {
        [Test]
        public void ValidateExists_Tools_InvalidateHandlePosition()
        {
            Assert.IsTrue(ToolsUtility.TestAccess.invalidateHandlePositionExists);
        }
    }
}