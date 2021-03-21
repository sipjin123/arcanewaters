using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System.Linq;
using TMPro;
using DG.Tweening;

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

   // Ability buttons that appear when targetting an enemy
   public AbilityButton[] abilityTargetButtons;

   [Space(4)]
   [Header("Player")]

   // Main gameobject that will hold all of the player UI inside the battle
   public CanvasGroup playerBattleCG;

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
   public TextMeshProUGUI usernameText;

   // Used for showing features that are not in place.
   public GameObject debugWIPFrame;

   // Subscribe to this event to have something done whenever we hover on an ability in battle
   [HideInInspector] public BattleTooltipEvent onAbilityHover = new BattleTooltipEvent();

   [Header("Stance")]
   [Space(2)]

   // Buttons that allow the player to change stances
   public Button[] stanceButtons;

   // The images that radially fill to represent the cooldown of changing stances
   public Image[] stanceCooldownImages;

   // Sprites for the stance buttons, when the stances are active/inactive
   public Sprite[] stanceActiveSprites, stanceInactiveSprites;

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

   // The selection id
   public int selectionId = 0;

   #endregion

   private void Awake () {
      self = this;
   }

   public void updateButtons () {
      updateButtons(_currentAbilityType);
   }

   public void updateButtons (AbilityType abilityType, int newStance = -1) {
      Battler.Stance playerStance;
      if (newStance == -1) {
         
         if (_playerLocalBattler) {
            playerStance = _playerLocalBattler.stance;
         } else {
            Battler player = BattleManager.self.getPlayerBattler();
            if (player) {
               playerStance = player.stance;
            } else {
               D.error("BattleUIManager: Couldn't get a reference to the player battler");
               return;
            }
         }
         
      } else {
         playerStance = (Battler.Stance) newStance;
      }

      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         bool doesStanceMatch = doesAttackMatchStance(abilityButton, playerStance);
         if (abilityButton.abilityType == abilityType && doesStanceMatch && !abilityButton.cooldownImage.enabled) {
            abilityButton.enableButton();
         } else {
            if (abilityType != AbilityType.Undefined) {
               D.adminLog("Disabled Ability Button :: " +
                  " Index: " + abilityButton.abilityIndex +
                  " TypeIndex: " + abilityButton.abilityTypeIndex +
                  " ButtonType: " + abilityButton.abilityType +
                  " AbilityType: " + abilityType +
                  " Match: " + doesStanceMatch, D.ADMIN_LOG_TYPE.Ability);
            }

            abilityButton.disableButton();
         }
      }
   }

   public void initializeAbilityCooldown (AbilityType abilityType, int index, float coolDown = -1) {
      deselectOtherAbilities();
      List<AbilityButton> abilitTargetButtons = abilityTargetButtons.ToList().FindAll(_ => _.abilityType == abilityType);

      try {
         AbilityButton selectedButton = abilitTargetButtons[index];
         if (coolDown > -1) {
            selectedButton.startCooldown(coolDown);
         }
         selectedButton.playSelectAnim();
      } catch {
         D.debug("Something went wrong when trying to set cooldown, AbilityButtonCount: " + abilitTargetButtons.Count + " Index: " + index + " AbilityType: " + abilityType);
      }
   }

   private void triggerAbilityByKey (int keySlot) {
      AbilityButton selectedButton = abilityTargetButtons.ToList().Find(_ => _.abilityIndex == keySlot);
      if (selectedButton != null) {
         if (selectedButton.isEnabled && BattleSelectionManager.self.selectedBattler != null) {
            if (BattleManager.self.getPlayerBattler().canCastAbility()) {
               if (selectedButton.cooldownValue < selectedButton.cooldownTarget - .1f) {
                  D.error("Ability is cooling down!: " + selectedButton.cooldownValue + " / " + selectedButton.cooldownTarget);
               } else {
                  selectedButton.abilityButton.onClick.Invoke();
               }
            } else {
               D.debug("Player cant cast ability yet! Ability ID: " + selectedButton.abilityTypeIndex);
            }
         } else {
            selectedButton.invalidButtonClick();
         }
      }
   }

   public void setupAbilityUI () {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(_playerLocalBattler.weaponManager.equipmentDataId);
      Weapon.Class weaponClass = (weaponData == null) ? Weapon.Class.Melee : weaponData.weaponClass;
      if (weaponData != null) {
         D.adminLog("Weapon fetched is :: " + " Name: {" + weaponData.equipmentName +
            "} ID: {" + weaponData.sqlId +
            "} Unique Id: {" + _playerLocalBattler.weaponManager.equippedWeaponId +
            "} SQL Id: {" + _playerLocalBattler.weaponManager.equipmentDataId +
            "} Sprite Type: {" + _playerLocalBattler.weaponManager.weaponType+ "}", D.ADMIN_LOG_TYPE.Ability);
      }

      int indexCounter = 0;
      int attackAbilityIndex = 0;
      int buffAbilityIndex = 0;

      List<BasicAbilityData> abilityDataList = _playerLocalBattler.getBasicAbilities();
      _lastAbilityList = _playerLocalBattler.basicAbilityIDList.ToList();

      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         if (indexCounter < abilityDataList.Count) {
            BasicAbilityData currentAbility = abilityDataList[indexCounter];

            if (currentAbility == null) {
               Debug.LogWarning("Missing Ability: " + abilityButton.abilityIndex);
               continue;
            }

            AbilityType abilityType = currentAbility.abilityType;

            if (abilityType == AbilityType.Standard || abilityType == AbilityType.BuffDebuff) {

               // Setup Button Display
               string iconPath = currentAbility.itemIconPath;
               Sprite skillSprite = ImageManager.getSprite(iconPath);
               if (abilityButton.abilityIcon != null) {
                  abilityButton.abilityIcon.sprite = skillSprite;
               } else {
                  D.editorLog("This ability does not have an icon", Color.red);
               }
               abilityButton.enableButton();

               // Setup Button Values 
               abilityButton.abilityIndex = indexCounter;
               abilityButton.setAbility(abilityType);
               if (abilityType == AbilityType.Standard) {
                  abilityButton.abilityTypeIndex = attackAbilityIndex;
                  attackAbilityIndex++;
               }
               if (abilityType == AbilityType.BuffDebuff) {
                  abilityButton.abilityTypeIndex = buffAbilityIndex;
                  buffAbilityIndex++;
               }

               // Button Click Setup
               abilityButton.getButton().onClick.RemoveAllListeners();
               abilityButton.getButton().onClick.AddListener(() => {
                  deselectOtherAbilities();

                  if (BattleSelectionManager.self.selectedBattler == null) {
                     abilityButton.invalidButtonClick();
                  } else {
                     if (!abilityButton.cooldownImage.enabled) {
                        if (abilityType == AbilityType.Standard) {
                           attackPanel.requestAttackTarget(abilityButton.abilityTypeIndex);
                        } else if (abilityType == AbilityType.BuffDebuff) {
                           attackPanel.requestBuffTarget(abilityButton.abilityTypeIndex);
                        }
                     } else {
                        abilityButton.invalidButtonClick();
                     }
                  }
               });

               abilityButton.enableButton();
               abilityButton.isInvalidAbility = false;

               bool isAbilityValid = (weaponClass == currentAbility.classRequirement);

               // Log the cause of invalid weapon class if admin log is enabled
               try {
                  D.adminLog("ValidAbility: {" + isAbilityValid +
                     "} AbilityName: {" + currentAbility.itemName +
                     "} AbilityId: {" + currentAbility.itemID +
                     "} AbilityClass: {" + currentAbility.classRequirement +
                     "} WepName: {" + weaponData.equipmentName +
                     "} WepClass: {" + ((weaponData == null) ? "No Weapon Equipped" : weaponData.weaponClass.ToString()) + "}", D.ADMIN_LOG_TYPE.Ability);
               } catch {
                  D.debug("Failed to process weapon data! WeaponID: " + _playerLocalBattler.weaponManager.equipmentDataId);
               }

               if (indexCounter > 0 && !isAbilityValid) {
                  D.adminLog("Disabled because Invalid! " + currentAbility.itemName + " : " + currentAbility.itemID + " : " + currentAbility.abilityType, D.ADMIN_LOG_TYPE.Ability);
                  abilityButton.disableButton();
                  abilityButton.isInvalidAbility = true;
               }

               if (weaponClass != currentAbility.classRequirement && currentAbility.itemID != AbilityManager.PUNCH_ID) {
                  D.adminLog("Class Requirement does not match! WepClass:" + weaponClass + " AbilityClass: " + currentAbility.classRequirement, D.ADMIN_LOG_TYPE.Ability);
                  abilityButton.disableButton();
                  abilityButton.isInvalidAbility = true;
               }

               abilityButton.cancelButton.onClick.AddListener(() => {
                  deselectOtherAbilities();

                  if (BattleSelectionManager.self.selectedBattler == null) {
                     abilityButton.invalidButtonClick();
                  } else {
                     attackPanel.cancelAbility(abilityType, abilityButton.abilityTypeIndex);
                  }
               });

            } else {
               Debug.LogWarning("Undefined ability Type: " + abilityButton.abilityIndex);
            }

            abilityButton.gameObject.SetActive(true);
            abilityButton.enabled = true;
         } else {
            // Disable skill button if equipped abilities does not reach 5 (max abilities in combat)
            abilityButton.abilityIndex = -1;
            abilityButton.abilityTypeIndex = -1;
            abilityButton.abilityType = AbilityType.Undefined;
            abilityButton.abilityIcon.sprite = null;
            abilityButton.disableButton();
            abilityButton.enabled = false;
            abilityButton.gameObject.SetActive(false);
         }
         indexCounter++;
      }

      if (BattleSelectionManager.self.selectedBattler == null) {
         updateButtons(AbilityType.Undefined, (int) Battler.Stance.Balanced);
      } else {
         BattleSelectionManager.self.selectedBattler.selectThis();
      }
   }

   public void deselectOtherAbilities () {
      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         abilityButton.playIdleAnim();
      }
   }

   private void Update () {
      // Normally I would only update these values when needed (updating when action timer var is not full, or when the player received damage)
      // But for now I will just update them every frame
      if (_playerLocalBattler != null) {
         updateAbilityButtons();

         if (Input.GetKeyDown(KeyCode.Alpha1)) {
            triggerAbilityByKey(0);
         } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
            triggerAbilityByKey(1);
         } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
            triggerAbilityByKey(2);
         } else if (Input.GetKeyDown(KeyCode.Alpha4)) {
            triggerAbilityByKey(3);
         } else if (Input.GetKeyDown(KeyCode.Alpha5)) {
            triggerAbilityByKey(4);
         } else if (Input.GetKeyDown(KeyCode.Tab)) {
            selectNextTarget();
         }

         if (Input.GetKeyDown(KeyCode.F1)) {
            changeBattleStance((int) Battler.Stance.Defense);
         } else if (Input.GetKeyDown(KeyCode.F2)) {
            changeBattleStance((int) Battler.Stance.Balanced);
         } else if (Input.GetKeyDown(KeyCode.F3)) {
            changeBattleStance((int) Battler.Stance.Attack);
         }

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

         updateStanceGUI();
      }
   }

   private void updateAbilityButtons () {
      // If the abilities of the battler change, update the UI
      if (_lastAbilityList.Count != _playerLocalBattler.basicAbilityIDList.Count) {
         setupAbilityUI();
         return;
      }

      for (int i = 0; i < _lastAbilityList.Count; i++) {
         if (_lastAbilityList[i] != _playerLocalBattler.basicAbilityIDList[i]) {
            setupAbilityUI();
            return;
         }
      }
   }

   private void updateStanceGUI () {
      // If our stance change is on cooldown
      if (_playerLocalBattler.stanceCurrentCooldown > 0.0f) {
         _playerLocalBattler.stanceCurrentCooldown -= Time.deltaTime;

         // Update the radial fills of cooldown bars
         float stanceCooldown = getStanceAbilityData(_playerLocalBattler.stance).abilityCooldown;
         float fillAmount = 1.0f - Mathf.Clamp01((float) _playerLocalBattler.stanceCurrentCooldown / stanceCooldown);

         foreach (Image image in stanceCooldownImages) {
            if (image.gameObject.activeSelf) {
               image.fillAmount = fillAmount;
            }
         }

      // If our stance change is not on cooldown
      } else {
         foreach (Button button in stanceButtons) {
            // Show that buttons just came off cooldown
            if (!button.gameObject.activeSelf) {
               button.transform.DORewind();
               button.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 0, 0).SetEase(Ease.OutElastic);
               button.gameObject.SetActive(true);
            }
         }

         foreach (Image image in stanceCooldownImages) {
            image.fillAmount = 0.0f;
         }
      }
   }

   public void selectNextTarget () {
      // Store a references
      Battler playerBattler = BattleManager.self.getPlayerBattler();
      Battler selectedBattler = BattleSelectionManager.self.selectedBattler;

      List<Battler> enemyBattlersAlive = BattleSelectionManager.self.getLiveTargets();

      if (enemyBattlersAlive.Count() > 1) {
         if (selectedBattler != null) {
            // Check if the selected battler is an opponent
            if (playerBattler.isAttacker() != selectedBattler.isAttacker()) {
               // Select the current index of the selected battler
               selectionId = enemyBattlersAlive.IndexOf(selectedBattler);
            }
         }

         // Iterate to the next opponent
         selectionId++;
         if (selectionId >= enemyBattlersAlive.Count()) {
            selectionId = 0;
         }

         // Simulate battle selection
         BattleSelectionManager.self.clickBattler(enemyBattlersAlive.ElementAt<Battler>(selectionId));
      } else if (enemyBattlersAlive.Count() == 1) {
         BattleSelectionManager.self.clickBattler(enemyBattlersAlive.ElementAt<Battler>(0));
      }
   }

   public void prepareBattleUI () {
      // Enable UI
      playerBattleCG.Show();
      abilitiesCG.gameObject.SetActive(true);

      // Battler stances are always reset to balanced when a new battle begins, so we reset the UI too.
      onStanceChanged(_playerLocalBattler.stance);

      StartCoroutine(setPlayerBattlerUIEvents());

      prepareUIEvents();
   }

   public void disableBattleUI () {
      mainPlayerRectCG.Hide();
      abilitiesCG.gameObject.SetActive(false);
   }

   // Changes the icon that is at the right side of the player battle ring UI
   public void changeBattleStance (int newStance) {
      Battler.Stance stance = (Battler.Stance) newStance;

      // Don't allow the player to change stance if it's on cooldown, or to change to the stance they're already in
      if (_playerLocalBattler.stanceCurrentCooldown > 0.0f || stance == _playerLocalBattler.stance) {
         // Shake the sprite to show they can't use it
         stanceButtons[newStance].transform.DORewind();
         stanceButtons[newStance].transform.DOShakeRotation(0.2f, Vector3.forward * 70.0f, vibrato: 40);
         stanceCooldownImages[newStance].transform.DORewind();
         stanceCooldownImages[newStance].transform.DOShakeRotation(0.2f, Vector3.forward * 70.0f, vibrato: 40);
         return;
      }

      switch (stance) {
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

      onStanceChanged((Battler.Stance) newStance);
      Global.player.rpc.Cmd_RequestStanceChange((Battler.Stance) newStance);

      // Whenever we have finished setting the new stance, we hide the frames
      updateButtons(_currentAbilityType, newStance);
   }

   private bool doesAttackMatchStance (AbilityButton button, Battler.Stance stance, bool logData = false) {
      AbilityType abilityType = button.abilityType;
      int abilityTypeIndex = button.abilityTypeIndex;
      BasicAbilityData abilityData = null;

      if (abilityType == AbilityType.Standard) {
         if (_playerLocalBattler.getAttackAbilities().Count > 0) {
            abilityData = _playerLocalBattler.getAttackAbilities()[abilityTypeIndex];
            if (logData) {
               D.adminLog("Ability Attack data is" + " : " + abilityData.itemName + " : " + abilityData.allowedStances.Length, D.ADMIN_LOG_TYPE.Ability);
            }
         } else {
            D.debug("The local battler {" + _playerLocalBattler.userId + "} has no attack abilities registered to it!");
         }
      } else if (abilityType == AbilityType.BuffDebuff) {
         if (_playerLocalBattler.getBuffAbilities().Count > 0) {
            abilityData = _playerLocalBattler.getBuffAbilities()[abilityTypeIndex];
            if (logData) {
               D.adminLog("Ability Buff data is" + " : " + abilityData.itemName + " : " + abilityData.allowedStances.Length, D.ADMIN_LOG_TYPE.Ability);
            }
         }
      }

      if (abilityData != null) {
         if (abilityData.allowedStances.Contains(stance)) {
            return true;
         }
      }

      return false;
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

   #endregion

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
   }

   // Sets combat UI events for the local player battler
   private IEnumerator setPlayerBattlerUIEvents () {

      // The transition takes 2 seconds
      yield return new WaitForSeconds(2);

      Battler playerBattler = BattleManager.self.getPlayerBattler();
      mainPlayerRectCG.Show();

      if (playerBattler != null) {
         if (playerBattler.clickBox != null) {
            // TODO: Remove try catch after confirmation that no issue occurs below
            try {
               Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);
               setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);

               playerBattler.onBattlerAttackStart.AddListener(() => {
                  mainPlayerRectCG.Hide();
               });

               playerBattler.onBattlerAttackEnd.AddListener(() => {
                  mainPlayerRectCG.Show();

                  playerBattler.pauseAnim(false);
               });

               // Whenever we select our local battler, we prepare UI positioning of the ring
               playerBattler.onBattlerSelect.AddListener(() => {
                  highlightLocalBattler();
               });

               playerHealthBar.maxValue = playerBattler.getStartingHealth();

               playerBattler.onBattlerDeselect.AddListener(() => {
                  playerBattleCG.Hide();
                  playerBattler.selectedBattleBar.toggleDisplay(false);
               });

               // Auto select a random enemy at the beginning of the battle
               BattleSelectionManager.self.selectedBattler = null;
               BattleSelectionManager.self.clickBattler(BattleSelectionManager.self.getRandomTarget());

               _playerLocalBattler = playerBattler;
            } catch {
               D.debug("Something went wrong with battle ui setup for player");
            }
         }
      }
   }

   public void highlightLocalBattler (bool showAbilities = true) {
      Battler playerBattler = BattleManager.self.getPlayerBattler();
      if (playerBattler == null) {
         D.debug("Local battler has not yet loaded");
         return;
      }

      Vector3 pointOffset = new Vector3(playerBattler.clickBox.bounds.size.x / 4, playerBattler.clickBox.bounds.size.y * 1.75f);
      setRectToScreenPosition(mainPlayerRect, playerBattler.battleSpot.transform.position, pointOffset);

      playerBattler.selectedBattleBar.toggleDisplay(false);
      usernameText.text = Global.player.entityName;
      usernameText.gameObject.SetActive(true);
      if (showAbilities) {
         playerBattleCG.Show();

         // Enable all buff abilities
         setAbilityType(AbilityType.BuffDebuff);
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

   private void onStanceChanged (Battler.Stance newStance) {
      int stanceInt = (int) newStance;

      for (int i = 0; i < stanceButtons.Length; i++) {
         Button button = stanceButtons[i];
         if (i == stanceInt) {
            button.gameObject.SetActive(true);
            button.image.sprite = stanceActiveSprites[i];
            stanceCooldownImages[i].fillAmount = 0.0f;
            button.Select();

            // Emphasise that a new stance was selected
            button.transform.DORewind();
            button.transform.DOPunchScale(Vector3.one * 0.35f, 0.2f, 0, 0).SetEase(Ease.OutElastic);
            continue;
         }

         button.gameObject.SetActive(false);
         button.image.sprite = stanceInactiveSprites[i];
         stanceCooldownImages[i].fillAmount = 1.0f;
      }
   }

   private Button getActiveStanceButton () {
      return stanceButtons[(int) _playerLocalBattler.stance];
   }

   private Image getActiveStanceImage () {
      return stanceCooldownImages[(int) _playerLocalBattler.stance];
   }

   private BasicAbilityData getStanceAbilityData (Battler.Stance stance) {
      switch (stance) {
         case Battler.Stance.Attack:
            return AbilityInventory.self.offenseStance;
         case Battler.Stance.Balanced:
            return AbilityInventory.self.balancedStance;
         case Battler.Stance.Defense:
            return AbilityInventory.self.defenseStance;
         default:
            return null;
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
      Vector3 damageSpawnPosition = new Vector3(damagedBattler.transform.position.x, damagedBattler.transform.position.y + .45f, -3f);

      damageText.setDamageAmount(action.damage, action.wasCritical, action.wasBlocked);
      damageText.transform.position = damageSpawnPosition;
      damageText.transform.SetParent(EffectManager.self.transform, false);
      damageText.name = "DamageText_" + abilityData.elementType;

      // The damage text should be on the same layer as the target's Battle Spot
      damageText.gameObject.layer = spot.gameObject.layer;

      // Color the text color and icon based on the damage type
      damageText.customizeForAction(action);

      // If the attack was blocked, show some cool text
      if (action.wasBlocked) {
         createBlockBattleText(damagedBattler, damageText.transform.position.z + 0.01f);
      }

      // If the attack was a critical, show some cool text
      if (action.wasCritical) {
         createCriticalBattleText(damagedBattler, damageText.transform.position.z + 0.01f);
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

   private void createBlockBattleText (Battler battler, float zValue) {
      GameObject blockEffect = Instantiate(PrefabsManager.self.blockPrefab, EffectManager.self.transform);
      blockEffect.transform.SetParent(battler.transform, false);
      Vector3 effectPosition = blockEffect.transform.position;
      effectPosition.z = zValue;
      blockEffect.transform.position = effectPosition;
      Destroy(blockEffect, 2.0f);
   }

   private void createCriticalBattleText (Battler battler, float zValue) {
      GameObject critEffect = Instantiate(PrefabsManager.self.critPrefab, EffectManager.self.transform);
      critEffect.transform.SetParent(battler.transform, false);
      Vector3 effectPosition = critEffect.transform.position;
      effectPosition.z = zValue;
      critEffect.transform.position = effectPosition;
      Destroy(critEffect, 2.0f);
   }

   #endregion

   public Battler getLocalBattler () {
      return _playerLocalBattler;
   }

   public void setLocalBattler (Battler localBattler) {
      _playerLocalBattler = localBattler;
   }

   public void setAbilityType (AbilityType abilityType) {
      _currentAbilityType = abilityType;
      updateButtons(_currentAbilityType);
   }

   #region Private Variables

   // Reference for the local player battler, used for setting the bars information only
   private Battler _playerLocalBattler;

   // The types of abilities allowed for use by the player, used to update which abilites are enabled / disabled
   private AbilityType _currentAbilityType = AbilityType.Standard;

   // The current abilities displayed by the ability buttons
   private List<int> _lastAbilityList = new List<int>();

   #endregion
}
