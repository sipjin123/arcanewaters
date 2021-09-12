[System.Serializable]
public class ShipSkinData : BaseItemData
{
   #region Public Variables

   // The type of ship skin
   [System.Xml.Serialization.XmlElement(Namespace = "Ship.SkinType")]
   public Ship.SkinType skinType;

   // The type of ship
   [System.Xml.Serialization.XmlElement(Namespace = "Ship.Type")]
   public Ship.Type shipType;

   #endregion
}