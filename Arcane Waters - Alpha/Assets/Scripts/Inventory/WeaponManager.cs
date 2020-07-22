using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class WeaponManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public List<WeaponLayer> weaponsLayers = new List<WeaponLayer>();

   // The unique database inventory id of the weapon
   [SyncVar]
   public int equippedWeaponId;

   // The Sprite Id
   [SyncVar]
   public int weaponType = 0;

   // Equipment sql Id
   [SyncVar]
   public int equipmentDataId = 0;

   // Weapon colors
   [SyncVar]
   public string palette1;
   [SyncVar]
   public string palette2;

   // The type of action this weapon is associated with
   [SyncVar]
   public Weapon.ActionType actionType = Weapon.ActionType.None;

   // The action value of the action type
   [SyncVar]
   public int actionTypeValue;

   // The current weapon data
   public WeaponStatData cachedWeaponData;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         Util.setAlpha(weaponLayer.getRenderer().material, (hasWeapon() ? bodySprite.color.a : 0f));
      }
   }

   public bool hasWeapon () {
      return (weaponType != 0);
   }

   public Weapon getWeapon () {
      return WeaponStatData.translateDataToWeapon(cachedWeaponData);
   }

   public void updateSprites () {
      this.updateSprites(this.weaponType, this.palette1, this.palette2);
   }

   public void updateSprites (int weaponType, string newPalette1, string newPalette2) {
      Gender.Type gender = getGender();

      // Update our Material
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         weaponLayer.setType(gender, weaponType);
         weaponLayer.recolor(newPalette1, newPalette2);
      }

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   public bool isHoldingWeapon () {
      if (WeaponStatData.translateDataToWeapon(cachedWeaponData).getDamage() != 0) {
         return true;
      }

      return false;
   }

   [ClientRpc]
   public void Rpc_EquipWeapon (string rawReaponData, string newPalette1, string newPalette2) {
      WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(rawReaponData);
      cachedWeaponData = weaponData;

      // Update the sprites for the new weapon type
      int newType = weaponData == null ? 0 : weaponData.weaponType;
      updateSprites(newType, newPalette1, newPalette2);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateWeaponSyncVars (int weaponDataId, int weaponId) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponDataId);

      if (weaponData == null) {
         D.debug("Weapon is null");
         weaponData = WeaponStatData.getDefaultData();
      }

      if (weaponData.weaponType == 0) {
         // No weapon to equip
         equippedWeaponId = 0;
         weaponType = 0;
         updateSprites(0, "", "");
         return;
      }

      actionTypeValue = weaponData.actionTypeValue;
      cachedWeaponData = weaponData;

      // Assign the weapon ID
      this.equippedWeaponId = weaponId;

      // Set the Sync Vars so they get sent to the clients
      this.equipmentDataId = weaponData.sqlId;
      this.weaponType = weaponData.weaponType;
      this.palette1 = weaponData.palette1;
      this.palette2 = weaponData.palette2;
      this.actionType = weaponData == null ? Weapon.ActionType.None : weaponData.actionType;

      // Send the Weapon Info to all clients
      Rpc_EquipWeapon(WeaponStatData.serializeWeaponStatData(weaponData), palette1, palette2);
   }

   #region Private Variables

   #endregion
}
