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

   [Header("Palette edit screen buttons")]
   // Button takes user to list of already created palettes
   public Button backToListButton;

   // Button saves progress in currently edited palette
   public Button savePaletteButton;

   [Header("Prefabs")]
   // Palette prefab which is present in the form of scrolldown list
   public GameObject paletteButtonRowPrefab;

   // Single pixel square, either source or destination
   public GameObject singleColorPrefab;

   // Contains pair of source and destination square to allow grouping in UI
   public GameObject horizontalColorHolderPrefab;

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
   public Text currentSizeText;
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

   [Header("Confirm deleting palette scene")]
   // Object holds UI with confirm/cancel button
   public GameObject confirmDeletingPaletteScene;

   // Confirm deleting palette and back to list (after reloading data)
   public Button confirmDeletingButton;

   // Cancel deleting palette and back to list (without reloading data)
   public Button cancelDeletingButton;

   [Header("Size buttons")]
   public Button setSize8;
   public Button setSize16;
   public Button setSize32;
   public Button setSize64;
   public Button setSize128;

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

      // Data of the palette
      public PaletteToolData paletteData;
   }

   [Header("Palette type dropwdown")]
   // Parent of elements used to pick palette type
   public GameObject dropdownPaletteTypeParent;

   // Dropdown element to populate with palette types to choose from
   public TMPro.TMP_Dropdown dropdownPaletteType;

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
      MAX = 8
   }

   #endregion

   public void Awake () {
      // After loading scene - download and preset xml data in list form
      loadXMLData();

      // Save/load buttons - sending or downloading data from database in xml form
      savePaletteButton.onClick.AddListener(() => {
         savePalette();
      });
      backToListButton.onClick.AddListener(() => {
         changePaletteScene.gameObject.SetActive(false);
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

      // Changing size of currently edited palette (available in edit scene)
      setSize8.onClick.AddListener(() => {
         changeArraySize(8);
      });
      setSize16.onClick.AddListener(() => {
         changeArraySize(16);
      });
      setSize32.onClick.AddListener(() => {
         changeArraySize(32);
      });
      setSize64.onClick.AddListener(() => {
         changeArraySize(64);
      });
      setSize128.onClick.AddListener(() => {
         changeArraySize(128);
      });

      // Confirm or cancel deleting palette (available in cofirm delete scene)
      confirmDeletingButton.onClick.AddListener(() => {
         deleteXMLData(_paletteDataList[_currentRow.dataIndex].paletteId);
         confirmDeletingPaletteScene.gameObject.SetActive(false);
      });
      cancelDeletingButton.onClick.AddListener(() => {
         confirmDeletingPaletteScene.gameObject.SetActive(false);
      });

      // Start picking color from current sprite preview
      pickColorButton.onClick.AddListener(() => {
         startPickingColorFromSprite();
      });

      // Fill dropdown, used for choosing preview sprite, with filenames
      prepareSpriteChooseDropdown();

      // Fill dropdown, used for choosing palette types, with values based on palette type enum
      preparePaletteTypeDropdown();
   }

   public void generateList () {
      // Ignore first element which is a prefab
      for (int i = 1; i < paletteRowParent.childCount; i++) {
         Destroy(paletteRowParent.GetChild(i).gameObject);
      }

      // Prepare rows and buttons based on data downloaded from database
      for (int i = 0; i < _paletteDataList.Count; i++) {
         PaletteButtonRow row = GameObject.Instantiate(paletteButtonRowPrefab).GetComponent<PaletteButtonRow>();
         PaletteDataPair data = _paletteDataList[i];

         row.paletteName.text = data.paletteData.paletteName;
         row.dataIndex = i;
         row.size.text = data.paletteData.size.ToString();
         row.editButton.onClick.AddListener(() => {
            showSingleRowEditor(row);
         });
         row.deleteButton.onClick.AddListener(() => {
            showDeleteConfirmation(row);
         });

         row.gameObject.GetComponent<RectTransform>().SetParent(paletteRowParent);
         row.gameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
         row.gameObject.SetActive(true);
      }
   }

   private void showDeleteConfirmation (PaletteButtonRow row) {
      _currentRow = row;
      confirmDeletingPaletteScene.gameObject.SetActive(true);
   }

   private void showSingleRowEditor (PaletteButtonRow row) {
      changePaletteScene.gameObject.SetActive(true);

      // Convert data from string (hex format RRGGBB) to Unity.Color
      PaletteToolData data = _paletteDataList[row.dataIndex].paletteData;
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
      int paletteTypeIndex = dropdownPaletteType.options.FindIndex((TMPro.TMP_Dropdown.OptionData optionData) => optionData.text.Equals(dropdownTextToFind));
      if (paletteTypeIndex >= 0) {
         dropdownPaletteType.SetValueWithoutNotify(paletteTypeIndex);
      } else {
         dropdownPaletteType.SetValueWithoutNotify(0);
      }

      // Present edit scene with downloaded data
      _currentRow = row;
      generatePaletteColorImages(src, dst, data.size);
      showPalettePreview();
      choosePaletteNameText.GetComponent<InputField>().text = data.paletteName;
   }

   private void changeArraySize (int size) {
      // Get current colors and pass to function generating new scene
      List<Color> src = new List<Color>();
      List<Color> dst = new List<Color>();
      for (int i = 0; i < _srcColors.Count; i++) {
         src.Add(_srcColors[i].color);
      }
      for (int i = 0; i < _dstColors.Count; i++) {
         dst.Add(_dstColors[i].color);
      }
      generatePaletteColorImages(src, dst, size);
   }

   public void deleteXMLData (int xmlID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePaletteXML(xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void saveXMLData (PaletteToolData data, int xmlID) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => { 
         DB_Main.updatePaletteXML(longString, data.paletteName, xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _paletteDataList = new List<PaletteDataPair>();

      if (XmlLoadingPanel.self == null) {
         Invoke("loadXMLData", 0.1f);
         return;
      }

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getPaletteXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               PaletteToolData paletteData = Util.xmlLoad<PaletteToolData>(newTextAsset);

               // Save the palette data in the memory cache
               PaletteDataPair newDataPair = new PaletteDataPair {
                  paletteData = paletteData,
                  creatorID = xmlPair.xmlOwnerId,
                  paletteId = xmlPair.xmlId
               };
               _paletteDataList.Add(newDataPair);
            }
            generateList();
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public static Color convertHexToRGB(string hex) {
      int red = translateHexLetterToInt(hex[0]) * 16 + translateHexLetterToInt(hex[1]);
      int green = translateHexLetterToInt(hex[2]) * 16 + translateHexLetterToInt(hex[3]);
      int blue = translateHexLetterToInt(hex[4]) * 16 + translateHexLetterToInt(hex[5]);
      return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
   }

   private static int translateHexLetterToInt (char letter) {
      switch (letter) {
         case 'A': return 10;
         case 'B': return 11;
         case 'C': return 12;
         case 'D': return 13;
         case 'E': return 14;
         case 'F': return 15;
      }
      string s = new string(letter, 1);
      return int.Parse(s);
   }

   public static string convertRGBToHex(Color color) {
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
      PaletteToolData data = new PaletteToolData(choosePaletteNameText.text, src.Length, src, dst, (int) _paletteImageType);
      saveXMLData(data, _currentRow == null ? -1 : _paletteDataList[_currentRow.dataIndex].paletteId);
   }

   private void showPalettePreview () {
      previewSprite.material.SetTexture("_Palette", generateTexture2D());
   }

   private void createNewPalette (int size) {
      _currentRow = null;
      generatePaletteColorImages(null, null, size);
      showPalettePreview();
      choosePaletteNameText.GetComponent<InputField>().text = "Choose name";
   }

   private void generatePaletteColorImages (List<Color> srcColors, List<Color> dstColors, int size) {
      // Genereate most often occurring colors and present as presets in Color Picker
      colorPicker.GetComponent<ColorPicker>().Setup.DefaultPresetColors = generateMostCommonPixelsInSprite(previewSprite.sprite).ToArray();

      // Clear old data from hierarchy
      for (int i = 0; i < colorsContainer.childCount; i++) {
         Destroy(colorsContainer.GetChild(i).gameObject);
      }
      
      // Clear cached colors
      _srcColors.Clear();
      _dstColors.Clear();

      if (srcColors == null) {
         srcColors = new List<Color>();
      }
      if (dstColors == null) {
         dstColors = new List<Color>();
      }

      if (srcColors.Count != dstColors.Count) {
         D.warning("Source and destination color arrays are of different size!");
         return;
      }

      currentSizeText.text = "Current size: " + size.ToString();

      // Fill source colors (with black) to desired size
      if (srcColors.Count < size) {
         int count = size - srcColors.Count;
         for (int i = 0; i < count; i++) {
            srcColors.Add(Color.black);
         }
      }

      // Fill destination colors (with black) to desired size
      if (dstColors.Count < size) {
         int count = size - dstColors.Count;
         for (int i = 0; i < count; i++) {
            dstColors.Add(Color.black);
         }
      }

      singleColorPrefab.gameObject.SetActive(true);
      for (int i = 0; i < size; i++) {
         // Create horizontal prefab which is holding source and destination pixels
         RectTransform parent = GameObject.Instantiate(horizontalColorHolderPrefab).GetComponent<RectTransform>();
         parent.GetComponent<RectTransform>().SetParent(colorsContainer);
         parent.gameObject.SetActive(true);

         GameObject srcPrefab = GameObject.Instantiate(singleColorPrefab);
         srcPrefab.GetComponent<Image>().color = srcColors[i];
         srcPrefab.GetComponent<Button>().onClick.AddListener(() => {
            activeColorPicker(srcPrefab.GetComponent<Button>());
         });
         srcPrefab.GetComponent<RectTransform>().SetParent(parent);
         _srcColors.Add(srcPrefab.GetComponent<Image>());

         GameObject dstPrefab = GameObject.Instantiate(singleColorPrefab);
         dstPrefab.GetComponent<Image>().color = dstColors[i];
         dstPrefab.GetComponent<Button>().onClick.AddListener(() => {
            activeColorPicker(dstPrefab.GetComponent<Button>());
         });
         dstPrefab.GetComponent<RectTransform>().SetParent(parent);
         _dstColors.Add(dstPrefab.GetComponent<Image>());
      }
      singleColorPrefab.gameObject.SetActive(false);
   }

   private void activeColorPicker (Button button) {
      _currentlyEditedElementInPalette = button;
      colorPicker.SetActive(true);
      colorPickerHideButton.SetActive(true);
      colorPicker.GetComponent<ColorPicker>().CurrentColor = button.GetComponent<Image>().color;

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

   public void updatePickingColorFromSprite(Color color) {
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

   private void preparePaletteTypeDropdown () {
      dropdownPaletteType.options.Clear();

      for (int i = (int) PaletteImageType.None + 1; i < (int) PaletteImageType.MAX; i++) {
         string text = System.Enum.GetName(typeof(PaletteImageType), (PaletteImageType) i);
         TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(text);
         dropdownPaletteType.options.Add(optionData);
      }

      dropdownPaletteType.onValueChanged.AddListener((int index) => {
         _paletteImageType = (PaletteImageType) (index + 1);
      });
   }

   private void prepareSpriteChooseDropdown () {
      const string spritePath = "Assets/Sprites";
      _cachedSpriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);
      dropdownFileChoose.options.Clear();

      foreach (ImageManager.ImageData imageData in _cachedSpriteIconFiles) {
         TMPro.TMP_Dropdown.OptionData optionData = new TMPro.TMP_Dropdown.OptionData(imageData.imageName);
         dropdownFileChoose.options.Add(optionData);
      }

      if (_cachedSpriteIconFiles.Count == 1) {
         choosePreviewSprite(0);
         dropdownFileChoose.value = 0;
      } else {
         dropdownFileChoose.value = -1;
         dropdownFileChoose.onValueChanged.AddListener((int index) => {
            choosePreviewSprite(index);
         });
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
      colorPresets.forceUpdateColors(generateMostCommonPixelsInSprite(previewSprite.sprite));

      // If preview changed for empty palette - fill with sprite colors
      if (isPaletteEmpty) {
         List<Color> allColors = generateMostCommonPixelsInSprite(previewSprite.sprite, 0);
         if (allColors.Count <= 8) {
            changeArraySize(8);
         } else if (allColors.Count <= 16) {
            changeArraySize(16);
         } else if (allColors.Count <= 32) {
            changeArraySize(32);
         } else if (allColors.Count <= 64) {
            changeArraySize(64);
         } else {
            changeArraySize(128);
         }

         int count = _srcColors.Count;
         if (allColors.Count < _srcColors.Count) {
            count = allColors.Count;
         }

         for (int i = 0; i < count; i++) {
            _srcColors[i].color = allColors[i];
            _dstColors[i].color = allColors[i];
         }
      }
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
   private PaletteImageType _paletteImageType;

   #endregion
}
