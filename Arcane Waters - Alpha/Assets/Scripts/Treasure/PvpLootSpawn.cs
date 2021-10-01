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

   // The powerup duration
   public int powerupDuration;

   // If this loot spawner is active
   [SyncVar]
   public bool isActive;

   // The instance id
   [SyncVar]
   public int instanceId;

   // The area key
   [SyncVar]
   public string areaKey;

   // If the powerup is showing
   [SyncVar]
   public bool isShowingPowerup;

   // The powerup visual indicators
   public GameObject powerupIndicator;
   public SpriteRenderer powerupSprite;

   // The powerup type this loot spawner will provide
   [SyncVar]
   public Powerup.Type powerupType;

   // List of sprite renderers
   public List<SpriteRenderer> spriteRendererList = new List<SpriteRenderer>();

   // The delay between spawning loot rewards (in seconds)
   public float spawnFrequency = 60;

   // The rarity level
   [SyncVar]
   public Rarity.Type rarity;

   // The rarity frame sprite renderer
   public SpriteRenderer rarityFrame;

   #endregion

   private void Awake () {
      spriteRendererList = GetComponentsInChildren<SpriteRenderer>(true).ToList();
   }

   private void Start () {
      spriteRendererList = GetComponentsInChildren<SpriteRenderer>(true).ToList();
      StartCoroutine(CO_SetAreaParent());

      // Enable powerups on client side if joining the match right after the active trigger was called
      if (!NetworkServer.active && isShowingPowerup) {
         updatePowerup(true, (int) powerupType);
      }
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
         rarity = (Rarity.Type) UnityEngine.Random.Range(1, Enum.GetValues(typeof(Rarity.Type)).Length);
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
            isShowingPowerup = false;
            playerEntity.rpc.Target_ReceivePowerup(powerupType, rarity, collision.transform.position);
            PowerupManager.self.addPowerupServer(playerEntity.userId, new Powerup {
               powerupDuration = powerupDuration,
               powerupRarity = rarity,
               powerupType = powerupType
            });

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
      if (lootGroupId > 0) {
         List<TreasureDropsData> powerupDataList = TreasureDropsDataManager.self.getTreasureDropsById(lootGroupId, rarity).FindAll(_ => _.powerUp != Powerup.Type.None);
         if (powerupDataList.Count > 0) {
            TreasureDropsData treasureDropsData = powerupDataList.ChooseRandom();
            powerupType = treasureDropsData.powerUp;
            rarity = treasureDropsData.rarity;
         } else {
            D.debug("No powerup data found in loot group {" + lootGroupId + "} with rarity {" + rarity + "}");
            powerupType = Powerup.Type.SpeedUp;
         }
      }
      isShowingPowerup = true;
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

      // Setup rarity frames
      Sprite[] borderSprites = Resources.LoadAll<Sprite>(Powerup.BORDER_SPRITES_LOCATION);
      rarityFrame.sprite = borderSprites[(int) rarity - 1];

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

         if (field.k.CompareTo(DataField.POWERUP_DURATION) == 0) {
            try {
               powerupDuration = int.Parse(field.v);
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