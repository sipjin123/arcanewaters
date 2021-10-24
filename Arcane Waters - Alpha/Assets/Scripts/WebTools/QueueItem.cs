#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class QueueItem {
   #region Public Variables

   // The queue item's id from the database
   public int id;

   // The queue item's JSON content
   public string jsonContent;

   #endregion

   public QueueItem () { }

   #if IS_SERVER_BUILD

   public QueueItem (MySqlDataReader dataReader) {
      this.id = dataReader.GetInt32("id");
      this.jsonContent = dataReader.GetString("jsonContent");
   }

   #endif

   #region Private Variables

   #endregion
}
