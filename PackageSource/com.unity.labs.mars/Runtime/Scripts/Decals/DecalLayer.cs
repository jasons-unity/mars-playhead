using UnityEngine;

namespace Unity.Labs.MARS
{
    [System.Serializable]
    public class DecalLayer
    {
        [HideInInspector]
        public Material materialInstance;
        public Texture texture;
        public BlendMode blendMode;
        public Vector2 transformPositionOffset = new Vector2(0.5f, 0.5f);
        public Vector2 size = Vector2.one;

        [SerializeField]
        public DecalLayer lastDecalChanges;

        public DecalLayer() {}

        public DecalLayer(Material materialInstance, Texture texture, BlendMode blendMode, Vector2 transformPositionOffset, Vector2 size)
        {
            this.texture = texture;
            this.blendMode = blendMode;
            this.transformPositionOffset = transformPositionOffset;
            this.materialInstance = materialInstance;
            this.size = size;
        }
    }

    public enum BlendMode
    {
        NORMAL,
        ADD,
        //DIFFERENCE,
        MULTIPLY,
        SCREEN,
        //OVERLAY,
        SUBSTRACT
    }
}
