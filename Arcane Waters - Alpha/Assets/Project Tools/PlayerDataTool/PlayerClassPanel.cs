using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PlayerClassPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public PlayerClassTool toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // List for toggle able tabs
   public List<TogglerClass> togglerList;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         PlayerClassData itemData = getClassData();
         if (itemData != null) {
            if (itemData.className != startingName) {
               toolManager.deleteDataFile(new PlayerClassData { className = startingName });
            }
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
            toolManager.loadXMLData();
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _classTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Jobclass, _classTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.JobIcons, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private PlayerClassData getClassData () {
      PlayerClassData classData = new PlayerClassData();
      classData.type = (Jobs.Type) Enum.Parse(typeof(Jobs.Type), _classTypeText.text);
      classData.className = _className.text;
      classData.description = _classDescription.text;

      classData.addedHP = int.Parse(_addedHP.text);
      classData.addedAP = int.Parse(_addedAP.text);
      classData.bonusMaxHP = int.Parse(_addedMaxHP.text);
      classData.bonusArmor = int.Parse(_addedArmor.text);
      classData.bonusATK = int.Parse(_addedATK.text);

      classData.bonusINT = int.Parse(_bonusINT.text);
      classData.bonusVIT = int.Parse(_bonusVIT.text);
      classData.bonusPRE = int.Parse(_bonusPRE.text);
      classData.bonusSPT = int.Parse(_bonusSPT.text);
      classData.bonusLUK = int.Parse(_bonusLUK.text);
      classData.bonusSTR = int.Parse(_bonusSTR.text);

      classData.bonusResistancePhys = int.Parse(_bonusDEFPhys.text);
      classData.bonusResistanceFire = int.Parse(_bonusDEFFire.text);
      classData.bonusResistanceEarth = int.Parse(_bonusDEFEarth.text);
      classData.bonusResistanceWind = int.Parse(_bonusDEFWind.text);
      classData.bonusResistanceWater = int.Parse(_bonusDEFWater.text);
      classData.bonusResistanceAll = int.Parse(_bonusDEFAll.text);

      classData.bonusDamagePhys = int.Parse(_bonusATKPhys.text);
      classData.bonusDamageFire = int.Parse(_bonusATKFire.text);
      classData.bonusDamageEarth = int.Parse(_bonusATKEarth.text);
      classData.bonusDamageWind = int.Parse(_bonusATKWind.text);
      classData.bonusDamageWater = int.Parse(_bonusATKWater.text);
      classData.bonusDamageAll = int.Parse(_bonusATKAll.text);

      classData.itemIconPath = _avatarSpritePath.text;

      return classData;
   }

   public void loadPlayerClassData (PlayerClassData classData) {
      startingName = classData.className;
      _classTypeText.text = classData.type.ToString();
      _className.text = classData.className;
      _classDescription.text = classData.description;

      _addedHP.text = classData.addedHP.ToString();
      _addedAP.text = classData.addedAP.ToString();
      _addedMaxHP.text = classData.bonusMaxHP.ToString();
      _addedArmor.text = classData.bonusArmor.ToString();
      _addedATK.text = classData.bonusATK.ToString();

      _bonusINT.text = classData.bonusINT.ToString();
      _bonusVIT.text = classData.bonusVIT.ToString();
      _bonusPRE.text = classData.bonusPRE.ToString();
      _bonusSPT.text = classData.bonusSPT.ToString();
      _bonusLUK.text = classData.bonusLUK.ToString();
      _bonusSTR.text = classData.bonusSTR.ToString();

      _bonusDEFPhys.text = classData.bonusResistancePhys.ToString();
      _bonusDEFFire.text = classData.bonusResistanceFire.ToString();
      _bonusDEFEarth.text = classData.bonusResistanceEarth.ToString();
      _bonusDEFWind.text = classData.bonusResistanceWind.ToString();
      _bonusDEFWater.text = classData.bonusResistanceWater.ToString();
      _bonusDEFAll.text = classData.bonusResistanceAll.ToString();

      _bonusATKPhys.text = classData.bonusDamagePhys.ToString();
      _bonusATKFire.text = classData.bonusDamageFire.ToString();
      _bonusATKEarth.text = classData.bonusDamageEarth.ToString();
      _bonusATKWind.text = classData.bonusDamageWind.ToString();
      _bonusATKWater.text = classData.bonusDamageWater.ToString();
      _bonusATKAll.text = classData.bonusDamageAll.ToString();

      _avatarSpritePath.text = classData.itemIconPath;
      if (classData.itemIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(classData.itemIconPath);
      } else {
         _avatarIcon.sprite = selectionPopup.emptySprite;
      }
   }

   #region Private Variables
#pragma warning disable 0649
   // Icon
   [SerializeField]
   private Button _changeAvatarSpriteButton;
   [SerializeField]
   private Text _avatarSpritePath;
   [SerializeField]
   private Image _avatarIcon;

   // Item Type
   [SerializeField]
   private Button _classTypeButton;
   [SerializeField]
   private Text _classTypeText;

   // Item Name
   [SerializeField]
   private InputField _className;

   // Item Info
   [SerializeField]
   private InputField _classDescription;

   // Default stats
   [SerializeField]
   private InputField _addedHP,
      _addedAP,
      _addedMaxHP,
      _addedArmor,
      _addedATK;

   // Attack stats
   [SerializeField]
   private InputField _bonusATKPhys,
      _bonusATKFire,
      _bonusATKEarth,
      _bonusATKWind,
      _bonusATKWater,
      _bonusATKAll;

   // Defensive stats
   [SerializeField]
   private InputField _bonusDEFPhys,
      _bonusDEFFire,
      _bonusDEFEarth,
      _bonusDEFWind,
      _bonusDEFWater,
      _bonusDEFAll;

   // Player stats
   [SerializeField]
   private InputField _bonusINT,
      _bonusVIT,
      _bonusPRE,
      _bonusSPT,
      _bonusLUK,
      _bonusSTR;
#pragma warning restore 0649 
   #endregion
}
