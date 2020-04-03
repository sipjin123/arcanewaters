using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureSiteCaptureCircle : MonoBehaviour
{
   #region Public Variables

   // The rotation speed of the circle
   public static float ROTATION_SPEED = -5f;

   // The color for the treasure site statuses
   public Color capturingColor;
   public Color blockedColor;

   // The alpha multiplier, set by the animator
   public float alphaMultiplier = 1f;

   // The radius multiplier, set by the animator
   public float radiusMultiplier = 1f;

   // The actual circle
   public GameObject circle;

   #endregion

   public void Awake () {
      _animator = GetComponent<Animator>();
      _treasureSite = GetComponentInParent<TreasureSite>();
      _circleMaterial = circle.GetComponent<MeshRenderer>().material;

      // Hide the icon by default
      hide();
   }

   public void Start () {
      setZ();
      _circleMaterial.SetFloat("_Radius", _treasureSite.getCaptureRadius() * radiusMultiplier);
   }

   public void Update () {
      // Only active on clients
      if (_treasureSite == null || !_treasureSite.isClient || Global.player == null ||
         !VoyageManager.isInVoyage(Global.player)) {
         return;
      }

      // If the site has no capture points, hide the circle
      if (_treasureSite.capturePoints <= 0) {
         hide();
         return;
      }
      
      // Show the circle
      show();

      // Set the z position
      setZ();

      // If the site is captured, play the capture animation
      _animator.SetBool("captured", _treasureSite.isCaptured());

      // Set the fill amount of the circle
      _circleMaterial.SetFloat("_FillAmount", _treasureSite.capturePoints);

      // Update the radius
      _circleMaterial.SetFloat("_Radius", _treasureSite.getCaptureRadius() * radiusMultiplier);

      // Set the color
      if (_treasureSite.voyageGroupId == Global.player.voyageGroupId &&
         _treasureSite.status != TreasureSite.Status.Blocked) {
         _circleMaterial.SetColor("_Color",
            new Color(capturingColor.r, capturingColor.g, capturingColor.b, alphaMultiplier));
      } else {
         _circleMaterial.SetColor("_Color",
            new Color(blockedColor.r, blockedColor.g, blockedColor.b, alphaMultiplier));
      }

      // Set the rotation speed
      float rotationSpeed;
      if (_treasureSite.voyageGroupId == Global.player.voyageGroupId) {
         rotationSpeed = ROTATION_SPEED;
      } else {
         rotationSpeed = -ROTATION_SPEED;
      }

      if (_treasureSite.status == TreasureSite.Status.Resetting) {
         rotationSpeed = -rotationSpeed / 2;
      }

      // Rotate the circle
      if (_treasureSite.status == TreasureSite.Status.Stealing ||
         _treasureSite.status == TreasureSite.Status.Capturing ||
         _treasureSite.status == TreasureSite.Status.Resetting) {
         _rotation = Mathf.Repeat(_rotation + rotationSpeed * Time.deltaTime, 360f);
         _circleMaterial.SetFloat("_Rotation", _rotation);
      }
   }

   public void show () {
      if (!circle.activeSelf) {
         circle.SetActive(true);
         _animator.SetBool("visible", true);
      }
   }

   public void hide () {
      if (circle.activeSelf) {
         circle.SetActive(false);
         _animator.SetBool("visible", false);
      }
   }

   private void setZ () {
      // Get the area
      Area area = AreaManager.self.getArea(_treasureSite.areaKey);

      // Set the z position of the capture circle, above the sea and below the land
      if (area != null && transform.position.z != (area.waterZ - 0.01f)) {
         Util.setZ(transform, area.waterZ - 0.01f);
         _circleMaterial.SetVector("_Position", transform.position);
      }
   }

   #region Private Variables

   // The associated treasure site
   public TreasureSite _treasureSite;

   // The animator component
   private Animator _animator;

   // The material that draws the circle
   private Material _circleMaterial;

   // The circle rotation
   private float _rotation = 0f;

   #endregion
}
