using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VenomResidue : MonoBehaviour {
   #region Public Variables

   // The list of entities to take damage
   public List<SeaEntity> targetEntities = new List<SeaEntity>();

   // The source of this attack
   public int creatorUserId;

   // The damage to the ships per second
   public int damagePerSec = 5;

   #endregion

   private void Start () {
      InvokeRepeating("damageEnemies", .75f, .75f);
   }

   private void damageEnemies () {
      if (NetworkServer.active) {
         if (targetEntities.Count > 0) {
            foreach (SeaEntity seaEntity in targetEntities) {
               seaEntity.currentHealth -= damagePerSec;
               seaEntity.Rpc_ShowExplosion(seaEntity.transform.position, damagePerSec, Attack.Type.Venom);
               seaEntity.Rpc_AttachEffect(damagePerSec, Attack.Type.Venom);
            }
         }
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      if (other.GetComponent<SeaEntity>() != null && other.GetComponent<PlayerShipEntity>() != null) {
         SeaEntity entity = other.GetComponent<SeaEntity>();
         if (!targetEntities.Find(_ => _.netId == entity.netId)) {
            targetEntities.Add(entity);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D other) {
      if (other.GetComponent<SeaEntity>() != null) {
         SeaEntity entity = other.GetComponent<SeaEntity>();
         if (targetEntities.Find(_ => _.netId == entity.netId)) {
            targetEntities.Remove(entity);
         }
      }
   }

   #region Private Variables

   #endregion
}
