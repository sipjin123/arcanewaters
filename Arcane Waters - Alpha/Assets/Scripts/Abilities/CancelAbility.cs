using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CancelAbility : Ability {
    #region Public Variables

    #endregion

    public CancelAbility() {
        this.type = Type.Cancel_Ability;
    }

    public override float getCooldown() {
        return 0f;
    }

    public override bool isIncludedFor(Battler battler) {
        return true;
    }

   // Now is using the new AbilityData method.
    /*public override IEnumerator display(float timeToWait, BattleAction battleAction, bool isFirstAction) {
        yield return new WaitForSeconds(timeToWait);

        // Make sure we have the right type of Action
        if (!(battleAction is CancelAction)) {
            D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
            yield break;
        }

        // Cast version of the Attack Action
        CancelAction action = (CancelAction)battleAction;

        // Look up our needed references
        Battle battle = BattleManager.self.getBattle(action.battleId);

        // If the battle has ended, no problem
        if (battle == null) {
            yield break;
        }

        // Display the cancel icon over the source's head
        Battler sourceBattler = battle.getBattler(action.sourceId);
        GameObject cancelPrefab = PrefabsManager.self.cancelIconPrefab;
        GameObject cancelInstance = (GameObject)GameObject.Instantiate(cancelPrefab);
        cancelInstance.transform.SetParent(sourceBattler.transform, false);
        cancelInstance.transform.position = new Vector3(
            sourceBattler.transform.position.x,
            sourceBattler.transform.position.y + .55f,
            -5f
        );
    }*/

   public IEnumerator attackDisplay (float timeToWait, BattleAction battleAction, bool isFirstAction) {
      yield return new WaitForSeconds(timeToWait);

      // Make sure we have the right type of Action
      if (!(battleAction is CancelAction)) {
         D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
         yield break;
      }

      // Cast version of the Attack Action
      CancelAction action = (CancelAction) battleAction;

      // Look up our needed references
      Battle battle = BattleManager.self.getBattle(action.battleId);

      // If the battle has ended, no problem
      if (battle == null) {
         yield break;
      }

      // Display the cancel icon over the source's head
      Battler sourceBattler = battle.getBattler(action.sourceId);
      GameObject cancelPrefab = PrefabsManager.self.cancelIconPrefab;
      GameObject cancelInstance = (GameObject) GameObject.Instantiate(cancelPrefab);
      cancelInstance.transform.SetParent(sourceBattler.transform, false);
      cancelInstance.transform.position = new Vector3(
          sourceBattler.transform.position.x,
          sourceBattler.transform.position.y + .55f,
          -5f
      );
   }

   #region Private Variables

   #endregion
}
