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

         // TODO - Zeronev, remove this when implemented correctly
         // Will be correctly made whenever we have fully stablished the buff/attack ability data.
         // Cause this will only allow us to change stance locally, but the value can be changed externally (needs to be networked and not in here)
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
      
      usernameText.text = Global.player.entityName;

      StartCoroutine(setPlayerBattlerUIEvents());

      prepareUIEvents();
   }

   public void disableBattleUI () {
      mainPlayerRectCG.Hide();
      playerMainUIHolder.Hide();
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
      switch ((Battler.Stance) newStance) {
         case Battler.Stance.Balanced:
            stanceMainIcon.sprite = balancedSprite;
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.balancedStance.getCooldown();
            stanceButtonFrameIcon.sprite = balancedSprite;
            break;
         case Battler.Stance.Attack:
            stanceMainIcon.sprite = offenseSprite;
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.offenseStance.getCooldown();
            stanceButtonFrameIcon.sprite = offenseSprite;
            break;
         case Battler.Stance.Defense:
            stanceMainIcon.sprite = defenseSprite;
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.defenseStance.getCooldown();
            stanceButtonFrameIcon.sprite = defenseSprite;
            break;
      }

      BattleManager.self.getPlayerBattler().stance = (Battler.Stance) newStance;

      // Whenever we have finished setting the new stance, we hide the frame
      hideActionStanceFrame();
      toggleStanceFrame();
   }

   #region Tooltips

   // Triggers the tooltip frame, showing a battle item data (called only in the onAbilityHover callback)
   public void triggerTooltip (BasicAbilityData battleItemData) {
      setTooltipActiveState(true);

      // Set the window to change depending if we hovered onto the enemy or the player (change grey or gold sprite)

      // Set top Sprite
      tooltipIcon.sprite = battleItemData.getItemIcon();

      // Set lvl requirement
      tooltipLevel.text = "lvl " + battleItemData.getLevelRequirement().ToString();

      // Set ability name
      tooltipName.text = battleItemData.getName();

      // Set cost
      tooltipCost.text = "AP: " + battleItemData.getAbilityCost().ToString();

      // Set description
      tooltipDescription.text = battleItemData.getDescription();
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

   #endregion

   /// <summary>
   /// Auto adjusts a UI RectTransform from world space to Screen space
   /// Mainly used for automatically adjusting the targeted enemy UI into the correct spot
   /// </summary>
   /// <param name="target"> Battler target that we want to put the targetRect on </param>
   public void triggerTargetUI (Battler target) {

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

   // Simulates a debug battle scenario
   private void prepareBattleScenario () {
      // Find an enemy present in the scene
      Enemy randomEnemy = FindObjectOfType<Enemy>();

      // Prepare battle parameters, to begin the debug battle scenario
      Global.player.rpc.Cmd_StartNewBattle(randomEnemy.netId);
   }

   // Prepares main listener for preparing the onAbilityHover event
   public void prepareUIEvents () {
      onAbilityHover.AddListener(triggerTooltip);
   }

   // Sets combat UI events for the local player battler
   private IEnumerator setPlayerBattlerUIEvents () {

      // TODO ZERONEV: Hardcoded value until I find a correct place to set this event correctly
      // I would want to set this whenever the transition into the battle has finished
      yield return new WaitForSeconds(2);

      Battler playerBattler = BattleManager.self.getPlayerBattler();
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

   #region Private Variables

   // Reference for the local player battler, used for setting the bars information only
   private Battler _playerLocalBattler;

   #endregion
}
