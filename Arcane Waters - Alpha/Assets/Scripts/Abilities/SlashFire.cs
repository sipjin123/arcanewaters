using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SlashFire : MeleeAbility {
    #region Public Variables

    #endregion

    public SlashFire() {
        this.type = Type.Slash_Fire;
        this.name = "Fire Strike";
    }

    public override int getApChange() {
        return -5;
    }

    public override float getModifier() {
        return 1.2f;
    }

    public override Element getElement() {
        return Element.Fire;
    }

    public override float getCooldown() {
        return 4.0f;
    }

    public override List<Battler.Stance> getAllowedStances() {
        return new List<Battler.Stance>() { Battler.Stance.Attack };
    }

    public override Weapon.Class getRequiredWeaponClass() {
        return Weapon.Class.Melee;
    }

    public override Effect.Type getEffectType(Weapon.Type weaponType) {
        return Effect.Type.Slash_Fire;
    }

    public override void playImpactSound(Battler attacker, Battler defender) {
        SoundManager.playClipAtPoint(SoundManager.Type.Slash_Fire, defender.transform.position);
    }

    #region Private Variables

    #endregion
}
