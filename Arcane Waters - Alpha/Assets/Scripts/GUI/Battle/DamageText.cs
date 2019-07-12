using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DamageText : MonoBehaviour {
   #region Public Variables

   // The amount of time it takes for the damage text to fade out completely
   public static float TEXT_LIFETIME = 1.3f;

   // The amount of time it takes the text to reach full size
   public static float SIZE_INCREASE_DURATION = .10f;

   // The Text component
   public Text text;

   // The Image component
   public Image iconImage;

   // The prefab we use for UI collisions
   public GameObject uiColliderPrefab;

   #endregion

   void Awake () {
      _creationTime = Time.time;

      // Look up the row that contains the text and image
      _row = GetComponentInChildren<HorizontalLayoutGroup>();

      // Start with the text completely scaled down
      _row.transform.localScale = new Vector3(0f, 0f, 1f);
   }

   void Start () {
      // Create a UI collider beneath us to bounce off of
      _uiCollider = (GameObject) GameObject.Instantiate(uiColliderPrefab);
      _uiCollider.name = "UI Collider";

      // Position us beneath the damage text so that it will fall and bounce
      _uiCollider.transform.SetParent(EffectManager.self.transform);
      _uiCollider.transform.position = new Vector3(
          this.transform.position.x,
          this.transform.position.y - .3f,
          this.transform.position.z
      );

      // The collider should be on the same layer as the damage text
      _uiCollider.layer = this.gameObject.layer;
   }

   public void Update () {
      float timeSinceCreation = Time.time - _creationTime;

      // Destroy ourself after enough time has passed
      if (timeSinceCreation > TEXT_LIFETIME) {
         Destroy(_uiCollider);
         Destroy(this.gameObject);
         return;
      }

      // Increase in size initially
      if (timeSinceCreation < SIZE_INCREASE_DURATION) {
         float targetScale = timeSinceCreation / SIZE_INCREASE_DURATION;
         _row.transform.localScale = new Vector3(targetScale, targetScale, 1f);
      }

      // Continually decrease our alpha
      Util.setAlpha(text, 1f - (timeSinceCreation / TEXT_LIFETIME) + .4f);
      // Util.setAlpha(iconImage, text.color.a);
   }

   public void setDamageAmount (int damageAmount, bool wasCritical, bool wasBlocked) {
      text.text = "" + damageAmount;

      // Increase the font size if it was a critical hit
      if (wasBlocked) {
         text.fontSize = (int) (text.fontSize * .75f);
      } else if (wasCritical) {
         text.fontSize = (int) (text.fontSize * 1.25f);
      }
   }

   public void customizeForAction (AttackAction action) {
      Ability ability = AbilityManager.getAbility(action.abilityType);
      Ability.Element element = ability.getElement();

      customizeForAction(element, action.wasCritical);
   }

   public void customizeForAction (Ability.Element element, bool wasCritical) {
      // Gradient gradient = text.GetComponent<Gradient>();
      text.font = Resources.Load<Font>("Fonts/PhysicalDamage");
      string fontString = "PhysicalDamage";

      switch (element) {
         case Ability.Element.Air:
            // gradient.vertex2 = Color.magenta;
            fontString = wasCritical ? "AirCrit" : "AirDamage";
            break;
         case Ability.Element.Earth:
            // gradient.vertex1 = Util.getColor(245, 117, 88);
            // gradient.vertex2 = Util.getColor(140, 70, 60);
            fontString = wasCritical ? "EarthCrit" : "EarthDamage";
            break;
         case Ability.Element.Fire:
            // gradient.vertex2 = Color.red;
            fontString = wasCritical ? "FireCrit" : "FireDamage";
            break;
         case Ability.Element.Water:
            // gradient.vertex2 = Color.blue;
            fontString = wasCritical ? "WaterCrit" : "WaterDamage";
            break;
         case Ability.Element.Physical:
            // gradient.vertex2 = Color.yellow;
            fontString = wasCritical ? "PhysicalCrit" : "PhysicalDamage";
            break;
         case Ability.Element.Heal:
            fontString = wasCritical ? "HealCrit" : "HealDamage";
            break;
      }

      // Update the font
      text.font = Resources.Load<Font>("Fonts/" + fontString);

      // Update the icon image based on the Elemental damage type
      // iconImage.sprite = Resources.Load<Sprite>("Icons/" + element);
   }

   #region Private Variables

   // The time at which we were created
   protected float _creationTime;

   // The row that contains the text and image
   protected HorizontalLayoutGroup _row;

   // The UI collider instance that we created
   protected GameObject _uiCollider;

   #endregion
}
