using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System.Linq;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;
using System;

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

   // Small pause after the land battle ends before user input is enabled
   public float PAUSE_AFTER_BATTLE = 0.8f;

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

   // Counts the ability triggers, for debugging purpose
   public int debugAbilityCounter = 0;

   // The battle camera
   public Camera battleCamera;

   // Keep track of when an enemy is targeted to start the battle
   public bool isInitialEnemySelected = false;

   #endregion

   private void Awake () {
      self = this;
   }

   public void resetButtonAnimations () {
      foreach (AbilityButton buttonRef in abilityTargetButtons) {
         buttonRef.playIdleAnim();
      }
   }
   
   public void updateButtons () {
      updateButtons(_currentAbilityType);
   }

   public void updateButtons (AbilityType abilityType, int newStance = -1) {
      Battler.Stance localPlayerStance;
      if (newStance == -1) {
         
         if (_playerLocalBattler) {
            localPlayerStance = _playerLocalBattler.stance;
         } else {
            Battler player = BattleManager.self.getPlayerBattler();
            if (player) {
               localPlayerStance = player.stance;
            } else {
               return;
            }
         }
         
      } else {
         localPlayerStance = (Battler.Stance) newStance;
      }

      foreach (AbilityButton abilityButton in abilityTargetButtons) {
         if (abilityButton.abilityType == abilityType) {
            if (!abilityButton.cooldownImage.enabled) {
               abilityButton.enableButton();
            } else {
               D.debug("Cannot enable a button {" + abilityButton.abilityIndex + "},already enabled! {" + abilityButton.isEnabled + ":" + abilityButton.cooldownImage.enabled + "}");
            }
         } else {
            if (abilityType != AbilityType.Undefined) {
               D.adminLog("Disabled Ability Button :: " +
                  " Index: " + abilityButton.abilityIndex +
                  " TypeIndex: " + abilityButton.abilityTypeIndex +
                  " ButtonType: " + abilityButton.abilityType +
                  " AbilityType: " + abilityType, D.ADMIN_LOG_TYPE.Ability);
            }
            abilityButton.disableButton("AbilityType:{" + abilityButton.abilityType + ":" + abilityType + "}::{" + abilityButton.cooldownImage.enabled + "}");
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

   public void triggerAbilityByKey (int keySlot) {
      if (_playerLocalBattler.isAttacking) {
         if (debugAbilityCounter >= 1) {
            D.adminLog("This unit is still attacking! Cant cast new skill", D.ADMIN_LOG_TYPE.AbilityCast);
            debugAbilityCounter = 0;
         } else {
            debugAbilityCounter++;
         }
         return;
      }
      debugAbilityCounter = 0;
      AbilityButton selectedButton = abilityTargetButtons.ToList().Find(_ => _.abilityIndex == keySlot);

      // If player is using keys 1-5 to attack with no target selected, then select a random target
      if ((BattleSelectionManager.self.selectedBattler == null) || BattleSelectionManager.self.selectedBattler.isDead()) {
         try {
            Battler randomTarget = BattleSelectionManager.self.getRandomTarget();
            if (randomTarget == null) {
               D.debug("Warning, no live targets found using Random Target Selection!");
            } else {
               BattleSelectionManager.self.clickBattler(randomTarget);
            }
         } catch {
            if (!Global.autoAttack) {
               D.debug("Unable to find an opponent to target");
            }
         }
      }

      if (selectedButton != null) {
         if (_pendingSelectedButton != null) {
            _pendingSelectedButton.togglePendingIndicator(false);
         }

         if (selectedButton.isEnabled && BattleSelectionManager.self.selectedBattler != null) {
            if (BattleManager.self.getPlayerBattler().canCastAbility() && !selectedButton.onCooldown) {
               //SoundEffectManager.self.playSoundEffect(SoundEffectManager.ABILITY_SELECTION, transform);
               SoundEffectManager.self.playGuiButtonConfirmSfx();

               triggerAbility(selectedButton, selectedButton.abilityType);
            } else {
               string lastCastBlockTime = "";
               string castBlockMessage = "";
               if (_playerLocalBattler) {
                  if ((NetworkTime.time - _playerLocalBattler.lastCastBlockTime) > 3) {
                     castBlockMessage = _playerLocalBattler.lastCastBlockReason;
                     lastCastBlockTime = _playerLocalBattler.lastCastBlockTime.ToString("f1");
                  }
               }
               D.debug("{" + (!selectedButton.onCooldown ? "" : "The ability is in cooldown! {" + selectedButton.cooldownValue.ToString("f1") + "}") + "}" +
                  "{" + (BattleManager.self.getPlayerBattler().canCastAbility() ? "" : "User Cant Cast ability") + "}{" + castBlockMessage + "}");
            }
         } else {
            D.debug("Invalid button click using hotkey! {" + (selectedButton.isEnabled ? "" : "Button disabled") + "}" +
               "{" + (BattleSelectionManager.self.selectedBattler == null ? "Null battler selected!" : "") + "} {" + selectedButton.lastDisableTrigger + "}");
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
            "} Sprite Type: {" + _playerLocalBattler.weaponManager.weaponType + "}", D.ADMIN_LOG_TYPE.Ability);
      } else {
         D.adminLog("Cant process ability info, weapon data is missing: {" + _playerLocalBattler.weaponManager.equipmentDataId + "}", D.ADMIN_LOG_TYPE.Ability);
      }

      int indexCounter = 0;
      int attackAbilityIndex = 0;
      int buffAbilityIndex = 0;

      List<BasicAbilityData> abilityDataList = _playerLocalBattler.getBasicAbilities();
      _lastAbilityList = _playerLocalBattler.basicAbilityIDList.ToList();
      string abilityString = "";
      foreach (BasicAbilityData ability in abilityDataList) {
         abilityString += ability.itemName + "/";
      }
      D.adminLog("Client is now processing ability UI Display! " +
         "Weapon: {" + (weaponData == null ? "Fists" : weaponData.equipmentName) + "} " +
         "Total Abilities: {" + abilityDataList.Count + "} {" + (abilityString.Length > 0 ? abilityString.Remove(abilityString.Length - 1) : "") + "}", D.ADMIN_LOG_TYPE.Ability);

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
                  if ((_playerLocalBattler != null) && !_playerLocalBattler.isAttacking) {
                     if (abilityButton != null) {
                        // If player is using keys 1-5 to attack with no target selected, then select a random target
                        if ((BattleSelectionManager.self.selectedBattler == null) || BattleSelectionManager.self.selectedBattler.isDead()) {
                           try {
                              Battler randomTarget = BattleSelectionManager.self.getRandomTarget();
                              if (randomTarget == null) {
                                 D.debug("Warning, no live targets found using Random Target Selection!");
                              } else {
                                 BattleSelectionManager.self.clickBattler(randomTarget);
                              }
                           } catch {
                              if (!Global.autoAttack) {
                                 D.debug("Unable to find an opponent to target");
                              }
                           }
                        }

                        if (abilityButton.isEnabled && BattleSelectionManager.self.selectedBattler != null) {
                           bool hasNoCooldownBlocker = abilityButton.cooldownValue >= abilityButton.cooldownTarget - .1f;
                           if (BattleManager.self.getPlayerBattler().canCastAbility() && hasNoCooldownBlocker) {
                              triggerAbility(abilityButton, abilityType);
                           } else {
                              string castBlockMessage = "";
                              if (_playerLocalBattler) {
                                 if ((NetworkTime.time - _playerLocalBattler.lastCastBlockTime) > 3) {
                                    castBlockMessage = _playerLocalBattler.lastCastBlockReason;
                                 }
                              }
                              D.debug("{" + (hasNoCooldownBlocker ? "" : "The ability is in cooldown! {" + abilityButton.cooldownValue.ToString("f1") + "}") + "}" +
                                 "{" + (BattleManager.self.getPlayerBattler().canCastAbility() ? "" : "User Cant Cast ability") + "}{" + castBlockMessage + "}");
                           }
                        } else {
                           D.debug("Block ability click because of " +
                              "{" + (abilityButton.isEnabled ? "AbilityButtonEnabled" : "AbilityButtonDisabled") + "}" +
                              "{" + (BattleSelectionManager.self.selectedBattler == null ? "Null Selected" : "Selected Battler" + BattleSelectionManager.self.selectedBattler.enemyType) + "}");
                        }
                     }
                  }
               });

               abilityButton.enableButton();
               abilityButton.isInvalidAbility = false;

               bool isAbilityValid = (weaponClass == currentAbility.classRequirement);

               // Log the cause of invalid weapon class if admin log is enabled
               if (_playerLocalBattler.weaponManager.equipmentDataId != 0) {
                  try {
                     D.adminLog("--> ValidAbility: {" + isAbilityValid +
                        "} AbilityName: {" + currentAbility.itemName +
                        "} AbilityId: {" + currentAbility.itemID +
                        "} AbilityClass: {" + currentAbility.classRequirement +
                        "} WepName: {" + weaponData.equipmentName +
                        "} WepClass: {" + ((weaponData == null) ? "No Weapon Equipped" : weaponData.weaponClass.ToString()) + "}", D.ADMIN_LOG_TYPE.Ability);
                  } catch {
                     D.debug("Failed to process weapon data! WeaponID: " + _playerLocalBattler.weaponManager.equipmentDataId);
                  }
               }

               if (indexCounter > 0 && !isAbilityValid) {
                  D.debug("Disabled because Invalid! " + currentAbility.itemName + " : " + currentAbility.itemID + " : " + currentAbility.abilityType);
                  abilityButton.clearButton();
                  abilityButton.isInvalidAbility = true;
               }

               if (weaponClass != currentAbility.classRequirement && currentAbility.itemID != AbilityManager.PUNCH_ID) {
                  D.adminLog("Class Requirement does not match! WepClass:" + weaponClass + " AbilityClass: " + currentAbility.classRequirement, D.ADMIN_LOG_TYPE.Ability);
                  abilityButton.clearButton();
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
            abilityButton.clearButton();
         }
         indexCounter++;
      }

      if (BattleSelectionManager.self.selectedBattler != null) {
         BattleSelectionManager.self.selectedBattler.selectThis();
      }
   }

   public void triggerAbility (AbilityButton abilityButton, AbilityType abilityType) {
      deselectOtherAbilities();

      // If no target, start selecting
      if (BattleSelectionManager.self.selectedBattler == null) {
         BattleSelectionManager.self.autoTargetNextOpponent();
      }

      // Only the local player can auto target
      if (BattleSelectionManager.self.selectedBattler.isDead() && _playerLocalBattler) {
         BattleSelectionManager.self.autoTargetNextOpponent();
      }

      if (!abilityButton.cooldownImage.enabled) {
         if (abilityType == AbilityType.Standard) {
            attackPanel.requestAttackTarget(abilityButton.abilityTypeIndex);
         } else if (abilityType == AbilityType.BuffDebuff) {
            attackPanel.requestBuffTarget(abilityButton.abilityTypeIndex);
         } else {
            D.debug("Unknown Ability request! {" + abilityButton.abilityTypeIndex + "}");
         }
      } else {
         D.adminLog("Cooldown is enabled for this ability {" + abilityButton.abilityIndex + "} " +
            "Wait for {" + abilityButton.cooldownValue.ToString("f1") + "/" + abilityButton.cooldownTarget.ToString("f1") + "}", D.ADMIN_LOG_TYPE.CancelAttack);
         abilityButton.invalidButtonClick();
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
         if (!isInitialEnemySelected) {
            Battler randomTarget = BattleSelectionManager.self.getRandomTarget();
            if (randomTarget == null) {
               D.debug("Warning, no live targets found using Random Target Selection!");
            } else {
               BattleSelectionManager.self.clickBattler(randomTarget);
            }
            isInitialEnemySelected = true;
         }
         updateAbilityButtons();

         // If the player is in the middle of an attack, ignore input
         if ((_playerLocalBattler != null)) {
            if (InputManager.self.inputMaster.LandBattle.Ability1.WasPressedThisFrame()) {
               triggerAbilityByKey(0);
            } else if (InputManager.self.inputMaster.LandBattle.Ability2.WasPressedThisFrame()) {
               triggerAbilityByKey(1);
            } else if (InputManager.self.inputMaster.LandBattle.Ability3.WasPressedThisFrame()) {
               triggerAbilityByKey(2);
            } else if (InputManager.self.inputMaster.LandBattle.Ability4.WasPressedThisFrame()) {
               triggerAbilityByKey(3);
            } else if (InputManager.self.inputMaster.LandBattle.Ability5.WasPressedThisFrame()) {
               triggerAbilityByKey(4);
            } else if (InputManager.self.inputMaster.LandBattle.NextTarget.WasPressedThisFrame()) {
               if (!_playerLocalBattler.isAttacking) {
                  selectNextTarget();
               } else {
                  D.debug("This unit is still attacking! Cant change target");
               }
            } 
         }

         if (_playerLocalBattler != null) {
            // Fire/Re-enable pending ability button when input is clicked
            if (InputManager.self.inputMaster.LandBattle.FirePending.WasPressedThisFrame()) {
               // Fire current pending ability button or set pending button with previous/initial pending index
               if (_pendingSelectedButton != default && _pendingSelectedButton.isAbilityPending) {
                  // Disable current pending ability button upon triggering ability
                  if (!_pendingSelectedButton.onCooldown && !_playerLocalBattler.isAttacking) {
                     triggerAbilityByKey(_pendingAbilityIndex);
                  }
               } else {
                  if (_pendingSelectedButton != null) {
                     _pendingAbilityIndex = _pendingSelectedButton.abilityIndex;
                     _pendingSelectedButton.togglePendingIndicator(true);      
                  }
               }
            }
         }

         // Ensure that chat panel is not in focus before doing ability switch
         if (!ChatManager.self.chatPanel.isHoveringChat) {
            // Read input mouse scroll value and check if scroll value is not equal to 0
            float scrollVal = InputManager.self.inputMaster.LandBattle.AbilitySwitch.ReadValue<float>();
            if (scrollVal != 0f) {
               int targetAbility = _pendingAbilityIndex;
               int switchValue = 0;
               // Check if user has current pending button, if not just re-enable previous/initial pending index 
               if (_pendingSelectedButton != default) {
                  // Check if scroll value is positive or negative to switch between previous or next ability
                  switchValue = scrollVal < 0 ? 1 : -1;

                  // Set pending ability button
                  setPendingAbility(targetAbility, switchValue);
               } else {
                  // Set first available button as initial pending button
                  if (abilityTargetButtons.Length > 0) {
                     AbilityButton availableButton = abilityTargetButtons.First(item => item.abilityType != AbilityType.Undefined);
                     setPendingButton(availableButton);
                  }
               }
            } 
         }

         Battler localBattler = BattleManager.self.getPlayerBattler();
         if (localBattler != null && !localBattler.isDead()) {
            if (InputManager.self.inputMaster.LandBattle.StanceDefense.WasPressedThisFrame()) {
               changeBattleStance((int) Battler.Stance.Defense);
            } else if (InputManager.self.inputMaster.LandBattle.StanceBalanced.WasPressedThisFrame()) {
               changeBattleStance((int) Battler.Stance.Balanced);
            } else if (InputManager.self.inputMaster.LandBattle.StanceAttack.WasPressedThisFrame()) {
               changeBattleStance((int) Battler.Stance.Attack);
            }
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

   private void setPendingAbility (int keySlot, int increment) {
      // Get all available ability
      List<AbilityButton> availableButton = abilityTargetButtons.ToList().FindAll(item => item.abilityType != AbilityType.Undefined);
      if (!availableButton.Any()) {
         return;
      }

      // Get index of keyslot on list of available buttons
      int pendingIndexChange = availableButton.FindIndex(item => item.abilityIndex == keySlot);
      int availableCount = availableButton.Count;

      // Return if all available button is on cooldown
      if (availableButton.All(item => item.onCooldown)) {
         return;
      }
      
      // Get the next available button that is not on cooldown
      AbilityButton targetButton;
      do {
         pendingIndexChange += increment;
         if (pendingIndexChange >= availableCount) {
            pendingIndexChange = 0;
         } else if (pendingIndexChange < 0) {
            pendingIndexChange = availableCount - 1;
         }
         
         targetButton = availableButton[pendingIndexChange];
      } while (targetButton.onCooldown && increment != 0);

      // Get target ability and check if ability return to current ability if yes ignore
      if (targetButton.abilityIndex == keySlot && _pendingSelectedButton != null) {
         _pendingSelectedButton.togglePendingIndicator(true);
         return;
      }

      setPendingButton(targetButton);
   }

   private void setPendingButton (AbilityButton targetButton) {
      // Disable pending indicator of previous pending selection
      _pendingSelectedButton?.togglePendingIndicator(false);
         
      // Make necessary changes on index and target ability button 
      _pendingAbilityIndex = targetButton.abilityIndex;
      _pendingSelectedButton = targetButton;
      _pendingSelectedButton.togglePendingIndicator(true);
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

   public void selectNextTarget (bool orderByUserId = false) {
      // Store a references
      Battler playerBattler = BattleManager.self.getPlayerBattler();
      Battler selectedBattler = BattleSelectionManager.self.selectedBattler;

      List<Battler> enemyBattlersAlive = BattleSelectionManager.self.getLiveTargets();
      if (orderByUserId && enemyBattlersAlive.Count > 0) {
         Battler newSelectedBattler = enemyBattlersAlive[0];
         D.adminLog("OrderByUserId: Enemies alive is: {" + enemyBattlersAlive.Count + "}, now selecting first index {0} " +
            "{" + (newSelectedBattler == null ? "Null" : (newSelectedBattler.userId + " : " + newSelectedBattler.enemyType)) + "}", D.ADMIN_LOG_TYPE.Battle_Selection);
         BattleSelectionManager.self.clickBattler(enemyBattlersAlive[0]);
         return;
      }

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
         Battler sectionBattlers = enemyBattlersAlive.ElementAt<Battler>(selectionId);
         D.adminLog("Enemies alive is: {" + enemyBattlersAlive.Count() + "}, now selecting index {" + selectionId + "} " +
            "{" + (selectedBattler == null ? "Null" : (selectedBattler.userId + " : " + selectedBattler.enemyType)) + "}", D.ADMIN_LOG_TYPE.Battle_Selection);
         BattleSelectionManager.self.clickBattler(sectionBattlers);
      } else if (enemyBattlersAlive.Count() == 1) {
         Battler newSelectedBattler = enemyBattlersAlive.ElementAt<Battler>(0);
         D.adminLog("Enemies alive is only {1} now selecting index {0} " +
            "{" + (newSelectedBattler == null ? "Null" : (newSelectedBattler.userId + " : " + newSelectedBattler.enemyType)) + "}", D.ADMIN_LOG_TYPE.Battle_Selection);
         BattleSelectionManager.self.clickBattler(newSelectedBattler);
      }
   }

   public void prepareBattleUI () {
      // Enable UI
      StartCoroutine(CO_FadeInBattleUI());
      
      // Battler stances are always reset to balanced when a new battle begins, so we reset the UI too.
      onStanceChanged(_playerLocalBattler.stance);

      StartCoroutine(setPlayerBattlerUIEvents());

      prepareUIEvents();

      // Reset button scales
      foreach (Button button in stanceButtons) {
         button.transform.localScale = Vector3.one;
      }
   }

   private IEnumerator CO_FadeInBattleUI () {
      // Disable input
      if (!Util.isBatch()) {
         InputManager.self.inputMaster.LandBattle.Enable();
      }

      abilitiesCG.alpha = 0.0f;
      abilitiesCG.gameObject.SetActive(true);

      if (CameraManager.defaultCamera == null) {
         D.debug("Error! Failed to Fade In due to default camera missing!");
      } else {
         float waitDuration = CameraManager.defaultCamera.getPixelFadeEffect().getFadeOutDuration();
         yield return new WaitForSeconds(waitDuration);
      }

      playerBattleCG.interactable = true;
      playerBattleCG.blocksRaycasts = true;

      if (CameraManager.defaultCamera == null) {
         D.debug("Error! Failed to Fade Out due to default camera missing!");
      } else {
         float fadeDuration = CameraManager.battleCamera.getPixelFadeEffect().getFadeInDuration();
         playerBattleCG.DOFade(1.0f, fadeDuration);
         abilitiesCG.DOFade(1.0f, fadeDuration);
      }
   }

   public void disableBattleUI () {
      StartCoroutine(CO_FadeOutBattleUI());
   }

   private IEnumerator CO_FadeOutBattleUI () {
      if (CameraManager.defaultCamera == null) {
         D.debug("Error! Failed to Fade Out due to default camera missing!");
      } else {
         float fadeDuration = CameraManager.battleCamera.getPixelFadeEffect().getFadeOutDuration();
         playerBattleCG.DOFade(0.0f, fadeDuration);

         mainPlayerRectCG.interactable = false;
         mainPlayerRectCG.blocksRaycasts = false;
         mainPlayerRectCG.DOFade(0.0f, fadeDuration);

         abilitiesCG.DOFade(0.0f, fadeDuration);
         yield return new WaitForSeconds(fadeDuration);
      }
      abilitiesCG.gameObject.SetActive(false);

      // Enable input
      yield return new WaitForSeconds(PAUSE_AFTER_BATTLE);
      if (!Util.isBatch()) {
         InputManager.self.inputMaster.LandBattle.Disable();
      }
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

      Global.player.rpc.Cmd_RequestStanceChange((Battler.Stance) newStance);
   }

   public void updateBattleStanceGUI (int newStance) {
      Battler.Stance stance = (Battler.Stance) newStance;

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

      //SoundEffectManager.self.playSoundEffect(SoundEffectManager.STANCE_SELECTION, transform);

      onStanceChanged((Battler.Stance) newStance);

      // Whenever we have finished setting the new stance, we hide the frames
      updateButtons(_currentAbilityType, newStance);
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
               });
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

   public void showDamagePerTick (Battler damagedBattler, int damage, Element element = Element.Physical) {
      // Create the Text instance from the prefab
      GameObject damageTextObject = (GameObject) Instantiate(PrefabsManager.self.damageTextPrefab);
      DamageText damageText = damageTextObject.GetComponent<DamageText>();

      // Place the damage numbers just above where the impact occurred for the given ability
      Vector3 damageSpawnPosition = new Vector3(damagedBattler.transform.position.x, damagedBattler.transform.position.y + .45f, -3f);

      damageText.setDamageAmount(damage, false, false);
      float offsetPosition = .2f;
      damageText.transform.position = new Vector3(UnityEngine.Random.Range(damageSpawnPosition.x - offsetPosition, damageSpawnPosition.x + offsetPosition), damageSpawnPosition.y, damageSpawnPosition.z);
      damageText.transform.SetParent(EffectManager.self.transform, false);
      damageText.name = "DamageText_" + element;

      // Update the font
      damageText.customizeForAction(element, false, DamageMagnitude.Default);

      // The damage text should be on the same layer as the target's Battle Spot
      damageText.gameObject.layer = damagedBattler.gameObject.layer;

      // Make note of the time at which we were last damaged
      damagedBattler.lastDamagedTime = (float) NetworkTime.time;
   }

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
      damagedBattler.lastDamagedTime = (float) NetworkTime.time;
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
      buffedBattler.lastDamagedTime = (float) NetworkTime.time;
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

   public void setBattleCameraHeight () {
      float height;
      if (ScreenSettingsManager.width < MIN_WIDTH) {
         height = RAISED_CAM_HEIGHT;
      } else {
         height = DEFAULT_CAM_HEIGHT;
      }

      battleCamera.transform.position = new Vector3(battleCamera.transform.position.x, height, battleCamera.transform.position.z);
   }

   #region Private Variables

   // Reference to current pending selected button
   private AbilityButton _pendingSelectedButton;
   
   // Reference for the current pending selected ability index
   private int _pendingAbilityIndex = 0;
   
   // Reference for the local player battler, used for setting the bars information only
   private Battler _playerLocalBattler;

   // The types of abilities allowed for use by the player, used to update which abilites are enabled / disabled
   private AbilityType _currentAbilityType = AbilityType.Standard;

   // The current abilities displayed by the ability buttons
   private List<int> _lastAbilityList = new List<int>();

   // Minimum screen resolution width before we need to raise the battle camera
   private float MIN_WIDTH = 1300;

   // Raised height of battle camera
   private float RAISED_CAM_HEIGHT = -10.3f;

   // Default height of battle camera
   private float DEFAULT_CAM_HEIGHT = -10.2f;

   #endregion
}