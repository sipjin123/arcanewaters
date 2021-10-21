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

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      if (!playerBody.isInvisible && playerBody.isLocalPlayer && collision.GetComponent<EnemyBattleCollider>() != null) {
         if (!playerBody.isInBattle() && combatInitCollider.enabled && !playerBody.isWithinEnemyRadius) {
            Enemy enemy = collision.GetComponent<EnemyBattleCollider>().enemy;
            if (playerBody.instanceId == enemy.instanceId && !enemy.isDefeated) {
               if (playerBody.voyageGroupId == enemy.voyageGroupId || enemy.voyageGroupId == -1) {
                  D.adminLog("Engaging combat:: EnemyType: {" + enemy.enemyType + "}" + " NetID: {" + enemy.netId + "}", D.ADMIN_LOG_TYPE.Combat);
                  combatInitCollider.enabled = false;
                  playerBody.isWithinEnemyRadius = true;
                  Global.player.rpc.Cmd_StartNewBattle(enemy.netId, Battle.TeamType.Attackers, Global.forceJoin);
                  TutorialManager3.self.tryCompletingStep(TutorialTrigger.EnterBattle);
                  TutorialManager3.self.onEnterBattle();
               } else {
                  Vector3 pos = this.transform.position + new Vector3(0f, .32f);
                  GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
                  messageCanvas.transform.position = pos;
                  messageCanvas.GetComponentInChildren<TextMeshProUGUI>().text = "Cannot join battle of different voyage group";
               }
            } 
         }
      }
   }

   #region Private Variables

   #endregion
}
