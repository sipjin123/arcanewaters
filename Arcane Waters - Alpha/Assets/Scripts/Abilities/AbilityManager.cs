using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityManager : MonoBehaviour {
   #region Public Variables

   // A convenient self reference
   public static AbilityManager self;

   #endregion

   void Start () {
      self = this;

      // Base ability classes
      _abilities.Add(Ability.Type.Basic_Attack, new BasicAttack());
      // _abilities.Add(Ability.Type.Stance_Ability, new StanceAbility());
      // _abilities.Add(Ability.Type.Cancel_Ability, new CancelAbility());

      // Melee abilities
      _abilities.Add(Ability.Type.Monster_Attack, new MonsterAttack());
      _abilities.Add(Ability.Type.Slash_Ice, new SlashIce());
      _abilities.Add(Ability.Type.Slash_Fire, new SlashFire());
      _abilities.Add(Ability.Type.Slash_Lightning, new SlashLightning());

      // Ranged abilities
      /*_abilities.Add(Ability.Type.Rockspire, new Rockspire());
      _abilities.Add(Ability.Type.BossShout, new BossShout());
      _abilities.Add(Ability.Type.Arrow_Coral, new ArrowCoral());
      _abilities.Add(Ability.Type.Arrow_Ent, new ArrowEnt());
      _abilities.Add(Ability.Type.Roots, new Roots());
      _abilities.Add(Ability.Type.Haste, new Haste());
      _abilities.Add(Ability.Type.Boulder, new Boulder());
      _abilities.Add(Ability.Type.Heal, new Heal());*/

      // _abilities.Add(Ability.Type.Lava_Glob, new LavaGlob());
      // _abilities.Add(Ability.Type.SlimeSpit, new SlimeSpit());
      // _abilities.Add(Ability.Type.SlimeRain, new SlimeRain());
   }

   public static Ability getAbility (Ability.Type abilityType) {
      return self._abilities[abilityType];
   }

   public static List<Ability> getAllowedAbilities (Battler battler) {
      List<Ability> visibleAbilities = new List<Ability>();

      // Cycle over each of the Ability instances in our internal mapping
      foreach (Ability ability in self._abilities.Values) {
         if (ability.isIncludedFor(battler)) {
            visibleAbilities.Add(ability);
         }
      }

      return visibleAbilities;
   }

   public void execute (BattleAction[] actions) {
      bool isFirst = true;

      foreach (BattleAction action in actions) {
         Battle battle = BattleManager.self.getBattle(action.battleId);
         Battler sourceBattler = battle.getBattler(action.sourceId);
         Battler targetBattler = battle.getBattler(action.targetId);

         // Update timestamps
         sourceBattler.lastAbilityEndTime = action.actionEndTime;
         sourceBattler.cooldownEndTime = action.actionEndTime + action.cooldownDuration;

         // Mark both battlers as animating for the duration
         sourceBattler.animatingUntil = action.actionEndTime;
         targetBattler.animatingUntil = action.actionEndTime;

         // Get the ability object for this action
         Ability ability = getAbility(action.abilityType);

         // Check how long we need to wait before displaying this action
         float timeToWait = action.actionEndTime - Util.netTime() - ability.getTotalAnimLength(sourceBattler, targetBattler);

         // Display the ability at the assigned time
         StartCoroutine(ability.display(timeToWait, action, isFirst));

         // Don't affect the source for any subsequent actions
         isFirst = false;
      }
   }

   public void execute (StanceAction action) {
      Battle battle = BattleManager.self.getBattle(action.battleId);
      Battler sourceBattler = battle.getBattler(action.sourceId);

      // Make note of the time that this action is going to occur
      sourceBattler.lastStanceChange = action.actionEndTime;

      // Check how long we need to wait before displaying this action
      float timeToWait = action.actionEndTime - Util.netTime();

      // Get the ability object for this action
      Ability ability = getAbility(Ability.Type.Stance_Ability);

      // Display the stance change at the assigned time
      StartCoroutine(ability.display(timeToWait, action, true));
   }

   public void execute (CancelAction action) {
      Battle battle = BattleManager.self.getBattle(action.battleId);
      Battler sourceBattler = battle.getBattler(action.sourceId);
      Battler targetBattler = battle.getBattler(action.targetId);

      // Get the ability object for this action
      Ability ability = getAbility(Ability.Type.Cancel_Ability);
      float animLength = ability.getTotalAnimLength(sourceBattler, targetBattler);

      // Update the battler's action timestamps
      sourceBattler.cooldownEndTime -= action.timeToSubtract;
      sourceBattler.animatingUntil -= animLength;
      targetBattler.animatingUntil -= animLength;
   }

   #region Private Variables

   // A mapping of Ability Type to the Ability object
   protected Dictionary<Ability.Type, Ability> _abilities = new Dictionary<Ability.Type, Ability>();

   #endregion
}
