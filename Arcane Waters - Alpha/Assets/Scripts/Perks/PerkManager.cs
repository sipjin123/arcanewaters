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

   #region Private Variables

   #endregion
}
