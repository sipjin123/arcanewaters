using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PlayerBattleCollider : MonoBehaviour {
   #region Public Variables

   // Reference to the player body
   public PlayerBodyEntity playerBody;

   // A reference to our combat collider
   public CircleCollider2D combatInitCollider;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      if (!playerBody.isInBattle() && combatInitCollider.enabled && collision.GetComponent<Enemy>() != null) {
         Enemy enemy = collision.GetComponent<Enemy>();
         if (!enemy.isDefeated) {
            if (playerBody.voyageGroupId == enemy.voyageGroupId || enemy.voyageGroupId == -1) {
               combatInitCollider.enabled = false;
               Global.player.rpc.Cmd_StartNewBattle(enemy.netId, Battle.TeamType.Attackers);
            } else {
               Vector3 pos = this.transform.position + new Vector3(0f, .32f);
               GameObject messageCanvas = Instantiate(PrefabsManager.self.warningTextPrefab);
               messageCanvas.transform.position = pos;
               messageCanvas.GetComponentInChildren<Text>().text = "Cannot join battle of different voyage group";
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
