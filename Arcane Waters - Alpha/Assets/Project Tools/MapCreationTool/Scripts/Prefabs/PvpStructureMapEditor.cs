using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;
using System;

namespace MapCreationTool {
   public class PvpStructureMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      #region Public Variables

      // The pvp related variables being set in the map editor
      public Text pvpLaneText;
      public Text pvpLaneNumberText;
      public Text pvpTeamTypeText;

      // Scale of this object when displayed in the palette panel
      public const float PALETTE_SCALE = 5f;

      // The starting scale of the model if its not fixed to one
      public float startingScale = 1;

      #endregion

      private void Awake () {
         _outline = GetComponentInChildren<SpriteOutline>();
         startingScale = transform.localScale.x;
      }

      public void dataFieldChanged (DataField field) {
         if (this is PvpShipyardEditor || this is PvpBaseEditor || this is PvpTowerMapEditor) {
            // Adjust the scale of this building when spawned in the drawing board
            if (transform.parent.GetComponent<Palette>() != null) {
               transform.localScale = new Vector3(startingScale / PALETTE_SCALE, startingScale / PALETTE_SCALE, 1);
            } else {
               transform.localScale = new Vector3(startingScale, startingScale, startingScale);
            }
         }

         if (field.k.CompareTo(DataField.PVP_LANE) == 0) {
            try {
               string pvpLane = Enum.Parse(typeof(PvpLane), field.v).ToString();
               pvpLaneText.text = pvpLane;
            } catch {

            }
         }
         if (field.k.CompareTo(DataField.PVP_LANE_NUMBER) == 0) {
            try {
               string pvpLaneNumber = field.v.ToString();
               pvpLaneNumberText.text = pvpLaneNumber;
            } catch {

            }
         }
         if (field.k.CompareTo(DataField.PVP_TEAM_TYPE) == 0) {
            try {
               string pvpTeam = Enum.Parse(typeof(PvpTeamType), field.v).ToString();
               pvpTeamTypeText.text = pvpTeam;
            } catch {

            }
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(_outline, hovered, selected, deleting);
      }

      #region Private Variables

      // The outline
      private SpriteOutline _outline;

      #endregion
   }
}