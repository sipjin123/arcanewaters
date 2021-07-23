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

   // If this loot spawner is active
   [SyncVar]
   public bool isActive;

   // The instance that this chest is in
   [SyncVar]
   public int instanceId;

   // The area key
   [SyncVar]
   public string areaKey;

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
      spriteRendererList = GetComponentsInChildren<SpriteRenderer>(true).ToList();
      StartCoroutine(CO_SetAreaParent());
   }


   protected IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      while (AreaManager.self.getArea(this.areaKey) == null) {
         yield return 0;
      }

      Area area = AreaManager.self.getArea(this.areaKey);
      bool worldPositionStays = area.cameraBounds.bounds.Contains((Vector2) transform.position);
      setAreaParent(area, worldPositionStays);
   }

   public void initializeSpawner (float delay) {
      if (isServer) {
         generatePowerup(delay);
      }
   }

   public void setAreaParent (Area area, bool worldPositionStays) {
      transform.SetParent(area.transform, worldPositionStays);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      if (NetworkServer.active && isActive) {
         PlayerShipEntity playerEntity = collision.GetComponent<PlayerShipEntity>();
         if (playerEntity != null && playerEntity.instanceId == instanceId) {
            playerEntity.rpc.Target_ReceivePowerup(powerupType, collision.transform.position);
            updatePowerup(false, 0);
            Rpc_ToggleDisplay(false, 0);
            initializeSpawner(spawnFrequency);
         }
      }
   }

   [Server]
   public void generatePowerup (float delay) {
      Invoke(nameof(delayPowerupTrigger), delay);
   }

   private void delayPowerupTrigger () {
      rarity = (Rarity.Type) UnityEngine.Random.Range(1, Enum.GetValues(typeof(Rarity.Type)).Length);

      if (lootGroupId > 0) {
         List<TreasureDropsData> powerupDataList = TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, rarity).FindAll(_ => _.powerUp != Powerup.Type.None);
         if (powerupDataList.Count > 0) {
            TreasureDropsData treasureDropsData = powerupDataList.ChooseRandom();
            powerupType = treasureDropsData.powerUp;
         } else {
            D.debug("No powerup data found in loot group {" + lootGroupId + "} with rarity {" + rarity + "}");
            powerupType = Powerup.Type.SpeedUp;
         }
      }

      updatePowerup(true, (int) powerupType);
      Rpc_ToggleDisplay(true, (int) powerupType);
   }

   [ClientRpc]
   public void Rpc_ToggleDisplay (bool isActive, int powerupVal) {
      updatePowerup(isActive, powerupVal);
   }

   private void updatePowerup (bool isEnabled, int powerupVal) {
      isActive = isEnabled;
      powerupIndicator.SetActive(isEnabled);

      foreach (SpriteRenderer spriteRender in spriteRendererList) {
         spriteRender.enabled = isEnabled;
      }

      if (isEnabled) {
         powerupSprite.sprite = PowerupManager.self.getPowerupData((Powerup.Type) powerupVal).spriteIcon;
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