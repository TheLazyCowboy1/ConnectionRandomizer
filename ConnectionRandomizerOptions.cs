using BepInEx.Logging;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ConnectionRandomizer;

public class ConnectionRandomizerOptions : OptionInterface
{
    private readonly ManualLogSource Logger;
    private ConnectionRandomizer ModInstance;

    public ConnectionRandomizerOptions(ConnectionRandomizer modInstance, ManualLogSource loggerSource)
    {
        ModInstance = modInstance;
        Logger = loggerSource;

        randomizeShelters = this.config.Bind<bool>("randomizeShelters", false);
        randomizeGates = this.config.Bind<bool>("randomizeGates", false);

        placementRandomness = this.config.Bind<float>("placementRandomness", 0.3f, new ConfigAcceptableRange<float>(0f, 2f));
        randomizationAmount = this.config.Bind<float>("randomizationAmount", 0.9f, new ConfigAcceptableRange<float>(0f, 1f));

        mirroredRooms = this.config.Bind<float>("mirroredRooms", 0.3f, new ConfigAcceptableRange<float>(0f, 1f));


        logicDistanceModifier = this.config.Bind<float>("logicDistanceModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicOrigDistModifier = this.config.Bind<float>("logicOrigDistModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicConnDistModifier = this.config.Bind<float>("logicConnDistModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicQuadConnDistModifier = this.config.Bind<float>("logicQuadConnDistModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicPureAngleModifier = this.config.Bind<float>("logicPureAngleModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicPlacementAngleModifier = this.config.Bind<float>("logicPlacementAngleModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicBonusAngleModifier = this.config.Bind<float>("logicBonusAngleModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicSameConnPenaltyModifier = this.config.Bind<float>("logicSameConnPenaltyModifier", 1f, new ConfigAcceptableRange<float>(0f, 20f));
        logicScorePotentialModifier = this.config.Bind<float>("logicScorePotentialModifier", 1.0f, new ConfigAcceptableRange<float>(0f, 20f));
        logicNextPotentialModifier = this.config.Bind<float>("logicNextPotentialModifier", 0.2f, new ConfigAcceptableRange<float>(0f, 2f));
        logicPotentialCapModifier = this.config.Bind<int>("logicPotentialCapModifier", 40, new ConfigAcceptableRange<int>(1, 100));
        logicPotentialConnDistLimit = this.config.Bind<float>("logicPotentialConnDistLimit", 6f, new ConfigAcceptableRange<float>(0.5f, 20f));

    }

    private UIelement[] UIArr;
    private UIelement[] RegArr = new UIelement[0];
    private UIelement[] AdvArr;

    public Configurable<bool> randomizeShelters;
    public Configurable<bool> randomizeGates;

    public Configurable<float> placementRandomness;
    public Configurable<float> randomizationAmount;

    public Configurable<float> mirroredRooms;

    //regions
    public Dictionary<string, Configurable<bool>> blacklistedRegions = new(); //unused
    public Dictionary<string, Configurable<bool>> ignoredRegions = new();

    //advanced options
    public Configurable<float> logicDistanceModifier;
    public Configurable<float> logicOrigDistModifier;
    public Configurable<float> logicConnDistModifier;
    public Configurable<float> logicQuadConnDistModifier;
    public Configurable<float> logicPureAngleModifier;
    public Configurable<float> logicPlacementAngleModifier;
    public Configurable<float> logicBonusAngleModifier;
    public Configurable<float> logicSameConnPenaltyModifier;
    public Configurable<float> logicScorePotentialModifier;
    public Configurable<float> logicNextPotentialModifier;
    public Configurable<int> logicPotentialCapModifier;
    public Configurable<float> logicPotentialConnDistLimit;


    public override void Initialize()
    {

        var opTab = new OpTab(this, "Options");
        //var modsTab = new OpTab(this, "Mod List");
        var regionsTab = new OpTab(this, "Regions");
        var advancedTab = new OpTab(this, "Advanced");
        this.Tabs = new[]
        {
            opTab,
            regionsTab,
            //modsTab
            advancedTab
        };

        OpHoldButton ClearDataButton = new OpHoldButton(new Vector2(100f, 20f), new Vector2(200f, 50f), "Clear Randomizer Data") { description = "Clears ALL files made by this randomizer.\nThe game may need to be restarted for the changes to fully take effect." };
        ClearDataButton.OnPressDone += (UIfocusable focus) => { ModInstance.ClearAllRandomizerFiles(); };

        float g = -30f; //g = "gap" = vertical spacing
        float h = 550f, s = 100f, w = 80f, l = 10f; //h = current height, s = horizontal spacing, w = width of configs, l = left margin

        UIArr = new UIelement[]
        {
            new OpLabel(l, h, "Randomizer Options", true),
            new OpLabel(s, h += g, "Randomize Shelters"), new OpCheckBox(randomizeShelters, new Vector2(l, h)) { description = "Randomizes the locations of shelters, just like other types of rooms.\nIt is recommended to keep this option disabled to ensure even shelter placement and some familiarity with the region." },
            new OpLabel(s, h += g, "Randomize Karma Gates"), new OpCheckBox(randomizeGates, new Vector2(l, h)) { description = "Randomizes the locations of karma gates, just like other types of rooms.\nIt is HIGHLY recommended to keep this option DISABLED to keep progress through regions somewhat meaningful." },
            new OpLabel(s, h += g+g, "Placement Randomness"), new OpUpdown(placementRandomness, new Vector2(l, h), w, 2) { description = "How much the randomizer should prioritize \"randomness\" over sensible connections.\n(0 will result in nearly identical (but hopefully sensible) room placements each time; 0.5 compromises; 1 is very random)" },
            new OpLabel(s, h += g, "Randomization Amount"), new OpUpdown(randomizationAmount, new Vector2(l, h), w, 2) { description = "The fraction of rooms that will be repositioned by the randomizer.\n(0 = no randomization, 0.5 = half random, 1 = fully random)" },
            new OpLabel(s, h += g+g, "Mirrored Rooms"), new OpUpdown(mirroredRooms, new Vector2(l, h), w, 2) { description = "The fraction of rooms that will be mirrored horizontally. This can make regions feel more unique,\nbut mirrored rooms come with a few visual oddities." },
            ClearDataButton
        };
        opTab.AddItems(UIArr);

        //mods tab
        /*
        try
        {
            OpScrollBox box = new OpScrollBox(modsTab, 3000f);
            box.AddItems(new OpLabelLong(new Vector2(10f, 0f), new Vector2(500f, 3000f), File.ReadAllText(AssetManager.ResolveFilePath("RoomConnectionRandomizer" + Path.DirectorySeparatorChar + "RoomConnectionRandomizerModList.txt"))));
        } catch (Exception ex) { }
        */
        //regions tab
        try
        {
            string[] regions = File.ReadAllLines(AssetManager.ResolveFilePath("World" + Path.DirectorySeparatorChar + "Regions.txt"));
            RegArr = new UIelement[regions.Length * 2 + 3];//* 3 + 5];
            RegArr[0] = new OpLabel(-5f, 580f, "Region Options", true);
            //RegArr[1] = new OpLabel(150f, 580f, "Forbid", false);
            RegArr[1] = new OpLabel(200f, 580f, "Ignore", false);
            //RegArr[3] = new OpLabel(400f, 580f, "Forbid", false);
            RegArr[2] = new OpLabel(450f, 580f, "Ignore", false);

            g = -25f; //gap/y-space between regions

            for (int i = 0; i < regions.Length; i++) {
                string r = regions[i];
                string displayString = r;
                try
                {
                    displayString += " - " + File.ReadAllText(AssetManager.ResolveFilePath("World" + Path.DirectorySeparatorChar + r + Path.DirectorySeparatorChar + "displayname.txt"));
                } catch (Exception ex) { }
                int idx = i * 2 + 3;//* 3 + 5;
                float y = 550f + g * ((i >= 23) ? i-23 : i);
                float dx = (i >= 23) ? 250 : 0;
                RegArr[idx] = new OpLabel(0f+dx, y, displayString);

                try
                {
                    //blacklistedRegions.Add(r, this.config.Bind<bool>("blacklistRegion" + r, false));
                    //RegArr[idx + 1] = new OpCheckBox(blacklistedRegions[r], 150f+dx, y) { description = "Check this if you want the region to never be entered. If you don't like a region, check this box." };
                } catch (Exception ex) { }
                try
                {
                    //ignoredRegions.Add(r, this.config.Bind<bool>("ignoreRegion" + r, false));
                    RegArr[idx + 1] = new OpCheckBox(ignoredRegions[r], 200f+dx, y) { description = "Check this if you want the randomizer to ignore this region. Its layout will remain unchanged." };
                }
                catch (Exception ex) { }
            }
        } catch (Exception ex) { Logger.LogError(ex); }
        regionsTab.AddItems(RegArr);



        //advanced tab
        g = -30f; //g = "gap" = vertical spacing
        h = 550f; s = 100f; w = 80f; l = 10f; //h = current height, s = horizontal spacing, w = width of configs, l = left margin

        AdvArr = new UIelement[]
        {
            new OpLabel(l, h, "Advanced Options", true), new OpLabel(s+s, h, "Modify the relative parameters of the randomizer algorithm."),
            new OpLabel(s, h += g+g, "Closeness Penalty"), new OpUpdown(logicDistanceModifier, new Vector2(l, h), w) { description = "Multiplies how severely the randomizer penalizes rooms being placed very close together.\n(Used to prevent rooms from bunching up or severely overlapping.)" },
            new OpLabel(s, h += g, "Grouping Strength"), new OpUpdown(logicOrigDistModifier, new Vector2(l, h), w) { description = "Multiplies how strongly the randomizer attempts to keep groups of rooms together or apart.\n(For example, this encourages rooms of one subregion to be near other rooms of the same subregion.)" },
            new OpLabel(s, h += g+g, "Connection Accuracy Reward"), new OpUpdown(logicConnDistModifier, new Vector2(l, h), w) { description = "Multiplies how strongly the randomizer attempts to make connections close to their original length." },
            new OpLabel(s, h += g, "Long Connection Penalty"), new OpUpdown(logicQuadConnDistModifier, new Vector2(l, h), w) { description = "Multiplies how severely the randomizer penalizes making very long connections (e.g: connections from one side of the region to the other)." },
            new OpLabel(s, h += g+g, "Opposing Connections Reward"), new OpUpdown(logicPureAngleModifier, new Vector2(l, h), w) { description = "Multiplies how strongly the randomizer attempts to make connections between opposite directions.\n(Used to encourage left pipes to connect to right pipes, etc.)" },
            new OpLabel(s, h += g, "Accurate Relative Angle Reward"), new OpUpdown(logicPlacementAngleModifier, new Vector2(l, h), w) { description = "Multiplies how strongly the randomizer attempts to ensure that a connection leading left will actually move the player to a room that is to the left, etc.\n(Used to make movement through regions fun: If you know something is up and right, going up and right brings you near there.)" },
            new OpLabel(s, h += g, "Placement Angle Reward"), new OpUpdown(logicBonusAngleModifier, new Vector2(l, h), w) { description = "Multiplies the bonus reward for placing an room at the correct angle.\n(As far as I know, this isn't too significant. It's intended purpose was to further space out rooms.)" },
            new OpLabel(s, h += g+g, "Room Chain Penalty"), new OpUpdown(logicSameConnPenaltyModifier, new Vector2(l, h), w) { description = "Multiplies how severely the randomizer penalizes placing a room with 2 connections next to another with 2 connections.\n(This prevents the randomizer from creating long chains of rooms that loop around the region.)" },
            new OpLabel(s, h += g+g, "Score Potential Modifier"), new OpUpdown(logicScorePotentialModifier, new Vector2(l, h), w, 2) { description = "Multiplies how strongly the randomizer should consider alternate potential connections.\n0 = ignore potential connections; 1 = fully consider potentials." },
            new OpLabel(s, h += g, "Potential Connection Chance"), new OpUpdown(logicNextPotentialModifier, new Vector2(l, h), w, 2) { description = "The assumed chance of the best connection NOT being taken.\nA high value should make the randomizer try to line up many potential connections, which may cause clumping." },
            new OpLabel(s, h += g, "Potential Connection Cap"), new OpUpdown(logicPotentialCapModifier, new Vector2(l, h), w) { description = "The maximum number of potential connections for each node. This cap only exists as a slight performance boost.\nHowever, setting this too low may cause increased failures by the randomizer." },
            new OpLabel(s, h += g, "Max Potential Connection Distance"), new OpUpdown(logicPotentialConnDistLimit, new Vector2(l, h), w) { description = "The maximum distance tested for potential connections." },
        };
        advancedTab.AddItems(AdvArr);

    }

    public void BindRegionsTab()
    {
        //regions tab
        try
        {
            string[] regions = File.ReadAllLines(AssetManager.ResolveFilePath("World" + Path.DirectorySeparatorChar + "Regions.txt"));
            for (int i = 0; i < regions.Length; i++)
            {
                string r = regions[i];
                try
                {
                    blacklistedRegions.Add(r, this.config.Bind<bool>("blacklistRegion" + r, false));
                }
                catch (Exception ex) { }
                try
                {
                    ignoredRegions.Add(r, this.config.Bind<bool>("ignoreRegion" + r, false));
                }
                catch (Exception ex) { }
            }
        }
        catch (Exception ex) { Logger.LogError(ex); }

    }

}