using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class WeaponManager : EquipmentManager {
   #region Public Variables

   // The Layers we're interested in
   public List<WeaponLayer> weaponsLayers = new List<WeaponLayer>();

   // The equipped weapon id
   [SyncVar]
   public int equippedWeaponId;

   // Weapon Type
   [SyncVar]
   public Weapon.Type weaponType = Weapon.Type.None;

   // Weapon colors
   [SyncVar]
   public ColorType color1;
   [SyncVar]
   public ColorType color2;

   #endregion

   public void Update () {
      // If we don't have anything equipped, turn off the animated sprite
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         Util.setAlpha(weaponLayer.getRenderer().material, (hasWeapon() ? bodySprite.color.a : 0f));
      }
   }

   public bool hasWeapon () {
      return (weaponType != Weapon.Type.None);
   }

   public Weapon getWeapon () {
      return _weapon;
   }

   public void updateSprites () {
      this.updateSprites(this.weaponType, this.color1, this.color2);
   }

   public void updateSprites (Weapon.Type weaponType, ColorType color1, ColorType color2) {
      Gender.Type gender = getGender();

      // Update our Material
      foreach (WeaponLayer weaponLayer in weaponsLayers) {
         weaponLayer.setType(gender, weaponType);
         ColorKey colorKey = new ColorKey(gender, weaponType);
         weaponLayer.recolor(colorKey, color1, color2);
      }

      // Sync up all our animations
      if (_body != null) {
         _body.restartAnimations();
      }
   }

   public bool isHoldingSword () {
      return weaponType.ToString().ToLowerInvariant().Contains("sword");
   }
   public bool isHoldingGun () {
      return weaponType.ToString().ToLowerInvariant().Contains("gun");
   }

   [ClientRpc]
   public void Rpc_EquipWeapon (Weapon newWeapon, ColorType color1, ColorType color2) {
      _weapon = newWeapon;
      updateSprites(newWeapon.type, color1, color2);

      // Play a sound
      SoundManager.create3dSound("equip_", this.transform.position, 2);
   }

   [Server]
   public void updateWeaponSyncVars (Weapon weapon) {
      _weapon = weapon;

      // Assign the weapon ID
      this.equippedWeaponId = (weapon.type == Weapon.Type.None) ? 0 : weapon.id;

      // Set the Sync Vars so they get sent to the clients
      this.weaponType = weapon.type;
      this.color1 = weapon.color1;
      this.color2 = weapon.color2;

      // Send the Weapon Info to all clients
      Rpc_EquipWeapon(weapon, color1, color2);
   }

   #region Private Variables

   // The equipped weapon, if any
   protected Weapon _weapon;

   #endregion
}
