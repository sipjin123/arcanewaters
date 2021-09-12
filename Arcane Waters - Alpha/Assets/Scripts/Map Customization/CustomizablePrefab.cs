using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine;

namespace MapCustomization
{
   public class CustomizablePrefab : ClientMonoBehaviour, IMapEditorDataReceiver
   {
      #region Public Variables

      // Bounds in which the mouse cursor has to be to interact with the prefab
      public Collider2D interactionCollider;

      // Size of prefab in tiles
      public Vector2Int size = Vector2Int.one;

      // Which type of map the prefab is used in
      public EditorType editorType = EditorType.Area;

      // Is prefab's state permanent
      public bool isPermanent;

      // ID of the prop item definition that corresponds to this prefab
      public int propDefinitionId;

      // State of the prefab that is set in map editor
      public PrefabState mapEditorState;

      // State of the prefab after server-confirmed customizations are applied
      public PrefabState customizedState;

      // Changes of state during user-end customization
      public PrefabState unappliedChanges;

      // Is this prefab replaced by a variation?
      public bool spawnedAPrefabVariation = false;

      // Is this a variation spawned from another prefab?
      public bool variationSpawnedFromPrefab = false;

      #endregion

      private void OnEnable () {
         _ren = GetComponent<SpriteRenderer>();
      }

      public void setOutline (bool ready, bool hovered, bool selected, bool valid) {
         if (_spriteOutline == null) {
            _spriteOutline = gameObject.AddComponent<SpriteOutline>();
         }

         _spriteOutline.setNewColor(getOutlineColor(ready, hovered, selected, valid));

         if (hovered || selected) {
            // Visibility is set to false to permenantly disable outlines in the customization mode
            _spriteOutline.setVisibility(false);
            if (GetComponentInChildren<SelectionSpriteBuildMode>() != null) {
               MapCustomizationManager.showSelectionArrows();
            }
         } else {
            _spriteOutline.setVisibility(false);
            if (GetComponentInChildren<SelectionSpriteBuildMode>() != null) {
               MapCustomizationManager.hideSelectionArrows();
            }
         }
      }

      private Color getOutlineColor (bool ready, bool hovered, bool selected, bool valid) {
         if (!ready) {
            return new Color(0, 0, 0, 0f);
         } else if (selected) {
            if (valid) {
               return MapCustomizationManager.self.prefabValidColor;
            } else {
               return MapCustomizationManager.self.prefabInvalidColor;
            }
         } else if (hovered) {
            return MapCustomizationManager.self.prefabHoveredColor;
         }

         return MapCustomizationManager.self.prefabReadyColor;
      }

      public void setGameInteractionsActive (bool active) {
         if (active) {
            foreach (Behaviour interaction in _disabledInteractions) {
               interaction.enabled = true;
            }
            _disabledInteractions.Clear();
         } else {
            _disabledInteractions = GetComponentsInChildren<Collider2D>().Where(c => !c.isTrigger).Select(c => c as Behaviour).ToList();
            foreach (Behaviour interaction in _disabledInteractions) {
               interaction.enabled = false;
            }
         }
      }

      public bool interactionOverlaps (Vector2 prefabPosition, Vector2 pointPosition, float minMargin) {
         if (interactionCollider == null) {
            D.error("Interaction collider not set");
            return false;
         }

         if (interactionCollider is CircleCollider2D) {
            return (pointPosition - prefabPosition).sqrMagnitude < Mathf.Pow(minMargin + (interactionCollider as CircleCollider2D).radius, 2);
         }

         if (interactionCollider is BoxCollider2D) {
            BoxCollider2D c = interactionCollider as BoxCollider2D;
            Vector2 min = prefabPosition + (c.offset - c.size * 0.5f) * transform.localScale - Vector2.one * minMargin;
            Vector2 max = prefabPosition + (c.offset + c.size * 0.5f) * transform.localScale + Vector2.one * minMargin;
            return pointPosition.x >= min.x && pointPosition.y >= min.y && pointPosition.x < max.x && pointPosition.y < max.y;
         }

         D.error($"Handling of collider type { interactionCollider.GetType().Name } not implemented.");
         return false;
      }

      public bool anyUnappliedState () {
         return unappliedChanges.isLocalPositionSet() || unappliedChanges.created || unappliedChanges.deleted;
      }

      public void revertUnappliedChanges () {
         transform.localPosition = customizedState.localPosition;
         GetComponent<ZSnap>()?.snapZ();

         if (!mapEditorState.created && !customizedState.created) {
            MapCustomizationManager.removeTracked(this);
            Destroy(gameObject);
         } else {
            unappliedChanges.clearAll();
         }
      }

      public void revertToMapEditor () {
         if (!mapEditorState.created) {
            D.error("Cannot revert changes to map editor, because prefab was not created in map editor");
            return;
         }

         transform.localPosition = mapEditorState.localPosition;

         customizedState = mapEditorState;

         unappliedChanges.clearAll();
      }

      public void submitUnappliedChanges () {
         if (unappliedChanges.deleted) {
            MapCustomizationManager.removeTracked(this);
            Destroy(gameObject);
            return;
         }

         if (unappliedChanges.isLocalPositionSet()) {
            customizedState.localPosition = unappliedChanges.localPosition;
            customizedState.created = customizedState.created || unappliedChanges.created;
         }

         revertUnappliedChanges();
      }

      public void receiveData (DataField[] dataFields) {
         foreach (DataField field in dataFields) {
            if (field.k.Equals(DataField.IS_PERMANENT_KEY)) {
               if (bool.TryParse(field.v, out bool value)) {
                  isPermanent = value;
               }
            }
         }
      }

      #region Private Variables

      // Main Sprite Renderer of the prefab
      private SpriteRenderer _ren;

      // Game interactions that are currently disabled in map customization process
      private List<Behaviour> _disabledInteractions = new List<Behaviour>();

      // Outline that is used for visual indication during customization process
      private SpriteOutline _spriteOutline;

      #endregion
   }
}
