using System;
using System.Collections.Generic;

namespace LiteDB.Benchmarks.Models.Generators
{
    public static class FileMetaGenerator<T> where T : FileMetaBase, new()
    {
        private static Random Random;

        private static T Generate()
        {
            var generatedFileMeta = new T
            {
                FileId = Guid.NewGuid(),
                Version = Random.Next(5),
                Title = $"Document-{Guid.NewGuid()}",
                MimeType = "application/pdf",
                IsFavorite = Random.Next(10) >= 9,
                ShouldBeShown = Random.Next(10) >= 7
            };

            if (Random.Next(10) >= 5)
            {
                generatedFileMeta.ValidFrom = DateTimeOffset.Now.AddDays(-20 + Random.Next(40));
                generatedFileMeta.ValidFrom = DateTimeOffset.UtcNow.AddDays(-10 + Random.Next(40));
            }

            return generatedFileMeta;
        }

        public static List<T> GenerateList(int amountToGenerate)
        {
            Random = new Random(0);

            var generatedList = new List<T>();
            for (var i = 0; i < amountToGenerate; i++) generatedList.Add(Generate());

            foreach (var fileMeta in generatedList)
            {
                if (Random.Next(100) <= 1)
                {
                    continue;
                }

                fileMeta.ParentId = generatedList[Random.Next(amountToGenerate)].Id;
            }

            return generatedList;
        }
    }
}