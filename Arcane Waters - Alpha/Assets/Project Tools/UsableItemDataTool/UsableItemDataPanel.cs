using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class UsableItemDataPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public UsableItemDataToolManager toolManager;

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
         UsableItemData itemData = getItemData();
         if (itemData != null) {
            if (itemData.itemName != startingName) {
               toolManager.deleteMonsterDataFile(new UsableItemData { itemName = startingName });
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

      _itemTypeButton.onClick.AddListener(() => {
         selectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.UsableItemType, _itemTypeText);
      });
      _changeAvatarSpriteButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.UsableItemIcon, _avatarIcon, _avatarSpritePath);
      });

      foreach (TogglerClass toggler in togglerList) {
         toggler.initListeners();
      }
   }

   private UsableItemData getItemData () {
      UsableItemData itemData = new UsableItemData();
      itemData.type = (UsableItem.Type) Enum.Parse(typeof(UsableItem.Type), _itemTypeText.text);
      itemData.itemName = _itemName.text;
      itemData.description = _itemDescription.text;

      itemData.addedHP = int.Parse(_addedHP.text);
      itemData.addedAP = int.Parse(_addedAP.text);
      itemData.bonusMaxHP = int.Parse(_addedMaxHP.text);
      itemData.bonusArmor = int.Parse(_addedArmor.text);
      itemData.bonusATK = int.Parse(_addedATK.text);

      itemData.bonusINT = int.Parse(_bonusINT.text);
      itemData.bonusVIT = int.Parse(_bonusVIT.text);
      itemData.bonusPRE = int.Parse(_bonusPRE.text);
      itemData.bonusSPT = int.Parse(_bonusSPT.text);
      itemData.bonusLUK = int.Parse(_bonusLUK.text);
      itemData.bonusSTR = int.Parse(_bonusSTR.text);

      itemData.bonusResistancePhys = int.Parse(_bonusDEFPhys.text);
      itemData.bonusResistanceFire = int.Parse(_bonusDEFFire.text);
      itemData.bonusResistanceEarth = int.Parse(_bonusDEFEarth.text);
      itemData.bonusResistanceWind = int.Parse(_bonusDEFWind.text);
      itemData.bonusResistanceWater = int.Parse(_bonusDEFWater.text);
      itemData.bonusResistanceAll = int.Parse(_bonusDEFAll.text);

      itemData.bonusDamagePhys = int.Parse(_bonusATKPhys.text);
      itemData.bonusDamageFire = int.Parse(_bonusATKFire.text);
      itemData.bonusDamageEarth = int.Parse(_bonusATKEarth.text);
      itemData.bonusDamageWind = int.Parse(_bonusATKWind.text);
      itemData.bonusDamageWater = int.Parse(_bonusATKWater.text);
      itemData.bonusDamageAll = int.Parse(_bonusATKAll.text);

      itemData.itemIconPath = _avatarSpritePath.text;

      return itemData;
   }

   public void loadUsableItemData (UsableItemData itemData) {
      startingName = itemData.itemName;
      _itemTypeText.text = itemData.type.ToString();
      _itemName.text = itemData.itemName;
      _itemDescription.text = itemData.description;

      _addedHP.text = itemData.addedHP.ToString();
      _addedAP.text = itemData.addedAP.ToString();
      _addedMaxHP.text = itemData.bonusMaxHP.ToString();
      _addedArmor.text = itemData.bonusArmor.ToString();
      _addedATK.text = itemData.bonusATK.ToString();

      _bonusINT.text = itemData.bonusINT.ToString();
      _bonusVIT.text = itemData.bonusVIT.ToString();
      _bonusPRE.text = itemData.bonusPRE.ToString();
      _bonusSPT.text = itemData.bonusSPT.ToString();
      _bonusLUK.text = itemData.bonusLUK.ToString();
      _bonusSTR.text = itemData.bonusSTR.ToString();

      _bonusDEFPhys.text = itemData.bonusResistancePhys.ToString();
      _bonusDEFFire.text = itemData.bonusResistanceFire.ToString();
      _bonusDEFEarth.text = itemData.bonusResistanceEarth.ToString();
      _bonusDEFWind.text = itemData.bonusResistanceWind.ToString();
      _bonusDEFWater.text = itemData.bonusResistanceWater.ToString();
      _bonusDEFAll.text = itemData.bonusResistanceAll.ToString();

      _bonusATKPhys.text = itemData.bonusDamagePhys.ToString();
      _bonusATKFire.text = itemData.bonusDamageFire.ToString();
      _bonusATKEarth.text = itemData.bonusDamageEarth.ToString();
      _bonusATKWind.text = itemData.bonusDamageWind.ToString();
      _bonusATKWater.text = itemData.bonusDamageWater.ToString();
      _bonusATKAll.text = itemData.bonusDamageAll.ToString();

      _avatarSpritePath.text = itemData.itemIconPath;
      if (itemData.itemIconPath != null) {
         _avatarIcon.sprite = ImageManager.getSprite(itemData.itemIconPath);
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
   private Button _itemTypeButton;
   [SerializeField]
   private Text _itemTypeText;

   // Item Name
   [SerializeField]
   private InputField _itemName;

   // Item Info
   [SerializeField]
   private InputField _itemDescription;

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
