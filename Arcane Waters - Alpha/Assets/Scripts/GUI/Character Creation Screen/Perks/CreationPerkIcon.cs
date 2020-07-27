using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Text;
using DG.Tweening;
using UnityEditor;
using System;
using TMPro;

public class CreationPerkIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
   #region Public Variables

   // The perk ID
   public int perkId;

   // The perk data
   [HideInInspector]
   public PerkData perkData;

   #endregion

   private void Awake () {
      _originalScale = transform.localScale;
   }

   public void initialize (PerkData data) {
      perkData = data;

      _iconImage.sprite = ImageManager.getSprite(data.iconPath);

      // Initialize the tooltip
      StringBuilder builder = new StringBuilder();
      builder.AppendLine($"<b>{perkData.name}</b>");
      builder.AppendLine();
      builder.AppendLine(perkData.description);
      builder.AppendLine();
      _tooltipText = builder.ToString();

      _tooltipAssignedPointsText = $"\nAssigned Points: 0";
   }

   private void showTooltip () {
      TooltipManager.self.showTooltip(_tooltipText + _tooltipAssignedPointsText);
   }

   public void setAssignedPoints (int points) {
      _borderImage.sprite = CreationPerksGrid.self.getBorderForLevel(points);
      _hasAssignedPoints = points > 0;

      if (_hasAssignedPoints) {
         _iconImage.color = Color.white;
         _borderImage.color = Color.white;
      } else {
         // Make the icons slightly transparent
         _iconImage.color = new Color(1, 1, 1, _unselectedTransparency);
         _borderImage.color = new Color(1, 1, 1, _unselectedTransparency);
      }

      _tooltipAssignedPointsText = $"\nAssigned Points: {points}";
      showTooltip();

      _assignedPointsText.text = points.ToString();
      _assignedPointsIndicator.gameObject.SetActive(_hasAssignedPoints);
   }

   #region Mouse Events

   public void OnPointerEnter (PointerEventData eventData) {
      transform.SetAsLastSibling();

      transform.localScale = _originalScale * _iconScaleOnHover;

      _borderImage.color = Color.white;
      _iconImage.color = Color.white;

      showTooltip();
   }

   public void OnPointerExit (PointerEventData eventData) {
      TooltipManager.self.hideTooltip();

      transform.localScale = _originalScale;

      if (!_hasAssignedPoints) {
         _borderImage.color = new Color(1, 1, 1, _unselectedTransparency);
         _iconImage.color = new Color(1, 1, 1, _unselectedTransparency);
      }
      
   }

   public void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left) {
         CreationPerksGrid.self.assignPoint(this);
      } else if (eventData.button == PointerEventData.InputButton.Right) {
         CreationPerksGrid.self.unassignPoint(this);
      }
   }

   #endregion

   #region Private Variables

   // The icon image
   [SerializeField]
   private Image _iconImage;

   // The border image
   [SerializeField]
   private Image _borderImage;

   // The upper-left number displaying the assigned points
   [SerializeField]
   private GameObject _assignedPointsIndicator;

   // The text displaying the number of assigned points
   [SerializeField]
   private TextMeshProUGUI _assignedPointsText;

   // The growth factor of the icon when hovering over
   [SerializeField]
   private float _iconScaleOnHover = 1.25f;

   // The transparency when the icon is not selected
   [SerializeField]
   private float _unselectedTransparency = 0.6f;

   // The tooltip text
   private string _tooltipText;

   // The tooltip text showing how many points have been assigned
   private string _tooltipAssignedPointsText;

   // Whether this is the chosen icon for its group
   private bool _hasAssignedPoints;

   // The original scale of the icon
   private Vector2 _originalScale;

   #endregion
}
