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
   public uint creatorNetId;

   // The instance id of the source
   public int instanceId;

   // The damage to the ships per second
   public int damagePerSec = 5;

   // The frequency of the residue to process the damage
   public const float DAMAGE_FREQUENCY = .75f;

   #endregion

   private void Start () {
      transform.position = new Vector3(transform.position.x, transform.position.y, 0);
      InvokeRepeating("damageEnemies", DAMAGE_FREQUENCY, DAMAGE_FREQUENCY);
   }

   private void damageEnemies () {
      if (NetworkServer.active) {
         if (targetEntities.Count > 0) {
            foreach (SeaEntity seaEntity in targetEntities) {
               seaEntity.currentHealth -= damagePerSec;
               seaEntity.Rpc_ShowExplosion(creatorNetId, seaEntity.transform.position, damagePerSec, Attack.Type.Venom);
               seaEntity.Rpc_AttachEffect(damagePerSec, Attack.Type.Venom);
            }
         }
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      PlayerShipEntity playerShipEntity = other.GetComponent<PlayerShipEntity>();

      if (playerShipEntity != null && playerShipEntity.instanceId == instanceId) {
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
