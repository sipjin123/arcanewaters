using System.Collections;
using Mirror;
using UnityEngine;

public class MonsterBattler : AutomatedBattler
{
   #region Public Variables

   // The Type of Enemy this is
   [SyncVar]
   public Enemy.Type enemyType;

   // Our colors
   [SyncVar]
   public ColorType bodyColor1;
   [SyncVar]
   public ColorType bodyColor2;

   // Holds the info of loots and their chances to drop
   public EnemyDropsData enemyDropData;

   #endregion

   public override void Awake () {
      base.Awake();

      // Look up our layers
      _enemyLayer = GetComponentInChildren<BodyLayer>();
   }

   public override void Start () {
      base.Start();

      // Make the monsters wait a bit before they can attack
      this.cooldownEndTime = Util.netTime() + 5f;
   }

   public override IEnumerator animateDeath () {
      playAnim(Anim.Type.Death_East);

      // Play our customized death sound
      playDeathSound();

      // Wait a little bit for it to finish
      yield return new WaitForSeconds(.25f);

      RewardManager.self.processLoots(enemyDropData.requestLootList());

      // Play a "Poof" effect on our head
      EffectManager.playPoofEffect(this);
   }

   public override void handleEndOfBattle (Battle.TeamType winningTeam) {
      // Check if we lost the battle
      if (this.teamType != winningTeam) {
         Enemy enemy = (Enemy) this.player;

         if (!enemy.isDefeated) {
            enemy.isDefeated = true;
         }
      }
   }

   #region Private Variables

   // Our Sprite Layer objects
   protected BodyLayer _enemyLayer;

   #endregion
}
