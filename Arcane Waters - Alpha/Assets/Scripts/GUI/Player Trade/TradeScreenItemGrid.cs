using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Linq;

public class TradeScreenItemGrid : ClientMonoBehaviour
{
   #region Public Variables

   #endregion

   protected override void Awake () {
      base.Awake();

      _goldCell.tooltip.message = "Gold";
   }

   private void Start () {
      _goldCell.leftClickEvent.AddListener(() => onGoldLeftClick());
      _goldCell.rightClickEvent.AddListener(onGoldRightClick);
   }

   public void setState (IDictionary<int, Item> items, int gold, bool allowModifyActions, bool showModifyActions) {
      // Set the gold
      _goldCountText.text = gold.ToString();

      // Set actions
      _showModifyActions = showModifyActions;
      _allowModifyActions = allowModifyActions;
      _addItemsButton.gameObject.SetActive(showModifyActions);
      _addItemsButton.interactable = allowModifyActions;

      // Check which new items we need to create
      foreach (var entry in items) {
         if (!_currentDisplayedItems.TryGetValue(entry.Key, out ItemCell existingCell)) {
            // New item arrived, needs to be created
            ItemCell cell = Instantiate(_itemCellPrefab, _itemCellsContainer.transform, false);
            _currentDisplayedItems.Add(entry.Key, cell);

            cell.setCellForItem(entry.Value);

            // Capture item id
            int itemId = entry.Key;
            cell.rightClickEvent.AddListener(() => onItemRightClick(itemId));
            cell.leftClickEvent.AddListener(() => onItemLeftClick(itemId));
         } else {
            // Existing item arrived, update count if needed
            if (existingCell.itemCache.count != entry.Value.count) {
               existingCell.setCellForItem(entry.Value);
            }
         }
      }

      // Check which items need to be deleted
      _deleteItems.Clear();
      foreach (var entry in _currentDisplayedItems) {
         if (!items.ContainsKey(entry.Key)) {
            _deleteItems.Add(entry.Key);
         }
      }

      foreach (int id in _deleteItems) {
         Destroy(_currentDisplayedItems[id].gameObject);
         _currentDisplayedItems.Remove(id);
      }

      // Set the item list text
      string listText = gold == 0 ? "" : gold + " Gold" + System.Environment.NewLine;
      listText += string.Join(System.Environment.NewLine, items.Values.Select(i => i.count + " x " + EquipmentXMLManager.self.getItemName(i)));
      if (gold == 0 && items.Values.All(i => i.count == 0)) {
         listText = "Nothing";
      }
      _itemListText.text = listText;
   }

   private void onItemRightClick (int itemId) {
      if (!_showModifyActions || !_allowModifyActions) {
         return;
      }

      PanelManager.self.contextMenuPanel.clearButtons();
      PanelManager.self.contextMenuPanel.addButton("Remove",
         () => PanelManager.self.get<PlayerTradeScreen>(Panel.Type.PlayerTrade).onRemoveItemClick(itemId));
      PanelManager.self.contextMenuPanel.addButton("Set Amount",
         () => PanelManager.self.get<PlayerTradeScreen>(Panel.Type.PlayerTrade).onSetItemAmountClick(itemId));
      PanelManager.self.contextMenuPanel.show("");
   }

   private void onGoldRightClick () {
      if (!_showModifyActions || !_allowModifyActions) {
         return;
      }

      PanelManager.self.contextMenuPanel.clearButtons();
      PanelManager.self.contextMenuPanel.addButton("Set Amount",
         () => PanelManager.self.get<PlayerTradeScreen>(Panel.Type.PlayerTrade).onSetGoldAmountClick());
      PanelManager.self.contextMenuPanel.show("");
   }

   private void onGoldLeftClick () {
      if (!_showModifyActions || !_allowModifyActions) {
         return;
      }

      PanelManager.self.get<PlayerTradeScreen>(Panel.Type.PlayerTrade).onSetGoldAmountClick();
   }

   private void onItemLeftClick (int itemId) {
      if (!_showModifyActions || !_allowModifyActions) {
         return;
      }

      PanelManager.self.get<PlayerTradeScreen>(Panel.Type.PlayerTrade).onSetItemAmountClick(itemId);
   }

   #region Private Variables

   // The prefab we use for creating item cells
   [SerializeField]
   private ItemCell _itemCellPrefab = null;

   // The container of the item cells
   [SerializeField]
   private GameObject _itemCellsContainer = null;

   // The count text of gold
   [SerializeField]
   private TMP_Text _goldCountText = null;

   // Button we use to add items
   [SerializeField]
   private Button _addItemsButton = null;

   // Cell we use for displaying gold
   [SerializeField]
   private ItemCell _goldCell = null;

   // Text that holds the list of items
   [SerializeField]
   private Text _itemListText = null;

   // The current items we are displaying
   private Dictionary<int, ItemCell> _currentDisplayedItems = new Dictionary<int, ItemCell>();

   // Set we use to check if item needs to be deleted
   private HashSet<int> _deleteItems = new HashSet<int>();

   // Do we show and do we allow item modification actions
   private bool _allowModifyActions;
   private bool _showModifyActions;

   #endregion
}
