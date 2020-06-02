using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class HelmManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public HelmLayer headGearLayer;

   [SyncVar]
   public int equippedHelmId;

   // Helm Type
   [SyncVar]
   public int headgearType = 0;

   // Helm colors
   [SyncVar]
   public string palette1;
   [SyncVar]
   public string palette2;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      if (headGearLayer != null) {
         Util.setAlpha(headGearLayer.getRenderer().material, (hasHelm() ? bodySprite.color.a : 0f));
      }
   }

   public bool hasHelm () {
      return (headgearType != 0);
   }

   public Helm getHelm () {
      if (_helm != null) {
         return _helm;
      }

      return new Helm(0, 0, "", "");
   }

   public void updateSprites () {
      this.updateSprites(this.headgearType, this.palette1, this.palette2);
   }

   public void updateSprites (int helmType, string palette1, string palette2) {
      Gender.Type gender = getGender();

      // Set the correct sheet for our gender and helm type
      headGearLayer.setType(gender, helmType);

      // Update our Material
      headGearLayer.recolor(palette1, palette2);

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   [ClientRpc]
   public void Rpc_EquipHelm (string rawHelmData, string palette1, string palette2) {
      HelmStatData helmData = Util.xmlLoad<HelmStatData>(rawHelmData);
      Helm newHelm = HelmStatData.translateDataToHelm(helmData);
      _helm = newHelm;

      // Update the sprites for the new helm type
      int newType = newHelm == null ? 0 : newHelm.itemTypeId;
      updateSprites(newType, palette1, palette2);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateHelmSyncVars (Helm newHeadgear) {
      if (newHeadgear == null) {
         D.debug("Helm is null");
         return;
      }

      if (newHeadgear.itemTypeId == 0) {
         // No Helm to equip
         return;
      }

      HelmStatData helmData = EquipmentXMLManager.self.getHelmData(newHeadgear.itemTypeId);
      if (helmData != null) {
         helmData.itemSqlId = newHeadgear.id;
      } else {
         helmData = HelmStatData.getDefaultData();
      }

      _helm = newHeadgear;

      // Assign the helm ID
      this.equippedHelmId = (newHeadgear.itemTypeId == 0) ? 0 : newHeadgear.id;

      // Set the Sync Vars so they get sent to the clients
      this.headgearType = helmData.helmType;
      this.palette1 = helmData.palette1;
      this.palette2 = helmData.palette2;
      newHeadgear.paletteName1 = helmData.palette1;
      newHeadgear.paletteName2 = helmData.palette2;

      // Send the Info to all clients
      Rpc_EquipHelm(HelmStatData.serializeHelmStatData(helmData), helmData.palette1, helmData.palette2);
   }

   #region Private Variables

   // The equipped helm, if any
   protected Helm _helm = new Helm(0, 0);

   #endregion
}
