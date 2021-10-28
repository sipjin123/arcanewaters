using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ColorCurveReferences : MonoBehaviour {
   #region Public Variables

   public static ColorCurveReferences self;

   // A gradient controlling the color of the boost cooldown bar outline
   public Gradient shipBoostCooldownBarOutlineColor;

   // A gradient controlling the color of the boost cooldown bar
   public Gradient shipBoostCooldownBarColor;

   // A gradient controlling the color of the boost fill circle
   public Gradient shipBoostCircleColor;

   // An animation curve controlling the alpha of the ship's wake
   public AnimationCurve shipBoostWakeAlpha;

   // The color that the targeting UI will be for a local battler
   public Color localBattlerTargeterColor;

   // The color that the targeting UI will be for a remote battler
   public Color remoteBattlerTargeterColor;

   // A gradient controlling the color of the attack timing indicator outline
   public Gradient attackTimingOutlineColor;

   // An animation curve controlling the movement of the powerup popup icon
   public AnimationCurve powerupPopupMovement;

   // A gradient controlling the color of the bot ship targeting parabola
   public Gradient botShipTargetingParabolaColor;

   // A gradient controlling the color of the projectile targeting indicator
   public Gradient projectileTargetingIndicatorColor;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}
