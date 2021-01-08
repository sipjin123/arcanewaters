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
   public string palettes;

   // The type of action this weapon is associated with
   [SyncVar]
   public Weapon.ActionType actionType = Weapon.ActionType.None;

   // The action value of the action type
   [SyncVar]
   public int actionTypeValue;

   // The current weapon data
   public WeaponStatData cachedWeaponData;

   // If weapons are set as hidden
   public bool isHidden;

   #endregion

   public void Update () {
      if (isHidden) {
         return;
      }

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
      this.updateSprites(this.weaponType, this.palettes);
   }

   public void updateSprites (int weaponType, string newPalettes) {
      Gender.Type gender = getGender();

      // Update our Material
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         weaponLayer.setType(gender, weaponType);
         weaponLayer.recolor(newPalettes);
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
   public void Rpc_HideWeapons (bool isHidden) {
      this.isHidden = isHidden;

      // If we don't have anything equipped, turn off the animated sprite
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         Util.setAlpha(weaponLayer.getRenderer().material, isHidden ? 0f : 1f);
      }
   }

   [ClientRpc]
   public void Rpc_EquipWeapon (string rawReaponData, string newPalettes) {
      WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(rawReaponData);
      cachedWeaponData = weaponData;

      // Update the sprites for the new weapon type
      int newType = weaponData == null ? 0 : weaponData.weaponType;
      updateSprites(newType, newPalettes);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateWeaponSyncVars (int weaponDataId, int weaponId, string palettes) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponDataId);

      if (weaponData == null) {
         weaponData = WeaponStatData.getDefaultData();
      }

      actionTypeValue = weaponData.actionTypeValue;
      cachedWeaponData = weaponData;

      // Assign the weapon ID
      this.equippedWeaponId = weaponId;

      // Set the Sync Vars so they get sent to the clients
      this.equipmentDataId = weaponData.sqlId;
      this.weaponType = weaponData.weaponType;
      this.palettes = palettes;
      this.actionType = weaponData == null ? Weapon.ActionType.None : weaponData.actionType;

      // Send the Weapon Info to all clients
      Rpc_EquipWeapon(WeaponStatData.serializeWeaponStatData(weaponData), this.palettes);
   }

   #region Private Variables

   #endregion
}
