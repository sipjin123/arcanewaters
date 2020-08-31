using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonBox : ClientMonoBehaviour {
   #region Public Variables

   // The attack type that this box selects
   public Attack.Type attackType;

   // Skill Icon
   public Image skillIcon;

   // The ability id
   public int abilityId;

   // Skill highlight
   public GameObject highlightSkill;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();
   }

   public void setCannons () {
      CannonPanel.self.resetAllHighlights();
      SeaManager.selectedAbilityId = abilityId;
      highlightSkill.SetActive(true);
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   #endregion
}
