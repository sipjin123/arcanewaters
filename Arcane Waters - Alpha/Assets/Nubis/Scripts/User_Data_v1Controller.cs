//#define NUBIS
#if NUBIS
using MySql.Data.MySqlClient;
#endif

namespace NubisTranslator {
   public class User_Data_v1Controller {
      public static string userData (int usrId) {
#if NUBIS
         try {
            using (MySqlConnection connection = DB_Main.getConnection()) {
               connection.Open();
               using (MySqlCommand command = new MySqlCommand(
                  "SELECT usrGold, accGems, usrGender, usrName, bodyType, hairType, hairPalette1, hairPalette2, eyesType, eyesPalette1, eyesPalette2, wpnId, armId " +
                  "FROM users " +
                  "JOIN accounts " +
                  "USING (accId) " +
                  "WHERE usrId=@usrId",
                  connection)) {
                  command.Parameters.AddWithValue("@usrId", usrId);

                  using (MySqlDataReader reader = command.ExecuteReader()) {
                     while (reader.Read()) {
                        int usrGold = reader.GetInt32("usrGold");
                        int accGems = reader.GetInt32("accGems");
                        int usrGender = reader.GetInt32("usrGender");
                        string usrName = reader.GetString("usrName");
                        int bodyType = reader.GetInt32("bodyType");
                        int hairType = reader.GetInt32("hairType");
                        string hairPalette1 = reader.GetString("hairPalette1");
                        string hairPalette2 = reader.GetString("hairPalette2");
                        int eyesType = reader.GetInt32("eyesType");
                        string eyesColor1 = reader.GetString("eyesPalette1");
                        string eyesColor2 = reader.GetString("eyesPalette2");
                        int wpnId = reader.GetInt32("wpnId");
                        int armId = reader.GetInt32("armId");
                        string result = $"usrGold:{usrGold}[space]accGems:{accGems}[space]usrGender:{usrGender}[space]usrName:{usrName}[space]bodyType:{bodyType}[space]hairType:{hairType}[space]hairPalette1:{hairPalette1}[space]hairPalette2:{hairPalette2}[space]eyesType:{eyesType}[space]eyesPalette1:{eyesColor1}[space]eyesPalette2:{eyesColor2}[space]wpnId:{wpnId}[space]armId:{armId}\n";
                        return result;
                     }
                  }
               }
            }
         } catch {
            return string.Empty;
         }
#endif
         return string.Empty;
      }
   }
}
