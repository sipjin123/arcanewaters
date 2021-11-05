using UnityEngine;

public class BattleAttackIndicators : MonoBehaviour
{
   #region Public Variables

   // The set of indicators
   public SpriteRenderer[] indicators;

   #endregion

   private bool isValidIndex(int index) {
      return indicators != null && 0 <= index && index < indicators.Length;
   }

   public void toggle(int index, bool show = true) {
      // Show the indicator that corresponds to the specified number
      if (!isValidIndex(index)) {
         return;
      }

      indicators[index].gameObject.SetActive(show);
   }

   public void clear () {
      // Hide all indicators
      if (indicators == null) {
         return;
      }

      foreach (SpriteRenderer indicator in indicators) {
         indicator.gameObject.SetActive(false);
      }
   }

   public bool isOn(int index) {
      if (!isValidIndex(index)) {
         return false;
      }

      return indicators[index].gameObject.activeInHierarchy;
   }

   public void adjustForBosses () {
      // Adjust the default positioning of the indicators, to accomodate for larger enemies (e.g. Bosses)
      if (indicators == null) {
         return;
      }

      Util.setLocalY(this.transform, -0.17f);

      // Indicator 2
      indicators[1].transform.Translate(0.0f, 0.02f, 0.0f);

      // Indicator 3
      indicators[2].transform.Translate(0.0f, 0.03f, 0.0f);

      // Indicator 4
      indicators[3].transform.Translate(0.0f, 0.03f, 0.0f);

      // Indicator 5
      indicators[4].transform.Translate(0.0f, 0.02f, 0.0f);
   }

   #region Private Variables

   #endregion
}
