﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStyleGrid : MonoBehaviour
{
   #region Public Variables

   // The gameobject we disable if this grid shouldn't be displayed. Can be left null.
   public GameObject gameObjectToDisable;

   // The toggle group
   public ToggleGroup toggleGroup;

   // Chosen gender
   public static Gender.Type chosenGender;

   #endregion

   private void Awake () {
      toggleGroup = GetComponent<ToggleGroup>();
   }

   private void Update () {
      if (_isHairstyleChanged) {
         toggleOffNonSelectedHairstyles();
         _isHairstyleChanged = false;
      }
   }

   public void show () {
      // If the gameObject we want to disable isn't assigned in the inspector, we assume it's the one that contains this script
      if (gameObjectToDisable == null) {
         gameObjectToDisable = this.gameObject;
      }

      initializeGrid();
      gameObjectToDisable.SetActive(true);
   }

   private bool isInitialized () {
      return _portraits != null && _portraits.Count > 0;
   }

   public void updateAllStacks () {
      if (!isInitialized()) {
         return;
      }

      // If the gameObject we want to disable isn't assigned in the inspector, we assume it's the one that contains this script
      if (gameObjectToDisable == null) {
         gameObjectToDisable = this.gameObject;
      }

      foreach (CharacterPortrait portrait in _portraits) {
         updateCharacterStack(portrait.characterStack, _spriteLayer);
      }
   }

   public void initializeGrid () {
      // If the gameObject we want to disable isn't assigned in the inspector, we assume it's the one that contains this script
      if (gameObjectToDisable == null) {
         gameObjectToDisable = this.gameObject;
      }

      // If the grid wasn't initialized yet or the gender changed, create new portraits. Otherwise, update the existing ones.
      if (!isInitialized() || _currentGender != CharacterCreationPanel.self.getGender()) {

         gameObject.DestroyChildren();
         _portraits = new List<CharacterPortrait>();
         _currentGender = CharacterCreationPanel.self.getGender();
         toggleGroup = GetComponent<ToggleGroup>();
         toggleGroup.enabled = false;

         // Temporarily enable the layout group
         _layoutGroup = _layoutGroup ?? GetComponentInChildren<LayoutGroup>(true);

         if (_layoutGroup != null) {
            _layoutGroup.enabled = true;
         }

         switch (_spriteLayer) {
            case Layer.Eyes:
               initializeEyeStyles(CharacterCreationPanel.self.getEyeList());
               break;

            case Layer.Hair:
               initializeHairStyles(CharacterCreationPanel.self.getOrderedHairList());
               break;

            case Layer.Armor:
               initializeArmorTypes(CharacterCreationPanel.self.getArmorList());
               break;
         }
      } else {
         updateAllStacks();
      }

      // Finish the set up after one frame so all the icons have been positioned and initialized correctly
      if (gameObject.activeInHierarchy) {
         StartCoroutine(CO_FinishSetUp());
      }
   }

   private void refreshToggles () {
      if (_spriteLayer == Layer.Armor) {
         refreshSelectedArmorToggle();
      } else if (_spriteLayer == Layer.Eyes) {
         refreshSelectedEyesToggle();
      } else if (_spriteLayer == Layer.Hair) {
         refreshSelectedHairToggle();
      }
   }

   private void refreshSelectedArmorToggle () {
      foreach (CharacterPortrait portrait in _portraits) {
         bool isSelected = portrait.characterStack.armorLayer.getType() == CharacterCreationPanel.self.getArmorId();
         portrait.setIsSelected(isSelected);
      }
   }

   private void refreshSelectedEyesToggle () {
      foreach (CharacterPortrait portrait in _portraits) {
         bool isSelected = portrait.characterStack.eyesLayer.getType() == CharacterCreationPanel.self.getEyesType();
         portrait.setIsSelected(isSelected);
      }
   }

   private void refreshSelectedHairToggle () {
      foreach (CharacterPortrait portrait in _portraits) {
         bool isSelected = portrait.characterStack.hairFrontLayer.getType() == CharacterCreationPanel.self.getHairType();
         portrait.setIsSelected(isSelected);
      }
   }

   private IEnumerator CO_FinishSetUp () {
      yield return null;

      // Round the position of the icons so pixels don't get distorted
      foreach (CharacterPortrait portrait in _portraits) {
         portrait.transform.position = new Vector3Int((int) portrait.transform.position.x, (int) portrait.transform.position.y, (int) portrait.transform.position.z);

         // For some reason, the CharacterStack of some icons is initialized with a scale of 0 the first time the panel is shown
         portrait.characterStack.transform.localScale = Vector3.one;
      }

      refreshToggles();

      // Reenable the toggle group
      toggleGroup.enabled = true;
   }

   public void displayHairstyles () {
      _layoutGroup.enabled = true;
      foreach (CharacterPortrait portrait in _portraits) {
         portrait.gameObject.SetActive(true);
      }
   }

   public void toggleOffNonSelectedHairstyles () {
      // Inactive portraits need to be manually toggled to unselected
      foreach (CharacterPortrait charPortrait in _portraits) {
         if (!charPortrait.gameObject.activeSelf) {
            charPortrait.toggle.isOn = false;
         }
      }
   }

   private void initializeHairStyles (List<HairLayer.Type> types) {
      foreach (HairLayer.Type type in types) {
         CharacterPortrait portrait = createPortrait();

         portrait.characterStack.hairBackLayer.setType(type);
         portrait.characterStack.hairFrontLayer.setType(type);

         portrait.toggle.onValueChanged.AddListener((selected) => {
            if (selected) {
               CharacterCreationPanel.self.setHairType(type);

               // Set gender based on chosen hair
               chosenGender = 0;
               if (type.ToString().Contains("Female")) {
                  chosenGender = Gender.Type.Female;
               } else {
                  chosenGender = Gender.Type.Male;
               }
               CharacterCreationPanel.self.setGender(chosenGender);

               // After the gender is set, we need to update the other traits to match the gender
               CharacterCreationPanel.self.refreshArmor();
               CharacterCreationPanel.self.refreshBody();
               CharacterCreationPanel.self.refreshEyes();

               _isHairstyleChanged = true;
            }
         });

         // Display default hairstyles
         displayHairstyles();
      }
   }

   private void initializeEyeStyles (List<EyesLayer.Type> types) {
      foreach (EyesLayer.Type type in types) {
         CharacterPortrait portrait = createPortrait();
         portrait.characterStack.eyesLayer.setType(type);
         portrait.characterStack.setDirection(Direction.South);

         portrait.toggle.onValueChanged.AddListener((selected) => {
            if (selected) {
               CharacterCreationPanel.self.setEyesType(type);
            }
         });
      }
   }

   private void initializeArmorTypes (List<int> types) {
      foreach (int type in types) {
         CharacterPortrait portrait = createPortrait();
         portrait.characterStack.armorLayer.setType(CharacterCreationPanel.self.getGender(), type, true);

         portrait.toggle.onValueChanged.AddListener((selected) => {
            if (selected) {
               CharacterCreationPanel.self.setArmor(type);
            }
         });
      }
   }

   private CharacterPortrait createPortrait () {
      CharacterPortrait portrait = Instantiate(_elementPrefab, transform);
      portrait.initializeComponents();

      toggleGroup.RegisterToggle(portrait.toggle);
      portrait.toggle.group = toggleGroup;

      updateCharacterStack(portrait.characterStack, _spriteLayer);

      _portraits.Add(portrait);
      return portrait;
   }

   private static void updateCharacterStack (CharacterStack stack, Layer ignoreLayer) {
      UserObjects userObjects = CharacterCreationPanel.self.getUserObjects();
      UserInfo userInfo = userObjects.userInfo;

      if (ignoreLayer != Layer.Eyes) {
         stack.eyesLayer.setType(CharacterCreationPanel.self.getEyesType());
      }

      if (ignoreLayer != Layer.Armor) {
         stack.armorLayer.setType(CharacterCreationPanel.self.getGender(), CharacterCreationPanel.self.getArmorId(), true);
      }

      if (ignoreLayer != Layer.Body) {
         stack.bodyLayer.setType(CharacterCreationPanel.self.getBodyType());
      }

      if (ignoreLayer != Layer.Hair) {
         stack.hairBackLayer.setType(CharacterCreationPanel.self.getHairType());
         stack.hairFrontLayer.setType(CharacterCreationPanel.self.getHairType());
      }

      // For mysterious Unity UI reasons, image materials don't get refreshed until deactivated and reactivated
      stack.gameObject.SetActive(false);

      stack.eyesLayer.recolor(userInfo.eyesPalettes);
      stack.hairBackLayer.recolor(userInfo.hairPalettes);
      stack.hairFrontLayer.recolor(userInfo.hairPalettes);
      stack.armorLayer.recolor(userObjects.armorPalettes);

      stack.gameObject.SetActive(true);
   }

   #region Private Variables

   // The element prefab
   [SerializeField]
   private CharacterPortrait _elementPrefab = default;

   // The sprite layer of the elements this grid should show
   [SerializeField]
   private Layer _spriteLayer = default;

   // The instantiated portraits
   private List<CharacterPortrait> _portraits = default;

   // The gender of the icons we're currently showing
   private Gender.Type _currentGender = default;

   // The layout group
   private LayoutGroup _layoutGroup = default;

   // Bool to track if the hairstyle was changed
   private bool _isHairstyleChanged = false;

   #endregion
}
