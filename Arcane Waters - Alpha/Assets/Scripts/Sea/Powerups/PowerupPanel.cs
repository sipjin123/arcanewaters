using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;
using DG.Tweening;

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

   // A reference to the rect transform of the layout group that contains this panel
   public RectTransform containingLayoutGroup;

   // Reference to the container of this Powerup Panel
   public RectTransform powerupPanelContainer;

   #endregion

   private void Awake () {
      self = this;
      _layoutGroup = GetComponent<GridLayoutGroup>();
      _rectTransform = GetComponent<RectTransform>();
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

      newPowerup.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f);

      // Show the powerup container
      powerupPanelContainer.gameObject.SetActive(true);
   }

   public void clearPowerups () {
      foreach (PowerupIcon icon in _powerupIcons) {
         Destroy(icon.gameObject);
      }
      _powerupIcons.Clear();

      // Hide the panel container if there are no powerups
      powerupPanelContainer.gameObject.SetActive(false);
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

   public void rebuildLayoutGroup () {
      LayoutRebuilder.ForceRebuildLayoutImmediate(containingLayoutGroup);
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

   // A reference to the rect transform of this panel
   private RectTransform _rectTransform;

   #endregion
}
