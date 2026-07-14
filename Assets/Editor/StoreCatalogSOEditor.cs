using UnityEditor;
using UnityEngine;
using RingFlow.Gameplay.Economy;

namespace RingFlow.Editor
{
    [CustomEditor(typeof(StoreCatalogSO))]
    public sealed class StoreCatalogSOEditor : UnityEditor.Editor
    {
        private Vector2 _scroll;

        public override void OnInspectorGUI()
        {
            var catalog = (StoreCatalogSO)target;

            EditorGUILayout.LabelField("Mağaza Ürün Kataloğu (Store Catalog)", RingFlowEditorUtils.HeaderStyle);
            EditorGUILayout.Space(4f);

            serializedObject.Update();
            var productsProp = serializedObject.FindProperty("Products");

            if (productsProp != null && productsProp.isArray)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"Katalogdaki Ürünler ({productsProp.arraySize})", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);

                    for (int i = 0; i < productsProp.arraySize; i++)
                    {
                        var product = productsProp.GetArrayElementAtIndex(i);
                        var idProp = product.FindPropertyRelative("Id");
                        var typeProp = product.FindPropertyRelative("Type");
                        var priceProp = product.FindPropertyRelative("PriceString");
                        var nameProp = product.FindPropertyRelative("DisplayNameKey");
                        var descProp = product.FindPropertyRelative("DescriptionKey");

                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField($"Ürün {i + 1}: {idProp.stringValue}", EditorStyles.boldLabel);
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("Kaldır", EditorStyles.miniButton, GUILayout.Width(60f)))
                                {
                                    productsProp.DeleteArrayElementAtIndex(i);
                                    break;
                                }
                            }

                            EditorGUILayout.Space(2f);
                            idProp.stringValue = EditorGUILayout.TextField("Ürün Kimliği (ID)", idProp.stringValue);
                            typeProp.enumValueIndex = (int)(ProductType)EditorGUILayout.EnumPopup("Ürün Türü", (ProductType)typeProp.enumValueIndex);
                            priceProp.stringValue = EditorGUILayout.TextField("Fiyat Etiketi", priceProp.stringValue);
                            nameProp.stringValue = EditorGUILayout.TextField("Yerelleştirme İsmi (Name Key)", nameProp.stringValue);
                            descProp.stringValue = EditorGUILayout.TextField("Yerelleştirme Açıklaması (Desc Key)", descProp.stringValue);
                        }
                        EditorGUILayout.Space(4f);
                    }

                    if (GUILayout.Button("+ Yeni Ürün Ekle", GUILayout.Height(26)))
                    {
                        productsProp.InsertArrayElementAtIndex(productsProp.arraySize);
                        var newElem = productsProp.GetArrayElementAtIndex(productsProp.arraySize - 1);
                        newElem.FindPropertyRelative("Id").stringValue = "new_product_id";
                        newElem.FindPropertyRelative("Type").enumValueIndex = 0;
                        newElem.FindPropertyRelative("PriceString").stringValue = "$0.99";
                        newElem.FindPropertyRelative("DisplayNameKey").stringValue = "store.new_product";
                        newElem.FindPropertyRelative("DescriptionKey").stringValue = "";
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
