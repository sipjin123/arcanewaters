using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackTrajectoryDot : ClientMonoBehaviour
{
   #region Public Variables

   // The actual dot sprite
   public SpriteRenderer dotRenderer;

   // The dot sprites over the trajectory
   public Sprite[] dotSprites;

   #endregion

   public void setPosition(Vector2 startPos, Vector2 endPos, Color dotColor, float lerpTime) {
      // Set the dot color
      dotRenderer.color = dotColor;

      // Set the dot position along the straight line
      Vector2 dotPos = Vector2.Lerp(startPos, endPos, lerpTime);
      Util.setXY(this.transform, dotPos);

      // Adjusts the height of the dot in an arch
      Util.setLocalY(dotRenderer.transform, AttackManager.getArcHeight(startPos, endPos, lerpTime, true));

      // Set the correct dot sprite depending on the position in the trajectory
      int spriteIndex = Mathf.FloorToInt(lerpTime * dotSprites.Length);
      spriteIndex = Mathf.Clamp(spriteIndex, 0, dotSprites.Length - 1);
      dotRenderer.sprite = dotSprites[spriteIndex];
   }

   public void hide () {
      gameObject.SetActive(false);
   }

   public void show () {
      gameObject.SetActive(true);
   }

   #region Private Variables

   #endregion
}
