using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System.Linq;

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
   // Main canvas group that holds the abilities that appears whenever we select a character
   public CanvasGroup abilitiesCG;

   // The row of ability buttons that appear when targetting an enemy
   public CanvasGroup targetAbilitiesRow;

   // The row of ability buttons that appear when targetting a player
   public CanvasGroup buffAbilitiesRow;

   // Ability buttons that appear when targetting an enemy
   public AbilityButton[] abilityTargetButtons;

   // Ability buttons that appear when targetting a player
   public AbilityButton[] buffPlayerButtons;

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

   [Header("Ability Description")]
   [Space(4)]
   // All the variables below store the information related to the description window that
   // appears whenever we hover on an ability icon UI, this gets filled whenever we trigger the window
   public GameObject descriptionPanel;
   public Image tooltipOutline;
   public Sprite greyTooltipSprite, goldTooltipSprite;
   public Image descriptionIcon;
   public Text descriptionName;
   public Text descriptionLevel;
   public Text descriptionCost;
   public Text descriptionText;

   [Space(4)]
   [Header("Debug")]
   // Can we do the key combination to enter debug simulated battle?
   public bool canSimulateEnterBattle = true;

   // Reference to the battle board to enable it whenever we want to enter a battle
   public GameObject battleBoard;

   // Self
   public static BattleUIManager self;

   // Reference to the attack panel
   public AttackPanel attackPanel;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initializeAbilityCooldown (AbilityType abilityType, int index, float coolDown) {
      AbilityButton selectedButton = abilityTargetButtons.ToList().FindAll(_ => _.abilityType == abilityType)[index];
      selectedButton.startCooldown(coolDown);
      selectedButton.playSelectAnim();
   }

   private void FixedUpdate () {
      if (Input.GetKeyUp(KeyCode.Alpha1)) {
         triggerAbilityByKey(0);
      } else if (Input.GetKeyUp(KeyCode.Alpha2)) {
         triggerAbilityByKey(1);
      } else if (Input.GetKeyUp(KeyCode.Alpha3)) {
         triggerAbilityByKey(2);
      } else if (Input.GetKeyUp(KeyCode.Alpha4)) {
         triggerAbilityByKey(3);
      } else if (Input.GetKeyUp(KeyCode.Alpha5)) {
         triggerAbilityByKey(4);
      }
   }

   private void triggerAbilityByKey (int keySlot) {
      AbilityButton selectedButton = abilityTargetButtons.ToList()[keySlot];
      if (selectedButton.isEnabled && BattleSelectionManager.self.selectedBattler != null) {
         selectedButton.abilityButton.onClick.Invoke();
      } else { 
         selectedButton.invalidButtonClick();
      }
   }

   public void SetupAbilityUI (AbilitySQLData[] abilitydata) {
      int indexCounter = 0;
      int attackAbilityIndex = 0;
      int buffAbilityIndex = 0;

      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         if (indexCounter < abilitydata.Length) {
            if (abilitydata[indexCounter].abilityType == AbilityType.Standard) {
               BasicAbilityData currentAbility = AbilityManager.getAbility(abilitydata[indexCounter].abilityID, abilitydata[indexCounter].abilityType);
               if (currentAbility != null) {
                  string iconPath = currentAbility.itemIconPath;
                  Sprite skillSprite = ImageManager.getSprite(iconPath);

                  if (abilityButton.abilityIcon != null) {
                     abilityButton.abilityIcon.sprite = skillSprite;
                     buffPlayerButtons[indexCounter].abilityIcon.sprite = skillSprite;
                  } else {
                     D.editorLog("This ability does not have an icon", Color.red);
                  }

                  abilityButton.enableButton();
                  buffPlayerButtons[indexCounter].disableButton();

                  int indexToSet = attackAbilityIndex;
                  abilityButton.abilityType = AbilityType.Standard; 
                  abilityButton.GetComponent<Button>().onClick.RemoveAllListeners();
                  abilityButton.GetComponent<Button>().onClick.AddListener(() => {
                     deselectOtherAbilities();

                     if (BattleSelectionManager.self.selectedBattler == null) {
                        abilityButton.invalidButtonClick();
                     } else {
                        attackPanel.requestAttackTarget(indexToSet);
                     }
                  });
                  
                  abilityButton.abilityIndex = indexCounter;
                  attackAbilityIndex++;
               } else {
                  Debug.LogWarning("Missing Ability: " + abilityButton.abilityIndex);
               }
            } else if (abilitydata[indexCounter].abilityType == AbilityType.BuffDebuff) {
               BasicAbilityData currentAbility = AbilityManager.getAbility(abilitydata[indexCounter].abilityID, abilitydata[indexCounter].abilityType);
               if (currentAbility != null) {
                  string iconPath = currentAbility.itemIconPath;
                  Sprite skillSprite = ImageManager.getSprite(iconPath);

                  if (abilityButton.abilityIcon != null) {
                     abilityButton.abilityIcon.sprite = skillSprite;
                     buffPlayerButtons[indexCounter].abilityIcon.sprite = skillSprite;
                  }

                  abilityButton.disableButton();
                  buffPlayerButtons[indexCounter].enableButton();

                  int indexToSet = buffAbilityIndex;
                  abilityButton.abilityType = AbilityType.BuffDebuff;
                  buffPlayerButtons[indexCounter].GetComponent<Button>().onClick.RemoveAllListeners();
                  buffPlayerButtons[indexCounter].GetComponent<Button>().onClick.AddListener(() => {
                     attackPanel.requestBuffTarget(indexToSet);
                  });

                  abilityButton.abilityIndex = indexCounter;
                  buffAbilityIndex++;
               } else {
                  Debug.LogWarning("Missing Ability: " + abilityButton.abilityIndex);
               }
            } else {
               Debug.LogWarning("Undefined ability Type: " + abilityButton.abilityIndex);
            }

            abilityButton.gameObject.SetActive(true);
            abilityButton.enabled = true;
            buffPlayerButtons[indexCounter].enabled = true;
         } else {
            // Disable skill button if equipped abilities does not reach 5 (max abilities in combat)
            abilityButton.abilityIcon.sprite = null;
            buffPlayerButtons[indexCounter].abilityIcon.sprite = null;

            abilityButton.disableButton();
            buffPlayerButtons[indexCounter].disableButton();

            abilityButton.enabled = false;
            buffPlayerButtons[indexCounter].enabled = false;

            abilityButton.gameObject.SetActive(false);
            buffPlayerButtons[indexCounter].gameObject.SetActive(false);
         }
         indexCounter++;
      }

      int abilityIndex = 0;
      if (abilitydata.Length > 0) {
         abilityIndex = abilitydata.Length - 1;
      }
   }

   private void deselectOtherAbilities () {
      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         abilityButton.playIdleAnim();
      }
   }

   private void Update () {
      // Normally I would only update these values when needed (updating when action timer var is not full, or when the player received damage)
      // But for now I will just update them every frame
      if (_playerLocalBattler != null) {
         // Display the health of the ally
         if (BattleSelectionManager.self.selectedBattler != null && BattleSelectionManager.self.selectedBattler != _playerLocalBattler && BattleSelectionManager.self.selectedBattler.battlerType == BattlerType.PlayerControlled) {
            playerHealthBar.maxValue = BattleSelectionManager.self.selectedBattler.getStartingHealth();
            playerApBar.value = BattleSelectionManager.self.selectedBattler.getActionTimerPercent();
            playerHealthBar.value = BattleSelectionManager.self.selectedBattler.displayedHealth;
         } else {
            playerApBar.value = _playerLocalBattler.getActionTimerPercent();
            playerHealthBar.maxValue = _playerLocalBattler.getStartingHealth();
            playerHealthBar.value = _playerLocalBattler.displayedHealth;
         }
         
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
      playerBattleCG.Show();

      // Battler stances are always reset to balanced when a new battle begins, so we reset the UI too.
      setStanceGraphics(Battler.Stance.Balanced);

      StartCoroutine(setPlayerBattlerUIEvents());

      prepareUIEvents();
   }

   public void disableBattleUI () {
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
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.balancedStance.abilityCooldown;
            break;
         case Battler.Stance.Attack:
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.offenseStance.abilityCooldown;
            break;
         case Battler.Stance.Defense:
            _playerLocalBattler.stanceCurrentCooldown = AbilityInventory.self.defenseStance.abilityCooldown;
            break;
      }

      setStanceGraphics((Battler.Stance) newStance);
      
      Global.player.rpc.Cmd_RequestStanceChange((Battler.Stance) newStance);

      // Whenever we have finished setting the new stance, we hide the frames
      hideActionStanceFrame();
      toggleStanceFrame();
   }

   #region Tooltips

   // Triggers the description panel, showing a battle item data (called only in the onAbilityHover callback)
   public void triggerDescriptionPanel (BasicAbilityData battleItemData) {
      setDescriptionActiveState(true);

      // Set top Sprite
      descriptionIcon.sprite = ImageManager.getSprite(battleItemData.itemIconPath);

      // Set lvl requirement
      descriptionLevel.text = "LvL: " + battleItemData.levelRequirement.ToString();

      // Set ability name
      descriptionName.text = battleItemData.itemName;

      // Set cost
      descriptionCost.text = "AP: " + battleItemData.abilityCost.ToString();

      // Set description
      descriptionText.text = battleItemData.itemDescription;
   }

   public void setDebugTooltipState (bool enabled) {
      debugWIPFrame.SetActive(enabled);
   }

   // Changes state of ability tooltip
   public void setDescriptionActiveState (bool enabled) {
      descriptionPanel.SetActive(enabled);
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

   public Sprite getStanceIcon (Battler.Stance stance) {
      switch (stance) {
         case Battler.Stance.Balanced:
            return balancedSprite;
         case Battler.Stance.Attack:
            return offenseSprite;
         case Battler.Stance.Defense:
            return defenseSprite;
         default:
            return null;
      }
   }

   #endregion

   public void triggerTargetUI (Battler target) {
      buffAbilitiesRow.Hide();
      targetAbilitiesRow.Show();
   }

   public void hidePlayerGameobjectUI () {
      playerBattleCG.Hide();
   }

   // Prepares main listener for preparing the onAbilityHover event
   public void prepareUIEvents () {
      onAbilityHover.AddListener(triggerDescriptionPanel);
   }

   public void updatePlayerUIPositions () {
      Battler playerBattler = BattleManager.self.getPlayerBattler();
      Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);

      setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);
      setRectToScreenPosition(playerMainUIHolder.GetComponent<RectTransform>(), playerBattler.battleSpot.transform.position, pointOffset);
   }

   // Sets combat UI events for the local player battler
   private IEnumerator setPlayerBattlerUIEvents () {

      // The transition takes 2 seconds
      yield return new WaitForSeconds(2);

      Battler playerBattler = BattleManager.self.getPlayerBattler();
      mainPlayerRectCG.Show();

      Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);
      setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);
      setRectToScreenPosition(playerMainUIHolder.GetComponent<RectTransform>(), playerBattler.battleSpot.transform.position, pointOffset);

      playerBattler.onBattlerAttackStart.AddListener(() => {
         playerMainUIHolder.Hide();
         mainPlayerRectCG.Hide();
      });

      playerBattler.onBattlerAttackEnd.AddListener(() => {
         playerMainUIHolder.Show();
         mainPlayerRectCG.Show();

         playerBattler.pauseAnim(false);
      });

      // Whenever we select our local battler, we prepare UI positioning of the ring
      playerBattler.onBattlerSelect.AddListener(() => {
         stanceChangeButton.gameObject.SetActive(true);
         highlightLocalBattler();
      });

      playerHealthBar.maxValue = playerBattler.getStartingHealth();

      playerBattler.onBattlerDeselect.AddListener(() => {
         playerStanceFrame.SetActive(false);
         playerMainUIHolder.gameObject.SetActive(false);
         playerBattleCG.Hide();
         playerBattler.selectedBattleBar.gameObject.SetActive(false);
      });

      _playerLocalBattler = playerBattler;
   }

   public void highlightLocalBattler (bool showAbilities = true) {
      Battler playerBattler = BattleManager.self.getPlayerBattler();
      Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);
      setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);
      setRectToScreenPosition(playerMainUIHolder.GetComponent<RectTransform>(), playerBattler.battleSpot.transform.position, pointOffset);

      playerMainUIHolder.gameObject.SetActive(true);

      playerBattler.selectedBattleBar.gameObject.SetActive(false);
      usernameText.text = Global.player.entityName;
      usernameText.gameObject.SetActive(true);
      if (showAbilities) {
         playerBattleCG.Show();
         buffAbilitiesRow.Show();
         targetAbilitiesRow.Hide();

         foreach (AbilityButton abilityButton in abilityTargetButtons) {
            if (abilityButton.isEnabled) {
               abilityButton.gameObject.SetActive(true);
               abilityButton.enableButton();
            }
         }
      } else {
         playerBattleCG.Hide();
      }
   }

   // Sets a RectTransform position from world coordinates to screen coordinates
   public void setRectToScreenPosition (RectTransform originRect, Vector3 worldPoint, Vector3 offset) {
      Vector2 viewportPosition = CameraManager.battleCamera.getCamera().WorldToViewportPoint(worldPoint + offset);
      Vector2 objectScreenPos = new Vector2(
      ((viewportPosition.x * mainCanvasRect.sizeDelta.x) - (mainCanvasRect.sizeDelta.x * 0.5f)),
      ((viewportPosition.y * mainCanvasRect.sizeDelta.y) - (mainCanvasRect.sizeDelta.y * 0.5f)));

      originRect.anchoredPosition = objectScreenPos;
   }

   public void setStanceGraphics (Battler.Stance stance) {
      switch (stance) {
         case Battler.Stance.Balanced:
            stanceMainIcon.sprite = balancedSprite;
            stanceButtonFrameIcon.sprite = balancedSprite;
            break;
         case Battler.Stance.Attack:
            stanceMainIcon.sprite = offenseSprite;
            stanceButtonFrameIcon.sprite = offenseSprite;
            break;
         case Battler.Stance.Defense:
            stanceMainIcon.sprite = defenseSprite;
            stanceButtonFrameIcon.sprite = defenseSprite;
            break;
      }
   }

   #region DamageText
   
   public void showDamageText (AttackAction action, Battler damagedBattler) {
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
      damageText.name = "DamageText_"+ abilityData.elementType;

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

   public void showHealText (BuffAction action, Battler buffedBattler) {
      BattleSpot spot = buffedBattler.battleSpot;

      BuffAbilityData abilityData = AbilityManager.getAbility(action.abilityGlobalID, AbilityType.BuffDebuff) as BuffAbilityData;

      // Create the Text instance from the prefab
      GameObject regenTextObject = (GameObject) Instantiate(PrefabsManager.self.damageTextPrefab);
      DamageText regenText = regenTextObject.GetComponent<DamageText>();

      // Place the heal value just above where the impact occurred for the given ability
      regenText.transform.position = new Vector3(buffedBattler.transform.position.x, buffedBattler.transform.position.y + .25f, -3f);
      regenText.setDamageAmount(action.buffValue, false, false);
      regenText.transform.SetParent(EffectManager.self.transform, false);
      regenText.name = "HealText_" + abilityData.elementType;
      regenText.customizeForAction(abilityData.elementType, false, DamageMagnitude.Default);

      // The regen text should be on the same layer as the target's Battle Spot
      regenText.gameObject.layer = spot.gameObject.layer;

      // Make note of the time at which we were last healed
      buffedBattler.lastDamagedTime = Time.time;
   }

   private void createBlockBattleText (Battler battler) {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(battler.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForBlock();
   }

   private void createCriticalBattleText (Battler battler) {
      GameObject battleTextInstance = Instantiate(PrefabsManager.self.battleTextPrefab);
      battleTextInstance.transform.SetParent(battler.transform, false);
      battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForCritical();
   }

   #endregion

   public Battler getLocalBattler () {
      return _playerLocalBattler;
   }

   #region Private Variables

   // Reference for the local player battler, used for setting the bars information only
   private Battler _playerLocalBattler;

   #endregion
}
