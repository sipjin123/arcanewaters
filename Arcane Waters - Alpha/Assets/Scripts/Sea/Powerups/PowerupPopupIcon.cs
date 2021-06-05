using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using DG.Tweening;

public class PowerupPopupIcon : MonoBehaviour {
   #region Public Variables

   // A reference to the image showing this powerup icon
   public Image powerupIcon;

   // A reference to the image showing the border of this powerup icon
   public Image borderIcon;

   // A reference to the text fade that will show the name of this powerup
   public RollingTextFade textFade;

   // A reference to the textmeshpro text that renders the name of this powerup
   public TextMeshProUGUI nameText;

   // A reference to the transform containing the powerup and border icons
   public Transform iconParent;

   #endregion

   public void init (Powerup.Type type, Rarity.Type rarity) {
      PowerupData powerupData = PowerupManager.self.getPowerupData(type);
      powerupIcon.sprite = powerupData.spriteIcon;

      Sprite[] borderSprites = Resources.LoadAll<Sprite>(Powerup.BORDER_SPRITES_LOCATION);
      borderIcon.sprite = borderSprites[(int) rarity - 1];

      _powerupName = powerupData.powerupName;
      nameText.faceColor = PowerupPanel.self.rarityColors[(int) rarity];
   }

   public void gravitateToPlayer (PlayerShipEntity player, float duration) {
      StartCoroutine(CO_GravitateToPlayer(player, duration));
   }

   private IEnumerator CO_GravitateToPlayer (PlayerShipEntity player, float duration) {
      iconParent.DOScale(0.5f, duration);
      float moveTime = 0.0f;

      while (moveTime < duration) {
         float moveAmount = Mathf.Clamp01(moveTime / duration);
         float adjustedMoveAmount = ColorCurveReferences.self.powerupPopupMovement.Evaluate(moveAmount);
         Vector3 targetPosition = Vector3.Lerp(transform.position, player.transform.position, adjustedMoveAmount);
         Util.setXY(transform, targetPosition);

         // If we get close enough to the player, pick up the powerup
         Vector3 toTarget = player.transform.position - transform.position;
         toTarget.z = 0.0f;
         float distanceToTarget = toTarget.magnitude;
         if (distanceToTarget < PICKUP_DISTANCE) {
            player.spritesContainer.transform.DORewind();
            player.spritesContainer.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f);
            iconParent.gameObject.SetActive(false);
            break;
         }

         moveTime += Time.deltaTime;
         yield return null;
      }

      transform.SetParent(player.transform);
      transform.localPosition = new Vector3(0.0f, 0.0f, -0.05f);

      // If another powerup is displaying its text, wait for it to finish
      while (NetworkTime.time - _lastIconPickupTime < TEXT_DISPLAY_TIME) {
         yield return null;
      }

      textFade.fadeInText(_powerupName);
      _lastIconPickupTime = NetworkTime.time;

      yield return new WaitForSeconds(TEXT_DISPLAY_TIME);
      Destroy(this.gameObject);
   }

   #region Private Variables

   // The distance at which the icon will be 'picked up' by the player
   private const float PICKUP_DISTANCE = 0.2f;

   // How long the powerup name text will display for
   private const float TEXT_DISPLAY_TIME = 2.0f;

   // The name to be displayed when we get the powerup
   private string _powerupName;

   // The time at which an icon was last picked up
   private static double _lastIconPickupTime = 0.0f;

   #endregion
}
