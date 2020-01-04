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

   // Skill highlight
   public GameObject highlightSkill;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();
   }

   private void Update () {
      // Make the box highlighted if we've equipped the associated attack type
      /*_containerImage.color = Color.white;
      if (this.attackType == SeaManager.selectedAttackType) {
         _containerImage.color = Util.getColor(255, 160, 160);
      }*/

      // Maybe swap out the image
      highlightSkill.SetActive(this.attackType == SeaManager.selectedAttackType);
   }

   public void setCannons () {
      SeaManager.selectedAttackType = this.attackType;
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   #endregion
}
