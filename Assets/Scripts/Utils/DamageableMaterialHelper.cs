
using System.Collections.Generic;
using Entity.Damageable;
using UnityEditor;
using UnityEngine;

namespace Utils {
    [CustomEditor(typeof(Damageable), true)]
    public class DamageableMaterialHelper : Editor {
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            if (GUILayout.Button("Assign All Damageable Materials"))
                AssignMaterials();
        }

        void AssignMaterials() {
            var damageable = (Damageable)target;
            var renderers = damageable.GetComponentsInChildren<MeshRenderer>(true);
            var materialData = new List<Damageable.DamageableMaterialData>();
            var uniqueMaterials = new HashSet<Material>();
            foreach (var renderer in renderers) {
                foreach (var material in renderer.sharedMaterials) {
                    if (!material || !uniqueMaterials.Add(material))
                        continue;

                    materialData.Add(new Damageable.DamageableMaterialData {
                        currentMaterial = material,
                        originalEmissionColor = material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) : Color.black
                    });
                }
            }

            Undo.RecordObject(damageable, "Assign Damageable Materials");

            var serializedObject = new SerializedObject(damageable);
            var property = serializedObject.FindProperty("currentDamageableMaterialData");
            property.ClearArray();
            for (int i = 0; i < materialData.Count; i++) {
                property.InsertArrayElementAtIndex(i);
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("currentMaterial").objectReferenceValue = materialData[i].currentMaterial;
                element.FindPropertyRelative("originalEmissionColor").colorValue = materialData[i].originalEmissionColor;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(damageable);
#if UNITY_EDITOR
            Debug.Log($"Assigned {materialData.Count} materials to {damageable.name}", damageable);
#endif
        }
    }
}
