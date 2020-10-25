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
      _perkData = new List<PerkData>();

      foreach (PerkData data in perkDataList) {
         _perkData.Add(data);
      }

      // Initialize the Perks Panel
      PerksPanel.self.receivePerkData(_perkData);
   }

   public float getPerkMultiplier (Perk.Category category) {
      // If the player hasn't assigned points to this type, return 1
      if (!_boostFactors.ContainsKey(category)) {
         return 1.0f;
      }

      return _boostFactors[category];
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
      return _perkData.Find(x => x.perkId == perkId);
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

   #region Private Variables

   // The organized perk points of the local player
   private Dictionary<Perk.Category, List<Perk>> _localPlayerPerkPointsByCategory = new Dictionary<Perk.Category, List<Perk>>();

   // The raw perk points of the local player
   private List<Perk> _localPlayerPerkPoints = new List<Perk>();

   // A cache for all the existing perks in the DB
   private List<PerkData> _perkData = new List<PerkData>();

   // The boost factor for the local player for each perk category
   private Dictionary<Perk.Category, float> _boostFactors = new Dictionary<Perk.Category, float>();

   #endregion
}
