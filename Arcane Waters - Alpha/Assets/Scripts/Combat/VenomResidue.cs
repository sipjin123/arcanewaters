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
      if (targetEntities.Count > 0) {
         for (int i = 0; i < targetEntities.Count; i++) {
            targetEntities[i].currentHealth -= damagePerSec;
            targetEntities[i].Rpc_ShowDamageText(damagePerSec, creatorUserId, Attack.Type.Venom);
            targetEntities[i].Rpc_ShowExplosion(transform.position, 0, Attack.Type.Venom);
         }
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      //Debug.LogError("STaying with : " + other.gameObject.name);
      if (other.GetComponent<SeaEntity>() != null && other.GetComponent<PlayerShipEntity>() != null) {
         SeaEntity entity = other.GetComponent<SeaEntity>();
         if (!targetEntities.Find(_ => _.netId == entity.netId)) {
           // Debug.LogError("ADDED THIS:: " + entity.netId);
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

   private float _burnTimer;

   #endregion
}
