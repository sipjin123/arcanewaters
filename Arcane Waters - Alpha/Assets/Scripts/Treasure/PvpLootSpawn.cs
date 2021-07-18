using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;
using System.Linq;

public class PvpLootSpawn : NetworkBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The loot group id reference
   [SyncVar]
   public int lootGroupId;

   // The instance id
   [SyncVar]
   public int instanceId;

   // If this loot spawner is active
   [SyncVar]
   public bool isActive;

   // The powerup visual indicators
   public GameObject powerupIndicator;
   public SpriteRenderer powerupSprite;

   // The powerup type this loot spawner will provide
   public Powerup.Type powerupType;

   // List of sprite renderers
   public List<SpriteRenderer> spriteRendererList = new List<SpriteRenderer>();

   // The delay between spawning loot rewards (in seconds)
   public float spawnFrequency = 60;

   // The rarity level
   public Rarity.Type rarity;

   #endregion

   private void Awake () {
      spriteRendererList = GetComponentsInChildren<SpriteRenderer>(true).ToList();
   }

   private void Start () {
      if (NetworkServer.active) {
         D.debug("Generating powerup");
         Invoke(nameof(generatePowerup), 10);
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (NetworkServer.active && isActive) {
         PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
         if (playerEntity != null && playerEntity.instanceId == instanceId) {
            playerEntity.rpc.Target_ReceivePowerup(powerupType, collision.transform.position);
            updatePowerup(false);

            D.debug("Collided with powerup: " + powerupType);
            Invoke(nameof(generatePowerup), spawnFrequency);
         }
      }
   }

   public void generatePowerup () {
      updatePowerup(true);

      int rarityLength = Enum.GetValues(typeof(Rarity.Type)).Length;
      rarity = (Rarity.Type) UnityEngine.Random.Range(1, rarityLength);
      D.debug("Rarity is: " + rarity);
      if (lootGroupId > 0) {
         List<TreasureDropsData> powerupDataList = TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, rarity).FindAll(_ => _.powerUp != Powerup.Type.None);
         if (powerupDataList.Count > 0) {
            TreasureDropsData treasureDropsData = powerupDataList.ChooseRandom();
            powerupType = treasureDropsData.powerUp;
            D.debug("Found powerup level: " + powerupType);
         } else {
            D.debug("No powerup data found in loot group {" + lootGroupId + "} with rarity {" + rarity + "}");
            powerupType = Powerup.Type.SpeedUp;
         }
      }

      powerupSprite.sprite = PowerupManager.self.getPowerupData(powerupType).spriteIcon;
   }

   private void updatePowerup (bool isEnabled) {
      isActive = isEnabled;
      powerupIndicator.SetActive(isEnabled);

      foreach (SpriteRenderer spriteRender in spriteRendererList) {
         spriteRender.enabled = true;
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.LOOT_GROUP_ID) == 0) {
            try {
               lootGroupId = int.Parse(field.v);
            } catch {

            }
         }

         if (field.k.CompareTo(DataField.SPAWN_FREQUENCY) == 0) {
            try {
               spawnFrequency = float.Parse(field.v);
            } catch {

            }
         }
      }
   }

   #region Private Variables

   #endregion
}