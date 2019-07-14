namespace Crosstales.BWF.Model
{
    /// <summary>Model for a source of bad words.</summary>
    [System.Serializable]
    public class BadWords
    {

        #region Variables

        /// <summary>Source-object.</summary>
        public Data.Source Source;

        /// <summary>List of all bad words (RegEx).</summary>
        public System.Collections.Generic.List<string> BadWordList = new System.Collections.Generic.List<string>();

        #endregion


        #region Constructor

        /// <summary>Instantiate the class.</summary>
        /// <param name="source">Source-object.</param>
        /// <param name="badWordList">List of all bad words (RegEx).</param>
        public BadWords(Data.Source source, System.Collections.Generic.List<string> badWordList)
        {
            Source = source;

            foreach (string badWord in badWordList)
            {
                BadWordList.Add(badWord.Split('#')[0]);
            }
        }

        #endregion


        #region Overridden methods

        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();

            result.Append(GetType().Name);
            result.Append(Util.Constants.TEXT_TOSTRING_START);

            result.Append("Source='");
            result.Append(Source);
            result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER);

            result.Append("BadWordList='");
            result.Append(BadWordList);
            result.Append(Util.Constants.TEXT_TOSTRING_DELIMITER_END);

            result.Append(Util.Constants.TEXT_TOSTRING_END);

            return result.ToString();
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)