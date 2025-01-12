using RainMeadow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConnectionRandomizer;

/**
 * Orignal file by Choc for RegionRandomizer
 * Repurposed for ConnectionRandomizer
 */
public partial class ConnectionRandomizer
{
	public static bool meadowEnabled = false;
	public static bool IsOnline => meadowEnabled && OnlineManager.lobby != null;
	public static bool IsHost => OnlineManager.lobby.isOwner;

	public static RandomizerData onlineData = new();

	public static void AddOnlineData()
	{
		if (!IsOnline) return;
		OnlineManager.lobby.AddData(onlineData);
	}

	public class RandomizerData : OnlineResource.ResourceData
	{
		public RandomizerData() { }

		public override ResourceDataState MakeState(OnlineResource resource)
		{
			return new State(this);
		}

		private class State : ResourceDataState
		{
			public override Type GetDataType() => GetType();

			//[OnlineField(nullable = true)]
			//Dictionary<string, string> CustomGateLocks;
			[OnlineField]
			string[] RandomizedRegions;
			[OnlineField]
			long[] RegionGenerationTimes; //times of when each region was randomized. If time different, download data
			[OnlineField]
			string[] RegionConnectionFiles;
			[OnlineField]
			string[] RegionMirrorFiles;
			[OnlineField]
			string[] RegionMapFiles;

			public State() { }
			public State(RandomizerData data)
			{
				RandomizedRegions = ConnectionRandomizer.Instance.RandomizedRegions.ToArray();

				//determine which regions to update
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
                    } catch (Exception ex) { ConnectionRandomizer.LogSomething(ex); }
				}

            }

			public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
			{
				//ConnectionRandomizer.CustomGateLocks = CustomGateLocksKeys.Zip(CustomGateLocksValues, (k, v) => (k, v)).ToDictionary(x => x.k, x => x.v);
				ConnectionRandomizer rando = ConnectionRandomizer.Instance;
				
				rando.RandomizedRegions = RandomizedRegions.ToList();

                //immediately CANCEL any randomization of regions already randomized
				if (rando.CurrentlyRandomizing != "" && rando.RandomizedRegions.Contains(rando.CurrentlyRandomizing))
				{
					if (rando.RandomizerTask != null)
						rando.RandomizerTask.Dispose();
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
                    } catch (Exception ex) { ConnectionRandomizer.LogSomething(ex); }
				}

				if (rando.RandomizedRegions.Contains(rando.CurrentlyRandomizing))
                    rando.CurrentlyRandomizing = "";

				//read and apply files
                if (rando.CurrentWorldLoader == null)
                {
                    ConnectionRandomizer.LogSomething("Failed to sync randomizer files due to null WorldLoader!");
                    return;
                }
                rando.ReadRandomizerFiles(rando.CurrentWorldLoader);
				
				rando.CurrentWorldLoader = null; //done

            }
		}
	}
}
