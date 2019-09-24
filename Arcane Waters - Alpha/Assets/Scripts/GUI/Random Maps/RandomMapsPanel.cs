using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Globalization;

public class RandomMapsPanel : Panel, IPointerClickHandler
{
   #region Public Variables

   [Header("References")]
   // The container for horizontal rows
   public RectTransform horizontalRowsContainer;

   // The scrollbar for panels content
   public Scrollbar scrollbar;

   // The container of our map rows
   public GameObject rowsContainer;

   // The prefab we use for creating new rows
   public RandomMapRow rowPrefab;

   [Header("Panel values")]
   // Maximum elements per horizontal panel
   public int maxHorizontalElements = 3;

   // Height of row containing several panels
   public float singleRowHeight = 160.0f;

   // Singleton reference
   static public RandomMapsPanel self;

   #endregion

   public void showPanelUsingMapSummaries (MapSummary[] mapSummaryArray) {
      // Create singleton object
      if (self == null) {
         self = this;
      } else if (this != self) {
         Destroy(this);
         return;
      }

      // Clear out any old data
      horizontalRowsContainer.gameObject.DestroyChildren();

      // Create rows and columns for presented sea map buttons
      int count = 0;

      // Create a row for each Random Map Data
      Transform horizontalRow = null;
      //TODO Temporary - spawn few times same row for debug purposes
      const int repeatingRows = 1;
      for (int i = 0; i < repeatingRows; i++) {
         foreach (MapSummary mapSummary in mapSummaryArray) {
            // Create new row for every x elements
            if (count % maxHorizontalElements == 0) {
               horizontalRow = Instantiate(rowsContainer).transform;
               horizontalRow.SetParent(horizontalRowsContainer);
            }

            // Create new random for future calculations
            _pseudoRandom = new System.Random(mapSummary.seed);

            // Create panel of created map
            RandomMapRow row = Instantiate(rowPrefab);
            row.transform.SetParent(horizontalRow.transform);
            row.setRowFromSummary(mapSummary);
            
            // Create random name based on biome type
            row.mapNameText.text = createRandomName(row);

            // Set default sprites on start, based on biome and difficulty
            setPanelSprites(row);

            // Count number of panels in current row
            count++;
         }
      }
      horizontalRowsContainer.sizeDelta = new Vector2(horizontalRowsContainer.sizeDelta.x, mapSummaryArray.Length / maxHorizontalElements * repeatingRows * singleRowHeight);
      scrollbar.SetValueWithoutNotify(1.0f);

      // Display the panel now that we have all of the data
      PanelManager.self.pushIfNotShowing(Type.RandomMaps);
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   public void clampScrollbarValue () {
      scrollbar.SetValueWithoutNotify(Mathf.Clamp(scrollbar.value, 0.0f, 1.0f));
   }

   private void setPanelSprites(RandomMapRow panel) {
      // Set plaque based on difficulty - they don't change on hover/pressed
      panel.setPlaqueNames();

      // Set sprites which use hover/pressed
      panel.OnPointerExit();
   }

   private string createRandomName (RandomMapRow panel) {
      string biomeAdj = getBiomeAdjList(panel.mapSummary.biomeType).ChooseRandom(_pseudoRandom.Next());
      string biomeNoun = getBiomeNounList(panel.mapSummary.biomeType).ChooseRandom(_pseudoRandom.Next());
      string endingAdjNoun = getGenericAdjAndNounList().ChooseRandom(_pseudoRandom.Next());
      if (getGenericAdjAndNounList().Count > 0) {
         getGenericAdjAndNounList().Remove(endingAdjNoun);
      }

      return "The " + biomeAdj + " " + biomeNoun + " of " + endingAdjNoun;
   }

   private List<string> getBiomeAdjList (Biome.Type biomeType) {
      // A text info for the purpose of adjusting string casing
      TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

      // Load asset based on biome
      TextAsset textAsset = Resources.Load<TextAsset>("Text/RandomMaps/BiomeAdjectives/RandomMap" + biomeType.ToString() + "Adj");
      if (textAsset == null) {
         D.error("Empty asset - cannot create list");
         return null;
      }

      // Create list of all lines
      string[] adjArray = textAsset.text.Split('\n');
      List<string> list = new List<string>(adjArray);

      return list;
   }

   private List<string> getBiomeNounList (Biome.Type biomeType) {
      // A text info for the purpose of adjusting string casing
      TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

      // Load asset based on biome
      TextAsset textAsset = Resources.Load<TextAsset>("Text/RandomMaps/BiomeNouns/RandomMap" + biomeType.ToString() + "Noun");
      if (textAsset == null) {
         D.error("Empty asset - cannot create list");
         return null;
      }

      // Create list of all lines
      string[] nounArray = textAsset.text.Split('\n');
      List<string> list = new List<string>(nounArray);

      return list;
   }

   private ref List<string> getGenericAdjAndNounList () {
      if (_genericAdjAndNounList != null && _genericAdjAndNounList.Count > 0) {
         return ref _genericAdjAndNounList;
      }

      // A text info for the purpose of adjusting string casing
      TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

      TextAsset textAsset = Resources.Load<TextAsset>("Text/RandomMaps/RandomMapAdjNouns");
      if (textAsset == null) {
         D.error("Empty asset - cannot create list");
         _genericAdjAndNounList = new List<string>();
      } else {
         string[] array = textAsset.text.Split('\n');
         _genericAdjAndNounList = new List<string>(array);
      }

      return ref _genericAdjAndNounList;
   }

   #region Private Variables

   // TODO Cached biome adjective informations
   private List<string> _forestAdjList;
   private List<string> _desertAdjList;
   private List<string> _lavaAdjList;
   private List<string> _mushroomAdjList;
   private List<string> _pineAdjList;
   private List<string> _snowAdjList;

   private List<string> _forestNounsList;
   private List<string> _desertNounsList;
   private List<string> _lavaNounsList;
   private List<string> _mushroomNounsList;
   private List<string> _pineNounsList;
   private List<string> _snowNounsList;

   // Adjective + noun list shared for all biome types
   private List<string> _genericAdjAndNounList;

   // Random used per MapSummary
   private System.Random _pseudoRandom;

   #endregion
}
