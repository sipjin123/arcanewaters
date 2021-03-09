using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class ShipHealthBlock : MonoBehaviour
{
   #region Public Variables

   // The amount of health represented by one health block in each tier
   public static int[] HP_PER_BLOCK = new int[] { 100, 400, 1600 };

   // The image component
   public Image image;

   // The colors for the different block tiers or statuses
   public Color[] tierColors;

   #endregion

   public void updateBlock (int tier, float health) {
      tier = Mathf.Clamp(tier, 0, tierColors.Length - 1);
      health = Mathf.Clamp(health, 0f, 1f);

      image.color = new Color(tierColors[tier].r, tierColors[tier].g, tierColors[tier].b, health);
   }

   #region Private Variables

   #endregion
}
