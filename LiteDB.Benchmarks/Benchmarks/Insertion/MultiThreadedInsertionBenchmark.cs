using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using LiteDB.Benchmarks.Models;
using LiteDB.Benchmarks.Models.Generators;

namespace LiteDB.Benchmarks.Benchmarks.Insertion
{
	[BenchmarkCategory(Constants.Categories.INSERTION)]
	public class MultiThreadedInsertionBenchmark : BenchmarkBase
	{ 
		[Params(1, 2, 5, 10, 20)]
		public int AmountOfThreads;

		private List<FileMetaBase> _data;

		private Action[] _insertionTasksArray;

		[GlobalSetup]
		public void GlobalSetup()
		{
			File.Delete(DatabasePath);

			DatabaseInstance = new LiteDatabase(ConnectionString());

			// Attempt to initialize the engine before benchmarks start running
			DatabaseInstance.GetCollectionNames();

			_data = FileMetaGenerator<FileMetaBase>.GenerateList(DatasetSize); // executed once for each benchmark run
		}

		[IterationSetup]
		public void IterationSetupGeneric()
		{
			_insertionTasksArray = new Action[AmountOfThreads];
			for (var i = 0; i < AmountOfThreads; i++)
			{
				var collectionName = ((char) (65 + i)).ToString(); // CollectionName starting with A, will go up to the "AmountOfThreads"-th character of the alphabet.
				_insertionTasksArray[i] = () => DatabaseInstance.GetCollection<FileMetaBase>(collectionName).Insert(_data);
			}
		}

		[Benchmark]
		public void InsertionYThreads()
		{
			Parallel.Invoke(_insertionTasksArray);
			DatabaseInstance.Checkpoint();
		}

		[IterationCleanup]
		public void IterationCleanup()
		{
			for (var i = 0; i < _insertionTasksArray.Length; i++)
			{
				_insertionTasksArray[i] = null;
			}

			_insertionTasksArray = null;

			foreach (var collectionName in DatabaseInstance.GetCollectionNames())
			{
				DatabaseInstance.DropCollection(collectionName);
			}

			DatabaseInstance.Checkpoint();
			DatabaseInstance.Rebuild();
		}

		[GlobalCleanup]
		public void GlobalCleanup()
		{
			foreach (var collectionName in DatabaseInstance.GetCollectionNames())
			{
				DatabaseInstance.DropCollection(collectionName);
			}

			DatabaseInstance.Checkpoint();
			DatabaseInstance?.Dispose();
			DatabaseInstance = null;

			File.Delete(DatabasePath);
		}
	}
}