﻿using UnityEngine;
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

      if (_grayscaleIntensityID < 0) {
         _grayscaleIntensityID = Shader.PropertyToID("_Intensity");
      }

      Material material = new Material(Shader.Find("UI/Grayscale"));
      _iconImage.material = material;
      _borderImage.material = material;

      _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
      gameObject.AddComponent<ToolTipComponent>();
      gameObject.GetComponent<ToolTipComponent>().tooltipPlacement = ToolTipComponent.TooltipPlacement.LeftSideOfPanel;
      gameObject.GetComponent<ToolTipComponent>().message = _tooltipText + _tooltipAssignedPointsText;
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

   public void setAssignedPoints (int points, bool updateTooltip) {
      _borderImage.sprite = CreationPerksGrid.self.getBorderForLevel(points);
      _hasAssignedPoints = points > 0;

      if (_hasAssignedPoints) {
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 0);
      } else {
         // Make the icons grayscale
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
      }

      _tooltipAssignedPointsText = $"\nAssigned Points: {points}";
      if (updateTooltip) {
         gameObject.GetComponent<ToolTipComponent>().message = _tooltipText + _tooltipAssignedPointsText;
      }

      _assignedPointsText.text = points.ToString();
      _assignedPointsIndicator.gameObject.SetActive(_hasAssignedPoints);
   }

   #region Mouse Events

   public void OnPointerEnter (PointerEventData eventData) {
      transform.SetAsLastSibling();

      transform.localScale = _originalScale * _iconScaleOnHover;

      _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 0);

      SoundManager.play2DClip(SoundManager.Type.GUI_Hover);
   }

   public void OnPointerExit (PointerEventData eventData) {
      transform.localScale = _originalScale;

      if (!_hasAssignedPoints) {
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
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

   // The tooltip text
   private string _tooltipText;

   // The tooltip text showing how many points have been assigned
   private string _tooltipAssignedPointsText;

   // Whether this is the chosen icon for its group
   private bool _hasAssignedPoints;

   // The original scale of the icon
   private Vector3 _originalScale;

   // The ID of the grayscale intensity property
   private static int _grayscaleIntensityID = -1;

   #endregion
}
