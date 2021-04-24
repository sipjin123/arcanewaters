using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;

public class PowerupPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Singleton instance
   public static PowerupPanel self;

   // A reference to the powerup icon prefab
   public GameObject powerupIconPrefab;

   // A list of colors associated with each rarity type
   public List<Color> rarityColors;

   // A reference to the canvas that this panel is in
   public Canvas parentCanvas;

   #endregion

   private void Awake () {
      self = this;
      _layoutGroup = GetComponent<GridLayoutGroup>();
   }

   public void addPowerup (Powerup.Type type, Rarity.Type rarity) {
      PowerupIcon newPowerup = Instantiate(powerupIconPrefab, transform).GetComponent<PowerupIcon>();
      
      newPowerup.init(type, rarity);
      _powerupIcons.Add(newPowerup);

      // When a new powerup icon is added, sort the list by rarity
      PowerupIcon[] orderedIcons = _powerupIcons.OrderBy(x => (int) x.rarity).ToArray();
      for (int i = 0; i < orderedIcons.Length; i++) {
         orderedIcons[i].transform.SetSiblingIndex(i);
      }
   }

   public void clearPowerups () {
      foreach (PowerupIcon icon in _powerupIcons) {
         Destroy(icon.gameObject);
      }
      _powerupIcons.Clear();
   }

   public void updatePowerups (List<Powerup> powerups) {
      clearPowerups();

      foreach (Powerup powerup in powerups) {
         addPowerup(powerup.powerupType, powerup.powerupRarity);
      }
   }

   private void Update () {
      float targetSpacing = (_isExpanded) ? EXPANDED_SPACING : COLLAPSED_SPACING;
      _layoutGroup.spacing = new Vector2(Mathf.Lerp(_layoutGroup.spacing.x, targetSpacing, Time.deltaTime * EXPAND_SPEED), _layoutGroup.spacing.y);
   }

   public void OnPointerEnter (PointerEventData eventData) {
      _isExpanded = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      _isExpanded = false;
   }

   #region Private Variables

   // A reference to the layout group that holds the powerup icons
   private GridLayoutGroup _layoutGroup;

   // Whether the powerup panel is expanded
   private bool _isExpanded = false;

   // The spacing of the horizontal layout group when icons are collapsed
   private const float COLLAPSED_SPACING = -32.0f;

   // The spacing of the horizontal layout group when icons are expanded
   private const float EXPANDED_SPACING = 4.0f;

   // How fast the icons expand / collapse
   private const float EXPAND_SPEED = 10.0f;

   // A list of references to our current powerups
   private List<PowerupIcon> _powerupIcons = new List<PowerupIcon>();

   #endregion
}
