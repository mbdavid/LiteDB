using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Database
{
    public class InvalidFile_Tests
    {
        [Fact]
        public void Test_AddDatabase_InvalidDatabase()
        {
            // Set the database name and file name
            string dbName = "invalidDb";
            string fileName = $"{dbName}.db";
            
            // Create an invalid LiteDB database file for testing
            File.WriteAllText(fileName, "Invalid content");

            // Verify the file exists and content is correct
            Assert.True(File.Exists(fileName));
            Assert.Equal("Invalid content", File.ReadAllText(fileName));

            // Act & Assert: Try to open the invalid database and expect an exception
            Assert.Throws<LiteException>(() =>
            {
                using (var db = new LiteDatabase(fileName))
                {
                    // Attempt to perform an operation to ensure the database file is read
                    var col = db.GetCollection("test");
                    col.Insert(new BsonDocument { ["name"] = "test" });
                }
            });

            // Clean up: Remove the invalid database file after the test
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        [Fact]
        public void Test_AddDatabase_InvalidDatabase_LargeFile()
        {
            // Set the database name and file name
            string dbName = "largeInvalidDb";
            string fileName = $"{dbName}.db";

            // Create an invalid LiteDB database file with content larger than 16KB for testing
            string invalidContent = new string('a', 16 * 1024 + 1);
            File.WriteAllText(fileName, invalidContent);

            // Verify the file exists and content is correct
            Assert.True(File.Exists(fileName));
            Assert.Equal(invalidContent, File.ReadAllText(fileName));

            // Act & Assert: Try to open the invalid database and expect an exception
            Assert.Throws<LiteException>(() =>
            {
                using (var db = new LiteDatabase(fileName))
                {
                    // Attempt to perform an operation to ensure the database file is read
                    var col = db.GetCollection("test");
                    col.Insert(new BsonDocument { ["name"] = "test" });
                }
            });

            // Clean up: Remove the invalid database file after the test
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }
}