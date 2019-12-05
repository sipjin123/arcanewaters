using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MapCreationTool
{
   [CustomEditor(typeof(BiomedPaletteData))]
   public class BiomedPaletteDataEditor : Editor
   {
      private Color[] clusterColors;

      private bool enableEditor = false;

      private Tool tool = Tool.None;
      private EditorType editorType = EditorType.Area;

      private int selectedLayer = 0;
      private int selectedSublayer = 0;
      private int selectedCluster = 0;
      private TileCollisionType selectedCollisionType = TileCollisionType.Disabled;

      private bool drawLayer = true;
      private bool drawClusters = true;
      private bool drawCollisions = true;

      private EditorConfig config;
      private BiomedPaletteData.TileSetupContainer tileSetups;

      private int targetMatrixWidth = 0;
      private int targetMatrixHeight = 0;

      public override void OnInspectorGUI () {
         base.OnInspectorGUI();

         tileSetups = (target as BiomedPaletteData).tileSetupContainer;

         if (config == null)
            config = Resources.Load<EditorConfig>("MapEditorConfig");

         GUILayout.Space(10);
         EditorGUILayout.LabelField("CUSTOM TILE SETUP EDITOR", EditorStyles.boldLabel);
         enableEditor = EditorGUILayout.Toggle("Enable editor", enableEditor);

         if (!enableEditor)
            return;

         GUILayout.BeginHorizontal();
         EditorGUILayout.PrefixLabel("Editor type");
         editorType = (EditorType) EditorGUILayout.EnumPopup(editorType);
         GUILayout.EndHorizontal();

         var setupContainer = serializedObject.FindProperty("tileSetupContainer");
         var setupArray = setupContainer.FindPropertyRelative("tileSetups");
         var setupMatrixHeight = setupContainer.FindPropertyRelative("matrixHeight");

         int height = setupMatrixHeight.intValue;
         int width = height == 0 ? 0 : setupArray.arraySize / height;
         
         EditorGUILayout.LabelField($"Tile setup matrix - {width}x{height}");

         GUILayout.Space(5);

         GUILayout.BeginHorizontal();
         EditorGUILayout.PrefixLabel("Tool");
         tool = (Tool) EditorGUILayout.EnumPopup(tool);
         GUILayout.EndHorizontal();

         switch(tool) {
            case Tool.Layer:
               drawToolLayer();
               break;
            case Tool.Sublayer:
               drawToolSublayer();
               break;
            case Tool.Cluster:
               drawToolCluster();
               break;
            case Tool.Collision:
               drawToolCollision();
               break;
         }

         GUILayout.Space(10);

         drawLayer = EditorGUILayout.Toggle("Show layers", drawLayer);
         drawClusters = EditorGUILayout.Toggle("Show clusters", drawClusters);
         drawCollisions = EditorGUILayout.Toggle("Show collisions", drawCollisions);

         GUILayout.Space(10);

         EditorGUILayout.LabelField("Resize tile set up matrix");
         GUILayout.BeginHorizontal();
         targetMatrixWidth = EditorGUILayout.IntField(targetMatrixWidth);
         targetMatrixHeight = EditorGUILayout.IntField(targetMatrixHeight);
         GUILayout.EndHorizontal();

         if(GUILayout.Button("Resize") && targetMatrixWidth != tileSetups.size.x && targetMatrixHeight != tileSetups.size.y) {
            Undo.RecordObject(target, "resized matrix size manually");
            tileSetups.resize(targetMatrixWidth, targetMatrixHeight);
            EditorUtility.SetDirty(target);
         }
      }

      private void OnEnable () {
         clusterColors = new Color[] {
            new Color(1, 0.5f, 1, 0.4f),
            new Color(1, 1, 0.5f, 0.4f),
            new Color(0.5f, 1, 1, 0.4f),
            new Color(1, 0.5f, 0, 0.4f)
         };
      }

      private void drawToolLayer() {
         selectedLayer = EditorGUILayout.Popup("Layer to set", selectedLayer, removeNulls(config.areaLayerMap));
      }

      private void drawToolSublayer() {
         selectedSublayer = EditorGUILayout.Popup("Sublayer to set", selectedSublayer, Enumerable.Range(0, 10).Select(n => n.ToString()).ToArray());
      }

      private void drawToolCluster() {
         selectedCluster = EditorGUILayout.Popup("Sublayer to set", selectedCluster, Enumerable.Range(0, 4).Select(n => n.ToString()).ToArray());
      }

      private void drawToolCollision() {
         GUILayout.BeginHorizontal();
         EditorGUILayout.PrefixLabel("Collision type");
         selectedCollisionType = (TileCollisionType) EditorGUILayout.EnumPopup(selectedCollisionType);
         GUILayout.EndHorizontal();
      }

      private void OnSceneGUI () {
         tileSetups = (target as BiomedPaletteData).tileSetupContainer;

         if (config == null)
            config = Resources.Load<EditorConfig>("MapEditorConfig");

         if (!enableEditor)
            return;

         drawTileSetupMatrix();

         if (tool == Tool.None)
            return;

         if (Event.current.type == EventType.Layout)
            HandleUtility.AddDefaultControl(GetHashCode());

         if (Event.current.button == 0 && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)) {
            Vector2Int index = getMouseTileIndex(SceneView.currentDrawingSceneView, Event.current);
            if (index.x >= 0 && index.y >= 0) {
               Undo.RecordObject(target, "changed tilesetup");

               switch(tool) {
                  case Tool.Layer:
                     tileSetups[index.x, index.y].layer = removeNulls(config.areaLayerMap)[selectedLayer];
                     break;
                  case Tool.Sublayer:
                     tileSetups[index.x, index.y].sublayer = selectedSublayer;
                     break;
                  case Tool.Cluster:
                     tileSetups[index.x, index.y].cluster = selectedCluster;
                     break;
                  case Tool.Collision:
                     tileSetups[index.x, index.y].collisionType = selectedCollisionType;
                     break;
               }

               EditorUtility.SetDirty(target);
               Repaint();
            }

         }
      }

      private Vector2Int getMouseTileIndex(SceneView view, Event ev) {
         Vector3 pos = ev.mousePosition;
         pos.y = view.camera.pixelHeight - pos.y;
         pos = view.camera.ScreenToWorldPoint(pos);

         return new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
      }

      private void drawTileSetupMatrix() {
         for(int i = 0; i < tileSetups.size.x; i++) {
            for(int j = 0; j < tileSetups.size.y; j++) {
               if (drawClusters) {
                  Handles.DrawSolidRectangleWithOutline(new Rect(new Vector3(i, j, 0), Vector3.one), clusterColors[tileSetups[i, j].cluster], Color.black);
               }

               if (drawLayer) {
                  drawTextUp(SceneView.currentDrawingSceneView, tileSetups[i, j].layer + " " + tileSetups[i, j].sublayer, new Vector3Int(i, j, 0), new GUIStyle());
               }
                  
               if(drawCollisions) {
                  drawTextDown(SceneView.currentDrawingSceneView, tileSetups[i, j].collisionType.ToString(), new Vector3Int(i, j, 0), new GUIStyle());
               }
            }
         }
         
      }

      static void drawTextUp (SceneView view, string text, Vector3Int tileIndex, GUIStyle style) {
         style.fontSize = 1 + (int) (32 / view.camera.orthographicSize);
         style.alignment = TextAnchor.UpperCenter;
         Vector3 worldPos = tileIndex + Vector3.up * 0.95f + Vector3.right * 0.5f;

         float offset = 0.01f;

         style.normal.textColor = Color.white;

         Handles.Label(worldPos + new Vector3(offset, 0, 0), text, style );
         Handles.Label(worldPos + new Vector3(-offset, 0, 0), text, style);
         Handles.Label(worldPos + new Vector3(0, offset, 0), text, style);
         Handles.Label(worldPos + new Vector3(0, -offset, 0), text, style);

         style.normal.textColor = Color.black;
         Handles.Label(worldPos, text, style);
      }

      static void drawTextDown (SceneView view, string text, Vector3Int tileIndex, GUIStyle style) {
         style.fontSize = 1 + (int) (32 / view.camera.orthographicSize);
         style.alignment = TextAnchor.LowerCenter;
         style.fixedHeight = 1;
         Vector3 worldPos = tileIndex + Vector3.right * 0.5f + Vector3.up * 0.05f;

         float offset = 0.01f;

         style.normal.textColor = Color.white;

         Handles.Label(worldPos + new Vector3(offset, 0, 0), text, style);
         Handles.Label(worldPos + new Vector3(-offset, 0, 0), text, style);
         Handles.Label(worldPos + new Vector3(0, offset, 0), text, style);
         Handles.Label(worldPos + new Vector3(0, -offset, 0), text, style);

         style.normal.textColor = Color.black;
         Handles.Label(worldPos, text, style);
      }


      private string[] removeNulls(string[] data) {
         return data.Where(d => d != null).ToArray();
      }

      public enum Tool
      {
         None,
         Layer,
         Sublayer,
         Cluster,
         Collision
      }
   }
}
