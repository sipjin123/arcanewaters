using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PlayerBattleCollider : MonoBehaviour {
   #region Public Variables

   // Reference to the player body
   public PlayerBodyEntity playerBody;

   // A reference to our combat collider
   public CircleCollider2D combatInitCollider;

   // The warning cooldown to prevent message spam
   public const float WARNING_COOLDOWN = 3;

   // The last time the warning was called
   double lastWarningTrigger;

   #endregion

   private void tryTriggerBattle (Collider2D collision) {
      if (playerBody.isInBattle()) {
         return;
      }

      if (!playerBody.isInvisible && playerBody.isLocalPlayer && collision.GetComponent<EnemyBattleCollider>() != null) {
         if (combatInitCollider.enabled && !playerBody.isWithinEnemyRadius) {
            Enemy enemy = collision.GetComponent<EnemyBattleCollider>().enemy;
            if (playerBody.instanceId == enemy.instanceId && !enemy.isDefeated && !playerBody.isBouncingOnWeb()) {
               if (playerBody.groupId == enemy.groupId || enemy.groupId == -1) {
                  D.adminLog("Engaging combat:: EnemyType: {" + enemy.enemyType + "}" + " NetID: {" + enemy.netId + "}", D.ADMIN_LOG_TYPE.Combat);
                  combatInitCollider.enabled = false;
                  playerBody.isWithinEnemyRadius = true;
                  Global.player.rpc.Cmd_StartNewBattle(enemy.netId, Battle.TeamType.Attackers, Global.forceJoin, isShipBattle: false);
                  TutorialManager3.self.tryCompletingStep(TutorialTrigger.EnterBattle);
                  TutorialManager3.self.onEnterBattle();
               } else {
                  if ((NetworkTime.time - lastWarningTrigger) > WARNING_COOLDOWN) {
                     lastWarningTrigger = NetworkTime.time;
                     Vector3 pos = this.transform.position + new Vector3(0f, .32f);
                     GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
                     messageCanvas.transform.position = pos;
                     messageCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "Cannot join battle of different group";
                  }
               }
            }
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      tryTriggerBattle(collision);
   }

   private void OnTriggerStay2D (Collider2D collision) {
      tryTriggerBattle(collision);
   }

   #region Private Variables

   #endregion
}
