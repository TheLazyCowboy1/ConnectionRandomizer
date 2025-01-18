using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConnectionRandomizer;
/**
* Orignal file by Choc for RegionRandomizer
* Repurposed for ConnectionRandomizer
*/

internal partial class RainMeadowCompat
{
	//public static bool meadowEnabled = false;
	public static bool IsOnline => OnlineManager.lobby != null;
	public static bool IsHost => OnlineManager.lobby.isOwner;

    //public static RandomizerData onlineData = new();
    //public static object onlineData = null;
    public static bool onlineDataAdded = false;

    public static void InitCompat()
    {
        //thanks Forthfora! https://github.com/forthfora/pearlcat/blob/1eea5c439cef8465e0639b8cecf63c9586bbf485/src/Scripts/ModCompat/RainMeadow/MeadowCompat.cs
        try
        {
            _ = new Hook(
                typeof(OnlineResource).GetMethod("Available", BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(RainMeadowCompat).GetMethod(nameof(OnLobbyAvailable), BindingFlags.Static | BindingFlags.NonPublic)
            );
            ConnectionRandomizer.LogSomething("Added Lobby hook!");

            // use this event instead when it's been pushed
            // Lobby.ResourceAvailable
        }
        catch (Exception ex)
        {
            ConnectionRandomizer.LogSomething(ex);
        }
    }

    //thanks again Forthfora!! https://github.com/forthfora/pearlcat/blob/1eea5c439cef8465e0639b8cecf63c9586bbf485/src/Scripts/ModCompat/RainMeadow/MeadowCompat.cs
    private delegate void orig_OnLobbyAvailable(OnlineResource self);
    private static void OnLobbyAvailable(orig_OnLobbyAvailable orig, OnlineResource self)
    {
        orig(self);

        if (onlineDataAdded) return;

        //onlineData ??= new RandomizerData();

        self.AddData(new RandomizerData());

        ConnectionRandomizer.LogSomething("Added online data!");

        onlineDataAdded = true;
    }

    public static void AddOnlineData()
	{
		if (!IsOnline) return;
		//onlineData ??= new RandomizerData();
		//OnlineManager.lobby.AddData(onlineData as RandomizerData);
		ConnectionRandomizer.LogSomething("Added online data");
	}

	public class RandomizerData : OnlineResource.ResourceData
	{
		public RandomizerData() { }

		private ulong[] generationTimes = new ulong[0];
		private State currentState = null;

		public override ResourceDataState MakeState(OnlineResource resource)
		{
			if (currentState == null || generationTimes.Length != ConnectionRandomizer.Instance.RandomizationTimes.Count)
                currentState = new State(this);
			else
			{
				for (int i = 0; i < generationTimes.Length; i++)
				{
					if ((generationTimes[i] != ConnectionRandomizer.Instance.RandomizationTimes[i])) {
						currentState = new State(this);
						break;
					}
				}
			}
			generationTimes = ConnectionRandomizer.Instance.RandomizationTimes.ToArray();
            return currentState;
		}

		private class State : ResourceDataState
		{
			public override Type GetDataType() => typeof(RandomizerData);

			//[OnlineField(nullable = true)]
			//Dictionary<string, string> CustomGateLocks;

			//DynamicOrderedStrings RandomizedRegions = new(new List<string>());
			
			[OnlineField]
			string[] RandomizedRegions = new string[0];
			[OnlineField]
			ulong[] RegionGenerationTimes = new ulong[0]; //times of when each region was randomized. If time different, download data
			[OnlineField]
			string[] RegionConnectionFiles = new string[0];
			[OnlineField]
			string[] RegionMirrorFiles = new string[0];
			[OnlineField]
			string[] RegionMapFiles = new string[0];
			

			public State() { }
			public State(RandomizerData data)
			{
				RandomizedRegions = ConnectionRandomizer.Instance.RandomizedRegions.ToArray();

				//determine which regions to update //nah, instead I'll just store ALL of them
				/*
				List<int> regionsToUpdate = new();
				for (int i = 0; i < RegionGenerationTimes.Length; i++)
				{
					if (RegionGenerationTimes[i] != ConnectionRandomizer.Instance.RandomizationTimes[i])
						regionsToUpdate.Add(i); //update regions with changed times
				}
				for (int i = RegionGenerationTimes.Length; i < ConnectionRandomizer.Instance.RandomizationTimes.Count; i++)
					regionsToUpdate.Add(i); //update new regions
				*/
				RegionGenerationTimes = ConnectionRandomizer.Instance.RandomizationTimes.ToArray();

				//load all files
				RegionConnectionFiles = new string[RandomizedRegions.Length];
				RegionMirrorFiles = new string[RandomizedRegions.Length];
				RegionMapFiles = new string[RandomizedRegions.Length];
				//foreach (int i in regionsToUpdate)
				for (int i = 0; i < RandomizedRegions.Length; i++)
				{
					try
					{
						string region = RandomizedRegions[i], slugcat = ConnectionRandomizer.Instance.CurrentSlugcat;
						RegionConnectionFiles[i] = File.ReadAllText(ConnectionRandomizer.GetRandomizerConnectionsFile(region, slugcat));
						RegionMapFiles[i] = File.ReadAllText(ConnectionRandomizer.GetRandomizerMapFile(region, slugcat));
						RegionMirrorFiles[i] = File.ReadAllText(ConnectionRandomizer.GetMirroredRoomsFile(region, slugcat));
						ConnectionRandomizer.LogSomething("Successfully sent " + region + "-" + slugcat);
					}
					catch (Exception ex) { ConnectionRandomizer.LogSomething(ex); }
				}

			}

			public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
			{
				try
				{
					//ConnectionRandomizer.CustomGateLocks = CustomGateLocksKeys.Zip(CustomGateLocksValues, (k, v) => (k, v)).ToDictionary(x => x.k, x => x.v);
					ConnectionRandomizer rando = ConnectionRandomizer.Instance;

					rando.RandomizedRegions = RandomizedRegions.ToList();

					//immediately CANCEL any randomization of regions already randomized
					if (rando.RandomizedRegions.Contains(rando.CurrentlyRandomizing))
					{
						rando.RandomizerThread?.Abort();
						ConnectionRandomizer.LogSomething("Aborted thread, if it even existed.");
					}


					//update region files

					//deetermine which regions to update
					List<int> regionsToUpdate = new();
					for (int i = 0; i < rando.RandomizationTimes.Count; i++)
					{
						if (RegionGenerationTimes[i] != rando.RandomizationTimes[i])
							regionsToUpdate.Add(i); //update regions with changed times
					}
					for (int i = rando.RandomizationTimes.Count; i < RegionGenerationTimes.Length; i++)
						regionsToUpdate.Add(i); //update new regions

					rando.RandomizationTimes = RegionGenerationTimes.ToList();

					foreach (int i in regionsToUpdate)
					{
						try
						{
							string region = RandomizedRegions[i];
							string slugcat = rando.CurrentSlugcat;
							File.WriteAllText(ConnectionRandomizer.GetRandomizerConnectionsFile(region, slugcat), RegionConnectionFiles[i]);
							File.WriteAllText(ConnectionRandomizer.GetRandomizerMapFile(region, slugcat), RegionMapFiles[i]);
							File.WriteAllText(ConnectionRandomizer.GetMirroredRoomsFile(region, slugcat), RegionMirrorFiles[i]);
							ConnectionRandomizer.LogSomething("Downloaded/read files for " + region + slugcat);
						}
						catch (Exception ex) { ConnectionRandomizer.LogSomething(ex); }
					}

					if (rando.RandomizedRegions.Contains(rando.CurrentlyRandomizing))
					{
						//stop randomizer (partially done above) and load the newly written files

						//read and apply files
						if (rando.CurrentWorldLoader == null)
						{
							ConnectionRandomizer.LogSomething("Failed to sync randomizer files due to null WorldLoader!");
							return;
						}
						rando.ReadRandomizerFiles(rando.CurrentWorldLoader);

						ConnectionRandomizer.LogSomething("Successfully applied randomizer files for " + rando.CurrentlyRandomizing);

						rando.CurrentlyRandomizing = "";
						rando.CurrentWorldLoader.creating_abstract_rooms_finished = true;
						rando.CurrentWorldLoader = null; //done
					}
				}
				catch (Exception ex)
				{
					ConnectionRandomizer.LogSomething(ex);
				}
			}
		}
	}
}