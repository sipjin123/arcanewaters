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

   // The equipped weapon id
   [SyncVar]
   public int equippedWeaponId;

   // Weapon Type
   [SyncVar]
   public int weaponType = 0;

   // Equipment ID
   [SyncVar]
   public int equipmentDataID = 0;

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
      return _weapon;
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
      if (_weapon.getDamage() != 0) {
         return true;
      }

      return false;
   }

   [ClientRpc]
   public void Rpc_EquipWeapon (string rawReaponData, string newPalette1, string newPalette2) {
      WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(rawReaponData);
      Weapon newWeapon = WeaponStatData.translateDataToWeapon(weaponData);
      _weapon = newWeapon;

      updateSprites(newWeapon.itemTypeId, newPalette1, newPalette2);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateWeaponSyncVars (Weapon weapon) {
      if (weapon == null) {
         D.debug("Weapon is null");
         return;
      }

      if (weapon.itemTypeId == 0) {
         // No weapon to equip
         equippedWeaponId = 0;
         weaponType = 0;
         updateSprites(0, "", "");
         return;
      }

      D.editorLog("The weapon Manager is fetching Weapon data of: " + weapon.itemTypeId, Color.magenta);
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
      if (weaponData != null) {
         weaponData.itemSqlId = weapon.id;
      } else {
         weaponData = WeaponStatData.getDefaultData();
      }

      actionTypeValue = weaponData.actionTypeValue;
      _weapon = weapon;

      // Assign the weapon ID
      this.equippedWeaponId = (weapon.itemTypeId == 0) ? 0 : weapon.id;

      // Set the Sync Vars so they get sent to the clients
      this.weaponType = weaponData.weaponType;
      this.palette1 = weapon.paletteName1;
      this.palette2 = weapon.paletteName2;
      this.actionType = weaponData == null ? Weapon.ActionType.None : weaponData.actionType;

      // Send the Weapon Info to all clients
      Rpc_EquipWeapon(WeaponStatData.serializeWeaponStatData(weaponData), palette1, palette2);
   }

   #region Private Variables

   // The equipped weapon, if any
   protected Weapon _weapon = new Weapon (0, 0);

   #endregion
}
