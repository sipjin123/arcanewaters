using System;

[Serializable]
public class ShipXMLContent
{
   // Id of the xml entry
   public int xmlId;

   // Data of the ship content
   public ShipData shipData;
   
   // Determines if the entry is enabled in the database
   public bool isEnabled;
}
