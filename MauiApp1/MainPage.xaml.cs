using LiteDB;
using System.Diagnostics;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	DBTest test = new();

	private void OnCounterClicked(object sender, EventArgs e)
	{
		test.Execute();
	}
}

class DBTest
{
	private const string DbName = "test.db";
	private const string LogName = "test-log.db";
	private const int Records = 100_0000;
	private const int Chunk = 1000;
	static string path = Path.Combine(FileSystem.Current.AppDataDirectory, $"{DbName}.db");


	public void Execute()
	{
		if (File.Exists(DbName))
		{
			File.Delete(DbName);
		}

		if (File.Exists(LogName))
		{
			File.Delete(LogName);
		}

		using (var db = new LiteDatabase(path))
		{
			var collection = db.GetCollection<TestRecord>();
			collection.EnsureIndex(x => x.Index);

			var data = GenerateData();

			var sw = Stopwatch.StartNew();

			foreach (var chunk in data)
			{
				collection.Upsert(chunk);
			}

			Console.Write(sw.Elapsed);
		}
	}
	private static IReadOnlyCollection<IReadOnlyCollection<TestRecord>> GenerateData()
	{
		var records = new List<TestRecord>(Records);

		for (var i = 0; i < Records; i++)
		{
			records.Add(new TestRecord
			{
				Index = i,
				First = Random.Shared.Next(),
				Second = Random.Shared.Next(),
			});
		}

		return records.Chunk(Chunk).ToArray();
	}
	private sealed class TestRecord
	{
		public int Index { get; init; }

		public int First { get; init; }

		public int Second { get; init; }
	}

}