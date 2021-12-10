using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class PerkManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static PerkManager self;

   // A list of perks that have been removed from use
   public static List<int> removedPerkIds = new List<int>() { 1 };

   #endregion

   private void Awake () {
      self = this;
   }

   public List<Perk> getPerksFromAnswers (int[] answers) {
      return CharacterCreationQuestionsScreen.self.getPerkResults(answers);
   }

   public void setPlayerPerkPoints (Perk[] perks) {
      _localPlayerPerkPointsByCategory = new Dictionary<Perk.Category, List<Perk>>();

      foreach (Perk perk in perks) {
         if (perk.perkId > 0) {
            Perk.Category category = Perk.getCategory(getPerkData(perk.perkId).perkCategoryId);

            if (!_localPlayerPerkPointsByCategory.ContainsKey(category)) {
               _localPlayerPerkPointsByCategory.Add(category, new List<Perk>());
            }

            _localPlayerPerkPointsByCategory[category].Add(perk);
         }
      }

      _localPlayerPerkPoints = perks.ToList();

      // Initialize the Perks Panel
      PerksPanel.self.receivePerkPoints(true, perks);

      initializeBoostFactors();
   }

   public void receiveListFromZipData (List<PerkData> perkDataList) {
      _perkData = new Dictionary<Perk.Category, PerkData>();

      foreach (PerkData data in perkDataList) {
         _perkData[(Perk.Category) data.perkCategoryId] = data;
         _perkCategoriesById[data.perkId] = (Perk.Category) data.perkCategoryId;
      }

      // Initialize the Perks Panel if there is a client running
      if (NetworkClient.active) {
         PerksPanel.self.receivePerkData(perkDataList);
      }
   }

   public void loadPerkDataFromDatabase () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         string perkData = DB_Main.getXmlContent(XmlVersionManagerServer.PERKS_DATA_TABLE);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            string splitter = "[next]";
            string[] xmlGroup = perkData.Split(new string[] { splitter }, StringSplitOptions.None);

            List<PerkData> perkDataList = new List<PerkData>();
            foreach (string subGroup in xmlGroup) {
               string[] xmlSubGroup = subGroup.Split(new string[] { XmlVersionManagerClient.SPACE_KEY }, StringSplitOptions.None);

               if (xmlSubGroup.Length == 2) {
                  int perkId = int.Parse(xmlSubGroup[0]);
                  PerkData data = Util.xmlLoad<PerkData>(xmlSubGroup[1]);
                  data.perkId = perkId;
                  perkDataList.Add(data);
               }
            }

            receiveListFromZipData(perkDataList);
         });
      });
   }

   public float getPerkMultiplier (Perk.Category category) {
      // If the player hasn't assigned points to this type, return 1
      if (!_boostFactors.ContainsKey(category)) {
         return 1.0f;
      }

      return _boostFactors[category];
   }

   public float getPerkMultiplierAdditive (Perk.Category category) {
      return getPerkMultiplier(category) - 1.0f;
   }

   public int getAssignedPointsByPerkId (int perkId) {      
      Perk perk = _localPlayerPerkPoints.FirstOrDefault(x => x.perkId == perkId);

      // If the perk isn't in the list, the player hasn't assigned any points
      if (perk == null) {
         return 0;
      }

      return perk.points;
   }

   public int getUnassignedPoints () {
      return getAssignedPointsByPerkId(Perk.UNASSIGNED_ID);
   }

   public PerkData getPerkData (int perkId) {
      if (_perkCategoriesById.ContainsKey(perkId)) {
         Perk.Category category = _perkCategoriesById[perkId];
         return _perkData[category];
      }

      return null;
   }

   public PerkData getPerkData (Perk.Category category) {
      if (!_perkData.ContainsKey(category)) {
         D.error("Cached perk data didn't have data for: " + category.ToString());
         return null;
      }
      
      return _perkData[category];
   }

   private void initializeBoostFactors () {
      _boostFactors = new Dictionary<Perk.Category, float>();

      Perk.Category[] categories = Enum.GetValues(typeof(Perk.Category)) as Perk.Category[];

      foreach (Perk.Category category in categories) {         
         float boostFactor = getPlayerBoostFactorForCategory(category);
         _boostFactors.Add(category, boostFactor);
      }
   }

   private float getPlayerBoostFactorForCategory (Perk.Category category) {
      float boostFactor = 1.0f;

      if (_localPlayerPerkPointsByCategory.ContainsKey(category)) {
         foreach (Perk perk in _localPlayerPerkPointsByCategory[category]) {
            boostFactor += getPerkData(perk.perkId).boostFactor * perk.points;
         }
      }

      return boostFactor;
   }

   private float getBoostFactorForCategory (int userId, Perk.Category category) {
      Dictionary<Perk.Category, int> perkPoints = _serverPerkPointsByUserId[userId];

      float boostFactor = 1.0f;
      boostFactor += getPerkData(category).boostFactor * perkPoints[category];

      return boostFactor;
   }

   public float getPerkMultiplier (int userId, Perk.Category category) {
      // Print a warning if this is called on a non-server
      if (!NetworkServer.active) {
         D.warning("getPerkMultiplier is being called on a non-server client, it should only be called on the server");
         return 1.0f;
      }
      
      // If we don't have the user in our dictionary, return 1
      if (!_serverPerkPointsByUserId.ContainsKey(userId)) {
         D.warning("PerkManager didn't have the stored perk multiplier for userId: " + userId);
         return 1.0f;
      }

      Dictionary<Perk.Category, int> perkPoints = _serverPerkPointsByUserId[userId];
      
      // If we don't have a value for this perk, return 1
      if (!perkPoints.ContainsKey(category)) {
         D.warning("Player perk dictionary didn't have a value for: " + category.ToString());
         return 1.0f;
      }

      return getBoostFactorForCategory(userId, category);
   }

   public float getPerkMultiplierAdditive (int userId, Perk.Category category) {
      return getPerkMultiplier(userId, category) - 1.0f;
   }

   public void updatePerkPointsForUser (int userId, List<Perk> perkData) {
      // If this user has stored perk data, clear it
      if (_serverPerkPointsByUserId.ContainsKey(userId)) {
         _serverPerkPointsByUserId.Remove(userId);
      }
      _serverPerkPointsByUserId.Add(userId, new Dictionary<Perk.Category, int>());

      Dictionary<Perk.Category, int> storedPerkData = _serverPerkPointsByUserId[userId];

      // Store perk data in dictionary
      foreach(Perk perk in perkData) {
         if (perk.perkId > 0) {
            Perk.Category category = Perk.getCategory(getPerkData(perk.perkId).perkCategoryId);
            storedPerkData[category] = perk.points;
         }
      }

      // Assign '0' perk points to empty categories
      foreach(Perk.Category category in Enum.GetValues(typeof(Perk.Category))) {
         if (!storedPerkData.ContainsKey(category)) {
            storedPerkData.Add(category, 0);
         }
      }
   }

   public void storePerkPointsForUser (int userId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<Perk> userPerks = DB_Main.getPerkPointsForUser(userId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            updatePerkPointsForUser(userId, userPerks);
         });
      });
   }

   public Dictionary<int, Perk.Category> getPerkCategories () {
      return _perkCategoriesById;
   }

   public bool perkActivationRoll (int userId, Perk.Category category) {
      // Performs a random roll to see if a perk will activate, based on the points the user has in the perk
      return (UnityEngine.Random.Range(0.0f, 1.0f) <= getPerkMultiplierAdditive(userId, category));
   }

   #region Private Variables

   // The organized perk points of the local player
   private Dictionary<Perk.Category, List<Perk>> _localPlayerPerkPointsByCategory = new Dictionary<Perk.Category, List<Perk>>();

   // The raw perk points of the local player
   private List<Perk> _localPlayerPerkPoints = new List<Perk>();

   // A cache for all the existing perks in the DB
   private Dictionary<Perk.Category, PerkData> _perkData = new Dictionary<Perk.Category, PerkData>();

   // A dictionary of which perkIds are which perk category
   private Dictionary<int, Perk.Category> _perkCategoriesById = new Dictionary<int, Perk.Category>();

   // The boost factor for the local player for each perk category
   private Dictionary<Perk.Category, float> _boostFactors = new Dictionary<Perk.Category, float>();

   // A cache of perk points for each player 
   private Dictionary<int, Dictionary<Perk.Category, int>> _serverPerkPointsByUserId = new Dictionary<int, Dictionary<Perk.Category, int>>();

   #endregion
}
