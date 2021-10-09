public class UserSuggestionData
{
   #region Public Variables

   #endregion

   public UserSuggestionData (string userName, string description, string input, string partial) {
      _userName = userName;
      _description = description;
      _partial = partial;
      _input = input;
   }

   public string getUserName () {
      return _userName;
   }

   public string getDescription () {
      return _description;
   }

   public string getPartial () {
      return _partial;
   }

   public string getInput () {
      return _input;
   }

   #region Private Variables

   // The name of the suggested user
   protected string _userName;

   // The description for this suggestion
   protected string _description;

   // The portion of the suggesion that the user has typed so far
   private string _partial;

   // The text the user typed
   private string _input;

   #endregion
}
