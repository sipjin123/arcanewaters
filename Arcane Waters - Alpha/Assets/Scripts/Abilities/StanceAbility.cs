using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class StanceAbility : Ability {
    #region Public Variables

    #endregion

    public StanceAbility() {
        this.type = Type.Stance_Ability;
    }

    public override float getCooldown() {
        return 6.0f;
    }

    public override bool isIncludedFor(Battler battler) {
        return true;
    }

   // TODO ZERONEV: RE-implement stance ability display into the general battle action.
   // After knowing exactly the details of an stance ability.
    /*public override IEnumerator display(float timeToWait, BattleAction battleAction, bool isFirstAction) {
        yield return new WaitForSeconds(timeToWait);

        // Make sure we have the right type of Action
        if (!(battleAction is StanceAction)) {
            D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
            yield break;
        }

        // Cast version of the Attack Action
        StanceAction action = (StanceAction)battleAction;

        // Look up our needed references
        Battle battle = BattleManager.self.getBattle(action.battleId);

        // If the battle has ended, no problem
        if (battle == null) {
            yield break;
        }

        // Look up the Battler
        Battler sourceBattler = battle.getBattler(action.sourceId);

        // If the battler has died, don't proceed
        if (sourceBattler.isDead()) {
            yield break;
        }

        // Play an appropriate sound
        SoundManager.playClipAtPoint(SoundManager.Type.Character_Block, sourceBattler.transform.position);

        // Create some battle text above our head
        GameObject battleTextInstance = (GameObject)GameObject.Instantiate(PrefabsManager.self.battleTextPrefab);
        battleTextInstance.transform.SetParent(sourceBattler.transform, false);
        battleTextInstance.GetComponentInChildren<BattleText>().customizeTextForStance(action.newStance);

        // Update our assigned stance
        sourceBattler.stance = action.newStance;
    }*/

    #region Private Variables

    #endregion
}
