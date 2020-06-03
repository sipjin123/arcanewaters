using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class HatManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public HatLayer hatLayer;

   [SyncVar]
   public int equippedHatId;

   // Hat Type
   [SyncVar]
   public int hatType = 0;

   // Hat colors
   [SyncVar]
   public string palette1;
   [SyncVar]
   public string palette2;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      if (hatLayer != null) {
         Util.setAlpha(hatLayer.getRenderer().material, (hasHat() ? bodySprite.color.a : 0f));
      }
   }

   public bool hasHat () {
      return (hatType != 0);
   }

   public Hats getHat () {
      if (_hat != null) {
         return _hat;
      }

      return new Hats(0, 0, "", "");
   }

   public void updateSprites () {
      this.updateSprites(this.hatType, this.palette1, this.palette2);
   }

   public void updateSprites (int hatType, string palette1, string palette2) {
      Gender.Type gender = getGender();

      // Set the correct sheet for our gender and hat type
      hatLayer.setType(gender, hatType);

      // Update our Material
      hatLayer.recolor(palette1, palette2);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   [ClientRpc]
   public void Rpc_EquipHat (string rawHatData, string palette1, string palette2) {
      HatStatData hatData = Util.xmlLoad<HatStatData>(rawHatData);
      Hats newHat = HatStatData.translateDataToHat(hatData);
      _hat = newHat;

      // Update the sprites for the new hat type
      int newType = newHat == null ? 0 : newHat.itemTypeId;
      updateSprites(newType, palette1, palette2);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateHatSyncVars (Hats newHat) {
      if (newHat == null) {
         D.debug("Hat is null");
         return;
      }

      if (newHat.itemTypeId == 0) {
         // No Hat to equip
         return;
      }

      HatStatData hatData = EquipmentXMLManager.self.getHatData(newHat.itemTypeId);
      if (hatData != null) {
         hatData.itemSqlId = newHat.id;
      } else {
         hatData = HatStatData.getDefaultData();
      }

      _hat = newHat;

      // Assign the hat ID
      this.equippedHatId = (newHat.itemTypeId == 0) ? 0 : newHat.id;

      // Set the Sync Vars so they get sent to the clients
      this.hatType = hatData.hatType;
      this.palette1 = hatData.palette1;
      this.palette2 = hatData.palette2;
      newHat.paletteName1 = hatData.palette1;
      newHat.paletteName2 = hatData.palette2;

      // Send the Info to all clients
      Rpc_EquipHat(HatStatData.serializeHatStatData(hatData), hatData.palette1, hatData.palette2);
   }

   #region Private Variables

   // The equipped hat, if any
   protected Hats _hat = new Hats(0, 0);

   #endregion
}
