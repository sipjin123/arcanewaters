using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;

public class PerksPanel : SubPanel
{
   #region Public Variables

   // The template for perks in the list
   [Header("Perks")]
   public PerkElementTemplate perkTemplate;

   // The parent for all the perks
   public Transform perkContainer;

   // The container grid layout
   public GridLayoutGroup perksGridLayoutGroup;

   // The unassigned perk points text
   public Text unassignedPerkPointsText;

   // The perk icon sprite borders
   public List<Sprite> perkIconBorders;

   // Blocks the outdated info while waiting for server response
   public GameObject loadingBlocker;

   // Set to true when we are waiting for a server response after assigning a perk point
   public bool isAssigningPerkPoint = false;

   // Self
   public static PerksPanel self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      Util.disableCanvasGroup(canvasGroup);
   }

   public void receivePerkData (List<PerkData> perkDataList) {
      perksGridLayoutGroup.enabled = true;
      perkContainer.gameObject.DestroyChildren();
      _perkIcons = new Dictionary<int, PerkElementTemplate>();

      _perkDataList = perkDataList;

      foreach (PerkData data in _perkDataList) {
         // Don't add unassigned points to the list 
         if ((Perk.Category) data.perkCategoryId == Perk.Category.None) {
            continue;
         }

         PerkElementTemplate template = Instantiate(perkTemplate, perkContainer);
         template.initializeData(data);
         _perkIcons.Add(data.perkId, template);
      }
   }

   public void receivePerkPoints (bool isLocalPlayer, Perk[] perks) {
      foreach (int perkId in _perkIcons.Keys) {
         Perk perk = perks.FirstOrDefault(x => x.perkId == perkId);
         int points = perk != null ? perk.points : 0;

         _perkIcons[perkId].initializePoints(points, isLocalPlayer);
      }

      if (isLocalPlayer) {
         Perk perk = perks.FirstOrDefault(x => x.perkId == Perk.UNASSIGNED_ID);
         setAvailablePoints(perk != null ? perk.points : 0);
      }

      unassignedPerkPointsText.gameObject.SetActive(isLocalPlayer);
   }

   private void setAvailablePoints (int points) {
      unassignedPerkPointsText.text = string.Format("Available Points: {0}", points);
   }

   public void onExitButtonPressed () {
      hide();
   }

   public override void show () {
      base.show();

      // Temporarily disable the automatic tooltips
      TooltipManager.self.isAutomaticTooltipEnabled = false;

      // Reenable the grid layout group in case it was disabled
      perksGridLayoutGroup.enabled = true;
   }

   public override void hide () {
      base.hide();

      // Disable any tooltip we may have enabled and reenable automatic tooltips
      TooltipHandler.self.cancelToolTip();
   }

   #region Private Variables

   // The selected item when creating an auction
   private Item _selectedItem = null;

   // The displayed auction when consulting or bidding
   private AuctionItemData _auction = null;

   // The list of all the perks
   protected List<PerkData> _perkDataList;

   // The panel perk icons 
   protected Dictionary<int, PerkElementTemplate> _perkIcons = new Dictionary<int, PerkElementTemplate>();

   #endregion
}