using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiscoveriesToolPanel : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool manager
   public DiscoveriesToolManager toolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         DiscoveryData itemData = getDiscoveryData();
         if (itemData != null) {
            toolManager.saveDiscoveryData(itemData);
            gameObject.SetActive(false);
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadDiscoveriesList();
      });

      _changeIconButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.DiscoverySprites, _discoverySourceImage, _sourceImagePath);
      });

      List<Rarity.Type> rarityTypes = ((Rarity.Type[]) System.Enum.GetValues(typeof(Rarity.Type))).ToList();
      List<TMP_Dropdown.OptionData> dropdownOptions = new List<TMP_Dropdown.OptionData>();

      _rarityDropdown.ClearOptions();

      foreach (Rarity.Type rarityType in rarityTypes) {
         TMP_Dropdown.OptionData option = new TMP_Dropdown.OptionData(rarityType.ToString());
         dropdownOptions.Add(option);
      }

      _rarityDropdown.AddOptions(dropdownOptions);
   }

   public void loadData (DiscoveryData discoveryData) {
      _discoveryName.text = discoveryData.name;
      _discoveryDescription.text = discoveryData.description;
      _rarityDropdown.value = (int)discoveryData.rarity;
      _discoveryId = discoveryData.discoveryId.ToString();

      if (!string.IsNullOrEmpty(discoveryData.spriteUrl) && selectionPopup.discoveriesSpriteList.ContainsKey(discoveryData.spriteUrl)) {
         _discoverySourceImage.sprite = selectionPopup.discoveriesSpriteList[discoveryData.spriteUrl];
      }
   }

   private DiscoveryData getDiscoveryData () {
      DiscoveryData data = new DiscoveryData();

      data.name = _discoveryName.text;
      data.description = _discoveryDescription.text;
      data.rarity = (Rarity.Type)_rarityDropdown.value;
      data.discoveryId = int.Parse(_discoveryId);
      data.spriteUrl = _sourceImagePath.text;

      return data;
   }

   #region Private Variables

   // UI for Icon 
   [SerializeField]
   private Button _changeIconButton;
   [SerializeField]
   private Image _discoverySourceImage;
   [SerializeField]
   private Text _sourceImagePath;

   // Name of the achievement
   [SerializeField]
   private InputField _discoveryName;

   // The dropdown for rarity selection
   [SerializeField]
   private TMP_Dropdown _rarityDropdown;

   // Description of the achievement
   [SerializeField]
   private InputField _discoveryDescription;

   // The unique ID of this discovery
   private string _discoveryId;

   #endregion
}
