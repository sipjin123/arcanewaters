using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonBox : ClientMonoBehaviour {
   #region Public Variables

   // The attack type that this box selects
   public Attack.Type attackType;

   // The attack type that this box selects
   public CannonPanel.CannonAttackOption cannonAttackOption;

   // Skill Icon
   public Image skillIcon;

   // The ability id
   public int abilityId;

   // The box order from the left
   public int boxIndex;

   // Skill highlight
   public GameObject highlightSkill;

   // Skill cooldown
   public Text cooldownText;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();
   }

   public void boxPressed () {
      CannonPanel.self.useCannonType(cannonAttackOption, boxIndex);
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
