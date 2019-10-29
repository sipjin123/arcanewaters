using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class BattleUIManager : MonoBehaviour {
   #region Public Variables

   // Steps for the UI ring to handle the fill for the image
   public float[] battleRingSteps;

   // Reference to the battle camera fade effect. Will be used for knowing whenever the fade effect is completed or not
   public PixelFadeEffect battleCamPixelFade;

   // Used for calculating world space to screen space, for correctly placing the battle UI
   public RectTransform mainCanvasRect;

   [Space(4)]
   [Header("Enemy Target")]
   // Main canvas group that holds the ring that appears whenever we select an enemy
   public CanvasGroup targetEnemyCG;

   // Main rectTransform that holds the abilities and the ring for whenever we select an enemy, 
   // Used for moving it depending on the selected enemy
   public RectTransform mainTargetRect;

   // Ring that holds the abilities for attacking
   public Image enemyRing;

   // Abilities icon that appears throughout the UI Ring
   public AbilityButton[] abilityTargetButtons;

   [Space(4)]
   [Header("Player")]

   // Main gameobject that will hold all of the player UI inside the battle
   public CanvasGroup playerBattleCG;

   // Gameobject that holds the buffs, name, and all the other bars
   public CanvasGroup playerMainUIHolder;

   // Main rect transform that holds all the player battle UI
   // Used for adjusting it itself at runtime depending on player spot position
   public RectTransform mainPlayerRect;

   // Used for fading effects
   public CanvasGroup mainPlayerRectCG;

   // Transform that will hold all the debuffs and buffs icons
   public Transform extraStatusHolder;

   // Object that will be instantiated inside the holder for the buffs/debuffs
   public GameObject statusPrefab;

   // Standard UI bars that will hold the health and AP points
   public Slider playerHealthBar;
   public Slider playerApBar;

   // UI element that shows the username in the battle UI
   public Text usernameText;

   [Header("Stance")]
   // All UI objects related to the player stance UI, starting with the main frame
   public GameObject playerStanceFrame;

   // Used for showing features that are not in place.
   public GameObject debugWIPFrame;

   // Stance main icon that appears at the right side of the player
   public Image stanceMainIcon;

   // Different sprites to change whenever we have changed out battle stance
   public Sprite balancedSprite, offenseSprite, defenseSprite;

   [Space(8)]

   // Reference to the stance change button
   public Button stanceChangeButton;

   // Window that appears whenever we hover on the stance change button
   public GameObject stanceButtonFrame;

   // Content that appears in the stance change window (cooldown or that if we are ready)
   public Text stanceButtonFrameText;

   // Icon that appears at the top of the stance change frame
   public Image stanceButtonFrameIcon;

   [Space(4)]
   [Header("Stance Action Tooltip")]

   // Main frame window that appears whenever we hover on a stance action button
   public GameObject stanceActionFrame;

   // The cooldown that will be shown depending on the stance
   public Text stanceUICooldown;

   // The description of the stance action frame.
   public Text stanceActionDescription;

   // Top icon that will appear at the top of the stance action frame
   public Image stanceActionIcon;

   // Subscribe to this event to have something done whenever we hover on an ability in battle
   [HideInInspector] public BattleTooltipEvent onAbilityHover = new BattleTooltipEvent();

   [Header("Ability Tooltip")]
   [Space(4)]
   // All the variables below store the information related to the tooltip window that
   // appears whenever we hover on an ability icon UI, this gets filled whenever we trigger the window
   public RectTransform tooltipWindow;
   public Image tooltipOutline;
   public Sprite greyTooltipSprite, goldTooltipSprite;
   public Image tooltipIcon;
   public Text tooltipName;
   public Text tooltipLevel;
   public Text tooltipCost;
   public Text tooltipDescription;

   [Space(4)]
   [Header("Debug")]
   // Can we do the key combination to enter debug simulated battle?
   public bool canSimulateEnterBattle = true;

   // Reference to the battle board to enable it whenever we want to enter a battle
   public GameObject battleBoard;

   public static BattleUIManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Update () {
      // Normally I would only update these values when needed (updating when action timer var is not full, or when the player received damage)
      // But for now I will just update them every frame
      if (_playerLocalBattler != null) {
         playerApBar.value = _playerLocalBattler.getActionTimerPercent();
         playerHealthBar.value = _playerLocalBattler.displayedHealth;
         
         if (_playerLocalBattler.stanceCurrentCooldown > 0) {
            _playerLocalBattler.stanceCurrentCooldown -= Time.deltaTime;
            stanceChangeButton.interactable = false;
            stanceButtonFrameText.text = "cooldown " + _playerLocalBattler.stanceCurrentCooldown.ToString("F0") + " s";
         } else {
            _playerLocalBattler.stanceCurrentCooldown = 0;
            stanceChangeButton.interactable = true;
            stanceButtonFrameText.text = "change stance";
         }
      }
   }

   public void prepareBattleUI () {
      // Enable UI
      targetEnemyCG.Show();
      playerBattleCG.Show();

      // Battler stances are always reset to balanced when a new battle begins, so we reset the UI too.
      setStanceGraphics(BattlerBehaviour.Stance.Balanced);

      usernameText.text = Global.player.entityName;

      StartCoroutine(setPlayerBattlerUIEvents());

      prepareUIEvents();
   }

   public void disableBattleUI () {
      mainPlayerRectCG.Hide();
      targetEnemyCG.Hide();
      mainPlayerRectCG.Hide();

      // If any of these are null, then we do not call anything.
      if (playerStanceFrame != null) {
         playerStanceFrame.SetActive(false);
         playerMainUIHolder.gameObject.SetActive(false);
         setStanceFrameActiveState(false);
      }

      hideActionStanceFrame();
   }

   // Changes the icon that is at the right side of the player battle ring UI
   public void changeBattleStance (int newStance) {
      switch ((BattlerBehaviour.Stance) newStance) {
         case BattlerBehaviour.Stance.Balanced:
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.balancedStance.abilityCooldown;
            break;
         case BattlerBehaviour.Stance.Attack:
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.offenseStance.abilityCooldown;
            break;
         case BattlerBehaviour.Stance.Defense:
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.defenseStance.abilityCooldown;
            break;
      }

      setStanceGraphics((BattlerBehaviour.Stance) newStance);
      
      Global.player.rpc.Cmd_RequestStanceChange((BattlerBehaviour.Stance) newStance);

      // Whenever we have finished setting the new stance, we hide the frames
      hideActionStanceFrame();
      toggleStanceFrame();
   }

   #region Tooltips

   // Triggers the tooltip frame, showing a battle item data (called only in the onAbilityHover callback)
   public void triggerTooltip (BasicAbilityData battleItemData) {
      setTooltipActiveState(true);

      // Set the window to change depending if we hovered onto the enemy or the player (change grey or gold sprite)

      // Set top Sprite
      tooltipIcon.sprite = ImageManager.getSprite(battleItemData.itemIconPath);

      // Set lvl requirement
      tooltipLevel.text = "lvl " + battleItemData.levelRequirement.ToString();

      // Set ability name
      tooltipName.text = battleItemData.itemName;

      // Set cost
      tooltipCost.text = "AP: " + battleItemData.abilityCost.ToString();

      // Set description
      tooltipDescription.text = battleItemData.itemDescription;
   }

   public void setDebugTooltipState (bool enabled) {
      debugWIPFrame.SetActive(enabled);
   }

   /// <summary>
   /// Sets the outline frame color
   /// </summary>
   /// <param name="frameType"> 0 = Silver, 1 = Gold </param>
   public void setTooltipFrame (int frameType) {
      tooltipOutline.sprite = frameType.Equals(1) ? greyTooltipSprite : goldTooltipSprite;
   }

   // Changes state of ability tooltip
   public void setTooltipActiveState (bool enabled) {
      tooltipWindow.gameObject.SetActive(enabled);
   }

   // Enable/Disable the stance main button frame window
   public void setStanceFrameActiveState (bool enabled, string frameDescription = "") {
      stanceButtonFrame.SetActive(enabled);
      stanceButtonFrameText.text = frameDescription;
   }

   // Toggles the stance frame
   public void toggleStanceFrame () {
      playerStanceFrame.SetActive(!playerStanceFrame.activeSelf);

      if (playerStanceFrame.activeSelf) {
         setStanceFrameActiveState(false, "");
      }
   }

   public void showActionStanceFrame (int cooldown, Sprite stanceIcon, string stanceDescription) {
      stanceActionFrame.SetActive(true);
      stanceActionDescription.text = stanceDescription;
      stanceUICooldown.text = "cooldown " + cooldown + " s";
      stanceActionIcon.sprite = stanceIcon;
   }

   public void hideActionStanceFrame () {
      if (stanceActionFrame != null) {
         stanceActionFrame.SetActive(false);
      }
   }

   public Sprite getStanceIcon (BattlerBehaviour.Stance stance) {
      switch (stance) {
         case BattlerBehaviour.Stance.Balanced:
            return balancedSprite;
         case BattlerBehaviour.Stance.Attack:
            return offenseSprite;
         case BattlerBehaviour.Stance.Defense:
            return defenseSprite;
         default:
            return null;
      }
   }

   #endregion

   public void triggerTargetUI (BattlerBehaviour target) {
      mainTargetRect.gameObject.SetActive(true);

      Vector2 viewportPosition = CameraManager.battleCamera.getCamera().WorldToViewportPoint(target.battleSpot.transform.position +
         new Vector3(0, target.clickBox.bounds.size.y));
      Vector2 objectScreenPos = new Vector2(
      ((viewportPosition.x * mainCanvasRect.sizeDelta.x) - (mainCanvasRect.sizeDelta.x * 0.5f)),
      ((viewportPosition.y * mainCanvasRect.sizeDelta.y) - (mainCanvasRect.sizeDelta.y * 0.5f)));

      mainTargetRect.anchoredPosition = objectScreenPos;
   }

   public void hideTargetGameobjectUI () {
      mainTargetRect.gameObject.SetActive(false);
   }

   // Prepares main listener for preparing the onAbilityHover event
   public void prepareUIEvents () {
      onAbilityHover.AddListener(triggerTooltip);
   }

   // Sets combat UI events for the local player battler
   private IEnumerator setPlayerBattlerUIEvents () {

      // The transition takes 2 seconds
      yield return new WaitForSeconds(2);

      BattlerBehaviour playerBattler = BattleManager.self.getPlayerBattler();
      mainPlayerRectCG.Show();

      Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);
      setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);
      setRectToScreenPosition(playerMainUIHolder.GetComponent<RectTransform>(), playerBattler.battleSpot.transform.position, pointOffset);

      playerBattler.onBattlerAttackStart.AddListener(() => {
         playerMainUIHolder.Hide();
         targetEnemyCG.Hide();
         mainPlayerRectCG.Hide();
      });

      playerBattler.onBattlerAttackEnd.AddListener(() => {
         playerMainUIHolder.Show();
         targetEnemyCG.Show();
         mainPlayerRectCG.Show();

         playerBattler.unPauseAnims();
      });

      // Whenever we select our local battler, we prepare UI positioning of the ring
      playerBattler.onBattlerSelect.AddListener(() => {
         playerMainUIHolder.gameObject.SetActive(true);
      });

      // Remove world space local battler canvas
      GameObject playerCanvas = playerBattler.GetComponentInChildren<Canvas>().gameObject;
      Destroy(playerCanvas);

      playerHealthBar.maxValue = playerBattler.getStartingHealth();

      playerBattler.onBattlerDeselect.AddListener(() => {
         playerStanceFrame.SetActive(false);
         playerMainUIHolder.gameObject.SetActive(false);
      });

      _playerLocalBattler = playerBattler;
   }

   // Sets a RectTransform position from world coordinates to screen coordinates
   private void setRectToScreenPosition (RectTransform originRect, Vector3 worldPoint, Vector3 offset) {
      Vector2 viewportPosition = CameraManager.battleCamera.getCamera().WorldToViewportPoint(worldPoint + offset);
      Vector2 objectScreenPos = new Vector2(
      ((viewportPosition.x * mainCanvasRect.sizeDelta.x) - (mainCanvasRect.sizeDelta.x * 0.5f)),
      ((viewportPosition.y * mainCanvasRect.sizeDelta.y) - (mainCanvasRect.sizeDelta.y * 0.5f)));

      originRect.anchoredPosition = objectScreenPos;
   }

   private void setStanceGraphics (BattlerBehaviour.Stance stance) {
      switch (stance) {
         case BattlerBehaviour.Stance.Balanced:
            stanceMainIcon.sprite = balancedSprite;
            stanceButtonFrameIcon.sprite = balancedSprite;
            break;
         case BattlerBehaviour.Stance.Attack:
            stanceMainIcon.sprite = offenseSprite;
            stanceButtonFrameIcon.sprite = offenseSprite;
            break;
         case BattlerBehaviour.Stance.Defense:
            stanceMainIcon.sprite = defenseSprite;
            stanceButtonFrameIcon.sprite = defenseSprite;
            break;
      }
   }

   #region DamageText
   
   public void showDamageText (AttackAction action, BattlerBehaviour damagedBattler) {
      BattleSpot spot = damagedBattler.battleSpot;

      AttackAbilityData abilityData = AbilityManager.getAbility(action.abilityGlobalID, AbilityType.Standard) as AttackAbilityData;

      // Create the Text instance from the prefab
      GameObject damageTextObject = (GameObject) Instantiate(PrefabsManager.self.damageTextPrefab);
      DamageText damageText = damageTextObject.GetComponent<DamageText>();

      // Place the damage numbers just above where the impact occurred for the given ability
      damageText.transform.position = abilityData.isProjectile() ?
          new Vector3(0f, .10f, -3f) + (Vector3) damagedBattler.getRangedEndPosition() :
          new Vector3(damagedBattler.transform.position.x, damagedBattler.transform.position.y + .25f, -3f);
      damageText.setDamageAmount(action.damage, action.wasCritical, action.wasBlocked);
      damageText.transform.SetParent(EffectManager.self.transform, false);
      damageText.name = "DamageText";

      // The damage text should be on the same layer as the target's Battle Spot
      damageText.gameObject.layer = spot.gameObject.layer;

      // Color the text color and icon based on the damage type
      damageText.customizeForAction(action);

      // If the attack was blocked, show some cool text
      if (action.wasBlocked) {
         createBlockBattleText(damagedBattler);
      }

      // If the attack was a critical, show some cool text
      if (action.wasCritical) {
         createCriticalBattleText(damagedBattler);
      }

      // Make note of the time at which we were last damaged
      damagedBattler.lastDamagedTime = Time.time;
   }

   private void createBlockBattleText (BattlerBehaviour battler) {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(battler.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForBlock();
   }

   private void createCriticalBattleText (BattlerBehaviour battler) {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(battler.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForCritical();
   }

   #endregion


   #region Private Variables

   // Reference for the local player battler, used for setting the bars information only
   private BattlerBehaviour _playerLocalBattler;

   #endregion
}
