using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TargetCone : MonoBehaviour {
   #region Public Variables

   // The radius at which the cone ends
   public float coneOuterRadius;

   // The radius at which the cone begins
   public float coneInnerRadius;

   // Half of the angle of the cone to be drawn
   public float coneHalfAngle;

   // How many degrees of space to leave between the cone center and border
   public float coneBorderSpace;

   // When set to true, the cone will scale to end at the user's mouse position where possible, limited by the inner and outer radii
   public bool dynamicSize;

   // References to  the two dotted lines that represent the edges of the cone
   public DottedLine dottedLineLower, dottedLineUpper;

   // Reference to the sprite that represents the center of the cone
   public SpriteRenderer coneCenter;

   #endregion

   private void Awake () {
      _coneMat = coneCenter.material;
   }

   private void Update () {
      updateCone();
   }

   public void updateCone () {
      Vector2 toMouse = Util.getMousePos() - transform.position;
      float mouseDist = toMouse.magnitude;
      float mouseDistClamped = Mathf.Clamp(mouseDist, coneInnerRadius, coneOuterRadius);
      toMouse = toMouse.normalized;

      float mouseAngle = Util.angle(toMouse);

      float outerRadiusDist = (dynamicSize) ? mouseDistClamped : coneOuterRadius;

      Vector2 rotatePos = ExtensionsUtil.Rotate(toMouse, coneHalfAngle);
      Vector2 rotateNeg = ExtensionsUtil.Rotate(toMouse, -coneHalfAngle);

      dottedLineLower.lineStart.position = transform.position + (rotateNeg * coneInnerRadius).ToVector3();
      dottedLineLower.lineEnd.position = transform.position + (rotateNeg * outerRadiusDist).ToVector3();

      dottedLineUpper.lineStart.position = transform.position + (rotatePos * coneInnerRadius).ToVector3();
      dottedLineUpper.lineEnd.position = transform.position + (rotatePos * outerRadiusDist).ToVector3();

      updateMaterial(mouseAngle);
   }

   private void updateMaterial (float mouseAngle) {
      _coneMat.SetVector("_Position", transform.position);
      _coneMat.SetFloat("_Radius", coneOuterRadius);
      _coneMat.SetFloat("_InnerRadius", coneInnerRadius);
      _coneMat.SetFloat("_HalfAngle", coneHalfAngle - coneBorderSpace);
      _coneMat.SetFloat("_MiddleAngle", mouseAngle);
      _coneMat.SetFloat("_ColorChangeWeight", coneOuterRadius * 1.27f);
   }

   #region Private Variables

   // Reference to the material on the cone center's sprite renderer
   private Material _coneMat;

   #endregion
}
