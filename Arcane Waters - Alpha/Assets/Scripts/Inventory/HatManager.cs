using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class HatManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public HatLayer hatLayer;

   // The unique database inventory id of the hat
   [SyncVar]
   public int equippedHatId;

   // The Sprite Id
   [SyncVar]
   public int hatType = 0;

   // Equipment sql Id
   [SyncVar]
   public int equipmentDataId = 0;

   // Hat colors
   [SyncVar]
   public string palettes;

   // The current hat data
   public HatStatData cachedHatData;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      if (hatLayer != null) {
         Util.setAlpha(hatLayer.getRenderer().material, (hasHat() ? bodySprite.material.GetColor("_Color").a : 0f));
      }
   }

   public bool hasHat () {
      return (hatType != 0);
   }

   public Hat getHat () {
      if (cachedHatData != null) {
         return HatStatData.translateDataToHat(cachedHatData);
      }

      return new Hat(0, 0, "");
   }

   public void updateSprites () {
      this.updateSprites(this.hatType, this.palettes);
   }

   public void updateSprites (int hatType, string palettes) {
      Gender.Type gender = getGender();

      // Set the correct sheet for our gender and hat type
      hatLayer.setType(gender, hatType);

      // Update our Material
      hatLayer.recolor(palettes);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }

      if (_battler != null) {
         _battler.syncAnimations();
      }
   }

   [TargetRpc]
   public void Rpc_EquipHat (NetworkConnection connection, string rawHatData, string palettes) {
      HatStatData hatData = Util.xmlLoad<HatStatData>(rawHatData);
      cachedHatData = hatData;

      // Update the sprites for the new hat type
      int newType = hatData == null ? 0 : hatData.hatType;
      updateSprites(newType, palettes);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);

      Global.getUserObjects().hat = new Hat {
         id = equippedHatId,
         category = Item.Category.Hats,
         itemTypeId = hatType
      };
   }

   [ClientRpc]
   public void Rpc_BroadcastEquipHat (string rawHatData, string palettes) {
      if (rawHatData.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
         HatStatData hatData = Util.xmlLoad<HatStatData>(rawHatData);
         cachedHatData = hatData;

         // Update the sprites for the new hat type
         int newType = hatData == null ? 0 : hatData.hatType;
         updateSprites(newType, palettes);
      }
   }

   [Server]
   public void updateHatSyncVars (int hatDataId, int hatId) {
      HatStatData hatData = EquipmentXMLManager.self.getHatData(hatDataId);

      if (hatData == null) {
         hatData = HatStatData.getDefaultData();
      }

      this.cachedHatData = hatData;

      // Assign the hat ID
      this.equippedHatId = hatId;

      // Set the Sync Vars so they get sent to the clients
      this.equipmentDataId = hatData.sqlId;
      this.hatType = hatData.hatType;
      this.palettes = hatData.palettes;

      NetworkConnection connection = null;

      if (_body != null) {
         connection = _body.connectionToClient;
      }

      if (_battler != null && _battler.player != null) {
         connection = _battler.player.connectionToClient;
      }

      if (connection == null) {
         D.debug("Connection to client was null!");
         return;
      }

      Rpc_EquipHat(connection, HatStatData.serializeHatStatData(hatData), palettes);

      // Send the Info to all clients
      Rpc_BroadcastEquipHat(HatStatData.serializeHatStatData(hatData), palettes);
   }

   #region Private Variables

   #endregion
}
