using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;
using System;
using System.Text;

public class PerkElementTemplate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
   #region Public Variables

   // The number of points assigned by the player
   public int assignedPoints;

   // True when the data we're seeing is from the local player, meaning points can be assigned
   public bool isLocalPlayer;

   #endregion

   private void Awake () {
      _rectTransform = transform as RectTransform;
      _originalPivotY = _rectTransform.pivot.y;
   }

   public void initializeData (PerkData data) {
      _icon.sprite = ImageManager.getSprite(data.iconPath);
      _perkData = data;

      // Initialize the tooltip
      StringBuilder builder = new StringBuilder();
      builder.AppendLine($"<b>{_perkData.name}</b>");
      builder.AppendLine();
      builder.AppendLine(_perkData.description);
      builder.AppendLine();
      _tooltipText = builder.ToString();
   }

   public void initializePoints (int assignedPoints, bool isLocalPlayer) {
      this.assignedPoints = assignedPoints;
      this.isLocalPlayer = isLocalPlayer;

      _tooltipAssignedPointsText = $"Assigned Points: {assignedPoints}";
            
      if (assignedPoints > 0) {
         int borderIndex = Mathf.Clamp(assignedPoints - 1, 0, CharacterInfoPanel.self.perkIconBorders.Count - 1);
         _perkBorder.sprite = CharacterInfoPanel.self.perkIconBorders[borderIndex];

         _icon.color = Color.white;
         _perkBorder.color = Color.white;

         if (assignedPoints >= Perk.MAX_POINTS_BY_PERK) {
            _tooltipAssignedPointsText += " <color=green>(Maximum level!)</color>";
         }
      } else {
         // Make the icons slightly transparent
         _icon.color = _noAssignedPointsColor;
         _perkBorder.color = _noAssignedPointsColor;
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (!isLocalPlayer) {
         showTooltip();
         return;
      }

      _fadeInfoSequence?.Kill();
      _fadeInfoSequence = DOTween.Sequence();

      // Disable the grid layout group so we can move the icons
      CharacterInfoPanel.self.perksGridLayoutGroup.enabled = false;

      _fadeInfoSequence.Join(_rectTransform.DOPivotY(_originalPivotY - _iconMoveUpOnHoverAmount, _infoOnHoverFadeTime));
      _fadeInfoSequence.AppendCallback(showTooltip);

      _fadeInfoSequence.Play();
   }

   public void OnPointerExit (PointerEventData eventData) {
      TooltipManager.self.hideTooltip();

      if (!isLocalPlayer) {
         return;
      }

      _fadeInfoSequence?.Kill();
      _fadeInfoSequence = DOTween.Sequence();


      _fadeInfoSequence.Join(_rectTransform.DOPivotY(_originalPivotY, _infoOnHoverFadeTime));

      _fadeInfoSequence.Play();
   }

   public void OnPointerClick (PointerEventData eventData) {
      if (!isLocalPlayer) {
         return;
      }

      // Perks can't be assigned more than Perk.MAX_POINTS_PER_PERK points
      if (PerkManager.self.getAssignedPointsByPerkId(_perkData.perkId) < Perk.MAX_POINTS_BY_PERK) {
         if (PerkManager.self.getUnassignedPoints() > 0) {
            PanelManager.self.showConfirmationPanel("Are you sure you want to assign one point to this perk?\n\nThis cannot be undone.",
               () => requestPerkPointsIncrement());
         } else {
            PanelManager.self.noticeScreen.show("You don't have points to assign.");
         }
      } else {
         PanelManager.self.noticeScreen.show("This perk already reached its maximum level.");
      }
   }

   private void requestPerkPointsIncrement () {
      if (Global.player == null || Global.player.rpc == null) {
         return;
      }

      // Show the increment locally so the UI doesn't look laggy
      assignedPoints++;

      SoundManager.play2DClip(SoundManager.Type.Perk_Point_Assigned);

      Global.player.rpc.Cmd_AssignPerkPoint(_perkData.perkId);
   }

   private void showTooltip () {
      TooltipManager.self.showTooltip(_tooltipText + _tooltipAssignedPointsText);
   }

   #region Private Variables

   // The perk icon
   [Header("References")]
   [SerializeField]
   private Image _icon;

   // The perk border image
   [SerializeField]
   private Image _perkBorder;

   [Header("Colors")]
   // The color to use when no points have been assigned to the perk
   [SerializeField]
   private Color _noAssignedPointsColor = new Color(1, 1, 1, 0.9f);

   // The duration of the fade in of the info when hovering over the icon
   [Header("Animation")]
   [SerializeField]
   private float _infoOnHoverFadeTime = 0.25f;

   // The growth factor of the icon when hovering over
   [SerializeField]
   [Range(0, 1)]
   private float _iconMoveUpOnHoverAmount = 0.25f;

   // The Sequence controlling the fade in/out of info
   private Sequence _fadeInfoSequence;

   // The original scale of the icon
   private float _originalPivotY;

   // The transform as rect transform
   private RectTransform _rectTransform;

   // The perk data 
   private PerkData _perkData;

   // The tooltip text
   private string _tooltipText;

   // The assigned points tooltip text
   private string _tooltipAssignedPointsText;

   #endregion
}
