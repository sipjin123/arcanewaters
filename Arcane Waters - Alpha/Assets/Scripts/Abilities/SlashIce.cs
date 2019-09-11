using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SlashIce : MeleeAbility {
    #region Public Variables

    #endregion

    public SlashIce() {
        this.type = Type.Slash_Ice;
        this.name = "Frost Cut";
    }

    public override int getApChange() {
        return -5;
    }

    public override float getModifier() {
        return 1.2f;
    }

    public override Element getElement() {
        return Element.Water;
    }

    public override float getCooldown() {
        return 4.0f;
    }

    public override List<Battler.Stance> getAllowedStances() {
        return new List<Battler.Stance>() { Battler.Stance.Defense };
    }

    public override Weapon.Class getRequiredWeaponClass() {
        return Weapon.Class.Melee;
    }

    public override Effect.Type getEffectType(Weapon.Type weaponType) {
        return Effect.Type.Slash_Ice;
    }

    public override void playImpactSound(Battler attacker, Battler defender) {
        SoundManager.playClipAtPoint(SoundManager.Type.Slash_Ice, defender.transform.position);
    }

    #region Private Variables

    #endregion
}
