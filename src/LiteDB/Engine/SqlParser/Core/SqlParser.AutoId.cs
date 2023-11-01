namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// auto_id::
    ///  | "GUID" 
    ///  | "INT" 
    ///  | "LONG" 
    ///  | "OBJECTID"
    /// </summary>
    private bool TryParseWithAutoId(out BsonAutoId autoId)
    {
        var with = _tokenizer.LookAhead();

        if (with.Type == TokenType.Colon)
        {
            _tokenizer.ReadToken();

            var type = _tokenizer.ReadToken().Expect(TokenType.Word);

            if (type.Value.Eq("GUID"))
                autoId = BsonAutoId.Guid;
            else if (type.Value.Eq("INT"))
                autoId = BsonAutoId.Int32;
            else if (type.Value.Eq("LONG"))
                autoId = BsonAutoId.Int64;
            else if (type.Value.Eq("OBJECTID"))
                autoId = BsonAutoId.ObjectId;
            else
                throw ERR_UNEXPECTED_TOKEN(type, "GUID, INT, LONG, OBJECTID");

            return true;
        }

        autoId = BsonAutoId.Int32;

        return false;
    }

}
