using System.Xml.Serialization;
using UnityEngine;

[System.Serializable]
public class BaseItemData
{
   #region Public Variables

   [XmlIgnore]
   // Identifier
   public int itemID;

   // The item name
   public string itemName;

   // The description of the item
   public string itemDescription;

   // The icon path
   public string itemIconPath;

   #endregion

   public BaseItemData () { }

   /// <summary>
   /// Creates a new instance of BaseItemData
   /// </summary>
   public static BaseItemData create (int itemID, string name, string desc, string itemIcon) {
      BaseItemData data = new BaseItemData();

      data.itemID = itemID;   
      data.itemName = name;
      data.itemDescription = desc;
      data.itemIconPath = itemIcon;

      return data;
   }

   public string serializeXML () {
      using (System.IO.StringWriter writer = new System.IO.StringWriter()) {
         XmlSerializer serializer = new XmlSerializer(GetType());
         serializer.Serialize(writer, this);
         return writer.ToString();
      }
   }
}
