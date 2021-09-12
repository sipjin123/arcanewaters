[System.Serializable]
public class ConsumableData : BaseItemData
{
   #region Public Variables

   // The type of consumable
   [System.Xml.Serialization.XmlElement(Namespace = "Consumable.Type")]
   public Consumable.Type consumableType;

   #endregion
}
