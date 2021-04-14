using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.InputSystem;

public class BattleSelectionManager : MonoBehaviour {
   #region Public Variables

   // The currently selected Battler, if any
   public Battler selectedBattler;

   // The selection sprite
   public BattleSelectionSprite selectionSprite;

   // Self reference
   public static BattleSelectionManager self;

   // Sprites that is determined by the selected battler if is an ally or enemy
   public GameObject enemySelection, allySelection;

   // Sprite Render used for the enemy
   public SpriteRenderer currentEnemySprite;

   // Sprites that are swapped depending on the size of the enemy
   public Sprite smallEnemyTarget, largeEnemyTarget;

   #endregion

   void Awake () {
      self = this;
   }

   void Update () {
      // If our target has died, deselect it
      if (selectedBattler != null && selectedBattler.isDead()) {
         selectedBattler.deselectThis();
         selectedBattler = null;
      }

      // Whenever a mouse-click ends, check if it was on a Battle sprite
      if (KeyUtils.GetButtonUp(MouseButton.Left) && !Util.isButtonClick() && Global.isInBattle()) {
         if (Global.player == null || !(Global.player is PlayerBodyEntity)) {
            return;
         }

         // Check where on the screen this click was
         Vector3 clickLocation = BattleCamera.self.getCamera().ScreenToWorldPoint(MouseUtils.mousePosition);
         clickedArea(clickLocation);
      }
   }

   public void clickedArea (Vector3 clickLocation) {
      // Look up the player's Battle ID
      PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
      if (body == null) {
         return;
      }
      int battleId = body.battleId;

      // Find the player's Battle object
      Battle playerBattle = null;

      foreach (Battle battle in FindObjectsOfType<Battle>()) {
         if (battle.battleId == battleId) {
            playerBattle = battle;
         }
      }

      if (playerBattle == null || playerBattle.battleId <= 0) {
         return;
      }

      bool clickedBattler = false;

      // Cycle over each of the Battlers in the Battle
      foreach (Battler battler in playerBattle.getParticipants()) {
         if (battler == null) {
            continue;
         }

         // Gets the Bounds of the sprite, but scale it down a bit since it contains a lot of transparent border
         Bounds bounds = battler.clickBox.bounds;

         // We don't care about the Z location for the purpose of Contains(), so make the click Z match the bounds Z
         clickLocation.z = bounds.center.z;

         // Check if the click was within the sprite bounds of this Battler
         if (bounds.Contains(clickLocation) && !battler.isDead()) {
            clickedBattler = true;

            clickBattler(battler);
            break;
         }
      }

      // If no Battler was clicked on, then select nothing
      if (!clickedBattler) {
         BattleUIManager.self.highlightLocalBattler(false);

         if (selectedBattler != null) {
            selectedBattler.deselectThis();
         }
         selectedBattler = null;
      }
   }

   public void clickBattler (Battler battler) {
      // Check if the newly selected battler is the same as the previous one, we deselect it
      if (selectedBattler == battler) {
         selectedBattler.deselectThis();
         selectedBattler = null;
      } else {
         BattleUIManager.self.highlightLocalBattler(false);

         // If it is another battler
         if (selectedBattler != null) {
            selectedBattler.deselectThis();
         }

         Battler currentBattler = BattleManager.self.getBattler(Global.player.userId);
         if (currentBattler == null) {
            D.debug("Battler has not loaded yet");
            return;
         }
         selectedBattler = battler;
         bool selectedSameTeam = currentBattler.teamType == selectedBattler.teamType;

         if (selectedSameTeam) {
            D.adminLog("Selected Ally", D.ADMIN_LOG_TYPE.Ability);
            enemySelection.SetActive(false);
            allySelection.SetActive(true);
         } else {
            D.adminLog("Selected Enemy", D.ADMIN_LOG_TYPE.Ability);
            currentEnemySprite.sprite = selectedBattler.isBossType ? largeEnemyTarget : smallEnemyTarget;
            enemySelection.SetActive(true);
            allySelection.SetActive(false);
         }
         selectedBattler.selectThis();
      }
   }

   public void deselectTarget () {
      enemySelection.SetActive(false);
      allySelection.SetActive(false);
      BattleUIManager.self.setAbilityType(AbilityType.Undefined);
   }

   public void autoTargetNextOpponent () {
      List<Battler> liveTargets = getLiveTargets();
      foreach (Battler enemy in liveTargets) {
         if (enemy != null) {
            clickBattler(enemy);
            return;
         }
      }
   }

   public Battler getRandomTarget () {
      // Choose a random, selectable enemy to autotarget at the start of the battle
      List<Battler> liveTargets = getLiveTargets();
      Battler selectedBattler = getLiveTargets().ElementAt<Battler>(Random.Range(0, liveTargets.Count));
      return selectedBattler;
   }

   public List<Battler> getLiveTargets () {
      // Store a references
      Battler playerBattler = BattleManager.self.getPlayerBattler();

      // Select the list of opponents
      List<Battler> battlerList = playerBattler.isAttacker()
         ? BattleManager.self.getBattle(playerBattler.battleId).getDefenders()
         : BattleManager.self.getBattle(playerBattler.battleId).getAttackers();

      // Make a new list of non dead oponents
      List<Battler> enemiesAlive = battlerList.Where<Battler>(battler => !battler.isDead()).ToList<Battler>();
      return enemiesAlive;
   }
   
   public List<Battler> getSelectableTargets () {
      List<Battler> selectableTargets = getLiveTargets().Where<Battler>(battler => !battler.isProtected(getPlayerBattle())).ToList<Battler>();
      return selectableTargets;
   }

   public Battle getPlayerBattle () {
      // Look up the player's Battle ID
      PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
      int battleId = body.battleId;

      // Find the player's Battle object
      Battle playerBattle = null;

      foreach (Battle battle in FindObjectsOfType<Battle>()) {
         if (battle.battleId == battleId) {
            playerBattle = battle;
         }
      }
      return playerBattle;
   }

   public static Vector3 getOffsetToFeet () {
      return new Vector2(0f, -.12f);
   }

   #region Private Variables

   #endregion
}
