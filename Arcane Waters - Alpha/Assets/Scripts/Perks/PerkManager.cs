using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class PerkManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static PerkManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public List<Perk> getPerksFromAnswers (List<int> answers) {
      return CharacterCreationQuestionsScreen.self.getPerkResults(answers);
   }

   public void setPlayerPerkPoints (Perk[] perks) {
      _localPlayerPerkPoints = new Dictionary<Perk.Type, List<Perk>>();

      foreach (Perk perk in perks) {
         if (perk.perkId > 0) {
            Perk.Type type = Perk.getType(getPerkData(perk.perkId).perkTypeId);

            if (!_localPlayerPerkPoints.ContainsKey(type)) {
               _localPlayerPerkPoints.Add(type, new List<Perk>());
            }

            _localPlayerPerkPoints[type].Add(perk);
         }
      }

      initializeBoostFactors();
   }

   public void receiveListFromZipData (List<PerkData> perkDataList) {
      _perkData = new List<PerkData>();

      foreach (PerkData data in perkDataList) {
         _perkData.Add(data);
      }
   }

   public float getPerkMultiplier (Perk.Type perkType) {
      // If the player hasn't assigned points to this type, return 1
      if (!_boostFactors.ContainsKey(perkType)) {
         return 1.0f;
      }

      return _boostFactors[perkType];
   }

   public PerkData getPerkData (int perkId) {
      return _perkData.Find(x => x.perkId == perkId);
   }

   private void initializeBoostFactors () {
      _boostFactors = new Dictionary<Perk.Type, float>();

      Perk.Type[] perkTypes = Enum.GetValues(typeof(Perk.Type)) as Perk.Type[];

      foreach (Perk.Type type in perkTypes) {         
         float boostFactor = getPlayerBoostFactorForType(type);
         _boostFactors.Add(type, boostFactor);
      }
   }

   private float getPlayerBoostFactorForType (Perk.Type type) {
      float boostFactor = 1.0f;

      if (_localPlayerPerkPoints.ContainsKey(type)) {
         foreach (Perk perk in _localPlayerPerkPoints[type]) {
            boostFactor += getPerkData(perk.perkId).boostFactor * perk.points;
         }
      }

      return boostFactor;
   }

   #region Private Variables

   // The perk points of the local player
   private Dictionary<Perk.Type, List<Perk>> _localPlayerPerkPoints = new Dictionary<Perk.Type, List<Perk>>();

   // A cache for all the existing perks in the DB
   private List<PerkData> _perkData = new List<PerkData>();

   // The boost factor for the local player for each perk category
   private Dictionary<Perk.Type, float> _boostFactors = new Dictionary<Perk.Type, float>();

   #endregion
}
