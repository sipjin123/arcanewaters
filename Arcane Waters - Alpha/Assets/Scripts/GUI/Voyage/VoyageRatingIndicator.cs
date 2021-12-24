using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class VoyageRatingIndicator : MonoBehaviour
{
   #region Public Variables

   // Reference to the control that will display the stars
   public Image ratingView;

   // Reference to the control that displays the current progress to the next rating level
   public Image progressView;

   // The set of sprites that represent each rating level
   public Sprite[] ratingSprites;

   // The set of colors assigned to each rating level
   public Color[] ratingColors;

   // Self reference
   public static VoyageRatingIndicator self;

   #endregion

   private void Start () {
      self = this;
      InvokeRepeating(nameof(voyageRatingIndicatorVisibilityCheck), 0.0f, 1.0f);
   }

   public void setRatingPoints (int ratingPoints) {
      if (ratingPoints != _ratingPoints) {
         _ratingPoints = ratingPoints;
         progressView.fillAmount = computeNormalizedRatingPoints();
         changeRatingSprite();
         animate();
      }
   }

   public void animate () {
      if (!_isAnimating) {
         ratingView.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0.0f), 0.5f, 0, 0.0f).OnComplete(() => _isAnimating = false);
         _isAnimating = true;
      }
   }

   private void changeRatingSprite () {
      Sprite ratingSprite = getRatingSprite();

      if (ratingSprite == null) {
         return;
      }

      ratingView.sprite = ratingSprite;
   }

   private Sprite getRatingSprite () {
      int ratingLevel = VoyageRatingManager.computeRatingLevelFromPoints(_ratingPoints);

      if (!isRatingValid(ratingLevel)) {
         return null;
      }

      return ratingSprites[ratingLevel];
   }

   private bool isRatingValid (int rating) {
      return 0 <= rating && rating <= VoyageRatingManager.getHighestRatingLevel();
   }

   private float computeNormalizedRatingPoints () {
      int currentRatingLevel = VoyageRatingManager.computeRatingLevelFromPoints(_ratingPoints);
      int maxPointsForCurrentRatingLevel = VoyageRatingManager.computeMaxPointsForRatingLevel(currentRatingLevel);
      int pointsToNextLevel = VoyageRatingManager.computeRatingPointsToNextLevel(_ratingPoints);
      int pointsForCurrentRatingLevel = maxPointsForCurrentRatingLevel - pointsToNextLevel;
      return (float) pointsForCurrentRatingLevel / maxPointsForCurrentRatingLevel;
   }

   private void voyageRatingIndicatorVisibilityCheck () {
      bool isPlayerInVoyage = Global.player != null && (VoyageManager.isAnyLeagueArea(Global.player.areaKey) || VoyageManager.isTreasureSiteArea(Global.player.areaKey));
      this.gameObject.SetActive(isPlayerInVoyage);
   }

   #region Private Variables

   // The points for the current level. When the points go down to zero, a new level is reached.
   private int _ratingPoints = 1;

   // Is the indicator animating?
   private bool _isAnimating = false;

   #endregion
}
