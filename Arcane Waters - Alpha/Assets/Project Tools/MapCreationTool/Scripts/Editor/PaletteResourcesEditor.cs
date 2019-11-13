using UnityEngine;
using UnityEditor;

namespace MapCreationTool
{
    [CustomEditor(typeof(PaletteResources))]
    public class PaletteResourcesEditor : Editor
    {
        private SerializedProperty sublayerArray;

        private int setTo;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            sublayerArray = serializedObject.FindProperty("subLayers");
            var draw = serializedObject.FindProperty("drawSublayers");
            if (!draw.boolValue)
                sublayerArray = null;

            if (sublayerArray == null)
                return;

            GUILayout.Space(10);
            setTo = EditorGUILayout.IntField("Set sublayer to", setTo);
        }

        private void OnSceneGUI()
        {
            if (sublayerArray == null)
                return;

            if (Event.current.button != 1 || Event.current.type != EventType.MouseDown)
                return;

            Vector3 pos = Event.current.mousePosition;
            pos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - pos.y;
            pos = SceneView.currentDrawingSceneView.camera.ScreenToWorldPoint(pos);

            if(pos.x > 0 && pos.y > 0)
            {
                Vector2Int index = new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
                if(index.x < 50 && index.y < 100)
                {
                    if(index.x * 100 + index.y < sublayerArray.arraySize)
                    {
                        Event.current.Use();
                        var element = sublayerArray.GetArrayElementAtIndex(index.x * 100 + index.y);
                        element.intValue = setTo;
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }
            
        }
    }

}