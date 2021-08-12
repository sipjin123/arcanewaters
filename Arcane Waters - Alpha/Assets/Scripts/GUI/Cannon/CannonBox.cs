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

   // The box order from the left
   public int boxIndex;

   // Skill highlight
   public GameObject highlightSkill;

   // Image used to show cooldown progress
   public Image cooldownHighlight;
   public Image cooldownNormal;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();
   }

   public void boxPressed () {
      CannonPanel.self.useCannonType(abilityId, boxIndex);
      if (Global.player != null && Global.player is PlayerShipEntity) {
         PlayerShipEntity playerShip = (PlayerShipEntity) Global.player;
         playerShip.Cmd_ChangeAttackOption(abilityId);
      }
   }

   public void setCannons () {
      CannonPanel.self.resetAllHighlights();
      SeaManager.selectedAbilityId = abilityId;

      cooldownHighlight.fillAmount = cooldownNormal.fillAmount;
      cooldownNormal.fillAmount = 0;
      highlightSkill.SetActive(true);
   }

   public void setAbilityIcon (int id) {
      bool allowDynamicAbilities = true;
      if (allowDynamicAbilities) {
         if (id < 1) {
            skillIcon.gameObject.SetActive(false);
            _containerImage.gameObject.SetActive(false);
            return;
         }

         skillIcon.gameObject.SetActive(true);
         _containerImage.gameObject.SetActive(true);

         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(id);
         if (shipAbilityData == null) {
            D.debug("Missing ability info: {" + id + "}");
            return;
         }

         skillIcon.sprite = ImageManager.getSprite(shipAbilityData.skillIconPath);
      }
   }

   public void skillReady () {
      cooldownHighlight.fillAmount = 0;
      cooldownNormal.fillAmount = 0;
   }

   public void setCooldown (float cooldownAmount) {
      if (highlightSkill.activeSelf) {
         cooldownHighlight.fillAmount = cooldownAmount;
         cooldownNormal.fillAmount = 0;
      } else {
         cooldownHighlight.fillAmount = 0;
         cooldownNormal.fillAmount = cooldownAmount;
      }
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   #endregion
}
