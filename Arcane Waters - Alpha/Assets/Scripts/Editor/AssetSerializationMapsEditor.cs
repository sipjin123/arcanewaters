using UnityEngine;
using UnityEditor;
using System.Linq;
using MapCreationTool;
using System;

[CustomEditor(typeof(AssetSerializationMaps))]
public class AssetSerializationMapsEditor : Editor
{
   #region Public Variables

   #endregion

   public override void OnInspectorGUI () {
      base.OnInspectorGUI();

      _target = target as AssetSerializationMaps;

      GUILayout.Space(10);
      EditorGUILayout.LabelField("Tile Attributes Editor", EditorStyles.boldLabel);
      _enableEditor = EditorGUILayout.Toggle("Enable editor", _enableEditor);

      if (!_enableEditor)
         return;

      GUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Attribute to paint");
      _selectedAttribute = (TileAttributes.Type) EditorGUILayout.EnumPopup(_selectedAttribute);
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Square highlight opacity");
      _tileOverlayOpacity = EditorGUILayout.FloatField(_tileOverlayOpacity);
      GUILayout.EndHorizontal();

      if (_selectedAttribute != TileAttributes.Type.None) {
         EditorGUILayout.LabelField("Left Mouse Button to Add", EditorStyles.miniLabel);
         EditorGUILayout.LabelField("Right Moust Button to Delete", EditorStyles.miniLabel);
         EditorGUILayout.LabelField(_tilesWithSelectedAttribute + " have attribute " + _selectedAttribute.ToString(), EditorStyles.miniLabel);
      }
   }

   private void OnSceneGUI () {
      if (!_enableEditor)
         return;

      _target = target as AssetSerializationMaps;

      // Get which index mouse is hovering over
      Vector2Int index = getMouseTileIndex(SceneView.currentDrawingSceneView, Event.current);

      bool forceRepaint = _previousHover != index;

      if (Event.current.type == EventType.Layout)
         HandleUtility.AddDefaultControl(GetHashCode());

      _tilesWithSelectedAttribute = drawAttributeBoxes(SceneView.currentDrawingSceneView, Event.current, _target, _selectedAttribute);
      drawTileHoverBox(SceneView.currentDrawingSceneView, Event.current, index, _target);


      // Paint - add/remove attributes
      if (_selectedAttribute != TileAttributes.Type.None) {
         if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) {
            bool modified = false;
            Undo.RecordObject(target, "changed tile matrix");

            if (index.x >= 0 && index.y >= 0) {

               if (Event.current.button == 0) {
                  modified = _target.tileAttributeMatrixEditor.addAttribute(index.x, index.y, _selectedAttribute);
                  Event.current.Use();
               } else if (Event.current.button == 1) {
                  modified = _target.tileAttributeMatrixEditor.removeAttribute(index.x, index.y, _selectedAttribute);
                  Event.current.Use();
               }
            }

            if (modified) {
               EditorUtility.SetDirty(target);

               forceRepaint = true;
            }
         }
      }

      if (forceRepaint) {
         SceneView.RepaintAll();
      }

      _previousHover = index;
   }

   private Vector2Int getMouseTileIndex (SceneView view, Event ev) {
      Vector3 pos = ev.mousePosition;
      pos.y = view.camera.pixelHeight - pos.y;
      pos = view.camera.ScreenToWorldPoint(pos);

      return new Vector2Int(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y));
   }

   private int drawAttributeBoxes (SceneView scene, Event e, AssetSerializationMaps targetMaps, TileAttributes.Type attribute) {
      int count = 0;

      for (int i = 0; i < targetMaps.tileAttributeMatrixEditor.attributeMatrixWidth; i++) {
         for (int j = 0; j < targetMaps.tileAttributeMatrixEditor.attributeMatrixHeight; j++) {
            if (targetMaps.tileAttributeMatrixEditor.hasAttribute(i, j, attribute)) {
               count++;

               Handles.DrawSolidRectangleWithOutline(
                  new Rect(new Vector3(i, j, 0),
                  Vector3.one),
                  new Color(1, 0, 1, _tileOverlayOpacity),
                  new Color(0.5f, 0, 0.5f, 1));
            }
         }
      }

      return count;
   }

   private void drawTileHoverBox (SceneView scene, Event e, Vector2Int hoveredTile, AssetSerializationMaps targetMaps) {
      Handles.color = Color.black;
      Handles.DrawSolidDisc(new Vector3(hoveredTile.x + 0.5f, hoveredTile.y + 0.5f, 0), Vector3.forward, 0.1f);
      Handles.DrawSolidRectangleWithOutline(
                  new Rect(new Vector3(hoveredTile.x, hoveredTile.y, 0),
                  Vector3.one),
                  new Color(0, 0, 0, 0),
                  new Color(0, 0, 0, 1));

      Handles.DrawSolidRectangleWithOutline(
                  new Rect(new Vector3(hoveredTile.x + 0.75f, hoveredTile.y + 0.25f - 3f, 0),
                  new Vector2(2f, 3f)),
                  new Color(0, 0, 0, 0.75f),
                  new Color(0, 0, 0, 1));

      string text = "";
      foreach (TileAttributes.Type t in (TileAttributes.Type[]) Enum.GetValues(typeof(TileAttributes.Type))) {
         if (targetMaps.tileAttributeMatrixEditor.hasAttribute(hoveredTile.x, hoveredTile.y, t)) {
            text += t.ToString() + Environment.NewLine;
         }
      }

      if (text.Length == 0) {
         text = "No\nAttributes\nApplied";
      }

      GUIStyle style = EditorStyles.boldLabel;

      var prevFont = style.fontSize;
      var prevAlign = style.alignment;
      var prevColor = style.normal.textColor;

      style.fontSize = 1 + (int) (64 / scene.camera.orthographicSize);
      style.alignment = TextAnchor.LowerLeft;
      Vector3 worldPos = (Vector3) (Vector2) hoveredTile + Vector3.up * 0.25f + Vector3.right * 0.85f;

      style.normal.textColor = Color.white;
      Handles.Label(worldPos, text, style);

      style.fontSize = prevFont;
      style.alignment = prevAlign;
      style.normal.textColor = prevColor;
   }


   #region Private Variables

   // Should we enable the custom editor
   private bool _enableEditor = false;

   // The target we are currently editing
   private AssetSerializationMaps _target;

   // Currently selected attribute
   private TileAttributes.Type _selectedAttribute;

   // Opacity of the square which indicates if a particular tile has an attribute
   private float _tileOverlayOpacity = 0.4f;

   // Previously hovered tile
   private Vector2Int _previousHover;

   // How many tiles have the selected attribute
   private int _tilesWithSelectedAttribute = 0;

   #endregion
}