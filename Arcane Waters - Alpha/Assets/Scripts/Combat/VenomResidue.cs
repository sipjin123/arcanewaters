﻿using UnityEngine;
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
   public int damagePerSec = 10;

   // The frequency of the residue to process the damage
   public const float DAMAGE_FREQUENCY = .75f;

   #endregion

   private void Start () {
      transform.position = new Vector3(transform.position.x, transform.position.y, 0);
      InvokeRepeating(nameof(damageEnemies), DAMAGE_FREQUENCY, DAMAGE_FREQUENCY);
   }

   private void damageEnemies () {
      if (NetworkServer.active) {
         if (targetEntities.Count > 0) {
            foreach (SeaEntity seaEntity in targetEntities) {
               if (seaEntity.isDead()) {
                  continue;
               }

               int finalDamage = seaEntity.applyDamage(damagePerSec, creatorNetId);
               seaEntity.Rpc_ShowExplosion(creatorNetId, seaEntity.transform.position, finalDamage, Attack.Type.Venom, false);
               seaEntity.Rpc_AttachEffect(finalDamage, Attack.Type.Venom);
            }
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D other) {
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
