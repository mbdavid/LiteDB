using System;
using LiteDB;

namespace LiteDbRepro
{
	class Program
	{
		static void Main(string[] args)
		{
			BsonMapper.Global.Entity<AudioLogEntry>()
				.Id(x => x.Id, true)
				.Index(x => x.UserInvokeId)
				.Index(x => x.Timestamp);

			using (var database = new LiteDatabase("history.db"))
			{
				var audioLogEntries = database.GetCollection<AudioLogEntry>("audioLogEntries");
				audioLogEntries.EnsureIndex(x => x.AudioResource.ResourceTitle);
				audioLogEntries.EnsureIndex(x => x.AudioResource.UniqueId, true);

				var ale = new AudioLogEntry(1, new AudioResource("a", "b", AudioType.MediaLink))
				{
					UserInvokeId = 42,
					Timestamp = DateTime.UtcNow,
					PlayCount = 1,
				};

				// Throws here:
				audioLogEntries.Insert(ale);
			}
		}
	}

	public class AudioLogEntry
	{
		/// <summary>A unique id for each <see cref="ResourceFactories.AudioResource"/>, given by the history system.</summary>
		public int Id { get; set; }
		/// <summary>The dbid of the teamspeak user, who played this song first.</summary>
		public uint UserInvokeId { get; set; }
		/// <summary>How often the song has been played.</summary>
		public uint PlayCount { get; set; }
		/// <summary>The last time this song has been played.</summary>
		public DateTime Timestamp { get; set; }

		public AudioResource AudioResource { get; set; }

		public AudioLogEntry()
		{
			PlayCount = 0;
		}

		public AudioLogEntry(int id, AudioResource resource) : this()
		{
			Id = id;
			AudioResource = resource;
		}

		public void SetName(string newName)
		{
			AudioResource = AudioResource.WithName(newName);
		}
	}

	public class AudioResource
	{
		/// <summary>The resource type.</summary>
		public AudioType AudioType { get; set; }
		/// <summary>An identifier to create the song. This id is uniqe among same <see cref="TS3AudioBot.AudioType"/> resources.</summary>
		public string ResourceId { get; set; }
		/// <summary>The display title.</summary>
		public string ResourceTitle { get; set; }
		/// <summary>An identifier wich is unique among all <see cref="AudioResource"/> and <see cref="TS3AudioBot.AudioType"/>.</summary>
		public string UniqueId => ResourceId + AudioType.ToString();

		public AudioResource() { }

		public AudioResource(string resourceId, string resourceTitle, AudioType type)
		{
			ResourceId = resourceId;
			ResourceTitle = resourceTitle;
			AudioType = type;
		}

		public AudioResource(AudioResource copyResource)
		{
			ResourceId = copyResource.ResourceId;
			ResourceTitle = copyResource.ResourceTitle;
			AudioType = copyResource.AudioType;
		}

		public AudioResource WithName(string newName) => new AudioResource(ResourceId, newName, AudioType);

		public override bool Equals(object obj)
		{
			var other = obj as AudioResource;
			if (other == null)
				return false;

			return AudioType == other.AudioType
				&& ResourceId == other.ResourceId;
		}

		public override int GetHashCode()
		{
			int hash = 0x7FFFF + (int)AudioType;
			hash = (hash * 0x1FFFF) + ResourceId.GetHashCode();
			return hash;
		}

		public override string ToString()
		{
			return $"{AudioType} ID:{ResourceId}";
		}
	}

	public enum AudioType
	{
		MediaLink,
		Youtube,
		Soundcloud,
		Twitch,
	}
}
