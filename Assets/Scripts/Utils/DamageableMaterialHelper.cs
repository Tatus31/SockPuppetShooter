
using System.Collections.Generic;
using Entity.Damageable;
using UnityEditor;
using UnityEngine;

namespace Utils {
    [CustomEditor(typeof(Damageable), true)]
    public class DamageableMaterialHelper : Editor {
        static readonly int EmissionColor = Shader.PropertyToID(EMISSION_COLOR);
        const string EMISSION_COLOR = "_EmissionColor";

        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Assign All Damageable Materials"))
                AssignMaterials();
        }

        void AssignMaterials() {
            var damageable = (Damageable)target;
            var materials = GetUniqueMaterials(damageable);
            Undo.RecordObject(damageable, "Assign Damageable Materials");
            UpdateMaterialData(materials);
            EditorUtility.SetDirty(damageable);
#if UNITY_EDITOR
            Debug.Log($"Assigned {materials.Count} materials to {damageable.name}", damageable);
#endif
        }

        List<Material> GetUniqueMaterials(Damageable damageable) {
            var materials = new List<Material>();
            var uniqueMaterials = new HashSet<Material>();
            var renderers = damageable.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
                AddRendererMaterials(renderer, materials, uniqueMaterials);

            return materials;
        }

        void AddRendererMaterials(MeshRenderer renderer, List<Material> materials, HashSet<Material> uniqueMaterials) {
            foreach (var material in renderer.sharedMaterials) {
                if (!material || !uniqueMaterials.Add(material))
                    continue;

                materials.Add(material);
            }
        }

        void UpdateMaterialData(List<Material> materials) {
            var serializedTarget = new SerializedObject(target);
            var property = serializedTarget.FindProperty("currentDamageableMaterialData");
            property.ClearArray();
            for (int i = 0; i < materials.Count; i++) {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                AssignMaterialReference(element, materials[i]);
                AssignOriginalEmissionColor(element, materials[i]);
            }

            serializedTarget.ApplyModifiedProperties();
        }

        void AssignMaterialReference(SerializedProperty element, Material material) => 
            element.FindPropertyRelative("currentMaterial").objectReferenceValue = material;

        void AssignOriginalEmissionColor(SerializedProperty element, Material material) {
            var emissionColor = material.HasProperty(EmissionColor)
                ? material.GetColor(EmissionColor)
                : Color.black;

            element.FindPropertyRelative("originalEmissionColor").colorValue = emissionColor;
        }
    }
}
