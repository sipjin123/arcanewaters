﻿using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpawnMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      // How much space should there be left between warps and spawns
      public const float SPACING_FROM_WARPS = 1f;

      [SerializeField]
      private RectTransform boundsRect = null;
      [SerializeField]
      private Text text = null;

      public string spawnName { get; private set; }

      private float width = 1f;
      private float height = 1f;

      // The spawn id autogenerated in the map editor
      public int spawnId = 0;

      // Where the user is facing after spawning
      public Direction arriveFacing;

      public Vector2 size
      {
         get { return new Vector2(width, height); }
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SPAWN_WIDTH_KEY) == 0) {
            if (field.tryGetFloatValue(out float w)) {
               width = Mathf.Clamp(w, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (field.k.CompareTo(DataField.SPAWN_HEIGHT_KEY) == 0) {
            if (field.tryGetFloatValue(out float h)) {
               height = Mathf.Clamp(h, 0.1f, 100);
               updateBoundsSize();
            }
         } else if (field.k.CompareTo(DataField.SPAWN_NAME_KEY) == 0) {
            spawnName = field.v;
            updateText();
         } else if (field.k.CompareTo(DataField.PLACED_PREFAB_ID) == 0) {
            string id = field.v.Trim(' ');
            spawnId = int.Parse(id);
         } else if (field.k.CompareTo(DataField.WARP_ARRIVE_FACING_KEY) == 0) {
            string rawData = field.v.Trim(' ');
            Direction parsedEnum = (Direction) System.Enum.Parse(typeof(Direction), rawData);
            arriveFacing = parsedEnum;
         }
      }

      public override void createdInPalette () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void createdForPreview () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void placedInEditor () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
         boundsRect.gameObject.SetActive(hovered || selected);
      }

      private void updateBoundsSize () {
         boundsRect.sizeDelta = new Vector2(width * 100, height * 100);
      }

      private void updateText () {
         text.text = spawnName;
      }
   }
}