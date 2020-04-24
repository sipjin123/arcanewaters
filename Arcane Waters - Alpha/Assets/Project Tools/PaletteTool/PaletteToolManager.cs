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

   [Header("Edit palette menu")]
   public Image previewSprite;
   public Text choosePaletteNameText;
   public Text currentSizeText;
   public GameObject changePaletteScene;

   [Header("Color picker")]
   // Object containing "Color Picker" script allowing user to pick color
   public GameObject colorPicker;
   
   // Background button - if user press anything beside Color Picker, it disappears
   public GameObject colorPickerHideButton;

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

   public class PaletteDataPair
   {
      // The xml ID of the palette
      public int paletteId;

      // The userID of the content creator
      public int creatorID;

      // Data of the palette
      public PaletteToolData paletteData;
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

      // Present edit scene with downloaded data
      _currentRow = row;
      generatePaletteColorImages(src, dst, data.size);
      showPalettePreview();
      choosePaletteNameText.GetComponent<InputField>().text = data.paletteName;
   }

   private void changeArraySize(int size) {
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

   private Color convertHexToRGB(string hex) {
      int red = translateHexLetterToInt(hex[0]) * 16 + translateHexLetterToInt(hex[1]);
      int green = translateHexLetterToInt(hex[2]) * 16 + translateHexLetterToInt(hex[3]);
      int blue = translateHexLetterToInt(hex[4]) * 16 + translateHexLetterToInt(hex[5]);
      return new Color(red / 255.0f, green / 255.0f, blue / 255.0f);
   }

   private int translateHexLetterToInt (char letter) {
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

   private string convertRGBToHex(Color color) {
      return convertIntToHex(color.r) + convertIntToHex(color.g) + convertIntToHex(color.b);
   }

   private string convertIntToHex(float val) {
      int dec = Mathf.RoundToInt(val * 255.0f);
      return translateIntToHexLetter(dec / 16) + translateIntToHexLetter(dec % 16);
   }

   private string translateIntToHexLetter(int val) {
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
      PaletteToolData data = new PaletteToolData(choosePaletteNameText.text, src.Length, src, dst);
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

   private List<Color> generateMostCommonPixelsInSprite (Sprite sprite) {
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

      // Clear list to reuse it as an output
      colorList.Clear();

      // Take 10 most often occurring colors
      for (int i = 0; i < 10; i++) {
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

   #endregion
}
