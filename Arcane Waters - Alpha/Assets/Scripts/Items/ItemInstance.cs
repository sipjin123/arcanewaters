using System;

/// <summary>
/// Represents an instance of an item inside the game. Actual information and behavior of an item is described in 'ItemDefinition' class.
/// </summary>
[Serializable]
public class ItemInstance
{
   #region Public Variables

   // Unique identifier of item's instance
   public int id;

   // Unique identifier of the this is item's description
   public int itemDefinitionId;

   // The number of these items that are stacked together
   public int count = 1;

   // Name of palette that changes color of item
   public string paletteName1 = "";

   // Name of palette that changes color of item
   public string paletteName2 = "";

   #endregion

   public T getDefinition<T> () where T : ItemDefinition {
      return _definition as T;
   }

   #region Private Variables

   // The definition of this item, describing it's behavior
   protected ItemDefinition _definition;

   #endregion
}
