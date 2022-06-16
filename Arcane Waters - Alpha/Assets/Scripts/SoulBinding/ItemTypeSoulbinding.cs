[System.Serializable]
public struct ItemTypeSoulbinding
{
   #region Public Variables

   // The itemTypeId of the target item
   public ushort itemTypeId;

   // The category of the target item
   public Item.Category itemCategory;

   // What's the soulbinding type of this item
   public Item.SoulBindingType bindingType;

   #endregion

   #region Private Variables

   #endregion
}
