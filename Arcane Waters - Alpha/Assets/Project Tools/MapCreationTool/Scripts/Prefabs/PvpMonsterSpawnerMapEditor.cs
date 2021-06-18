using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;
using System;
using System.Linq;

namespace MapCreationTool {
   public class PvpMonsterSpawnerMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {

      #region Public Variables

      // Displays the monster assigned to this spawner
      public SpriteRenderer monsterDisplay;

      // Displays the powerup assigned to this spawner
      public SpriteRenderer powerupDisplay;

      // The name of the powerup
      public Text powerupName;

      #endregion

      private void Awake () {
         _outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SEA_ENEMY_DATA_KEY) == 0) {
            try {
               SeaMonsterEntity.Type seaMonsterType = (SeaMonsterEntity.Type) Enum.Parse(typeof(SeaMonsterEntity.Type), field.v);
               SeaMonsterXMLContent monsterData = MonsterManager.instance.getSeaMonsters().Find(_ => _.seaMonsterData.seaMonsterType == seaMonsterType);
               monsterDisplay.sprite = ImageManager.getSprite(monsterData.seaMonsterData.avatarSpritePath);
            } catch {

            }
         }

         if (field.k.CompareTo(DataField.PVP_MONSTER_POWERUP) == 0) {
            try {
               Powerup.Type powerupType = (Powerup.Type) Enum.Parse(typeof(Powerup.Type), field.v);
               Sprite[] iconSprites = Resources.LoadAll<Sprite>(Powerup.ICON_SPRITES_LOCATION);
               powerupDisplay.sprite = iconSprites[(int) powerupType - 1];
               powerupName.text = powerupType.ToString();
            } catch {

            }
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         if (_outline != null) {
            setOutlineHighlight(_outline, hovered, selected, deleting);
         }
      }

      #region Private Variables

      // The outline
      private SpriteOutline _outline;

      #endregion
   }
}