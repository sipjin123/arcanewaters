using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PlayerBodyEntity : BodyEntity
{
   #region Public Variables

   // The farming trigger component used for detecting crops
   public FarmingTrigger farmingTrigger;

   // Max collision check around the player
   public static int MAX_COLLISION_COUNT = 32;

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
         bool isNearInteractables = false;
         float overlapRadius = .5f;
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, overlapRadius);
         List<Enemy> enemiesNearby = new List<Enemy>();
         List<NPC> npcsNearby = new List<NPC>();
         List<TreasureChest> treasuresNearby = new List<TreasureChest>();

         int currentCount = 0;
         if (hits.Length > 0) {
            foreach (Collider2D hit in hits) {
               if (currentCount > MAX_COLLISION_COUNT) {
                  break;
               }
               currentCount++;

               if (hit.GetComponent<Enemy>() != null) {
                  enemiesNearby.Add(hit.GetComponent<Enemy>());
               }

               if (hit.GetComponent<NPC>() != null) {
                  npcsNearby.Add(hit.GetComponent<NPC>());
               }

               if (hit.GetComponent<TreasureChest>() != null) {
                  treasuresNearby.Add(hit.GetComponent<TreasureChest>());
               }

               if (treasuresNearby.Count > 0 || enemiesNearby.Count > 0 || npcsNearby.Count > 0) { 
                  // Prevent the player from playing attack animation when interacting NPC's / Enemies / Loot Bags
                  isNearInteractables = true;
               }
            }

            // Check first if there are enemies nearby
            engageNearestEnemy(enemiesNearby);

            // If there are no enemies, loot the nearest lootbag/treasure chest
            if (enemiesNearby.Count < 1) {
               interactNearestLoot(treasuresNearby);
            }

            // If there are no loots or enemies nearby, interact with nearest npc
            if (treasuresNearby.Count < 1 && enemiesNearby.Count < 1) {
               interactNearestNpc(npcsNearby);
            }
         }

         if (!isNearInteractables) {
            if (facing == Direction.East || facing == Direction.SouthEast || facing == Direction.NorthEast
               || facing == Direction.West || facing == Direction.SouthWest || facing == Direction.NorthWest) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_East);
            } else if (facing == Direction.North) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_North);
            } else if (facing == Direction.South) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_South);
            }

            farmingTrigger.interactFarming();
         }
      }
   }

   private void engageNearestEnemy (List<Enemy> enemyList) {
      Enemy targetEnemy = null;

      float nearestTargetDistance = 10;
      foreach (Enemy enemy in enemyList) {
         float newTargetDistance = Vector2.Distance(transform.position, enemy.transform.position);
         if (newTargetDistance < nearestTargetDistance && !enemy.isDefeated) {
            nearestTargetDistance = newTargetDistance;
            targetEnemy = enemy;
         }
      }

      if (targetEnemy != null) {
         targetEnemy.clientClickedMe();
      } else {
         enemyList.Clear();
      }
   }

   private void interactNearestLoot (List<TreasureChest> chestList) {
      TreasureChest targetChest = null;

      float nearestTargetDistance = 10;
      foreach (TreasureChest chest in chestList) {
         float newTargetDistance = Vector2.Distance(transform.position, chest.transform.position);
         if (newTargetDistance < nearestTargetDistance && !chest.hasBeenOpened()) {
            nearestTargetDistance = newTargetDistance;
            targetChest = chest;
         }
      }

      if (targetChest != null) {
         targetChest.sendOpenRequest();
      } else {
         chestList.Clear();
      }
   }

   private void interactNearestNpc (List<NPC> npcList) {
      NPC targetNpc = null;

      float nearestTargetDistance = 10;
      foreach (NPC npc in npcList) {
         float newTargetDistance = Vector2.Distance(transform.position, npc.transform.position);
         if (newTargetDistance < nearestTargetDistance) {
            nearestTargetDistance = newTargetDistance;
            targetNpc = npc;
         }
      }

      if (targetNpc != null) {
         targetNpc.clientClickedMe();
      } else {
         npcList.Clear();
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
