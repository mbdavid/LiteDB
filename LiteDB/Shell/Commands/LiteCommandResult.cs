using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell
{
    public enum LiteCommandResponseType { Error, Void, Text, BsonValue, BsonDocument, BsonDocuments }

    /// <summary>
    /// Represent a result from a command shell
    /// </summary>
    public class LiteCommandResult
    {
        public LiteCommandResponseType ResponseType { get; private set; }

        public bool Ok { get; private set; }

        public BsonValue Response { get; private set; }

        public string ResponseText { get; private set; }

        public string ErrorMessage { get; private set; }

        #region Response Converters

        public BsonValue ToBson()
        {
            return (BsonValue)this.Response;
        }

        public BsonDocument ToBsonDocument()
        {
            return (BsonDocument)this.Response;
        }

        public IEnumerable<BsonDocument> ToBsonDocuments()
        {
            return (IEnumerable<BsonDocument>)this.Response;
        }

        public string ToText()
        {
            return (string)this.Response;
        }

        #endregion

        #region Static Constructors

        internal static LiteCommandResult Error(Exception ex)
        {
            return new LiteCommandResult { Ok = false, ResponseType = LiteCommandResponseType.Error, ErrorMessage = ex.Message };
        }

        internal static LiteCommandResult Text(string text)
        {
            return new LiteCommandResult { Ok = true, ResponseType = LiteCommandResponseType.Text, Response = text };
        }

        internal static LiteCommandResult Bson(BsonValue value)
        {
            return new LiteCommandResult { Ok = true, ResponseType = LiteCommandResponseType.BsonValue, Response = value };
        }

        #endregion

    }
}
