using System.Collections.Generic;
using Mirror;

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

   // The durability of the weapons
   [SyncVar]
   public int weaponDurability = 0;

   // Weapon colors
   [SyncVar]
   public string palettes;

   // The weapon count
   [SyncVar]
   public int count = 1;

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
         Util.setAlpha(weaponLayer.getRenderer().material, (hasWeapon() ? bodySprite.material.GetColor("_Color").a : 0f));
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

      // Update the alpha value of the weapon layers depending if isHidden value is true or false
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         Util.setAlpha(weaponLayer.getRenderer().material, isHidden ? 0f : 1f);
      }
   }


   [TargetRpc]
   public void Target_EquipWeapon(NetworkConnection connection, int newWeaponId, int newWeaponSqlId, int newWeaponType, string rawWeaponData, string newPalettes, int count) {
      WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(rawWeaponData);
      cachedWeaponData = weaponData;

      // Update the sprites for the new weapon type
      updateSprites(newWeaponType, newPalettes);
      D.adminLog("Equipped weapon" + " SQL: {" + weaponData.sqlId +
         "} Name: {" + weaponData.equipmentName +
         "} Type: {" + weaponData.weaponType +
         "} Class: {" + weaponData.weaponClass + "}", D.ADMIN_LOG_TYPE.Equipment);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);

      Global.getUserObjects().weapon = new Weapon {
         id = newWeaponId,
         category = Item.Category.Weapon,
         itemTypeId = newWeaponSqlId,
         count = count
      };
   }

   [ClientRpc]
   public void Rpc_BroadcastEquipWeapon (string rawWeaponData, string newPalettes) {
      WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(rawWeaponData);
      cachedWeaponData = weaponData;

      // Update the sprites for the new weapon type
      int newType = weaponData == null ? 0 : weaponData.weaponType;
      updateSprites(newType, newPalettes);
   }
   
   public void updateDurability (int newDurability) {
      D.adminLog("Weapon durability modified from [" + weaponDurability + "] to [" + newDurability + "]", D.ADMIN_LOG_TYPE.Refine);
      weaponDurability = newDurability;
   }

   [Server]
   public void updateWeaponSyncVars (int weaponDataId, int weaponId, string palettes, int durability, int count) {
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
      this.weaponDurability = durability;
      this.count = count;

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

      // Send the weapon info to the owner client
      Target_EquipWeapon(connection, weaponId, weaponData.sqlId, weaponData.weaponType, WeaponStatData.serializeWeaponStatData(weaponData), this.palettes, count);

      // Send the Weapon Info to all clients
      Rpc_BroadcastEquipWeapon(WeaponStatData.serializeWeaponStatData(weaponData), this.palettes);
   }

   #region Private Variables

   #endregion
}
