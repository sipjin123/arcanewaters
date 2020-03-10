using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropsDataPanel : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public CropsDataToolManager cropToolManager;

   // Holds the selection popup
   public GenericSelectionPopup selectionPopup;

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Reference to current xml id
   public int currentXmlId;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         CropsData newCropData = getCropData();
         if (newCropData != null) {
            cropToolManager.saveXMLData(newCropData, currentXmlId, _isEnabled.isOn);
            gameObject.SetActive(false);
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         cropToolManager.loadXMLData();
      });

      _cropTypeSlider.maxValue = Enum.GetValues(typeof(Crop.Type)).Length;
      _cropTypeSlider.onValueChanged.AddListener(_ => {
         _cropTypeLabel.text = ((Crop.Type) _).ToString();
      });

      _cropIconButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.CropsIcon, _cropImage, _cropIconPath);
      });
   }

   public void loadData (CropsData cropData, int xml_id) {
      currentXmlId = xml_id;

      _cropTypeSlider.value = cropData.cropsType;
      _cropIconPath.text = cropData.iconPath;
      _isEnabled.isOn = cropData.isEnabled;
      _cropNameField.text = cropData.xmlName;
      _cropDescriptionField.text = cropData.xmlDescription;
      _cropGrowthRate.text = cropData.growthRate.ToString();
      _cropCost.text = cropData.cost.ToString();

      if (cropData.iconPath != "") {
         _cropImage.sprite = ImageManager.getSprite(cropData.iconPath);
      }
   }

   public CropsData getCropData () {
      CropsData cropData = new CropsData();

      cropData.cropsType = (int) _cropTypeSlider.value;
      cropData.iconPath = _cropIconPath.text;
      cropData.isEnabled = _isEnabled.isOn;
      cropData.xmlId = currentXmlId;
      cropData.xmlName = _cropNameField.text;
      cropData.xmlDescription = _cropDescriptionField.text;
      cropData.cost = int.Parse(_cropCost.text);
      cropData.growthRate = float.Parse(_cropGrowthRate.text);

      return cropData;
   }

   #region Private Variables
#pragma warning disable 0649

   // The name of the crop
   [SerializeField, Header("Input Data")]
   private InputField _cropNameField;

   // The info of the crop
   [SerializeField]
   private InputField _cropDescriptionField;

   // The crop type if it is a tomato etc
   [SerializeField]
   private Slider _cropTypeSlider;
   [SerializeField]
   private Text _cropTypeLabel;

   // The growth rate of the crop
   [SerializeField]
   private InputField _cropGrowthRate;

   // The cost of the crop
   [SerializeField]
   private InputField _cropCost;

   // The Crop image
   [SerializeField]
   private Button _cropIconButton;
   [SerializeField]
   private Text _cropIconPath;
   [SerializeField]
   private Image _cropImage;

   // Determines if the crop data is enabled
   [SerializeField]
   private Toggle _isEnabled;

#pragma warning restore 0649
   #endregion
}