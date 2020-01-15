using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class MagnitudeHandler : MonoBehaviour {
   #region Public Variables

   // The list of sprites depending on their magnitude
   public List<SpriteMagnitude> spriteMagnitudeSetup;

   // References the sprite renderer to update
   public SpriteRenderer spriteRenderReference;

   #endregion

   public void setSprite (Attack.ImpactMagnitude impactType) {
      SpriteMagnitude magnitudeReference = spriteMagnitudeSetup.Find(_ => _.impactMagnitude == impactType);
      if (magnitudeReference == null) {
         D.warning("Missing sprite assignment for impact type: " + impactType);
         return;
      }

      spriteRenderReference.sprite = magnitudeReference.spriteReference;
   }

   #region Private Variables
      
   #endregion
}

[Serializable]
public class SpriteMagnitude
{
   // The type of impact
   public Attack.ImpactMagnitude impactMagnitude;

   // The sprite to set depending on impact
   public Sprite spriteReference;
}