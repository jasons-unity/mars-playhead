using UnityEngine.UI;

// this class exists to allow testing of the overload for 
// MaterialUtils.GetMaterialClone that takes a Graphic-derived class

namespace Unity.Labs.Utils
{
    public class TestImage : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) {}
    }
}
