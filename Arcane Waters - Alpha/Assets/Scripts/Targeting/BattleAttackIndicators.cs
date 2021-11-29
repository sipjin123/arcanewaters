using UnityEngine;

public class BattleAttackIndicators : MonoBehaviour
{
   #region Public Variables

   // The set of indicators
   public SpriteRenderer[] indicators;

   // Reference to the holder
   public Transform indicatorHolder;

   #endregion

   public static BattleAttackIndicators createFor (Battler battler) {
      if (battler == null || battler.battleSpot == null) {
         return null;
      }

      BattleAttackIndicators indicatorsPrefab = battler.battlerType == BattlerType.PlayerControlled ? PrefabsManager.self.battleAttackIndicatorsAlliesPrefab : PrefabsManager.self.battleAttackIndicatorsEnemiesPrefab;
      BattleAttackIndicators indicators =  Instantiate(indicatorsPrefab);
      indicators.setBattler(battler);
      indicators.transform.position = battler.battleSpot.transform.position;

      if (battler.isBossType) {
         indicators.adjustForBosses();
      }

      Util.setZ(indicators.transform, 0);
      return indicators;
   }

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

   private void adjustForBosses () {
      // Adjust the default positioning of the indicators, to accomodate for larger enemies (e.g. Bosses)
      if (indicators == null || indicatorHolder == null) {
         return;
      }

      Util.setLocalX(indicatorHolder.transform, 0.438f);
   }

   public void detach () {
      Destroy(this.gameObject);
   }

   public Battler getBattler () {
      return _battler;
   }

   public void setBattler(Battler battler) {
      _battler = battler;
   }

   #region Private Variables

   // Reference to the battler this attack indicators refer to
   private Battler _battler;

   #endregion
}
