using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipBars : MonoBehaviour {
   #region Public Variables

   // The maximum number of hp blocks in the hp bar
   public static int MAX_BLOCKS = 12;
   
   // Our reload bar image
   public Image reloadBarImage;

   // The container for our bars
   public GameObject barsContainer;
   
   // The Image icon that shows when we're being targeted
   public Image targetedIcon;

   // The Image icon that shows when we're considered an enemy
   public Image enemyIcon;

   // The Image that holds the background for our bars
   public Image barBackgroundImage;

   // An alternate sprite we use for the health bar background on non-player ships
   public Sprite barsBackgroundAlt;

   // The prefab we use for creating health units
   public ShipHealthBlock shipHealthBlockPrefab;

   // The container of health units
   public GameObject healthBlockContainer;

   #endregion

   void Awake () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _entity = GetComponentInParent<SeaEntity>();

      // Start out hidden
      barsContainer.SetActive(false);
   }

   protected virtual void Start () {
      if (_entity == null) {
         return;
      }
   }

   protected virtual void Update () {
      if (_entity == null) {
         return;
      }

      // Set our health bar based on our current health
      handleHealthBar();

      // Update our reload bar when we recently fired
      reloadBarImage.fillAmount = (float)(NetworkTime.time - _entity.getLastAttackTime()) / _entity.reloadDelay;

      // Hide our bars if we haven't had a combat action and if the player is not targetting this ship
      barsContainer.SetActive(_entity.hasAnyCombat() || _entity.isAttackCursorOver());

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }
      
      // Hide the canvas if the ship is dead
      _canvasGroup.alpha = _entity.isDead() ? 0 : 1;

      /*if (_entity.hasRecentCombat()) {
         _canvasGroup.alpha = 1f;
      } else {
         _canvasGroup.alpha -= 1f * Time.smoothDeltaTime;
      }*/
   }

   protected void handleStatusIcons () {
      // Default to hidden
      targetedIcon.gameObject.SetActive(false);
      enemyIcon.gameObject.SetActive(false);

      // Don't show if our bars aren't showing
      if (!barsContainer.activeSelf) {
         return;
      }

      // Show the red skull icon if we're an enemy of the player
      if (_entity.isEnemyOf(Global.player)) {
         enemyIcon.gameObject.SetActive(true);
      } else if (_entity.hasAttackers()) {
         // Show the target icon if we're being attacked
         targetedIcon.gameObject.SetActive(true);
      }
   }

   protected void handleHealthBar () {
      if (_entity.currentHealth == _lastHealth) {
         return;
      }

      int tier = getHealthBlockTier();
      int hpPerBlock = ShipHealthBlock.HP_PER_BLOCK[tier];

      // Update each hp block
      int hpStep = 0;
      for (int i = 0; i < _healthBlocks.Count; i++) {
         float blockHealth = (float)((_entity.currentHealth - hpStep)) / hpPerBlock;
         _healthBlocks[i].updateBlock(tier, blockHealth);
         hpStep += hpPerBlock;
      }

      _lastHealth = _entity.currentHealth;
   }

   public void initializeHealthBar () {
      healthBlockContainer.DestroyChildren();
      _healthBlocks.Clear();

      int tier = getHealthBlockTier();
      int hpPerBlock = ShipHealthBlock.HP_PER_BLOCK[tier];

      // Instantiate enough hp blocks to display the entity max hp
      for (int i = 0; i < Mathf.Ceil((float) _entity.maxHealth / hpPerBlock); i++) {
         ShipHealthBlock block = Instantiate(shipHealthBlockPrefab, healthBlockContainer.transform, false);
         block.updateBlock(tier, 1);
         _healthBlocks.Add(block);
      }
   }

   protected int getHealthBlockTier () {
      // Get the lowest hp block tier that can display the full entity max hp
      int tier = ShipHealthBlock.HP_PER_BLOCK.Length - 1;
      for (int i = 0; i < ShipHealthBlock.HP_PER_BLOCK.Length; i++) {
         if (_entity.maxHealth < ShipHealthBlock.HP_PER_BLOCK[i] * MAX_BLOCKS) {
            tier = i;
            break;
         }
      }
      return tier;
   }

   #region Private Variables

   // Our associated Sea Entity
   protected SeaEntity _entity;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // The list of all health unit objects
   private List<ShipHealthBlock> _healthBlocks = new List<ShipHealthBlock>();

   // The last registered ship health value
   private int _lastHealth = 0;

   #endregion
}
