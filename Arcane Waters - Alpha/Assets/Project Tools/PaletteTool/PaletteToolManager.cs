using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using UnityEngine.SceneManagement;

public class PaletteToolManager : XmlDataToolManager {
   #region Public Variables

   [Header("Main screen buttons")]
   // Button takes user to Master Tool
   public Button mainMenuButton;

   // Button initiates new palette creation process
   public Button createPaletteButton;

   // Dropdown sorting palette rows by type
   public TMPro.TMP_Dropdown mainMenuCategoryDropdown;

   [Header("Palette edit screen buttons")]
   // Button takes user to list of already created palettes
   public Button backToListButton;

   // Button saves progress in currently edited palette
   public Button savePaletteButton;

   // Button which initiates resetting palette scene
   public Button resetPaletteButton;

   // Button which initiates removing all currently set colors
   public Button deleteColorsButton;

   [Header("Prefabs")]
   // Palette prefab which is present in the form of scrolldown list
   public GameObject paletteButtonRowPrefab;

   // Single pixel square, either source or destination
   public GameObject singleColorPrefab;

   // Contains pair of source and destination square to allow grouping in UI
   public GameObject horizontalColorHolderPrefab;

   // Header separating recolors
   public GameObject recolorHeaderPrefab;

   // Slider which allows to do hue shift on all colors
   public GameObject hueSliderPrefab;

   [Header("Containers")]
   // Container for single pixel in palette (source - destination)
   public RectTransform colorsContainer;

   // Container for list of palettes downloaded from database
   public RectTransform paletteRowParent;

   // Main canvas gameobject showing palette options
   public GameObject paletteDataScene;

   [Header("Edit palette menu")]
   public Image previewSprite;
   public Text choosePaletteNameText;
   public Text choosePaletteTagText;
   public GameObject changePaletteScene;

   [Header("Color picker")]
   // Object containing "Color Picker" script allowing user to pick color
   public GameObject colorPicker;

   // Script controlling available presets in Color Picker
   public ColorPresets colorPresets;
   
   // Background button - if user press anything beside Color Picker, it disappears
   public GameObject colorPickerHideButton;

   // Start picking color from currently chosen sprite
   public Button pickColorButton;

   // Sprite preview using SpriteRender to allow using color picking
   public SpriteRenderer spritePreviewForColorPicker;

   // Text presenting current hue shift
   public Text hueValueText;

   [Header("Confirm deleting palette scene")]
   // Object holds UI with confirm/cancel button
   public GameObject confirmDeletingPaletteScene;

   // Confirm deleting palette and back to list (after reloading data)
   public Button confirmDeletingButton;

   // Cancel deleting palette and back to list (without reloading data)
   public Button cancelDeletingButton;

   [Header("Confirm resetting palette scene")]
   // Object holds UI with confirm/cancel button
   public GameObject confirmResettingPaletteScene;

   // Confirm resetting palette and using colors from current sprite
   public Button confirmResettingButton;

   // Cancel resetting palette - nothing happends then
   public Button cancelResettingButton;

   [Header("Size buttons")]
   public Button increaseSize;
   public Button decreaseSize;

   [Header("Choose file dropdown")]
   // Parent of elements used to choose file from user HDD
   public GameObject dropdownFileParent;

   // Dropdown element to populate with filenames to choose from
   public TMPro.TMP_Dropdown dropdownFileChoose;

   public class PaletteDataPair
   {
      // The xml ID of the palette
      public int paletteId;

      // The userID of the content creator
      public int creatorID;

      // Determines if palette is enabled
      public bool isEnabled;

      // Tag classifying palette
      public string tag;

      // Data of the palette
      public PaletteToolData paletteData;
   }

   public class PaletteRepresentation
   {
      public string name;
      public Color color;
   }

   [Header("Palette category")]
   // Dropdown element to populate with palette categories to choose from
   public TMPro.TMP_Dropdown dropdownPaletteCategory;

   [Header("Sprite references")]
   // Checkbox checked sprite for enabled palette
   public Sprite checkboxCheckedPaletteSprite;

   // Checkbox unchecked sprite for disabled palette
   public Sprite checkboxUncheckedPaletteSprite;

   // Checkbox checked sprite for static color box
   public Sprite checkboxCheckedBoxSprite;

   // Checkbox unchecked sprite for not static color box
   public Sprite checkboxUncheckedBoxSprite;

   [Header("Preview multiple layers")]
   // Turn on/off character preview in place of sprite preview
   public Button previewCharacterButton;

   // Show options of character preview
   public Button previewCharacterOptions;

   // GameObject containing all preview character options
   public GameObject multipleLayersScene;

   // Preview of player character - main window
   public Image playerBody;
   public Image playerHairBack;
   public Image playerHairFront;
   public Image playerEyes;
   public Image playerArmor;
   public Image playerWeaponFront;
   public Image playerWeaponBack;

   // Preview of player character - option screen
   public Image previewBody;
   public Image previewHairBack;
   public Image previewHairFront;
   public Image previewEyes;
   public Image previewArmor;
   public Image previewWeaponFront;
   public Image previewWeaponBack;

   [Header("Character preview - buttons")]
   // Buttons used to navigate between character preview options
   public Button showHairOptions;
   public Button showEyesOptions;
   public Button showArmorOptions;
   public Button showWeaponOptions;
   public Button hideOptions;
   public Button closeMultipleLayersPreview;

   [Header("Character preview - option containers")]
   // Objects containing UI of character preview options
   public GameObject optionHair;
   public GameObject optionEyes;
   public GameObject optionArmor;
   public GameObject optionWeapon;

   [Header("Character preview - dropdowns")]
   // Dropdowns allowing to choose different sprites for character preview
   public TMPro.TMP_Dropdown chooseSpriteHair;
   public TMPro.TMP_Dropdown chooseSpriteEyes;
   public TMPro.TMP_Dropdown chooseSpriteArmor;
   public TMPro.TMP_Dropdown chooseSpriteWeapon;

   // Dropdowns allowing to choose different palettes for character preview
   public TMPro.TMP_Dropdown[] palettesHair;
   public TMPro.TMP_Dropdown[] palettesEyes;
   public TMPro.TMP_Dropdown[] palettesArmor;
   public TMPro.TMP_Dropdown[] palettesWeapon;

   [Header("Character preview - dropdown buttons")]
   // After pressing these buttons, user can navigate to palette currently chosen in dropdown
   public Button[] palettesHairButton;
   public Button[] palettesEyesButton;
   public Button[] palettesArmorButton;
   public Button[] palettesWeaponButton;

   public enum PaletteImageType
   {
      None = 0,
      Armor = 1,
      Weapon = 2,
      Hair = 3,
      Eyes = 4,
      Body = 5,
      NPC = 6,
      Ship = 7,
      GuildIconBackground = 8,
      GuildIconSigil = 9,
      Guild = 10,
      Flag = 11,
      SeaStructure = 12,
      MAX = 13
   }

   public static string[] paletteImageTypePaths = {
      "",
      "Assets/Sprites/Armor/",
      "Assets/Sprites/Weapons/",
      "Assets/Sprites/Hair/",
      "Assets/Sprites/Eyes/",
      "Assets/Sprites/Bodies/",
      "Assets/Sprites/NPCs/",
      "Assets/Sprites/Ships/",
      "Assets/Sprites/GUI/Guild/Icons/Backgrounds/",
      "Assets/Sprites/GUI/Guild/Icons/Sigils/",
      "",
      "Assets/Sprites/Ships/",
      "Assets/Sprites/SeaStructures/",
      "",
   };

   #endregion

   protected override void Awake () {
      base.Awake();
      // After loading scene - download and preset xml data in list form
      loadXMLData();

      // Save/load buttons - sending or downloading data from database in xml form
      savePaletteButton.onClick.AddListener(() => {
         savePalette();
      });
      backToListButton.onClick.AddListener(() => {
         _isEditingRow = false;
         changePaletteScene.gameObject.SetActive(false);
         mainMenuCategoryDropdown.transform.parent.gameObject.SetActive(true);
         loadXMLData();
      });

      // Create new palette and open edit scene
      createPaletteButton.onClick.AddListener(() => {
         changePaletteScene.gameObject.SetActive(true);
         createNewPalette(8);
      });

      // Back to master tool
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      // Sort palette rows by type
      mainMenuCategoryDropdown.onValueChanged.AddListener((int index) => {
         if (index <= 0) {
            generateList(PaletteImageType.None);
         } else {
            generateList((PaletteImageType) index);
         }
      });

      // Changing size of currently edited palette (available in edit scene)
      increaseSize.onClick.AddListener(() => {
         changeArraySize(_srcColors.Count * 2, false);
      });
      decreaseSize.onClick.AddListener(() => {
         changeArraySize(_srcColors.Count / 2, false);
      });

      // Confirm or cancel deleting palette (available in cofirm delete scene)
      confirmDeletingButton.onClick.AddListener(() => {
         deleteXMLData(_paletteDataList[_currentRow.dataIndex].paletteId);
         confirmDeletingPaletteScene.gameObject.SetActive(false);
      });
      cancelDeletingButton.onClick.AddListener(() => {
         confirmDeletingPaletteScene.gameObject.SetActive(false);
      });

      // Confirm or cancel resetting palette
      resetPaletteButton.onClick.AddListener(() => {
         confirmResettingPaletteScene.gameObject.SetActive(true);
      });
      confirmResettingButton.onClick.AddListener(() => {
         generatePaletteColorImages(null, null);
         confirmResettingPaletteScene.gameObject.SetActive(false);
      });
      cancelResettingButton.onClick.AddListener(() => {
         confirmResettingPaletteScene.gameObject.SetActive(false);
      });

      deleteColorsButton.onClick.AddListener(() => {
         _srcColors.Clear();
         _dstColors.Clear();
         changeArraySize(MINIMUM_ALLOWED_SIZE, true);
         showPalettePreview();
      });

      // Start picking color from current sprite preview
      pickColorButton.onClick.AddListener(() => {
         startPickingColorFromSprite();
      });

      // Preview character with multiple layers
      previewCharacterButton.onClick.AddListener(() => {
         if (!playerBody.gameObject.activeSelf) {
            previewSprite.gameObject.SetActive(false);

            playerBody.gameObject.SetActive(true);
            playerHairBack.gameObject.SetActive(true);
            playerHairFront.gameObject.SetActive(true);
            playerEyes.gameObject.SetActive(true);
            playerArmor.gameObject.SetActive(true);
            playerWeaponFront.gameObject.SetActive(true);
            playerWeaponBack.gameObject.SetActive(true);

            updatePlayerCharacterSprite();
         } else {
            previewSprite.gameObject.SetActive(true);

            playerBody.gameObject.SetActive(false);
            playerHairBack.gameObject.SetActive(false);
            playerHairFront.gameObject.SetActive(false);
            playerEyes.gameObject.SetActive(false);
            playerArmor.gameObject.SetActive(false);
            playerWeaponFront.gameObject.SetActive(false);
            playerWeaponBack.gameObject.SetActive(false);
         }
      });
      previewCharacterOptions.onClick.AddListener(() => {
         multipleLayersScene.SetActive(true);

         optionHair.SetActive(false);
         optionEyes.SetActive(false);
         optionArmor.SetActive(false);
         optionWeapon.SetActive(false);

         showHairOptions.gameObject.SetActive(true);
         showEyesOptions.gameObject.SetActive(true);
         showArmorOptions.gameObject.SetActive(true);
         showWeaponOptions.gameObject.SetActive(true);
      });
      closeMultipleLayersPreview.onClick.AddListener(() => {
         updatePlayerCharacterSprite();
         bool updateHair, updateWeapon, updateArmor, updateEyes;
         checkIfPreviewContainsEditedColor(out updateHair, out updateWeapon, out updateArmor, out updateEyes);
         updateCharacterPreviewHue(updateHair, updateWeapon, updateArmor, updateEyes);
         multipleLayersScene.SetActive(false);

         optionHair.SetActive(false);
         optionEyes.SetActive(false);
         optionArmor.SetActive(false);
         optionWeapon.SetActive(false);

         showHairOptions.gameObject.SetActive(false);
         showEyesOptions.gameObject.SetActive(false);
         showArmorOptions.gameObject.SetActive(false);
         showWeaponOptions.gameObject.SetActive(false);
      });
      showHairOptions.onClick.AddListener(() => {
         optionHair.SetActive(true);

         hideShowOptionButtons();
         hideOptions.gameObject.SetActive(true);
      });
      showEyesOptions.onClick.AddListener(() => {
         optionEyes.SetActive(true);

         hideShowOptionButtons();
         hideOptions.gameObject.SetActive(true);
      });
      showArmorOptions.onClick.AddListener(() => {
         optionArmor.SetActive(true);

         hideShowOptionButtons();
         hideOptions.gameObject.SetActive(true);
      });
      showWeaponOptions.onClick.AddListener(() => {
         optionWeapon.SetActive(true);

         hideShowOptionButtons();
         hideOptions.gameObject.SetActive(true);
      });

      hideOptions.onClick.AddListener(() => {
         optionHair.SetActive(false);
         optionEyes.SetActive(false);
         optionArmor.SetActive(false);
         optionWeapon.SetActive(false);

         showHairOptions.gameObject.SetActive(true);
         showEyesOptions.gameObject.SetActive(true);
         showArmorOptions.gameObject.SetActive(true);
         showWeaponOptions.gameObject.SetActive(true);

         hideOptions.gameObject.SetActive(false);
      });
      prepareSpriteDropdown(PaletteImageType.Hair, chooseSpriteHair);
      prepareSpriteDropdown(PaletteImageType.Eyes, chooseSpriteEyes);
      prepareSpriteDropdown(PaletteImageType.Armor, chooseSpriteArmor);
      prepareSpriteDropdown(PaletteImageType.Weapon, chooseSpriteWeapon);

      chooseSpriteHair.onValueChanged.AddListener((int index) => {
         var list = ImageManager.getSpritesInDirectory(paletteImageTypePaths[(int) PaletteImageType.Hair]);
         List<ImageManager.ImageData> finalList = new List<ImageManager.ImageData>();
         
         foreach (ImageManager.ImageData imageData in list) {
            TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(imageData.imageName);
            if (imageData.imageName.ToLower().Contains("back")) {
               continue;
            }
            if (imageData.imagePath.ToLower().Contains(_isFemale ? "male" : "female")) {
               if (_isFemale) {
                  if (!imageData.imagePath.ToLower().Contains("female")) {
                     continue;
                  }
               } else {
                  continue;
               }
            }
            finalList.Add(imageData);
         }
         previewHairFront.sprite = finalList[index].sprites[INDEX_OF_FRONT_PREVIEW];

         string backPath = finalList[index].imagePath.Replace("front", "back");
         ImageManager.ImageData backSpriteData = list.Find((ImageManager.ImageData data) => data.imagePath.Equals(backPath));
         if (backSpriteData.sprites != null) {
            previewHairBack.enabled = true;
            previewHairBack.sprite = backSpriteData.sprites[INDEX_OF_FRONT_PREVIEW];
         } else {
            previewHairBack.enabled = false;
         }
         if (previewHairBack.sprite == null) {
            previewHairBack.enabled = false;
         }
      });
      chooseSpriteEyes.onValueChanged.AddListener((int index) => {
         var list = ImageManager.getSpritesInDirectory(paletteImageTypePaths[(int) PaletteImageType.Eyes]);
         List<ImageManager.ImageData> finalList = new List<ImageManager.ImageData>();

         foreach (ImageManager.ImageData imageData in list) {
            if (imageData.imagePath.ToLower().Contains(_isFemale ? "male" : "female")) {
               if (_isFemale) {
                  if (!imageData.imagePath.ToLower().Contains("female")) {
                     continue;
                  }
               } else {
                  continue;
               }
            }
            finalList.Add(imageData);
         }

         previewEyes.sprite = finalList[index].sprites[INDEX_OF_FRONT_PREVIEW];
      });
      chooseSpriteArmor.onValueChanged.AddListener((int index) => {
         var list = ImageManager.getSpritesInDirectory(paletteImageTypePaths[(int) PaletteImageType.Armor]);
         List<ImageManager.ImageData> finalList = new List<ImageManager.ImageData>();

         foreach (ImageManager.ImageData imageData in list) {
            if (imageData.imagePath.ToLower().Contains(_isFemale ? "male" : "female")) {
               if (_isFemale) {
                  if (!imageData.imagePath.ToLower().Contains("female")) {
                     continue;
                  }
               } else {
                  continue;
               }
            }
            finalList.Add(imageData);
         }

         previewArmor.sprite = finalList[index].sprites[INDEX_OF_FRONT_PREVIEW];
      });
      chooseSpriteWeapon.onValueChanged.AddListener((int index) => {
         var list = ImageManager.getSpritesInDirectory(paletteImageTypePaths[(int) PaletteImageType.Weapon]);
         List<ImageManager.ImageData> finalList = new List<ImageManager.ImageData>();

         foreach (ImageManager.ImageData imageData in list) {
            TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(imageData.imageName);
            if (imageData.imageName.ToLower().Contains("back")) {
               continue;
            }
            if (imageData.imagePath.ToLower().Contains(_isFemale ? "male" : "female")) {
               if (_isFemale) {
                  if (!imageData.imagePath.ToLower().Contains("female")) {
                     continue;
                  }
               } else {
                  continue;
               }
            }
            finalList.Add(imageData);
         }
         previewWeaponFront.sprite = finalList[index].sprites[INDEX_OF_FRONT_PREVIEW];

         string backPath = finalList[index].imagePath.Replace("front", "back");
         ImageManager.ImageData backSpriteData = list.Find((ImageManager.ImageData data) => data.imagePath.Equals(backPath));
         if (backSpriteData.sprites != null) {
            previewWeaponBack.enabled = true;
            previewWeaponBack.sprite = backSpriteData.sprites[INDEX_OF_FRONT_PREVIEW];
         } else {
            previewWeaponBack.enabled = false;
         }
         if (previewWeaponBack.sprite == null) {
            previewWeaponBack.enabled = false;
         }
      });

      prepareCharacterPreviewPaletteChoose();

      // Fill dropdown, used for choosing preview sprite, with filenames
      prepareSpriteChooseDropdown();

      // Fill dropdown, used for choosing palette types, with values based on palette type enum
      preparePaletteTypeDropdown();
   }

   public static List<PaletteRepresentation> getColors (PaletteImageType type, string subcategoryName, int tagToFind) {
      if ((int) type >= (int) PaletteImageType.MAX || (int) type <= (int) PaletteImageType.None) {
         D.error("Incorrect palette type specified");
         return new List<PaletteRepresentation>();
      }

      string key = type.ToString() + "_" + subcategoryName + "_" + tagToFind;
      if (_cachedGetColorData.ContainsKey(key)) {
         return _cachedGetColorData[key];
      }

      List<PaletteRepresentation> paletteRepresentations = new List<PaletteRepresentation>();
      List<PaletteToolData> paletteList = PaletteSwapManager.self.getPaletteList();
      if (paletteList == null || paletteList.Count == 0) {
         return new List<PaletteRepresentation>();
      }

      foreach (PaletteToolData paletteData in paletteList) {
         if (paletteData.paletteType == (int) type && paletteData.tagId == tagToFind && paletteData.subcategory == subcategoryName) {
            PaletteRepresentation paletteRepresentation = new PaletteRepresentation();
            paletteRepresentation.name = paletteData.paletteName;
            paletteRepresentation.color = PaletteSwapManager.getRepresentingColor(paletteData.dstColor);
            paletteRepresentations.Add(paletteRepresentation);
         }
      }

      if (!_cachedGetColorData.ContainsKey(key)) {
         _cachedGetColorData.Add(key, paletteRepresentations);
         return paletteRepresentations;
      }

      return new List<PaletteRepresentation>();
   }

   public static List<PaletteRepresentation> getColors (PaletteImageType type, string subcategoryName) {
      if ((int) type >= (int) PaletteImageType.MAX || (int) type <= (int) PaletteImageType.None) {
         D.error("Incorrect palette type specified");
         return new List<PaletteRepresentation>();
      }

      string key = type.ToString() + "_" + subcategoryName;
      if (_cachedGetColorData.ContainsKey(key)) {
         return _cachedGetColorData[key];
      }

      List<PaletteRepresentation> paletteRepresentations = new List<PaletteRepresentation>();
      List<PaletteToolData> paletteList = PaletteSwapManager.self.getPaletteList();
      if (paletteList == null || paletteList.Count == 0) {
         return new List<PaletteRepresentation>();
      }

      foreach (PaletteToolData paletteData in paletteList) {
         if (paletteData.paletteType == (int) type && paletteData.subcategory == subcategoryName) {
            PaletteRepresentation paletteRepresentation = new PaletteRepresentation();
            paletteRepresentation.name = paletteData.paletteName;
            paletteRepresentation.color = PaletteSwapManager.getRepresentingColor(paletteData.dstColor);
            paletteRepresentations.Add(paletteRepresentation);
         }
      }

      if (!_cachedGetColorData.ContainsKey(key)) {
         _cachedGetColorData.Add(key, paletteRepresentations);
         return paletteRepresentations;
      }

      return new List<PaletteRepresentation>();
   }

   public void updatePickingColorFromSprite (Color color) {
      colorPicker.GetComponent<ColorPicker>().CurrentColor = color;
   }

   public void finalizePickingColorFromSprite (bool ignoreColorChange = false) {
      paletteDataScene.SetActive(true);
      spritePreviewForColorPicker.gameObject.SetActive(false);
      changePaletteScene.gameObject.SetActive(true);
      colorPickerHideButton.gameObject.SetActive(true);
      colorPickerHideButton.gameObject.SetActive(false);
      colorPicker.SetActive(false);
   }

   public void finalizePickingColorFromSprite (bool ignoreColorChange, Color revertColor) {
      if (ignoreColorChange) {
         changeColorInPalette(revertColor);
      }
      finalizePickingColorFromSprite(ignoreColorChange);
   }

   public static Color convertHexToRGB (string hex) {
      hex = hex.Replace("#", "");
      int red = translateHexLetterToInt(hex[0]) * 16 + translateHexLetterToInt(hex[1]);
      int green = translateHexLetterToInt(hex[2]) * 16 + translateHexLetterToInt(hex[3]);
      int blue = translateHexLetterToInt(hex[4]) * 16 + translateHexLetterToInt(hex[5]);
      return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
   }

   #region Private methods

   private void hideShowOptionButtons () {
      showHairOptions.gameObject.SetActive(false);
      showEyesOptions.gameObject.SetActive(false);
      showArmorOptions.gameObject.SetActive(false);
      showWeaponOptions.gameObject.SetActive(false);
   }

   public void generateList (PaletteImageType type = PaletteImageType.None) {
      // Ignore first element which is a prefab
      for (int i = 1; i < paletteRowParent.childCount; i++) {
         Destroy(paletteRowParent.GetChild(i).gameObject);
      }

      // Prepare rows and buttons based on data downloaded from database
      for (int i = 0; i < _paletteDataList.Count; i++) {
         PaletteDataPair data = _paletteDataList[i];
         if (type != PaletteImageType.None && (PaletteImageType)data.paletteData.paletteType != type) {
            continue;
         }
         createRow(data, i);
      }
   }

   private PaletteButtonRow createRow (PaletteDataPair data, int index) {
      PaletteButtonRow row = GameObject.Instantiate(paletteButtonRowPrefab).GetComponent<PaletteButtonRow>();

      row.paletteName.text = data.paletteData.paletteName;
      row.dataIndex = index;
      row.editButton.onClick.AddListener(() => {
         showSingleRowEditor(row);
      });
      row.deleteButton.onClick.AddListener(() => {
         showDeleteConfirmation(row);
      });
      row.duplicateButton.onClick.AddListener(() => {
         duplicateSingleRow(row);
      });
      row.enableButton.onClick.AddListener(() => {
         if (row.enableButton.image.sprite == checkboxCheckedPaletteSprite) {
            row.enableButton.image.sprite = checkboxUncheckedPaletteSprite;
         } else {
            row.enableButton.image.sprite = checkboxCheckedPaletteSprite;
         }
         enableSingleRow(row);
      });

      row.enableButton.image.sprite = data.isEnabled ? checkboxCheckedPaletteSprite : checkboxUncheckedPaletteSprite;
      row.gameObject.GetComponent<RectTransform>().SetParent(paletteRowParent);
      row.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
      row.gameObject.SetActive(true);

      return row;
   }

   private void duplicateSingleRow (PaletteButtonRow row) {
      showSingleRowEditor(row);
      _currentRow = null;
      choosePaletteNameText.GetComponent<InputField>().text = _initialPaletteName;
      choosePaletteTagText.GetComponent<InputField>().text = "";
      _isEditingRow = true;
   }

   private void enableSingleRow (PaletteButtonRow row) {
      PaletteToolData data = _paletteDataList[row.dataIndex].paletteData;
      saveXMLData(data, findPaletteId(data), (isPaletteEnabled(data) + 1) % 2, choosePaletteTagText.text);
   }

   private void showDeleteConfirmation (PaletteButtonRow row) {
      _currentRow = row;
      confirmDeletingPaletteScene.gameObject.SetActive(true);
   }

   private void showSingleRowEditor (PaletteButtonRow row) {
      _isEditingRow = false;
      mainMenuCategoryDropdown.value = 0;

      changePaletteScene.gameObject.SetActive(true);
      mainMenuCategoryDropdown.transform.parent.gameObject.SetActive(false);

      // Convert data from string (hex format RRGGBB) to Unity.Color
      PaletteToolData data = _paletteDataList[row.dataIndex].paletteData;
      PaletteDataPair dataPair = _paletteDataList[row.dataIndex];
      List<Color> src = new List<Color>();
      List<Color> dst = new List<Color>();
      for (int i = 0; i < data.srcColor.Length; i++) {
         src.Add(convertHexToRGB(data.srcColor[i]));
      }
      for (int i = 0; i < data.dstColor.Length; i++) {
         dst.Add(convertHexToRGB(data.dstColor[i]));
      }
      _paletteImageType = (PaletteImageType) data.paletteType;
      string dropdownTextToFind = System.Enum.GetName(typeof(PaletteImageType), _paletteImageType);
      int paletteTypeIndex = dropdownPaletteCategory.options.FindIndex((TMPro.TMP_Dropdown.OptionData optionData) => optionData.text.Equals(dropdownTextToFind));
      if (paletteTypeIndex >= 0) {
         dropdownPaletteCategory.value = paletteTypeIndex;
      } else {
         dropdownPaletteCategory.value = 0;
      }

      prepareCharacterPreviewPaletteChoose();

      // Present edit scene with downloaded data
      _currentRow = row;
      generatePaletteColorImages(src, dst);
      showPalettePreview();
      choosePaletteNameText.GetComponent<InputField>().text = data.paletteName;
      choosePaletteTagText.GetComponent<InputField>().text = dataPair.tag;

      _isEditingRow = true;
   }

   private void changeArraySize (int size, bool clearColors) {
      if (getSourceColorSplitIndices(_paletteImageType).Count > 0) {
         return;
      }

      // Get current colors and pass to function generating new scene
      if (!clearColors) {
         if (size > MAXIMUM_ALLOWED_SIZE) {
            D.warning("Maximum allowed size reached (" + MAXIMUM_ALLOWED_SIZE + "), specified size of " + size + " is too big for palette");
            return;
         }
         if (size < MINIMUM_ALLOWED_SIZE) {
            D.warning("Minimum allowed size reached (" + MINIMUM_ALLOWED_SIZE + "), specified size of " + size + " is too small for palette");
            return;
         }

         List<Color> src = new List<Color>();
         List<Color> dst = new List<Color>();
         for (int i = 0; i < _srcColors.Count; i++) {
            src.Add(_srcColors[i].color);
         }
         for (int i = 0; i < _dstColors.Count; i++) {
            dst.Add(_dstColors[i].color);
         }
         generatePaletteColorImages(src, dst);
      } else {
         generatePaletteColorImages(null, null);
      }
   }

   private void deleteXMLData (int xmlID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePaletteXML(xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   private void saveXMLData (PaletteToolData data, int xmlID, int isEnabled, string tag) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => { 
         DB_Main.updatePaletteXML(longString, data.paletteName, xmlID, isEnabled, tag);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   private void loadXMLData () {
      _paletteDataList = new List<PaletteDataPair>();

      if (XmlLoadingPanel.self == null) {
         Invoke("loadXMLData", 0.1f);
         return;
      }

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getPaletteXML(false);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(newTextAsset);

               // Save the palette data in the memory cache
               PaletteDataPair newDataPair = new PaletteDataPair {
                  paletteData = paletteData,
                  creatorID = xmlPair.xmlOwnerId,
                  paletteId = xmlPair.xmlId,
                  isEnabled = xmlPair.isEnabled,
                  tag = xmlPair.tag
               };
               _paletteDataList.Add(newDataPair);
            }
            PaletteSwapManager.self.updateData();
            generateList();

            // Update palette categories after saving palette
            prepareCharacterPreviewPaletteChoose();

            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   private static int translateHexLetterToInt (char letter) {
      string s = new string(letter, 1);
      s = s.ToLower();
      switch (s) {
         case "a": return 10;
         case "b": return 11;
         case "c": return 12;
         case "d": return 13;
         case "e": return 14;
         case "f": return 15;
      }
      return int.Parse(s);
   }

   private static string convertRGBToHex (Color color) {
      return convertIntToHex(color.r) + convertIntToHex(color.g) + convertIntToHex(color.b);
   }

   private static string convertIntToHex(float val) {
      int dec = Mathf.RoundToInt(val * 255.0f);
      return translateIntToHexLetter(dec / 16) + translateIntToHexLetter(dec % 16);
   }

   private static string translateIntToHexLetter(int val) {
      switch (val) {
         case 10: return "A";
         case 11: return "B";
         case 12: return "C";
         case 13: return "D";
         case 14: return "E";
         case 15: return "F";
      }
      return val.ToString();
   }

   private void savePalette () {
      string[] src = new string[_srcColors.Count];
      string[] dst = new string[_dstColors.Count];
      for (int i = 0; i < _srcColors.Count; i++) {
         src[i] = convertRGBToHex(_srcColors[i].color);
         dst[i] = convertRGBToHex(_dstColors[i].color);
      }
      if (src.Length != dst.Length) {
         D.error("Source and destination color palette sizes are different. Cannot save palette in database!");
         return;
      }

      PaletteToolData data = new PaletteToolData(choosePaletteNameText.text, src, dst, (int) _paletteImageType);
      saveXMLData(data, findPaletteId(data), isPaletteEnabled(data), choosePaletteTagText.text);
   }

   private int findPaletteId (PaletteToolData data) {
      if (_currentRow == null) {
         int index = _paletteDataList.FindIndex((PaletteDataPair paletteDataPair) => paletteDataPair.paletteData.paletteName == data.paletteName);
         return (index != -1) ? _paletteDataList[index].paletteId : -1;
      }
      return _paletteDataList[_currentRow.dataIndex].paletteId;
   }

   private int isPaletteEnabled (PaletteToolData data) {
      if (_currentRow == null) {
         int index = _paletteDataList.FindIndex((PaletteDataPair paletteDataPair) => paletteDataPair.paletteData.paletteName == data.paletteName);
         return (index != -1) ? (_paletteDataList[index].isEnabled ? 1 : 0) : 1;
      }
      return _paletteDataList[_currentRow.dataIndex].isEnabled ? 1 : 0;
   }

   private void showPalettePreview () {
      previewSprite.material.SetTexture("_Palette", generateTexture2D());
   }

   private void createNewPalette (int size) {
      _isEditingRow = true;
      _currentRow = null;
      generatePaletteColorImages(null, null);
      showPalettePreview();
      choosePaletteNameText.GetComponent<InputField>().text = _initialPaletteName;
      choosePaletteTagText.GetComponent<InputField>().text = "";
   }

   private void generatePaletteColorImages (List<Color> srcColors, List<Color> dstColors) {
      // Genereate most often occurring colors and present as presets in Color Picker
      colorPicker.GetComponent<ColorPicker>().Setup.DefaultPresetColors = generateMostCommonPixelsInSprite(previewSprite.sprite).ToArray();

      // Clear old data from hierarchy
      for (int i = 0; i < colorsContainer.childCount; i++) {
         Destroy(colorsContainer.GetChild(i).gameObject);
      }
      
      // Get data
      List<Color> oldSrc = getSrcColors();
      srcColors = generateSourceColorsForType(_paletteImageType);

      // Clear cached colors
      _srcColors.Clear();
      _dstColors.Clear();

      // Check lists to avoid any nulls
      if (srcColors == null) {
         D.error("Source colors cannot be empty");
         return;
      }
      if (dstColors == null) {
         dstColors = new List<Color>();
      }

      // Use same number as src and dst colors
      for (int i = 0; i < srcColors.Count; i++) {
         if (dstColors.Count <= i) {
            dstColors.Add(srcColors[i]);
         }
      }
      if (dstColors.Count > srcColors.Count) {
         dstColors.RemoveRange(srcColors.Count, dstColors.Count - srcColors.Count);
      }

      // If color box was never changed - treat it as such and download new source color
      for (int i = 0; i < Mathf.Min(dstColors.Count, oldSrc.Count); i++) {
         if (dstColors[i] == oldSrc[i]) {
            dstColors[i] = srcColors[i];
         }
      }

      // Shouldn't ever fail but check for safety
      if (srcColors.Count != dstColors.Count) {
         D.warning("Source and destination color arrays are of different size!");
         return;
      }

      List<string> recolorNames = getNamesOfRecolors(_paletteImageType);
      List<int> splitIndices = getSourceColorSplitIndices(_paletteImageType);
      int splitIndex = -1;
      RectTransform parent = null;

      singleColorPrefab.gameObject.SetActive(true);
      for (int i = 0; i < srcColors.Count; i++) {
         // Create horizontal prefab which is holding source and destination pixels
         if (i == 0 || (splitIndices.Count > splitIndex && splitIndices[splitIndex] == i)) {
            if (splitIndices.Count > 0) {
               RectTransform headerParent = GameObject.Instantiate(horizontalColorHolderPrefab).GetComponent<RectTransform>();
               headerParent.GetComponent<RectTransform>().SetParent(colorsContainer);
               headerParent.gameObject.SetActive(true);

               recolorHeaderPrefab.SetActive(true);
               GameObject headerPrefab = GameObject.Instantiate(recolorHeaderPrefab);
               headerPrefab.GetComponentInChildren<Text>().text = recolorNames[splitIndex + 1];
               headerPrefab.GetComponent<RectTransform>().SetParent(headerParent);
               recolorHeaderPrefab.SetActive(false);
            }

            parent = GameObject.Instantiate(horizontalColorHolderPrefab).GetComponent<RectTransform>();
            parent.GetComponent<RectTransform>().SetParent(colorsContainer);
            parent.gameObject.SetActive(true);

            if (splitIndices.Count > 0) {
               RectTransform sliderParent = GameObject.Instantiate(horizontalColorHolderPrefab).GetComponent<RectTransform>();
               sliderParent.GetComponent<RectTransform>().SetParent(colorsContainer);
               sliderParent.gameObject.SetActive(true);

               hueSliderPrefab.gameObject.SetActive(true);
               GameObject sliderPrefab = GameObject.Instantiate(hueSliderPrefab).gameObject;
               Slider slider = sliderPrefab.GetComponentInChildren<Slider>();
               slider.name = splitIndex.ToString();
               slider.transform.parent.GetChild(2).GetComponent<Text>().text = "0";
               sliderPrefab.GetComponent<RectTransform>().SetParent(sliderParent);
               hueSliderPrefab.gameObject.SetActive(false);

               // Call function when changing hue slider value
               slider.onValueChanged.AddListener((float val) => {
                  changeColorsWithHueShift((int) val, int.Parse(slider.name));
                  slider.transform.parent.GetChild(2).GetComponent<Text>().text = ((int) val).ToString();
               });

               _hueShiftValues.Add(0);
            }

            splitIndex++;
         }

         // Source colors
         GameObject srcPrefab = GameObject.Instantiate(singleColorPrefab);
         srcPrefab.GetComponent<Image>().color = new Color(srcColors[i].r, srcColors[i].g, srcColors[i].b);
         srcPrefab.GetComponent<Button>().onClick.AddListener(() => {
            activeColorPicker(srcPrefab.GetComponent<Button>());
         });
         srcPrefab.GetComponent<RectTransform>().SetParent(parent);
         srcPrefab.gameObject.SetActive(false);
         _srcColors.Add(srcPrefab.GetComponent<Image>());

         // Destination colors
         GameObject dstPrefab = GameObject.Instantiate(singleColorPrefab);
         dstPrefab.GetComponent<Image>().color = new Color(dstColors[i].r, dstColors[i].g, dstColors[i].b);
         dstPrefab.GetComponent<Button>().onClick.AddListener(() => {
            activeColorPicker(dstPrefab.GetComponent<Button>());
         });
         dstPrefab.GetComponent<RectTransform>().SetParent(parent);
         _dstColors.Add(dstPrefab.GetComponent<Image>());
      }
      singleColorPrefab.gameObject.SetActive(false);
   }

   private List<string> getNamesOfRecolors (PaletteImageType type) {
      List<string> names = new List<string>();

      switch (type) {
         case PaletteImageType.Armor:
            names.Add(PaletteDef.Armor.primary.name);
            names.Add(PaletteDef.Armor.secondary.name);
            names.Add(PaletteDef.Armor.accent.name);
            break;
         case PaletteImageType.Weapon:
            names.Add(PaletteDef.Weapon.primary.name);
            names.Add(PaletteDef.Weapon.secondary.name);
            names.Add(PaletteDef.Weapon.power.name);
            break;
         case PaletteImageType.Hair:
            names.Add(PaletteDef.Hair.primary.name);
            names.Add(PaletteDef.Hair.secondary.name);
            break;
         case PaletteImageType.Eyes:
            names.Add(PaletteDef.Eyes.primary.name);
            break;
         case PaletteImageType.Body:
            break;
         case PaletteImageType.NPC:
            break;
         case PaletteImageType.Ship:
            names.Add(PaletteDef.Ship.hull.name);
            names.Add(PaletteDef.Ship.sail.name);
            names.Add(PaletteDef.Ship.flag.name);
            break;
         case PaletteImageType.GuildIconBackground:
            break;
         case PaletteImageType.GuildIconSigil:
            break;
         case PaletteImageType.Flag:
            names.Add(PaletteDef.Flag.flag.name);
            break;
         case PaletteImageType.SeaStructure:
            names.Add(PaletteDef.SeaStructure.fill.name);
            names.Add(PaletteDef.SeaStructure.outline.name);
            break;
      }

      return names;
   }

   private List<Color> generateSourceColorsForType(PaletteImageType type) {
      List<string> hexColors = new List<string>();

      switch (type) {
         case PaletteImageType.Armor:
            hexColors.AddRange(PaletteDef.Armor.primary.colorsHex);
            hexColors.AddRange(PaletteDef.Armor.secondary.colorsHex);
            hexColors.AddRange(PaletteDef.Armor.accent.colorsHex);
            break;
         case PaletteImageType.Weapon:
            hexColors.AddRange(PaletteDef.Weapon.primary.colorsHex);
            hexColors.AddRange(PaletteDef.Weapon.secondary.colorsHex);
            hexColors.AddRange(PaletteDef.Weapon.power.colorsHex);
            break;
         case PaletteImageType.Hair:
            hexColors.AddRange(PaletteDef.Hair.primary.colorsHex);
            hexColors.AddRange(PaletteDef.Hair.secondary.colorsHex);
            break;
         case PaletteImageType.Eyes:
            hexColors.AddRange(PaletteDef.Eyes.primary.colorsHex);
            break;
         case PaletteImageType.Body:
            break;
         case PaletteImageType.NPC:
            break;
         case PaletteImageType.Ship:
            hexColors.AddRange(PaletteDef.Ship.hull.colorsHex);
            hexColors.AddRange(PaletteDef.Ship.sail.colorsHex);
            hexColors.AddRange(PaletteDef.Ship.flag.colorsHex);
            break;
         case PaletteImageType.GuildIconBackground:
            break;
         case PaletteImageType.GuildIconSigil:
            break;
         case PaletteImageType.Flag:
            hexColors.AddRange(PaletteDef.Flag.flag.colorsHex);
            break;
         case PaletteImageType.SeaStructure:
            hexColors.AddRange(PaletteDef.SeaStructure.fill.colorsHex);
            hexColors.AddRange(PaletteDef.SeaStructure.outline.colorsHex);
            break;
      }

      List<Color> colors = new List<Color>();
      foreach (string hex in hexColors) {
         colors.Add(convertHexToRGB(hex));
      }

      return colors;
   }

   private List<int> getSourceColorSplitIndices (PaletteImageType type) {
      List<int> indices = new List<int>();

      switch (type) {
         case PaletteImageType.Armor:
            indices.Add(PaletteDef.Armor.primary.colorsHex.Length);
            indices.Add(PaletteDef.Armor.secondary.colorsHex.Length);
            indices.Add(PaletteDef.Armor.accent.colorsHex.Length);
            break;
         case PaletteImageType.Weapon:
            indices.Add(PaletteDef.Weapon.primary.colorsHex.Length);
            indices.Add(PaletteDef.Weapon.secondary.colorsHex.Length);
            indices.Add(PaletteDef.Weapon.power.colorsHex.Length);
            break;
         case PaletteImageType.Hair:
            indices.Add(PaletteDef.Hair.primary.colorsHex.Length);
            indices.Add(PaletteDef.Hair.secondary.colorsHex.Length);
            break;
         case PaletteImageType.Eyes:
            indices.Add(PaletteDef.Eyes.primary.colorsHex.Length);
            break;
         case PaletteImageType.Body:
            break;
         case PaletteImageType.NPC:
            break;
         case PaletteImageType.Ship:
            indices.Add(PaletteDef.Ship.hull.colorsHex.Length);
            indices.Add(PaletteDef.Ship.sail.colorsHex.Length);
            indices.Add(PaletteDef.Ship.flag.colorsHex.Length);
            break;
         case PaletteImageType.GuildIconBackground:
            break;
         case PaletteImageType.GuildIconSigil:
            break;
         case PaletteImageType.Flag:
            indices.Add(PaletteDef.Flag.flag.colorsHex.Length);
            break;
         case PaletteImageType.SeaStructure:
            indices.Add(PaletteDef.SeaStructure.fill.colorsHex.Length);
            indices.Add(PaletteDef.SeaStructure.outline.colorsHex.Length);
            break;
      }

      for (int i = 1; i < indices.Count; i++) {
         indices[i] += indices[i - 1];
      }

      return indices;
   }

   private void createRecolor () {

   }

   private void activeColorPicker (Button button) {
      _currentlyEditedElementInPalette = button;
      colorPicker.SetActive(true);
      colorPickerHideButton.SetActive(true);
      colorPicker.GetComponent<ColorPicker>().CurrentColor = new Color(button.GetComponent<Image>().color.r, button.GetComponent<Image>().color.g, button.GetComponent<Image>().color.b);

      colorPicker.GetComponent<ColorPicker>().onValueChanged.AddListener((Color color) => {
         changeColorInPalette(color);
      });
   }

   private void changeColorInPalette(Color color) {
      if (colorPicker.activeSelf) {
         _currentlyEditedElementInPalette.GetComponent<Image>().color = color;
         showPalettePreview();
      }
   }

   private void changeColorsWithHueShift (int newHueShift, int index) {
      List<int> splitIndices = getSourceColorSplitIndices(_paletteImageType);
      int startIndex = index == -1 ? 0 : splitIndices[index];
      int endIndex = splitIndices.Count > index + 1 ? splitIndices[index + 1] : getSrcColors().Count;

      hueValueText.text = newHueShift.ToString();
      float diff = (newHueShift - _hueShiftValues[index + 1]) / 255.0f;
      _hueShiftValues[index + 1] = newHueShift;

      for (int i = startIndex; i < endIndex; i++) {
         Image image = _dstColors[i];
         Color.RGBToHSV(image.color, out float H, out float S, out float V);
         H += diff;
         H %= 1.0f;
         if (H < 0.0f) {
            H += 1.0f;
         }
         image.color = Color.HSVToRGB(H, S, V, false);
      }
      showPalettePreview();

      // Modify elements for character preview, if they're using currently edited palette
      bool updateHair, updateWeapon, updateArmor, updateEyes;
      checkIfPreviewContainsEditedColor(out updateHair, out updateWeapon, out updateArmor, out updateEyes);
      updateCharacterPreviewHue(updateHair, updateWeapon, updateArmor, updateEyes);
   }

   private void checkIfPreviewContainsEditedColor (out bool hair, out bool weapon, out bool armor, out bool eyes) {
      hair = false;
      weapon = false;
      armor = false;
      eyes = false;

      if (_currentRow == null) {
         return;
      }
      string currentName = _paletteDataList[_currentRow.dataIndex].paletteData.paletteName;

      foreach (TMPro.TMP_Dropdown dropdown in palettesHair) {
         if (dropdown.options[dropdown.value].text == _paletteDataList[_currentRow.dataIndex].paletteData.paletteName) {
            hair = true;
         }
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesEyes) {
         if (dropdown.options[dropdown.value].text == _paletteDataList[_currentRow.dataIndex].paletteData.paletteName) {
            eyes = true;
         }
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesArmor) {
         if (dropdown.options[dropdown.value].text == _paletteDataList[_currentRow.dataIndex].paletteData.paletteName) {
            armor = true;
         }
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesWeapon) {
         if (dropdown.options[dropdown.value].text == _paletteDataList[_currentRow.dataIndex].paletteData.paletteName) {
            weapon = true;
         }
      }
   }

   private void updateCharacterPreviewHue (bool updateHair, bool updateWeapon, bool updateArmor, bool updateEyes) {
      Texture tex = generateTexture2D();
      const string texParam = "_Palette";

      // Weapon
      previewWeaponBack.material.SetTexture(texParam, _useCurrentPaletteWeapon || updateWeapon ? tex : null);
      previewWeaponFront.material.SetTexture(texParam, _useCurrentPaletteWeapon || updateWeapon ? tex : null);
      playerWeaponBack.material.SetTexture(texParam, _useCurrentPaletteWeapon || updateWeapon ? tex : null);
      playerWeaponFront.material.SetTexture(texParam, _useCurrentPaletteWeapon || updateWeapon ? tex : null);

      // Armor
      previewArmor.material.SetTexture(texParam, _useCurrentPaletteArmor || updateArmor ? tex : null);
      playerArmor.material.SetTexture(texParam, _useCurrentPaletteArmor || updateArmor ? tex : null);

      // Eyes
      previewEyes.material.SetTexture(texParam, _useCurrentPaletteEyes || updateEyes ? tex : null);
      playerEyes.material.SetTexture(texParam, _useCurrentPaletteEyes || updateEyes ? tex : null);

      // Hair
      previewHairBack.material.SetTexture(texParam, _useCurrentPaletteHair || updateHair ? tex : null);
      previewHairFront.material.SetTexture(texParam, _useCurrentPaletteHair || updateHair ? tex : null);
      playerHairBack.material.SetTexture(texParam, _useCurrentPaletteHair || updateHair ? tex : null);
      playerHairFront.material.SetTexture(texParam, _useCurrentPaletteHair || updateHair ? tex : null);
   }

   private void startPickingColorFromSprite () {
      paletteDataScene.SetActive(false);
      changePaletteScene.gameObject.SetActive(false);
      colorPickerHideButton.gameObject.SetActive(false);
      spritePreviewForColorPicker.gameObject.SetActive(true);
      spritePreviewForColorPicker.sprite = previewSprite.sprite;
      spritePreviewForColorPicker.material.SetTexture("_Palette", previewSprite.material.GetTexture("_Palette"));

      float desiredWidth = Screen.width * 0.4f;
      float startWidth = spritePreviewForColorPicker.sprite.textureRect.size.x;
      float scale = desiredWidth / startWidth;
      spritePreviewForColorPicker.transform.localScale = new Vector3(scale, scale, 1.0f);

      PaletteToolColorUnderCursor.self.activate(_currentlyEditedElementInPalette.GetComponent<Image>().color);
   }

   private void updatePlayerCharacterSprite () {
      // Update sprites
      playerBody.sprite = previewBody.sprite;
      playerHairBack.sprite = previewHairBack.sprite;
      playerHairFront.sprite = previewHairFront.sprite;
      playerEyes.sprite = previewEyes.sprite;
      playerArmor.sprite = previewArmor.sprite;
      playerWeaponFront.sprite = previewWeaponFront.sprite;
      playerWeaponBack.sprite = previewWeaponBack.sprite;

      // Update material if current one, doesn't allow recoloring with palette
      playerBody.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerHairBack.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerHairFront.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerEyes.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerArmor.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerWeaponFront.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      playerWeaponBack.GetComponent<RecoloredSprite>().checkMaterialAvailability();

      previewBody.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewHairBack.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewHairFront.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewEyes.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewArmor.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewWeaponFront.GetComponent<RecoloredSprite>().checkMaterialAvailability();
      previewWeaponBack.GetComponent<RecoloredSprite>().checkMaterialAvailability();

      // Copy palette texture
      const string texName = "_Palette2";
      if (playerBody.material.HasProperty(texName) && previewBody.material.HasProperty(texName)) {
         playerBody.material.SetTexture(texName, previewBody.material.GetTexture(texName));
         playerHairBack.material.SetTexture(texName, previewHairBack.material.GetTexture(texName));
         playerHairFront.material.SetTexture(texName, previewHairFront.material.GetTexture(texName));
         playerEyes.material.SetTexture(texName, previewEyes.material.GetTexture(texName));
         playerArmor.material.SetTexture(texName, previewArmor.material.GetTexture(texName));
         playerWeaponFront.material.SetTexture(texName, previewWeaponFront.material.GetTexture(texName));
         playerWeaponBack.material.SetTexture(texName, previewWeaponBack.material.GetTexture(texName));
      }

      // Enable or disable sprite based on preview objects
      playerBody.enabled = previewBody.enabled;
      playerHairBack.enabled = previewHairBack.enabled;
      playerHairFront.enabled = previewHairFront.enabled;
      playerEyes.enabled = previewEyes.enabled;
      playerArmor.enabled = playerArmor.enabled;
      playerWeaponFront.enabled = previewWeaponFront.enabled;
      playerWeaponBack.enabled = previewWeaponBack.enabled;
   }

   private void prepareSpriteDropdown (PaletteImageType type, TMPro.TMP_Dropdown dropdown) {
      dropdown.options.Clear();

      var list = ImageManager.getSpritesInDirectory(paletteImageTypePaths[(int) type]);

      foreach (ImageManager.ImageData imageData in list) {
         TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(imageData.imageName);
         if (imageData.imageName.ToLower().Contains("back")) {
            continue;
         }
         if (imageData.imagePath.ToLower().Contains(_isFemale ? "male" : "female")) {
            if (_isFemale) {
               if (!imageData.imagePath.ToLower().Contains("female")) {
                  continue;
               }
            } else {
               continue;
            }
         }
         dropdown.options.Add(optionData);
      }
   }

   private void preparePalleteDropdown (TMPro.TMP_Dropdown dropdown, PaletteImageType type) {
      dropdown.options.Clear();

      dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("BASE"));
      dropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("CURRENT PALETTE"));
      foreach (PaletteDataPair pair in _paletteDataList) {
         if (type == PaletteImageType.None || (PaletteImageType)pair.paletteData.paletteType == type) {
            TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(pair.paletteData.paletteName);
            dropdown.options.Add(optionData);
         }
      }
   }

   private void preparePaletteTypeDropdown () {
      dropdownPaletteCategory.options.Clear();

      for (int i = (int) PaletteImageType.None + 1; i < (int) PaletteImageType.MAX; i++) {
         string text = System.Enum.GetName(typeof(PaletteImageType), (PaletteImageType) i);
         TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(text);
         dropdownPaletteCategory.options.Add(optionData);
      }

      dropdownPaletteCategory.onValueChanged.AddListener((int index) => {
         _paletteImageType = (PaletteImageType) (index + 1);
         prepareSpriteChooseDropdown(paletteImageTypePaths[(int) _paletteImageType]);
         generatePaletteColorImages(null, getDstColors());
      });
      _paletteImageType = (PaletteImageType)1;
      prepareSpriteChooseDropdown(paletteImageTypePaths[(int) _paletteImageType]);
      generatePaletteColorImages(null, getDstColors());

      // Available dropdown values should be the same in row view as well as, when editing single row
      mainMenuCategoryDropdown.options.Clear();
      mainMenuCategoryDropdown.options.Add(new TMPro.TMP_Dropdown.OptionData("All"));
      mainMenuCategoryDropdown.options.AddRange(dropdownPaletteCategory.options);
   }

   private List<Color> getSrcColors () {
      if (_srcColors == null) {
         return new List<Color>();
      }

      List<Color> colors = new List<Color>();
      foreach (Image image in _srcColors) {
         colors.Add(image.color);
      }
      return colors;
   }

   private List<Color> getDstColors () {
      if (_dstColors == null) {
         return new List<Color>();
      }

      List<Color> colors = new List<Color>();
      foreach(Image image in _dstColors) {
         colors.Add(image.color);
      }
      return colors;
   }

   private void prepareCharacterPreviewPaletteChoose () {
      foreach (TMPro.TMP_Dropdown dropdown in palettesHair) {
         dropdown.onValueChanged.RemoveAllListeners();
         preparePalleteDropdown(dropdown, PaletteImageType.Hair);

         dropdown.onValueChanged.AddListener((int index) => {
            if (!previewHairBack.GetComponent<RecoloredSprite>()) {
               previewHairBack.gameObject.AddComponent<RecoloredSprite>();
            }
            if (!previewHairFront.GetComponent<RecoloredSprite>()) {
               previewHairFront.gameObject.AddComponent<RecoloredSprite>();
            }
            List<string> paletteNames = new List<string>();
            _useCurrentPaletteHair = false;
            foreach (TMPro.TMP_Dropdown d in palettesHair) {
               // Add names of correct palettes (skip base and currently edited palette)
               if (d.value > 1) {
                  paletteNames.Add(d.options[d.value].text);
               } else if (d.value == 1) {
                  _useCurrentPaletteHair = true;
               }
            }

            previewHairBack.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);
            previewHairFront.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);

            previewHairBack.material.SetTexture("_Palette", _useCurrentPaletteHair ? generateTexture2D() : null);
            previewHairFront.material.SetTexture("_Palette", _useCurrentPaletteHair ? generateTexture2D() : null);
         });
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesEyes) {
         dropdown.onValueChanged.RemoveAllListeners();
         preparePalleteDropdown(dropdown, PaletteImageType.Eyes);

         dropdown.onValueChanged.AddListener((int index) => {
            if (!previewEyes.GetComponent<RecoloredSprite>()) {
               previewEyes.gameObject.AddComponent<RecoloredSprite>();
            }
            List<string> paletteNames = new List<string>();
            _useCurrentPaletteEyes = false;
            foreach (TMPro.TMP_Dropdown d in palettesEyes) {
               // Add names of correct palettes (skip base and currently edited palette)
               if (d.value > 1) {
                  paletteNames.Add(d.options[d.value].text);
               } else if (d.value == 1) {
                  _useCurrentPaletteEyes = true;
               }
            }

            previewEyes.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);

            previewEyes.material.SetTexture("_Palette", _useCurrentPaletteEyes ? generateTexture2D() : null);
         });
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesArmor) {
         dropdown.onValueChanged.RemoveAllListeners();
         preparePalleteDropdown(dropdown, PaletteImageType.Armor);

         dropdown.onValueChanged.AddListener((int index) => {
            if (!previewArmor.GetComponent<RecoloredSprite>()) {
               previewArmor.gameObject.AddComponent<RecoloredSprite>();
            }
            List<string> paletteNames = new List<string>();
            _useCurrentPaletteArmor = false;
            foreach (TMPro.TMP_Dropdown d in palettesArmor) {
               // Add names of correct palettes (skip base and currently edited palette)
               if (d.value > 1) {
                  paletteNames.Add(d.options[d.value].text);
               } else if (d.value == 1) {
                  _useCurrentPaletteArmor = true;
               }
            }

            previewArmor.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);

            previewArmor.material.SetTexture("_Palette", _useCurrentPaletteArmor ? generateTexture2D() : null);
         });
      }
      foreach (TMPro.TMP_Dropdown dropdown in palettesWeapon) {
         dropdown.onValueChanged.RemoveAllListeners();
         preparePalleteDropdown(dropdown, PaletteImageType.Weapon);

         dropdown.onValueChanged.AddListener((int index) => {
            if (!previewWeaponBack.GetComponent<RecoloredSprite>()) {
               previewWeaponBack.gameObject.AddComponent<RecoloredSprite>();
            }
            if (!previewWeaponFront.GetComponent<RecoloredSprite>()) {
               previewWeaponFront.gameObject.AddComponent<RecoloredSprite>();
            }
            List<string> paletteNames = new List<string>();
            _useCurrentPaletteWeapon = false;
            foreach (TMPro.TMP_Dropdown d in palettesWeapon) {
               // Add names of correct palettes (skip base and currently edited palette)
               if (d.value > 1) {
                  paletteNames.Add(d.options[d.value].text);
               } else if (d.value == 1) {
                  _useCurrentPaletteWeapon = true;
               }
            }

            previewWeaponBack.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);
            previewWeaponFront.GetComponent<RecoloredSprite>().recolor(Item.parseItmPalette(paletteNames.ToArray()), 1);

            previewWeaponBack.material.SetTexture("_Palette", _useCurrentPaletteWeapon ? generateTexture2D() : null);
            previewWeaponFront.material.SetTexture("_Palette", _useCurrentPaletteWeapon ? generateTexture2D() : null);
         });
      }

      // Prepare buttons to move to chosen palette
      foreach (Button button in palettesHairButton) {
         button.onClick.AddListener(() => {
            for (int i = 0; i < palettesHairButton.Length; i++) {
               if (button == palettesHairButton[i] && palettesHair[i].value > 1) {
                  string text = palettesHair[i].options[palettesHair[i].value].text;
                  int indexPair = _paletteDataList.FindIndex((PaletteDataPair data) => data.paletteData.paletteName == text);
                  showSingleRowEditor(createRow(_paletteDataList[indexPair], indexPair));
                  multipleLayersScene.SetActive(false);
                  break;
               }
            }
         });
      }

      foreach (Button button in palettesArmorButton) {
         button.onClick.AddListener(() => {
            for (int i = 0; i < palettesArmorButton.Length; i++) {
               if (button == palettesArmorButton[i] && palettesArmor[i].value > 1) {
                  string text = palettesArmor[i].options[palettesArmor[i].value].text;
                  int indexPair = _paletteDataList.FindIndex((PaletteDataPair data) => data.paletteData.paletteName == text);
                  showSingleRowEditor(createRow(_paletteDataList[indexPair], indexPair));
                  multipleLayersScene.SetActive(false);
                  break;
               }
            }
         });
      }

      foreach (Button button in palettesEyesButton) {
         button.onClick.AddListener(() => {
            for (int i = 0; i < palettesEyesButton.Length; i++) {
               if (button == palettesEyesButton[i] && palettesEyes[i].value > 1) {
                  string text = palettesEyes[i].options[palettesEyes[i].value].text;
                  int indexPair = _paletteDataList.FindIndex((PaletteDataPair data) => data.paletteData.paletteName == text);
                  showSingleRowEditor(createRow(_paletteDataList[indexPair], indexPair));
                  multipleLayersScene.SetActive(false);
                  break;
               }
            }
         });
      }

      foreach (Button button in palettesWeaponButton) {
         button.onClick.AddListener(() => {
            for (int i = 0; i < palettesWeaponButton.Length; i++) {
               if (button == palettesWeaponButton[i] && palettesWeapon[i].value > 1) {
                  string text = palettesWeapon[i].options[palettesWeapon[i].value].text;
                  int indexPair = _paletteDataList.FindIndex((PaletteDataPair data) => data.paletteData.paletteName == text);
                  showSingleRowEditor(createRow(_paletteDataList[indexPair], indexPair));
                  multipleLayersScene.SetActive(false);
                  break;
               }
            }
         });
      }
   }

   private void prepareSpriteChooseDropdown (string path = "") {
      if (path == "") {
         return;
      }

      _cachedSpriteIconFiles = ImageManager.getSpritesInDirectory(path);
      dropdownFileChoose.options.Clear();

      foreach (ImageManager.ImageData imageData in _cachedSpriteIconFiles) {
         TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(imageData.imageName);
         dropdownFileChoose.options.Add(optionData);
      }

      dropdownFileChoose.onValueChanged.RemoveAllListeners();

      if (_cachedSpriteIconFiles.Count == 1) {
         choosePreviewSprite(0);
         dropdownFileChoose.value = 0;
      } else {
         dropdownFileChoose.onValueChanged.AddListener((int index) => {
            choosePreviewSprite(index);
         });
         choosePreviewSprite(0);
         dropdownFileChoose.value = 0;
      }
   }

   private void choosePreviewSprite (int dropdownIndex) {
      bool isPaletteEmpty = true;
      foreach (Image image in _srcColors) {
         if (image.color != Color.black) {
            isPaletteEmpty = false;
            break;
         }
      }

      if (isPaletteEmpty) {
         foreach (Image image in _dstColors) {
            if (image.color != Color.black) {
               isPaletteEmpty = false;
               break;
            }
         }
      }

      // Works only for "single sprite". We do not have enough information for slicing here
      previewSprite.sprite = _cachedSpriteIconFiles[dropdownIndex].sprite;
      StartCoroutine(updateColorsPresets());

      // If preview changed for empty palette - fill with sprite colors
      if (isPaletteEmpty) {
         generatePaletteColorImages(null, null);
      }
   }

   private IEnumerator updateColorsPresets () {
      yield return new WaitForSeconds(0.1f);
      colorPresets.forceUpdateColors(generateMostCommonPixelsInSprite(previewSprite.sprite));
   }

   private Texture2D generateTexture2D () {
      if (_srcColors.Count != _dstColors.Count) {
         D.error("Source and destination palette has different element count in canvas. Cannot generate Texture2D");
         return null;
      }

      Texture2D tex = new Texture2D(2, _srcColors.Count);
      tex.filterMode = FilterMode.Point;
      tex.wrapMode = TextureWrapMode.Clamp;

      for (int i = 0; i < _srcColors.Count; i++) {
         tex.SetPixel(0, i, _srcColors[i].color);
         tex.SetPixel(1, i, _dstColors[i].color);
      }

      tex.Apply();
      return tex;
   }

   private Color findNearestColor(Color referenceColor, List<Color> availableColors) {
      Color nearestColor = Color.black;
      float dotDiff = float.MaxValue;

      foreach (Color color in availableColors) {
         float tmpDiffDot = Vector3.Distance(new Vector3(color.r, color.g, color.b), new Vector3(referenceColor.r, referenceColor.g, referenceColor.b));

         if (tmpDiffDot < dotDiff) {
            nearestColor = color;
            dotDiff = tmpDiffDot;
         }
      }

      return nearestColor;
   }

   private void generateLutTextureFromFilePalette(Texture2D tex) {
      #if UNITY_EDITOR
      if (tex.width != 16 || tex.height != 16) {
         D.error("Please provide texture of size 16x16");
         return;
      }

      // Get texture to work with
      Texture2D readableCopy = createReadableTextureCopy(tex);

      // Fill list with pixels
      List<Color> availableColors = new List<Color>(readableCopy.GetPixels());

      Color[] finalList = new Color[256*16];

      for (int z = 0; z < 16; z++) {
         for (int y = 0; y < 16; y++) {
            for (int x = 0; x < 16; x++) {
               Color refColor = new Color((float) x / 15.0f, 1.0f - (float) z / 15.0f, (float) y / 15.0f);
               finalList[x + y * 16 + z * 256] = findNearestColor(refColor, availableColors); ;
            }
         }
      }

      // Save texture to png
      Texture2D outTex = new Texture2D(256, 16);
      outTex.wrapMode = TextureWrapMode.Clamp;
      outTex.filterMode = FilterMode.Point;
      outTex.SetPixels(finalList);
      byte[] atlasPng = outTex.EncodeToPNG();
      string path2 = Application.dataPath + "/Project Tools/PaletteTool/" + "lutTex.png";
      File.WriteAllBytes(path2, atlasPng);
      AssetDatabase.Refresh();
      #endif
   }

   private void generateTextureFromFilePalette(Texture2D tex, int popularRejectCount = 1) {
      #if UNITY_EDITOR
      // Get atlas texture to work with
      Texture2D readableCopy = createReadableTextureCopy(tex);

      List<Color> colorList = new List<Color>();
      Dictionary<Color, int> popularColors = new Dictionary<Color, int>();

      // Get all colors and check how often they occur
      Color[] pixels = readableCopy.GetPixels();
      foreach (Color color in pixels) {
         if (color.a > 0) {
            bool failed = false;
            foreach (Color arrayColor in colorList) {
               if (arrayColor.r == color.r && arrayColor.g == color.g && arrayColor.b == color.b) {
                  if (popularColors.ContainsKey(color)) {
                     popularColors[color]++;
                  } else {
                     popularColors.Add(color, 1);
                  }
                  failed = true;
                  break;
               }
            }
            if (failed) {
               continue;
            }
            colorList.Add(color);
         }
      }

      // Remove number of pixels that are not related to palette that we need (usually single color in-between palette colors)
      for (int i = 0; i < popularRejectCount; i++) {
         int maxValue = int.MinValue;
         Color currentColor = Color.black;
         foreach (KeyValuePair<Color, int> colorWeightPair in popularColors) {
            if (colorWeightPair.Value > maxValue) {
               maxValue = colorWeightPair.Value;
               currentColor = colorWeightPair.Key;
            }
         }

         colorList.Remove(currentColor);
         popularColors.Remove(currentColor);
         if (popularColors.Keys.Count == 0) {
            break;
         }
      }

      // Fill with data to correct texture size
      int size = Mathf.RoundToInt(Mathf.Sqrt(colorList.Count));
      for (int i = 0; i < (size * size) - colorList.Count; i++) {
         colorList.Add(Color.black);
      }

      colorList.Sort(sortColors);

      // Save texture to png
      Texture2D outTex = new Texture2D(size, size);
      outTex.wrapMode = TextureWrapMode.Clamp;
      outTex.filterMode = FilterMode.Point;      
      outTex.SetPixels(colorList.ToArray());
      byte[] atlasPng = outTex.EncodeToPNG();
      string path2 = Application.dataPath + "/Project Tools/PaletteTool/" + "mainPalette.png";
      File.WriteAllBytes(path2, atlasPng);
      AssetDatabase.Refresh();
      #endif
   }

   private List<Color> generateMostCommonPixelsInSprite (Sprite sprite, int count = 10) {
      // Get atlas texture to work with
      Texture2D readableCopy = createReadableTextureCopy(sprite.texture);

      // Create texture from cropped sprite
      Texture2D tex = new Texture2D((int) sprite.rect.width, (int) sprite.rect.height);
      Color[] spritePixels = readableCopy.GetPixels((int) sprite.textureRect.x, (int) sprite.textureRect.y, (int) sprite.textureRect.width, (int) sprite.textureRect.height);
      tex.SetPixels(spritePixels);
      tex.Apply();
      
      List<Color> colorList = new List<Color>();
      Dictionary<Color, int> popularColors = new Dictionary<Color, int>();

      // Get all colors and check how often they occur
      Color[] pixels = tex.GetPixels();
      foreach (Color color in pixels) {
         if (color.a > 0) {
            bool failed = false;
            foreach (Color arrayColor in colorList) {
               if (arrayColor.r == color.r && arrayColor.g == color.g && arrayColor.b == color.b) {
                  if (popularColors.ContainsKey(color)) {
                     popularColors[color]++;
                  } else {
                     popularColors.Add(color, 1);
                  }
                  failed = true;
                  break;
               }
            }
            if (failed) {
               continue;
            }
            colorList.Add(color);
         }
      }

      if (count <= 0) {
         return colorList;
      }

      // Clear list to reuse it as an output
      colorList.Clear();

      // Take most often occurring colors
      for (int i = 0; i < count; i++) {
         int maxValue = int.MinValue;
         Color currentColor = Color.black;
         foreach (KeyValuePair<Color, int> colorWeightPair in popularColors) {
            if (colorWeightPair.Value > maxValue) {
               maxValue = colorWeightPair.Value;
               currentColor = colorWeightPair.Key;
            }
         }

         colorList.Add(currentColor);
         popularColors.Remove(currentColor);
         if (popularColors.Keys.Count == 0) {
            break;
         }
      }

      return colorList;
   }

   private Texture2D createReadableTextureCopy (Texture2D tex) {
      // Create temporary render and copy texture data
      RenderTexture tmp = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
      Graphics.Blit(tex, tmp);

      // Save previous render to variable
      RenderTexture previous = RenderTexture.active;
      RenderTexture.active = tmp;

      // Create copy of Texture2D which is readable
      Texture2D copyTex = new Texture2D(tex.width, tex.height);
      copyTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
      copyTex.Apply();

      // Revert to old render texture
      RenderTexture.active = previous;
      RenderTexture.ReleaseTemporary(tmp);

      return copyTex;
   }

   private void generatePaletteFromAllAssets () {
      #if UNITY_EDITOR
      string[] spritesFolder = { "Assets/Sprites" };
      string[] allPaths = AssetDatabase.FindAssets("t:texture2D");

      HashSet<Color> uniqueColors = new HashSet<Color>();

      foreach (string path in allPaths) {
         string assetPath = AssetDatabase.GUIDToAssetPath(path);
         TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
         if (importer == null) {
            continue;
         }

         // Change import settings only if asset is not readable (and save this information)
         bool readableChanged = false;
         if (!importer.isReadable) {
            readableChanged = true;
            importer.isReadable = true;
            AssetDatabase.ImportAsset(assetPath);
         }

         // Enable read/write in texture
         Texture2D texture = (Texture2D) AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
         if (texture == null || !texture.isReadable) {
            if (readableChanged) {
               importer.isReadable = false;
               AssetDatabase.ImportAsset(assetPath);
            }
            continue;
         }

         // Add unique colors
         Color[] pixels = texture.GetPixels();
         foreach (Color color in pixels) {
            if (color.a > 0) {
               uniqueColors.Add(new Color(color.r, color.g, color.b, 1));
            }
         }

         // Revert to not readable if texture was not set as "read/write enabled"
         if (readableChanged) {
            importer.isReadable = false;
            AssetDatabase.ImportAsset(assetPath);
         }
      }

      AssetDatabase.Refresh();

      // Copy results to List; It's easier to work with it
      List<Color> finalColors = new List<Color>();
      finalColors.AddRange(uniqueColors);

      // Add elements to fill texture to 1024 width
      int demandedSize = (1024 * ((finalColors.Count / 1024) + 1));
      int height = (finalColors.Count / 1024) + 1;
      while (finalColors.Count < demandedSize) {
         finalColors.Add(new Color(0, 0, 0, 1));
      }
      finalColors.Sort(sortColors);

      // Save texture to png
      Texture2D tex = new Texture2D(1024, height);
      tex.SetPixels(finalColors.ToArray());
      byte[] atlasPng = tex.EncodeToPNG();
      string path2 = Application.dataPath + "/Project Tools/PaletteTool/" + "allColorsInProject.png";
      File.WriteAllBytes(path2, atlasPng);
      AssetDatabase.Refresh();
      #endif
   }

   private int sortColors (Color a, Color b) {
      if (a.r < b.r)
         return 1;
      else if (a.r > b.r)
         return -1;
      else {
         if (a.g < b.g)
            return 1;
         else if (a.g > b.g)
            return -1;
         else {
            if (a.b < b.b)
               return 1;
            else if (a.b > b.b)
               return -1;
         }
      }
      return 0;
   }

   #endregion

   #region Private Variables

   // Button representing palette pixel which can be edited
   private Button _currentlyEditedElementInPalette;

   // Data downloaded from database table
   private List<PaletteDataPair> _paletteDataList;

   // Cached source colors which are currently edited
   private List<Image> _srcColors = new List<Image>();

   // Cached destination colors which are currently edited
   private List<Image> _dstColors = new List<Image>();

   // Currently edited row of data
   private PaletteButtonRow _currentRow;

   // Cached all images available in project used for preview sprite
   private List<ImageManager.ImageData> _cachedSpriteIconFiles = new List<ImageManager.ImageData>();

   // Current palette image type
   private PaletteImageType _paletteImageType = PaletteImageType.None;

   // Cached data for getColors function
   private static Dictionary<string, List<PaletteRepresentation>> _cachedGetColorData = new Dictionary<string, List<PaletteRepresentation>>();

   // Value of hue shift in current palette;
   private List<int> _hueShiftValues = new List<int>();

   // Checking if user is browsing list or editing single row
   private bool _isEditingRow = false;

   // Initial name of palette which can be changed by user
   private static string _initialPaletteName = "Choose name";

   // Maximum allowed size of palette
   private const int MAXIMUM_ALLOWED_SIZE = 128;

   // Minimum allowed size of palette
   private const int MINIMUM_ALLOWED_SIZE = 4;

   // Index in spritesheet which indicate frontfacing element
   private int INDEX_OF_FRONT_PREVIEW = 8;

   // Sex of currently previewed character
   private bool _isFemale = true;

   // Check if character preview is using current palette
   private bool _useCurrentPaletteHair = false;
   private bool _useCurrentPaletteEyes = false;
   private bool _useCurrentPaletteArmor = false;
   private bool _useCurrentPaletteWeapon = false;

   #endregion
}
