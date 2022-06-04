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

   // The Image icon that shows enemy is regenerating
   public Image regenerateIcon;

   // The Image that holds the background for our bars
   public Image barBackgroundImage;

   // An alternate sprite we use for the health bar background on non-player ships
   public Sprite barsBackgroundAlt;

   // The prefab we use for creating health units
   public ShipHealthBlock shipHealthBlockPrefab;

   // The container of health units
   public GameObject healthBlockContainer;

   // The target value for the transparency of the ship bars
   public float targetAlpha = 1.0f;

   #endregion

   void Awake () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _entity = GetComponentInParent<SeaEntity>();

      // Start out hidden
      if (barsContainer) {
         barsContainer.SetActive(false);
      }
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
      reloadBarImage.fillAmount = (float) (NetworkTime.time - _entity.getLastAttackTime()) / _entity.reloadDelay;

      // Hide our bars if we haven't had a combat action and if the player is not targetting this ship
      barsContainer.SetActive(_entity.hasAnyCombat() || _entity.isAttackCursorOver() || _entity.hasRegenerationBuff());

      // Hide and show our status icons accordingly
      handleStatusIcons();

      // Show a different background for ships owned by other players
      if (Global.player != null && _entity != Global.player) {
         barBackgroundImage.sprite = barsBackgroundAlt;
      }

      // Hide the canvas if the ship is dead
      _canvasGroup.alpha = _entity.isDead() ? 0 : targetAlpha;

      /*if (_entity.hasRecentCombat()) {
         _canvasGroup.alpha = 1f;
      } else {
         _canvasGroup.alpha -= 1f * Time.smoothDeltaTime;
      }*/
   }

   protected void handleStatusIcons () {
      // Don't show if our bars aren't showing
      if (!barsContainer.activeSelf) {
         targetedIcon.gameObject.SetActiveIfNeeded(false);
         enemyIcon.gameObject.SetActiveIfNeeded(false);
         return;
      }

      if (_entity.isEnemyOf(Global.player)) {
         // Show the red skull icon if we're an enemy of the player
         enemyIcon.gameObject.SetActiveIfNeeded(true);
         targetedIcon.gameObject.SetActiveIfNeeded(false);
      } else if (_entity.hasAttackers()) {
         // Show the target icon if we're being attacked
         targetedIcon.gameObject.SetActiveIfNeeded(true);
         enemyIcon.gameObject.SetActiveIfNeeded(false);
      }

      if (_entity.isSeaMonsterPvp()) {
         regenerateIcon.gameObject.SetActive(_entity.hasRegenerationBuff());
      }
   }

   protected void handleHealthBar () {
      bool isAnEnemy = _entity.isEnemyOf(Global.player, false);

      if (_entity.currentHealth == _lastHealth && isAnEnemy == _lastIsAnEnemy) {
         return;
      }

      int tier = getHealthBlockTier();
      float hpPerBlock = getHealthPerBlockForDisplay(_entity.maxHealth, tier);

      // Update each hp block
      float hpStep = 0;
      for (int i = 0; i < _healthBlocks.Count; i++) {
         float blockHealth = (_entity.currentHealth - hpStep) / hpPerBlock;

         _healthBlocks[i].updateBlock(tier, blockHealth, isAnEnemy);
         hpStep += hpPerBlock;
      }

      _lastHealth = _entity.currentHealth;
      _lastIsAnEnemy = isAnEnemy;
   }

   public void initializeHealthBar () {
      _lastHealth = _entity.currentHealth;
      _lastIsAnEnemy = _entity.isEnemyOf(Global.player, false);

      healthBlockContainer.DestroyChildren();
      _healthBlocks.Clear();

      int tier = getHealthBlockTier();
      float hpPerBlock = getHealthPerBlockForDisplay(_entity.maxHealth, tier);
      int blockCount = Mathf.RoundToInt(_entity.maxHealth / hpPerBlock);
      float cumulativeBlockTotal = 0f;
      float blockOpacity = 0f;
      float partialBlockHealth = 0;

      // Instantiate enough hp blocks to display the entity max hp
      for (int i = 0; i < blockCount; i++) {
         cumulativeBlockTotal += hpPerBlock;

         // Analyze health to determine the opactiy of this block
         if (_lastHealth >= cumulativeBlockTotal) {
            blockOpacity = 1f;
         } else {
            if (Mathf.Abs(_lastHealth - cumulativeBlockTotal) < hpPerBlock) {
               partialBlockHealth = ((i + 1) * hpPerBlock) - _lastHealth;
               blockOpacity = partialBlockHealth / hpPerBlock;
            } else {
               blockOpacity = 0;
            }
         }

         ShipHealthBlock block = Instantiate(shipHealthBlockPrefab, healthBlockContainer.transform, false);
         block.updateBlock(tier, blockOpacity, _lastIsAnEnemy);
         _healthBlocks.Add(block);
      }
   }

   public int getHealthBlockTier () {
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

   /// <summary>
   /// Modifies the health per block to distribute the hp evenly and ensure that, at max hp, the last block is full
   /// </summary>
   public float getHealthPerBlockForDisplay (float maxHealth, int tier) {
      int baseHpPerBlock = ShipHealthBlock.HP_PER_BLOCK[tier];
      int blockCount = Mathf.RoundToInt(maxHealth / baseHpPerBlock);
      float leftoverHealth = maxHealth - blockCount * baseHpPerBlock;

      // Add or remove some health in each block
      return baseHpPerBlock + (leftoverHealth / blockCount);
   }

   public static float getHealthBlockPerRarity (Rarity.Type rarity, int health) {
      switch (rarity) {
         case Rarity.Type.Common:
            return health;
         case Rarity.Type.Uncommon:
            return health;
         case Rarity.Type.Rare:
            return health * 1.5f;
         case Rarity.Type.Epic:
            return health * 1.5f;
         case Rarity.Type.Legendary:
            return health * 2;
         default:
            return health;
      }
   }

   public static bool ifRarityAddsCurrentHealth (Rarity.Type rarity) {
      return rarity == Rarity.Type.Uncommon || rarity == Rarity.Type.Epic || rarity == Rarity.Type.Legendary;
   }

   public float getAlpha () {
      return _canvasGroup.alpha;
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

   // The last registered ship enemy status (compared to the global player)
   private bool _lastIsAnEnemy = false;

   #endregion
}
