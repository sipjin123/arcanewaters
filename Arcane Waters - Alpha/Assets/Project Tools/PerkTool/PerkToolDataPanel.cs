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

      populatePerkTypeDropdown();

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

   private void populatePerkTypeDropdown () {
      List<TMP_Dropdown.OptionData> perkTypeOptions = new List<TMP_Dropdown.OptionData>();
      Perk.Type[] perkTypes = Enum.GetValues(typeof(Perk.Type)) as Perk.Type[];

      foreach (Perk.Type type in perkTypes) {
         // Don't show the "None" type since we use it for saving unassigned points
         if (type != Perk.Type.None) {
            perkTypeOptions.Add(new TMP_Dropdown.OptionData(type.ToString()));
         } else {
            perkTypeOptions.Add(new TMP_Dropdown.OptionData("<color=red>Please assign the type of this perk"));
         }
      }

      _perkTypeIdDropdown.ClearOptions();
      _perkTypeIdDropdown.AddOptions(perkTypeOptions);
   }

   private PerkData getPerkData () {
      PerkData perkData = new PerkData();

      perkData.perkId = _currentPerkId;
      perkData.perkTypeId = _perkTypeIdDropdown.value;
      perkData.name = _perkName.text;
      perkData.description = _perkDescription.text;
      perkData.boostFactor = int.Parse(_boostFactor.text) / 100.0f;
      perkData.iconPath = _iconPath.text;

      return perkData;
   }

   public void loadData (PerkData data) {
      _perkName.text = data.name;
      _perkDescription.text = data.description;
      _perkTypeIdDropdown.value = data.perkTypeId;
      _boostFactor.text = (data.boostFactor * 100.0f).ToString();
      _currentPerkId = data.perkId;
      _iconImage.sprite = string.IsNullOrEmpty(data.iconPath) ? selectionPopup.emptySprite : ImageManager.getSprite(data.iconPath);
      _iconPath.text = data.iconPath;

      updateSaveButton();
   }

   #region Private Variables

   // The field for the name of the perk
   [SerializeField]
   private InputField _perkName;

   // The field for the description of the perk
   [SerializeField]
   private TMP_InputField _perkDescription;

   // The dropdown to choose the perk type
   [SerializeField]
   private TMP_Dropdown _perkTypeIdDropdown;

   // The boost factor of the perk
   [SerializeField]
   private InputField _boostFactor;

   // The text for the icon path
   [SerializeField]
   private Text _iconPath;

   // The icon of this perk
   [SerializeField]
   private Image _iconImage;

   // The button to change the perk icon
   [SerializeField]
   private Button _changeIconButton;

   // The id of the current perk in the DB
   private int _currentPerkId;

   #endregion
}
