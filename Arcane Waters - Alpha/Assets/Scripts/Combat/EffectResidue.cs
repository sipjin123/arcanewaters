using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EffectResidue : MonoBehaviour {
   #region Public Variables

   // The damage per tick that is applied to the attached entity
   public int damagePerTick = 0;

   // The number of seconds this residue lasts until it destroys itself
   public int duration = 10;

   // The time interval at which the damage is applied
   public const float DAMAGE_FREQUENCY = .75f;

   // The source of this attack
   public uint creatorNetId = 0;

   // The attack type this residue applies
   public Attack.Type attackType = Attack.Type.None;

   // The entity to which this residue is attached
   public SeaEntity seaEntity = null;

   #endregion

   public void Start () {
      if (!NetworkServer.active) {
         return;
      }

      InvokeRepeating(nameof(damageEntity), DAMAGE_FREQUENCY, DAMAGE_FREQUENCY);
   }

   [Server]
   private void damageEntity () {
      if (!NetworkServer.active) {
         return;
      }

      if (seaEntity == null || damagePerTick == 0) {
         return;
      }

      int finalDamage = seaEntity.applyDamage(damagePerTick, creatorNetId);
      seaEntity.Rpc_ShowExplosion(creatorNetId, seaEntity.transform.position, finalDamage, attackType, false);
   }

   public void restart () {
      CancelInvoke(nameof(destroyMe));
      Invoke(nameof(destroyMe), duration);
   }

   public void destroyMe () {
      seaEntity.removeResidue(this);
      Destroy(gameObject);
   }

   #region Private Variables

   #endregion
}
