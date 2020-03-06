using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PlayerBodyEntity : BodyEntity
{
   #region Public Variables

   #endregion

   protected override void Update () {
      base.Update();

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();

      if (!isLocalPlayer || !Util.isGeneralInputAllowed()) {
         return;
      }

      // Allow right clicking people to bring up their info, only if no panel is opened
      if (Input.GetMouseButtonUp(1) && !PanelManager.self.hasPanelInStack()) {
         PlayerBodyEntity body = getClickedBody();
         if (body != null) {
            this.rpc.Cmd_RequestCharacterInfoFromServer(body.userId);
         }
      }

      if (Input.GetKeyUp(KeyCode.Alpha1)) {
         Cmd_EquipWeapon(1);
      } else if (Input.GetKeyUp(KeyCode.Alpha2)) {
         Cmd_EquipWeapon(2);
      } else if (Input.GetKeyUp(KeyCode.Alpha3)) {
         Cmd_EquipWeapon(3);
      }

      if (InputManager.isActionKeyPressed()) {
         if (facing == Direction.East || facing == Direction.SouthEast || facing == Direction.NorthEast
            || facing == Direction.West || facing == Direction.SouthWest || facing == Direction.NorthWest) {
            rpc.Cmd_InteractAnimation(Anim.Type.Interact_East);
         } else if (facing == Direction.North) {
            rpc.Cmd_InteractAnimation(Anim.Type.Interact_North);
         } else if (facing == Direction.South) {
            rpc.Cmd_InteractAnimation(Anim.Type.Interact_South);
         }
      }
   }

   [Command]
   public void Cmd_EquipWeapon (int keyNumber) {
      List<Weapon> weaponList = new List<Weapon>();
      Weapon newWeapon = new Weapon();

      // Look up the weapons that this user has in the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         weaponList = DB_Main.getWeaponsForUser(this.userId);

         foreach (Weapon weapon in weaponList) {
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
            if (keyNumber == 1 && weaponData.actionType == Weapon.ActionType.PlantCrop) {
               newWeapon = weapon;
            }
            if (keyNumber == 2 && weaponData.actionType == Weapon.ActionType.WaterCrop) {
               newWeapon = weapon;
            }
            if (keyNumber == 3 && weaponData.actionType == Weapon.ActionType.HarvestCrop) {
               newWeapon = weapon;
            }
         }

         if (newWeapon.id > 0) {
            DB_Main.setWeaponId(this.userId, newWeapon.id);
         }

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (newWeapon.itemTypeId != 0) {
               this.weaponManager.updateWeaponSyncVars(newWeapon);
            }
         });
      });
   }

   [Command]
   public void Cmd_ToggleClothes () {
      // If they currently have clothes on, then take them off
      if (armorManager.hasArmor()) {
         // DB thread to remove the current armor
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.setArmorId(this.userId, 0);

            // Back to Unity
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               armorManager.updateArmorSyncVars(new Armor());
            });
         });
      } else {
         // Look up our most recent armor in the database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<Armor> armorList = DB_Main.getArmorForUser(this.userId);
            if (armorList.Count > 0) {
               Armor armor = armorList.Last();
               DB_Main.setArmorId(this.userId, armor.id);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  armorManager.updateArmorSyncVars(armor);
               });
            }
         });
      }
   }

   protected PlayerBodyEntity getClickedBody () {
      float closest = float.MaxValue;
      PlayerBodyEntity clickedBody = null;

      // Cycle through all of the player bodies we know about
      foreach (PlayerBodyEntity body in FindObjectsOfType<PlayerBodyEntity>()) {
         Vector2 screenPos = Camera.main.WorldToScreenPoint(body.transform.position);
         float distance = Vector2.Distance(screenPos, Input.mousePosition);

         // Check if this is the closest player we've found so far
         if (distance < closest) {
            clickedBody = body;
            closest = distance;
         }
      }

      // If no one was close enough, return null
      if (closest > 48f) {
         return null;
      }

      return clickedBody;
   }

   #region Private Variables

   #endregion
}
