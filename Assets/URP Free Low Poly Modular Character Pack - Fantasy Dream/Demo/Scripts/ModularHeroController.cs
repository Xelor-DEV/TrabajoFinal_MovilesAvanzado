using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace GanzSe
{
    public class ModularHeroController : MonoBehaviour
    {
        [Header("Armor Parts")]
        public Transform armorPartsRoot;

        [Header("Face Details Parts")]
        public Transform facePartsRoot;

        [Header("Toggle Helmet")]
        public bool showHelmet = true;

        [Header("Main Character Model")]
        [SerializeField] private SkinnedMeshRenderer mainModel; // Referencia al modelo principal humanoide

        [Header("Effect Materials")]
        [SerializeField] private Material pushedEffectMaterial; // Material para cuando es empujado

        // Lista para guardar los props activos y sus materiales originales
        private List<SkinnedMeshRenderer> activeProps = new List<SkinnedMeshRenderer>();
        private Dictionary<SkinnedMeshRenderer, Material[]> originalMaterials = new Dictionary<SkinnedMeshRenderer, Material[]>();

        private void Awake()
        {
            // Randomizar las partes al inicio
            RandomizeArmorParts();
            RandomizeFaceParts();
            ToggleHelmet();

            // Guardar los props activos y sus materiales originales
            StoreActivePropsAndMaterials();
        }

        public void RandomizeArmorParts()
        {
            if (armorPartsRoot == null) return;
            foreach (Transform category in armorPartsRoot)
            {
                SetRandomActiveChild(category);
            }
        }

        public void RandomizeFaceParts()
        {
            if (facePartsRoot == null) return;
            foreach (Transform category in facePartsRoot)
            {
                SetRandomActiveChild(category);
            }
        }

        public void ToggleHelmet()
        {
            Transform heads = armorPartsRoot.Find("HEADS");
            Transform faceParent = facePartsRoot;

            if (heads == null || faceParent == null) return;

            heads.gameObject.SetActive(showHelmet);
            faceParent.gameObject.SetActive(!showHelmet);
        }

        private void SetRandomActiveChild(Transform category)
        {
            if (category.childCount == 0) return;

            foreach (Transform child in category)
            {
                child.gameObject.SetActive(false);
            }

            int rand = Random.Range(0, category.childCount);
            category.GetChild(rand).gameObject.SetActive(true);
        }

        // Guarda todos los props activos (SkinnedMeshRenderer) en la lista y almacena sus materiales originales
        private void StoreActivePropsAndMaterials()
        {
            activeProps.Clear();
            originalMaterials.Clear();

            // Agregar el modelo principal si está asignado
            if (mainModel != null)
            {
                activeProps.Add(mainModel);
                originalMaterials[mainModel] = mainModel.materials;
            }

            // Buscar en las partes de armadura
            if (armorPartsRoot != null)
            {
                FindActiveSkinnedMeshRenderers(armorPartsRoot);
            }

            // Buscar en las partes de la cara
            if (facePartsRoot != null)
            {
                FindActiveSkinnedMeshRenderers(facePartsRoot);
            }
        }

        // Recorre un transform y agrega los SkinnedMeshRenderer activos
        private void FindActiveSkinnedMeshRenderers(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf)
                {
                    SkinnedMeshRenderer[] renderers = child.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach (SkinnedMeshRenderer renderer in renderers)
                    {
                        if (renderer.gameObject.activeSelf)
                        {
                            activeProps.Add(renderer);
                            originalMaterials[renderer] = renderer.materials;
                        }
                    }
                }
            }
        }

        // Método para cambiar el material de todos los props activos
        public void SetPropsMaterial(Material newMaterial)
        {
            foreach (SkinnedMeshRenderer prop in activeProps)
            {
                if (prop != null)
                {
                    // Crear un array de materiales del mismo tamaño y llenarlo con newMaterial
                    Material[] newMaterials = new Material[prop.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++)
                    {
                        newMaterials[i] = newMaterial;
                    }
                    prop.materials = newMaterials;
                }
            }
        }

        // Método para restaurar los materiales originales
        public void RestoreOriginalMaterials()
        {
            foreach (SkinnedMeshRenderer prop in activeProps)
            {
                if (prop != null && originalMaterials.ContainsKey(prop))
                {
                    prop.materials = originalMaterials[prop];
                }
            }
        }

        // Método específico para cuando es empujado (usa el material de efecto configurado)
        public void ApplyPushedEffect()
        {
            if (pushedEffectMaterial != null)
            {
                SetPropsMaterial(pushedEffectMaterial);
            }
        }

        // Método para quitar el efecto de empujado
        public void RemovePushedEffect()
        {
            RestoreOriginalMaterials();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ModularHeroController))]
    public class ModularCharacterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ModularHeroController controller = (ModularHeroController)target;

            GUILayout.Space(10);
            GUILayout.Label("Editor Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Randomize Armor Parts"))
            {
                controller.RandomizeArmorParts();
            }

            if (GUILayout.Button("Randomize Face Parts"))
            {
                controller.RandomizeFaceParts();
            }

            if (GUILayout.Button(controller.showHelmet ? "Hide Helmet" : "Show Helmet"))
            {
                controller.showHelmet = !controller.showHelmet;
                controller.ToggleHelmet();
            }

            // Botones para probar los efectos
            GUILayout.Space(10);
            GUILayout.Label("Effect Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply Pushed Effect"))
            {
                controller.ApplyPushedEffect();
            }

            if (GUILayout.Button("Remove Pushed Effect"))
            {
                controller.RemovePushedEffect();
            }
        }
    }
#endif
}