[System.Serializable]
public class HaircutData : BaseItemData
{
   #region Public Variables

   // The type of the haircut
   [System.Xml.Serialization.XmlElement(Namespace = "HairLayer.Type")]
   public HairLayer.Type type;

   #endregion

   public string getNumber() {
      return type.ToString().Split('_')[2];
   }

   public Gender.Type getGender () {
      return type.ToString().ToLower().Contains("female") ? Gender.Type.Female : Gender.Type.Male;
   }
}
