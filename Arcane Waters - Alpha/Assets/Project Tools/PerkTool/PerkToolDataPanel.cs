using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using TMPro;

public class PerkToolDataPanel : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool manager
   public PerkToolManager toolManager;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // The icon selection popup
   public GenericSelectionPopup selectionPopup;

   // Caches the initial type incase it is changed
   public string startingName;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         PerkData itemData = getPerkData();
         if (itemData != null) {
            toolManager.saveXMLData(itemData);
            gameObject.SetActive(false);
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _changeIconButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.PerkIcon, _iconImage, _iconPath);
      });

      populatePerkCategoryDropdown();

      // Only enable the "save" button for valid values
      _perkTypeIdDropdown.onValueChanged.AddListener((x) => updateSaveButton());
      _perkName.onValueChanged.AddListener((x) => updateSaveButton());
   }

   private void updateSaveButton () {
      saveButton.interactable = isInputValid();
   }

   private bool isInputValid () {
      if (string.IsNullOrEmpty(_perkName.text)) {
         return false;
      }

      if (_perkTypeIdDropdown.value == 0) {
         return false;
      }

      return true;
   }

   private void populatePerkCategoryDropdown () {
      List<TMP_Dropdown.OptionData> perkCategoryOptions = new List<TMP_Dropdown.OptionData>();
      Perk.Category[] categories = Enum.GetValues(typeof(Perk.Category)) as Perk.Category[];

      foreach (Perk.Category category in categories) {
         // Don't show the "None" type since we use it for saving unassigned points
         if (category != Perk.Category.None) {
            perkCategoryOptions.Add(new TMP_Dropdown.OptionData(category.ToString()));
         } else {
            perkCategoryOptions.Add(new TMP_Dropdown.OptionData("<color=red>Please assign the type of this perk"));
         }
      }

      _perkTypeIdDropdown.ClearOptions();
      _perkTypeIdDropdown.AddOptions(perkCategoryOptions);
   }

   private PerkData getPerkData () {
      PerkData perkData = new PerkData();

      perkData.perkId = _currentPerkId;
      perkData.perkCategoryId = _perkTypeIdDropdown.value;
      perkData.name = _perkName.text;
      perkData.description = _perkDescription.text;
      perkData.boostFactor = int.Parse(_boostFactor.text) / 100.0f;
      perkData.iconPath = _iconPath.text;

      return perkData;
   }

   public void loadData (PerkData data) {
      _perkName.text = data.name;
      _perkDescription.text = data.description;
      _perkTypeIdDropdown.value = data.perkCategoryId;
      _boostFactor.text = (data.boostFactor * 100.0f).ToString();
      _currentPerkId = data.perkId;
      _iconImage.sprite = string.IsNullOrEmpty(data.iconPath) ? selectionPopup.emptySprite : ImageManager.getSprite(data.iconPath);
      _iconPath.text = data.iconPath;

      updateSaveButton();
   }

   #region Private Variables

   // The field for the name of the perk
   [SerializeField]
   private InputField _perkName = default;

   // The field for the description of the perk
   [SerializeField]
   private TMP_InputField _perkDescription = default;

   // The dropdown to choose the perk type
   [SerializeField]
   private TMP_Dropdown _perkTypeIdDropdown = default;

   // The boost factor of the perk
   [SerializeField]
   private InputField _boostFactor = default;

   // The text for the icon path
   [SerializeField]
   private Text _iconPath = default;

   // The icon of this perk
   [SerializeField]
   private Image _iconImage = default;

   // The button to change the perk icon
   [SerializeField]
   private Button _changeIconButton = default;

   // The id of the current perk in the DB
   private int _currentPerkId;

   #endregion
}
