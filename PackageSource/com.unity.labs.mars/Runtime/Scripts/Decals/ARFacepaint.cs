using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ExecuteInEditMode]
    public class ARFacepaint : MonoBehaviour
    {
        [SerializeField]
        //Mesh to target with decals
        MeshRenderer targetMeshRenderer;

        //Original material of the targetMesh
        Material meshOriginalMaterial;

        //Material to instance different decalMaterials from
        [HideInInspector]
        Material decalMaterial;

        public List<DecalLayer> decalLayerList = new List<DecalLayer>();

        int m_SrcBlendID = -1;
        int m_DstBlendID = -1;
        int m_BlendOpID = -1;

        void OnEnable()
        {
            decalMaterial = (Material)Resources.Load("Materials/DecalMaterial", typeof(Material));

            meshOriginalMaterial = (Material)Resources.Load("Materials/DecalFaceDefault", typeof(Material));

            m_SrcBlendID = Shader.PropertyToID("_SrcBlend");
            m_DstBlendID = Shader.PropertyToID("_DstBlend");
            m_BlendOpID = Shader.PropertyToID("_BlendOp");

            if(targetMeshRenderer == null)
                targetMeshRenderer = GameObject.Find("FaceMesh").GetComponent<MeshRenderer>();

            if (targetMeshRenderer == null)
            {
                Debug.LogWarning("No face mesh detected in scene - cannot enable");
                enabled = false;
                return;
            }

            SaveLastChangesOnDecalLayers();

            ResetDecalLayerMaterialsList();
        }

        void OnDestroy()
        {
            foreach (DecalLayer decalLayer in decalLayerList)
            {
                DestroyDecalLayerMaterial(decalLayer);

                decalLayer.materialInstance = null;
            }
        }

        void InitializeDecalLayerMaterial(DecalLayer decalLayer)
        {
            Material materialInstance = new Material(decalMaterial.shader);

            List<Material> decalLayerMaterials = new List<Material>();

            decalLayerMaterials.AddRange(targetMeshRenderer.sharedMaterials);

            decalLayer.materialInstance = materialInstance;

            UpdateDecalLayerMaterial(decalLayer);

            decalLayerMaterials.Add(materialInstance);

            targetMeshRenderer.sharedMaterials = decalLayerMaterials.ToArray();
        }

        void DestroyDecalLayerMaterial(DecalLayer decalLayer)
        {
            if(targetMeshRenderer != null)
            {
                List<Material> decalLayerMaterials = new List<Material>();

                decalLayerMaterials.AddRange(targetMeshRenderer.sharedMaterials);

                if (decalLayerMaterials.Contains(decalLayer.materialInstance))
                    decalLayerMaterials.Remove(decalLayer.materialInstance);

                targetMeshRenderer.sharedMaterials = decalLayerMaterials.ToArray();
            }
        }

        public void RemoveDecalLayer(int index)
        {
            decalLayerList.RemoveAt(index);

            ResetDecalLayerMaterialsList();
        }

        public void ResetDecalLayerMaterialsList()
        {
            List<Material> decalLayerMaterials = new List<Material>();

            if (false == Application.isPlaying)
            {
                decalLayerMaterials.Add(meshOriginalMaterial);
            }

            foreach(DecalLayer decalLayer in decalLayerList)
            {
                if (decalLayer.materialInstance != null && decalLayer.texture != null)
                {
                    decalLayerMaterials.Add(decalLayer.materialInstance);
                }
            }

            targetMeshRenderer.sharedMaterials = decalLayerMaterials.ToArray();

            UpdateDecalLayerMaterials();
        }

        public void UpdateDecalLayerMaterials()
        {
            foreach (DecalLayer decalLayer in decalLayerList)
            {
                UpdateDecalLayerMaterial(decalLayer);
            }
        }

        void UpdateDecalLayerMaterial(DecalLayer decalLayer)
        {
            if(decalLayer.materialInstance != null)
            {
                if (decalLayer.texture == null)
                {
                    DestroyDecalLayerMaterial(decalLayer);
                }
                else
                {
                    decalLayer.materialInstance.name = decalLayer.texture.name;

                    decalLayer.materialInstance.SetTexture("_DecalTex", decalLayer.texture);

                    decalLayer.materialInstance.SetVector("_DecalPositionOffset", decalLayer.transformPositionOffset);
                    decalLayer.materialInstance.SetVector("_DecalSize", decalLayer.size);

                    if (decalLayer.lastDecalChanges != null)
                    {
                        decalLayer.materialInstance.DisableKeyword("BLEND_" + decalLayer.lastDecalChanges.blendMode.ToString());
                    }

                    decalLayer.materialInstance.EnableKeyword("BLEND_" + decalLayer.blendMode.ToString());

                    switch (decalLayer.blendMode)
                    {
                        case BlendMode.NORMAL:
                            decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Add);
                            decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case BlendMode.ADD:
                            decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Add);
                            decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            break;
                        case BlendMode.SUBSTRACT:
                            decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
                            decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            break;
                        //case BlendMode.DIFFERENCE:
                            //decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Difference);
                            //decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            //decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            //break;
                        case BlendMode.MULTIPLY:
                            decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Add);
                            decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.DstColor);
                            decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case BlendMode.SCREEN:
                            decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Add);
                            decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                            decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            break;
                        //case BlendMode.OVERLAY:
                            //decalLayer.materialInstance.SetInt(m_BlendOpID, (int)UnityEngine.Rendering.BlendOp.Add);
                            //decalLayer.materialInstance.SetInt(m_SrcBlendID, (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                            //decalLayer.materialInstance.SetInt(m_DstBlendID, (int)UnityEngine.Rendering.BlendMode.One);
                            //break;
                    }

                    SaveLastChangesOnDecalLayer(decalLayer);
                }
            }
        }

        void Update()
        {
            //If there is no Decals on the list, start with an empty element
            if (decalLayerList.Count.Equals(0))
                decalLayerList.Add(new DecalLayer());

            foreach (DecalLayer decalLayer in decalLayerList)
            {
                //Prevents problems when texture is null (on adding component or when deleted by User)
                if (decalLayer != null && decalLayer.texture != null)
                {
                    if (decalLayer.materialInstance == null)
                    {
                        InitializeDecalLayerMaterial(decalLayer);
                    }

                    if (decalLayer != null && decalLayer.texture != null)
                    {
                        CheckForChangesInDecalLayer(decalLayer);
                    }
                }
            }
        }

        void CheckForChangesInDecalLayer(DecalLayer decalLayer)
        {
            //If anything changed in the decalLayer, including size due to transform scale, we update
            if (IsChangeOfDecalVariables(decalLayer))
            {
                UpdateDecalLayerMaterial(decalLayer);
            }
        }

        DecalLayer DuplicateDecalLayer(DecalLayer targetDecal)
        {
            if (targetDecal != null && targetDecal.texture != null)
            {
                DecalLayer newDecalLayer = new DecalLayer(targetDecal.materialInstance, targetDecal.texture,
                                                          targetDecal.blendMode, targetDecal.transformPositionOffset,
                                                          targetDecal.size);
                return newDecalLayer;
            }
            else return null;
        }

        bool IsChangeOfDecalVariables(DecalLayer decalLayer)
        {
            DecalLayer lastDecalChanges = decalLayer.lastDecalChanges;

            if (lastDecalChanges == null ||
                lastDecalChanges.texture == null ||
                !lastDecalChanges.texture.Equals(decalLayer.texture) ||
                !lastDecalChanges.blendMode.Equals(decalLayer.blendMode) ||
                !lastDecalChanges.size.Equals(decalLayer.transformPositionOffset) ||
                !lastDecalChanges.size.Equals(decalLayer.size))
                return true;

            return false;
        }

        void SaveLastChangesOnDecalLayer(DecalLayer decalLayer)
        {
            decalLayer.lastDecalChanges = DuplicateDecalLayer(decalLayer);
        }

        void SaveLastChangesOnDecalLayers()
        {
            foreach(DecalLayer decalLayer in decalLayerList)
            {
                SaveLastChangesOnDecalLayer(decalLayer);
            }
        }
    }
}
