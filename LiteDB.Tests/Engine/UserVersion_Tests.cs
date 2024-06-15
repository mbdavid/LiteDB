namespace LiteDB.Tests.Engine;

using FluentAssertions;
using Xunit;

public class UserVersion_Tests
{
    [Fact]
    public void UserVersion_Get_Set()
    {
        using (var file = new TempFile())
        {
            using (var db = new LiteDatabase(file.Filename))
            {
                db.UserVersion.Should().Be(0);
                db.UserVersion = 5;
                db.Checkpoint();
            }

            using (var db = new LiteDatabase(file.Filename))
            {
                db.UserVersion.Should().Be(5);
            }
        }
    }
}