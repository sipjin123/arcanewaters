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

      if (_grayscaleIntensityID < 0) {
         _grayscaleIntensityID = Shader.PropertyToID("_Intensity");
      }

      Material material = new Material(Shader.Find("UI/Grayscale"));
      _iconImage.material = material;
      _borderImage.material = material;

      _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
   }

   public void initialize (PerkData data) {
      perkData = data;

      _iconImage.sprite = ImageManager.getSprite(data.iconPath);
   }

   public void setAssignedPoints (int points) {
      _borderImage.sprite = CreationPerksGrid.self.getBorderForLevel(points);
      _hasAssignedPoints = points > 0;

      if (_hasAssignedPoints) {
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 0);
      } else {
         // Make the icons grayscale
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
      }

      _assignedPointsText.text = points.ToString();
      _assignedPointsIndicator.gameObject.SetActive(_hasAssignedPoints);
   }

   #region Mouse Events

   public void OnPointerEnter (PointerEventData eventData) {
      transform.localScale = _originalScale * _iconScaleOnHover;

      _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 0);

      // Update perk name
      CharacterCreationPanel.self.perkName.enabled = true;
      CharacterCreationPanel.self.perkName.text = perkData.name;

      // Update perk description
      CharacterCreationPanel.self.perkDescription.enabled = true;
      CharacterCreationPanel.self.perkDescription.text = perkData.description;

      // Update assigned points
      CharacterCreationPanel.self.perkAssignedPoints.enabled = true;
      CharacterCreationPanel.self.perkAssignedPoints.text = $"Assigned Points: " + _assignedPointsText.text;

      SoundManager.play2DClip(SoundManager.Type.GUI_Hover);
   }

   public void OnPointerExit (PointerEventData eventData) {
      transform.localScale = _originalScale;

      if (!_hasAssignedPoints) {
         _iconImage.materialForRendering.SetFloat(_grayscaleIntensityID, 1);
      }

      // Turn off the perk text when not hovering over it
      CharacterCreationPanel.self.perkName.enabled = false;
      CharacterCreationPanel.self.perkDescription.enabled = false;
      CharacterCreationPanel.self.perkAssignedPoints.enabled = false;
   }

   public void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left) {
         CreationPerksGrid.self.assignPoint(this);
      } else if (eventData.button == PointerEventData.InputButton.Right) {
         CreationPerksGrid.self.unassignPoint(this);
      }
      CharacterCreationPanel.self.perkAssignedPoints.text = $"Assigned Points: " + _assignedPointsText.text;
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

   // Whether this is the chosen icon for its group
   private bool _hasAssignedPoints;

   // The original scale of the icon
   private Vector3 _originalScale;

   // The ID of the grayscale intensity property
   private static int _grayscaleIntensityID = -1;

   #endregion
}
