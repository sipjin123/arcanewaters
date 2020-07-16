using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PlayerBodyEntity : BodyEntity {
   #region Public Variables

   // The farming trigger component used for detecting crops
   public FarmingTrigger farmingTrigger;

   // The mining trigger component used for interacting with ores
   public MiningTrigger miningTrigger;

   // Max collision check around the player
   public static int MAX_COLLISION_COUNT = 32;

   // Speedup variables
   public float speedMeter = 10;
   public static float SPEEDUP_METER_MAX = 10;
   public bool isReadyToSpeedup = true;
   public float fuelDepleteValue = 2;
   public float fuelRecoverValue = 1.2f;

   // The effect that indicates this player is speeding up
   public GameObject speedUpEffect;
   public Canvas speedupGUI;
   public Transform speedupEffectPivot;
   public Image speedUpBar;

   // Color indications if the fuel is usable or not
   public Color recoveringColor, defaultColor;

   // Script handling the battle initializer
   public PlayerBattleCollider playerBattleCollider;

   // If the player is near an enemy
   public bool isWithinEnemyRadius;

   // The reference to the sprite animators
   public Animator[] animators;

   // The speedup value of the animation
   public static float ANIM_SPEEDUP_VALUE = 1.5f;

   // Strength of the jump
   public float jumpUpMagnitude = 1.2f;
   public float jumpDownMagnitude = 1.5f;

   // Max height of the jump
   public float jumpHeightMax = .15f;

   // Reference to the transform of the sprite holder
   public Transform spritesTransform;

   #endregion

   protected override void Awake () {
      base.Awake();
      speedMeter = SPEEDUP_METER_MAX;
   }

   protected override void FixedUpdate () {
      // Blocks user input and user movement if an enemy is within player collider radius
      if (!isWithinEnemyRadius) {
         base.FixedUpdate();
      }

      float y = spritesTransform.localPosition.y;
      if (isJumping) {
         y += y < jumpHeightMax ? Time.fixedDeltaTime * jumpUpMagnitude : 0;
      } else {
         y -= y > 0 ? Time.fixedDeltaTime * jumpDownMagnitude : 0;
      }
      spritesTransform.localPosition = new Vector3(spritesTransform.localPosition.x, y, spritesTransform.localPosition.z);
   }

   protected override void Update () {
      base.Update();

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();

      if (!isLocalPlayer || !Util.isGeneralInputAllowed()) {
         return;
      }

      foreach (Animator animator in animators) {
         animator.speed = 1;
      }

      // Allow right clicking people to bring up the context menu, only if no panel is opened
      if (Input.GetMouseButtonUp(1) && !PanelManager.self.hasPanelInStack()) {
         PlayerBodyEntity body = getClickedBody();
         if (body != null) {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(body.userId, body.entityName);
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
            requestAnimationPlay(Anim.Type.NC_Jump_East);
            rpc.Cmd_InteractAnimation(Anim.Type.NC_Jump_East);
         } else if (facing == Direction.North) {
            requestAnimationPlay(Anim.Type.NC_Jump_North);
            rpc.Cmd_InteractAnimation(Anim.Type.NC_Jump_North);
         } else if (facing == Direction.South) {
            requestAnimationPlay(Anim.Type.NC_Jump_South);
            rpc.Cmd_InteractAnimation(Anim.Type.NC_Jump_South);
         }
      }

      if (Input.GetKeyDown(KeyCode.Mouse1)) {
         bool isNearInteractables = false;
         float overlapRadius = .5f;
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, overlapRadius);
         List<NPC> npcsNearby = new List<NPC>();
         List<TreasureChest> treasuresNearby = new List<TreasureChest>();

         int currentCount = 0;
         if (hits.Length > 0) {
            foreach (Collider2D hit in hits) {
               if (currentCount > MAX_COLLISION_COUNT) {
                  break;
               }
               currentCount++;

               if (hit.GetComponent<NPC>() != null) {
                  npcsNearby.Add(hit.GetComponent<NPC>());
               }

               if (hit.GetComponent<TreasureChest>() != null) {
                  treasuresNearby.Add(hit.GetComponent<TreasureChest>());
               }

               if (treasuresNearby.Count > 0 || npcsNearby.Count > 0) { 
                  // Prevent the player from playing attack animation when interacting NPC's / Enemies / Loot Bags
                  isNearInteractables = true;
               }
            }

            // Loot the nearest lootbag/treasure chest
            interactNearestLoot(treasuresNearby);

            // If there are no loots nearby, interact with nearest npc
            if (treasuresNearby.Count < 1) {
               interactNearestNpc(npcsNearby);
            }
         }

         if (!isNearInteractables) {
            if (facing == Direction.East || facing == Direction.SouthEast || facing == Direction.NorthEast
               || facing == Direction.West || facing == Direction.SouthWest || facing == Direction.NorthWest) {
               requestAnimationPlay(Anim.Type.Interact_East);
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_East);
            } else if (facing == Direction.North) {
               requestAnimationPlay(Anim.Type.Interact_North);
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_North);
            } else if (facing == Direction.South) {
               requestAnimationPlay(Anim.Type.Interact_South);
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_South);
            }

            farmingTrigger.interactFarming();
            miningTrigger.interactOres();
         }
      }

      // Speed ship boost feature
      if (Input.GetKey(KeyCode.LeftShift) && isReadyToSpeedup && !isWithinEnemyRadius) {
         isSpeedingUp = true;
         if (speedMeter > 0) {
            speedMeter -= Time.deltaTime * fuelDepleteValue;

            foreach (Animator animator in animators) {
               animator.speed = ANIM_SPEEDUP_VALUE;
            }
            Cmd_UpdateSpeedupDisplay(true);
         } else {
            isReadyToSpeedup = false;
            isSpeedingUp = false;
            Cmd_UpdateSpeedupDisplay(false);
         }
      } else {
         // Only notify other clients once if disabling
         if (isSpeedingUp) {
            Cmd_UpdateSpeedupDisplay(false);
            isSpeedingUp = false;
         }

         if (speedMeter < SPEEDUP_METER_MAX) {
            speedMeter += Time.deltaTime * fuelRecoverValue;
         } else {
            isReadyToSpeedup = true;
         }
      }

      updateSpeedUpDisplay(speedMeter, isSpeedingUp, isReadyToSpeedup, false);
   }

   private void updateSprintEffects (bool isOn) {
      // Handle sprite effects
      if (isOn) {
         speedUpEffect.SetActive(true);
         speedupEffectPivot.transform.localEulerAngles = new Vector3(0, 0, -Util.getAngle(facing));
      } else {
         speedUpEffect.SetActive(false);
      }

      foreach (Animator animator in animators) {
         animator.speed = isOn ? ANIM_SPEEDUP_VALUE : 1;
      }
   }

   private void updateSpeedUpDisplay (float meter, bool isOn, bool isReadySpeedup, bool forceDisable) {
      // Handle GUI
      if (!forceDisable && (meter < SPEEDUP_METER_MAX)) {
         speedupGUI.enabled = true;
         speedUpBar.fillAmount = meter / SPEEDUP_METER_MAX;
      } else {
         speedupGUI.enabled = false;
      }

      speedUpBar.color = isReadySpeedup ? defaultColor : recoveringColor;
   }

   public override void resetCombatInit () {
      base.resetCombatInit();
      Rpc_ResetMoveDisable();
   }

   [ClientRpc]
   public void Rpc_ResetMoveDisable () {
      isWithinEnemyRadius = false;
      playerBattleCollider.combatInitCollider.enabled = true;
   }

   [Command]
   void Cmd_UpdateSpeedupDisplay (bool isOn) {
      Rpc_UpdateSpeedupDisplay(isOn);
   }

   [ClientRpc]
   public void Rpc_UpdateSpeedupDisplay (bool isOn) {
      updateSprintEffects(isOn);
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

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.userParent, worldPositionStays);
   }

   #region Private Variables

   #endregion
}
