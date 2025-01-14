using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using System.IO;
using System.Text.RegularExpressions;
using static ConnectionRandomizer.LogicalRando;
using Vector2 = UnityEngine.Vector2;
using IntVector2 = RWCustom.IntVector2;
using MapObject = DevInterface.MapObject;
using Custom = RWCustom.Custom;
using System.Threading.Tasks;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(System.Security.Permissions.SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ConnectionRandomizer;

[BepInDependency("henpemaz.rainmeadow", BepInDependency.DependencyFlags.SoftDependency)]

[BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
public partial class ConnectionRandomizer : BaseUnityPlugin
{
    public const string MOD_ID = "LazyCowboy.RoomConnectionRandomizer";
    public const string MOD_NAME = "Room Connection Randomizer";
    public const string MOD_VERSION = "0.0.1";
    /*
     * TODO Notes:
     * 
     * DONE Add config for original_distance_modifier
     * DONE Add config for everything else, cuz why not??
     * 
     * DONE Rewrite scoreDiffWhenConnectionTaken or whatever
     * 
     * Improve randomization speeds for the first part (connecting notConnected)
     * 
     * Attempt to display a message when first loading a campaign
     * 
     * DONE Experiment with mirroring some rooms.
     *  DONE Option 1: a. mirror map image (easy), b. mirror map connections (easy), c. mirror room camera, d. mirror input x direction
     *  NOPE, WAY TOO HARD Option 2: a. same, b. same, c. mirror room geometry, d. mirror room image
     * 
     * PARTIAL add room logic that ensures that the player cannot get softlocked in any room
     * DONE build a map image when starting a region not through a karma gate
     * DONE figure out which rooms to randomize AND blacklist rooms
     * DONE clear this data when starting a new randomizer campaign
     * add special compatibility with Region Randomizer (how important is this really??)
    */

    #region Setup

    public static ConnectionRandomizer Instance;

    public static ConnectionRandomizerOptions Options;

    public ConnectionRandomizer()
    {
        try
        {
            Instance = this;
            Options = new ConnectionRandomizerOptions(this, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }

    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;

        //TestConnectibles();

    }

    private void OnDisable()
    {
        if (IsInit)
        {
            On.PlayerProgression.WipeSaveState -= PlayerProgression_WipeSaveState;
            //On.PlayerProgression.IsThereASavedGame -= PlayerProgression_IsThereASavedGame;
            On.Menu.SlugcatSelectMenu.StartGame -= SlugcatSelectMenu_StartGame;

            //On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues -= WorldLoader_ctor;
            //On.OverWorld.WorldLoaded -= OverWorld_WorldLoaded;
            //On.HUD.Map.MapData.PositionOfRoom -= Map_RoomToMapPos;
            On.WorldLoader.CreatingAbstractRoomsThread -= WorldLoader_CreatingAbstractRoomsThread;
            //On.World.LoadMapConfig -= World_LoadMapConfig;
            On.WorldLoader.NextActivity -= WorldLoader_NextActivity;
            //On.WorldLoader.UpdateThread -= WorldLoader_UpdateThread;
            //On.WorldLoader.CreatingWorld -= WorldLoader_CreatingWorld;
            On.SaveState.ctor -= SaveState_ctor;

            On.HUD.Map.MapData.ctor -= MapData_ctor;
            //On.HUD.Map.LoadConnectionPositions -= Map_LoadConnectionPositions;
            On.OverWorld.WorldLoaded -= OverWorld_WorldLoaded;
            //On.WorldLoader.ReturnWorld -= WorldLoader_ReturnWorld;
            On.RainWorldGame.ctor -= RainWorldGame_ctor;
            //On.HUD.Map.Update -= Map_Update;

            //On.ShortcutHandler.SpitOutCreature -= ShortcutHandler_SpitOutCreature;

            //On.RainWorldGame.Update -= RainWorldGame_Update;

            //room mirroring
            On.RWInput.PlayerInputLogic_int_int -= RWInput_PlayerInputLogic;
            On.HUD.Map.RoomToMapPos -= Map_RoomToMapPos;
            On.HUD.Map.OnTexturePos -= Map_OnTexturePos;

            On.RoomCamera.ctor -= RoomCameraExtension.RoomCameraCtorHook;
            On.RoomCamera.DrawUpdate -= RoomCameraExtension.RoomCameraDrawUpdateHook;

            On.RoomCamera.ChangeRoom -= RoomCamera_ChangeRoom;
        }
    }


    private bool IsInit;
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        try
        {
            if (IsInit) return;

            //Your hooks go here
            On.PlayerProgression.WipeSaveState += PlayerProgression_WipeSaveState;
            //On.PlayerProgression.IsThereASavedGame += PlayerProgression_IsThereASavedGame;
            On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;

            //On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += WorldLoader_ctor;
            //On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
            //On.HUD.Map.MapData.PositionOfRoom += Map_RoomToMapPos;
            On.WorldLoader.CreatingAbstractRoomsThread += WorldLoader_CreatingAbstractRoomsThread;
            //On.World.LoadMapConfig += World_LoadMapConfig;
            On.WorldLoader.NextActivity += WorldLoader_NextActivity;
            //On.WorldLoader.UpdateThread += WorldLoader_UpdateThread;
            On.SaveState.ctor += SaveState_ctor;

            //map patcher
            On.HUD.Map.MapData.ctor += MapData_ctor;
            //On.HUD.Map.LoadConnectionPositions += Map_LoadConnectionPositions;
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;
            //On.WorldLoader.CreatingWorld += WorldLoader_CreatingWorld;
            //On.WorldLoader.ReturnWorld += WorldLoader_ReturnWorld;
            On.RainWorldGame.ctor += RainWorldGame_ctor;
            //On.HUD.Map.Update += Map_Update;

            //On.RainWorldGame.Update += RainWorldGame_Update;


            //room mirroring
            On.RWInput.PlayerInputLogic_int_int += RWInput_PlayerInputLogic;
            On.HUD.Map.RoomToMapPos += Map_RoomToMapPos;
            On.HUD.Map.OnTexturePos += Map_OnTexturePos;

            On.RoomCamera.ctor += RoomCameraExtension.RoomCameraCtorHook;
            On.RoomCamera.DrawUpdate += RoomCameraExtension.RoomCameraDrawUpdateHook;

            On.RoomCamera.ChangeRoom += RoomCamera_ChangeRoom;

            //On.ShortcutHandler.SpitOutCreature += ShortcutHandler_SpitOutCreature;


            //mirror room shader
            try
            {
                UnityEngine.AssetBundle assetBundle = UnityEngine.AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("AssetBundles\\LazyCowboy\\ConnectionRandomizer.assets")); //"C:\Program Files (x86)\Steam\steamapps\common\Rain World\RainWorld_Data\StreamingAssets\mods\Room Connection Randomizer\AssetBundles\LazyCowboy\ConnectionRandomizer.assets"
                UnityEngine.Shader mirrorShader = assetBundle.LoadAsset<UnityEngine.Shader>("MirrorRoomEffect.shader");
                if (mirrorShader == null)
                    Logger.LogDebug("Shader is null");
                self.Shaders.Add("LazyCowboy_MirrorRoomPP", FShader.CreateShader("LazyCowboy_MirrorRoomPP", mirrorShader));
                /*
                UnityEngine.Shader mirrorShader2 = assetBundle.LoadAsset<UnityEngine.Shader>("MirrorRoomEffect2.shader");
                if (mirrorShader2 == null)
                    Logger.LogDebug("Shader2 is null");
                self.Shaders.Add("LazyCowboy_MirrorRoomPP2", FShader.CreateShader("LazyCowboy_MirrorRoomPP2", mirrorShader2));
                */
            } catch (Exception ex)
            {
                Logger.LogError(ex);
            }


            //detect is Rain Meadow is enabled
            foreach (ModManager.Mod mod in ModManager.ActiveMods)
            {
                if (mod.id == "henpemaz.rainmeadow")
                {
                    meadowEnabled = true;
                    break;
                }
            }


            MachineConnector.SetRegisteredOI(MOD_ID, Options);
            IsInit = true;

            Options.BindRegionsTab();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            throw;
        }
    }


    #endregion

    #region Hooks
    //clears randomizer files when starting a new campaign
    private void PlayerProgression_WipeSaveState(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber)
    {
        orig(self, saveStateNumber);

        ClearAllRandomizerFiles(saveStateNumber.value);
    }
    private void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, Menu.SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
    {
        orig(self, storyGameCharacter);

        if (!self.manager.rainWorld.progression.IsThereASavedGame(storyGameCharacter))
            ClearAllRandomizerFiles(storyGameCharacter.value);
    }
    
    public string CurrentlyRandomizing = "";
    public Task RandomizerTask = null;
    public WorldLoader CurrentWorldLoader = null;
    private void WorldLoader_CreatingAbstractRoomsThread(On.WorldLoader.orig_CreatingAbstractRoomsThread orig, WorldLoader self)
    {
        CurrentSlugcat = self.playerCharacter.value;

        orig(self);

        try
        {
            if (!IsOnline && IgnoredRegion(self.worldName)) //disable ignored regions for Rain Meadow, just to simplify
                return; //welp, nothing to do here

            self.creating_abstract_rooms_finished = false; //NOPE! I've still got stuff to do! I gotta reorder these!

            //initiate randomization
            //but wait, now I have all the abstract rooms I need!
            CurrentlyRandomizing = self.worldName;
            CurrentWorldLoader = self;
            RandomizerTask = new Task(() =>
            {
                if (IsOnline && !IsHost)
                    Task.Delay(1000); //wait 1 second for host to go first

                if (NeedToRandomizeRegion(self))
                {
                    if (self.game.overWorld != null && self.game.overWorld.reportBackToGate != null)
                        self.game.overWorld.reportBackToGate.room.NewMessageInRoom("Randomizing region " + self.worldName + "...", 40);
                    RandomizeRegion(self);
                }
                else
                    ReadRandomizerFiles(self);
                //ReadOrCreateRandomizer(self);

                if (IsOnline)
                    Task.Delay(1000); //adds an extra delay to be extra careful about syncing everything up!

                CurrentlyRandomizing = "";
                self.creating_abstract_rooms_finished = true; //okay, you can move on now
                CurrentWorldLoader = null;
            });
            RandomizerTask.Start();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            self.creating_abstract_rooms_finished = true;
            CurrentlyRandomizing = "";
        }
    }


    //create map image
    //for karma gates
    private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
    {
        //display finished message
        if (LastRandomizedRegion == self.worldLoader.worldName)
                self.reportBackToGate?.room?.NewMessageInRoom("Finished randomizing " + LastRandomizedRegion, 0);

        orig(self);

        //MapResetWorld = LastRandomizedRegion;
        LastRandomizedRegion = "";

        //CheckIfNewMapNeeded(self.activeWorld);
    }
    
    //for game startup
    //RainWorldGame_ctor is ultimately what makes the call to load the first world
    //this should happen BEFORE the map is initialized...?
    private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
    {
        orig(self, manager);

        LastRandomizedRegion = "";

        //CheckIfNewMapNeeded(self.world);
    }

    //ACTUAL map patcher
    private void MapData_ctor(On.HUD.Map.MapData.orig_ctor orig, HUD.Map.MapData self, World initWorld, RainWorld rainWorld)
    {
        orig(self, initWorld, rainWorld);

        CheckIfNewMapNeeded(initWorld);
    }
    private void CheckIfNewMapNeeded(World world)
    {
        try
        {
            string slugcat = world.game.StoryCharacter.value;
            if (File.Exists(GetRandomizerMapFile(world.name, slugcat)) && !File.Exists(GetRandomizerMapPngFile(world.name, slugcat)))
            {
                UpdateRegionMap(world);
            }
            else
                Logger.LogDebug("No need to re-generate map for " + world.name);
        } catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    //the idea is to absolutely force the world loader not to move on until I'm finished
    private void WorldLoader_NextActivity(On.WorldLoader.orig_NextActivity orig, WorldLoader self)
    {
        if (CurrentlyRandomizing == "")
            orig(self);
        //else
            //Logger.LogDebug("Halted World Loader");
    }


    //mirrors the player's position in the room
    //private Vector2 Map_RoomToMapPos(On.HUD.Map.MapData.orig_PositionOfRoom orig, HUD.Map.MapData self, int room)
    private Vector2 Map_RoomToMapPos(On.HUD.Map.orig_RoomToMapPos orig, HUD.Map self, Vector2 pos, int room, float timeStacker)
    {
        /* //old code for completely different stuff
        if (RoomMapPositions.TryGetValue(self.NameOfRoom(room), out Vector2 customPos))
            return customPos;
        //else
        return orig(self, room);
        */

        //mirror the player's position within the room if the room is mirrored
        if (MirroredRooms.Contains(self.mapData.NameOfRoom(room)))
        {
            pos.x = (self.mapData.SizeOfRoom(room).x - 1) * 20f - pos.x;
        }

        return orig(self, pos, room, timeStacker);
    }


    //room mirroring
    //mirror inputs
    //currently mirrors player inputs in mirrored rooms
    //UNLESS the map button is down
    private Player.InputPackage RWInput_PlayerInputLogic(On.RWInput.orig_PlayerInputLogic_int_int orig, int categoryID, int playerNumber)
    {
        Player.InputPackage input = orig(categoryID, playerNumber);

        try
        {
            if (!input.mp
                && Custom.rainWorld.IsPlayerActive(playerNumber)
                && Custom.rainWorld.processManager.currentMainLoop is RainWorldGame
                && MirroredRooms.Contains((Custom.rainWorld.processManager.currentMainLoop as RainWorldGame).Players[playerNumber].Room.name))
            {
                input.x = -input.x;
                input.analogueDir.x = -input.analogueDir.x;
            }
            return input;
        }
        catch (Exception ex)
        {
            return input;
        }
    }

    //attempts to patch map discovery textures looking all weird
    private Vector2 Map_OnTexturePos(On.HUD.Map.orig_OnTexturePos orig, HUD.Map self, Vector2 pos, int room, bool accountForLayer)
    {
        if (MirroredRooms.Contains(self.mapData.NameOfRoom(room)))
        {
            pos.x = (self.mapData.SizeOfRoom(room).x - 1) * 20f - pos.x;
        }

        return orig(self, pos, room, accountForLayer);
    }

    //mirrors room image
    //private bool mirrorEffectActive = false;
    private Dictionary<RoomCamera, bool> mirrorEffectActive = new();
    private void RoomCamera_ChangeRoom(On.RoomCamera.orig_ChangeRoom orig, RoomCamera self, Room newRoom, int cameraPosition)
    {
        orig(self, newRoom, cameraPosition);

        if (!mirrorEffectActive.ContainsKey(self))
            mirrorEffectActive.Add(self, false);

        if (!mirrorEffectActive[self] && MirroredRooms.Contains(newRoom.abstractRoom.name))
        {
            //enable mirror effect
            RoomCameraExtension.AddPPEffect(self, new MirrorRoomEffect());
            mirrorEffectActive[self] = true;
        }
        else if (mirrorEffectActive[self] && !MirroredRooms.Contains(newRoom.abstractRoom.name))
        {
            //disable mirror effect
            RoomCameraExtension.RemovePPEffect(self, typeof(MirrorRoomEffect));
            mirrorEffectActive[self] = false;
        }
    }


    //updates CurrentSlugcat. That's it
    private void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
    {
        CurrentSlugcat = saveStateNumber.value;

        orig(self, saveStateNumber, progression);
    }

    #endregion

    #region File_Readers
    public string CurrentSlugcat = "nullSlugcat"; //currently set at SaveState_ctor and CreatingAbstractRoomsThread. Hopefully that's soon enough...?
    public static string GetRandomizerMapFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "map_",
            region,
            "-",
            slugcat,
            ".txt"
        }));
    }
    public static string GetRandomizerConnectionsFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "room_connections_",
            region,
            "-",
            slugcat,
            ".txt"
        }));
    }
    /*public static string GetRandomizerPositionsFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "room_positions_",
            region,
            "-",
            slugcat,
            ".txt"
        }));
    }*/
    public static string GetMirroredRoomsFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "mirrored_rooms_",
            region,
            "-",
            slugcat,
            ".txt"
        }));
    }
    private static string GetRandomizerMapPngFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "map_",
            region,
            "-",
            slugcat,
            ".png"
        }));
    }
    private static string GetRandomizerMapImageFile(string region, string slugcat)
    {
        return AssetManager.ResolveFilePath(string.Concat(new string[] {
            "RoomConnectionRandomizer",
            Path.DirectorySeparatorChar.ToString(),
            "map_image_",
            region,
            "-",
            slugcat,
            ".txt"
        }));
    }

    //mostly copied from RegionRandomizer.GetGateData
    private List<string> GetRoomData(string region, string slugcat)
    {
        List<string> rooms = new();

        string filePath = AssetManager.ResolveFilePath(string.Concat(new string[]
        {
                "World",
                Path.DirectorySeparatorChar.ToString(),
                region,
                Path.DirectorySeparatorChar.ToString(),
                "world_",
                region,
                ".txt"
        }));

        try
        {
            string[] fileLines = File.ReadAllLines(filePath);
            bool roomsStart = false;
            bool roomsDone = false;
            bool conditionalsStart = false;
            bool conditionalsDone = false;
            List<string> blacklist = new();
            Dictionary<string, string> replaceList = new Dictionary<string, string>();
            foreach (string l in fileLines)
            {
                string line = l;
                if (line.Length < 4) //if it can't contain any rooms
                {
                    continue;
                }
                if (roomsStart)
                {
                    if (line[0] == '{') //ignored anything in {}
                    {
                        if (line[1] == '!')
                        {
                            continue;
                        }
                        int nameStart = 1;
                        for (; line[nameStart] != '}' && nameStart < line.Length - 1; nameStart++) { }
                        if (nameStart >= line.Length - 1)
                        {
                            Logger.LogDebug("Broken syntax for " + line);
                            continue;
                        }
                        line = line.Substring(nameStart + 1).Trim();
                    }

                    if (line.StartsWith("END"))
                    {
                        roomsDone = true;
                        roomsStart = false;
                        if (conditionalsDone)
                            break;
                    }
                    else if (line.Contains(':'))
                    {
                        string[] spl = Regex.Split(line, " : ");
                        if (rooms.Contains(spl[0])) //don't re-add rooms
                            continue;
                        rooms.Add(line);
                        /*
                        rooms.Add(spl[0]);
                        if (spl.Length > 2)
                        {
                            if (line.EndsWith(" : GATE")) //add gate room
                                GateRooms.Add(spl[0]);
                            else if (line.EndsWith(" : SHELTER")) //add shelter room
                                ShelterRooms.Add(spl[0]);
                        }
                        */
                    }
                }
                else if (conditionalsStart)
                {
                    if (line.StartsWith("END"))
                    {
                        conditionalsDone = true;
                        conditionalsStart = false;
                        if (roomsDone)
                            break;
                        continue;
                    }
                    string[] d = line.Split(':');
                    if (d.Length < 3)
                        continue;
                    d[0] = d[0].Trim();
                    d[1] = d[1].Trim();
                    bool nameFound = false;
                    foreach (string slug in Regex.Split(d[0], ","))
                    {
                        if (slug == slugcat)
                        {
                            nameFound = true;
                            if (d[1] == "HIDEROOM")
                                blacklist.Add(d[2].Trim());
                            else if (d[1] == "REPLACEROOM")
                                replaceList.Add(d[2].Trim(), d[3].Trim());
                        }
                    }
                    if (!nameFound && d[1] == "EXCLUSIVEROOM")
                        blacklist.Add(d[2].Trim());
                }
                else if (line.StartsWith("ROOMS"))
                    roomsStart = true;
                else if (line.StartsWith("CONDITIONAL"))
                    conditionalsStart = true;
                //std::getline(inFS, line);
            }

            //might be important to use renamed room transitions... however, I'm not sure these rooms are truly being "renamed"
            //reference docs: https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/File_Formats#world_xx.txt
            //rename replaced gates
            /*
            for (int k = rooms.Count - 1; k >= 0; k--)
            {
                string[] splitgate = Regex.Split(rooms.ElementAt(k), " : ");
                if (replaceList.ContainsKey(splitgate[0]))
                {
                    //regionGates[k] = replaceList[shortgate];
                    //regionGates[k] = Regex.Replace(regionGates[k], shortgate, replaceList[shortgate]);
                    //regionGates[k] = replaceList[splitgate[0]] + " : " + splitgate[1];
                    //Logger.LogDebug("Replaced " + splitgate[0] + " with " + regionGates[k]);
                    //no longer replacing names... no need to do so
                }
            }
            */
            //remove blacklisted rooms
            for (int k = rooms.Count - 1; k >= 0; k--)
            {
                if (blacklist.Contains(Regex.Split(rooms[k], " : ")[0]))
                {
                    Logger.LogDebug("Blacklisted " + rooms[k]);
                    rooms.RemoveAt(k);
                }
            }

            replaceList.Clear();
            blacklist.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            //throw;
        }

        return rooms;

    }
    private List<string> GetRoomNames(List<string> roomData)
    {
        List<string> rooms = new();
        foreach (string room in roomData)
        {
            rooms.Add(Regex.Split(room, " : ")[0]);
        }
        return rooms;
    }
    private List<string> GetGateRooms(List<string> roomData)
    {
        List<string> rooms = new();
        foreach (string room in roomData)
        {
            if (room.EndsWith(" : GATE"))
                rooms.Add(Regex.Split(room, " : ")[0]);
        }
        return rooms;
    }
    private List<string> GetShelterRooms(List<string> roomData)
    {
        List<string> rooms = new();
        foreach (string room in roomData)
        {
            if (room.EndsWith(" : SHELTER"))
                rooms.Add(Regex.Split(room, " : ")[0]);
        }
        return rooms;
    }
    private Dictionary<string, List<string>> GetRoomConnections(List<string> roomData, List<string> roomNames)
    {
        Dictionary<string, List<string>> connections = new();
        foreach (string room in roomData)
        {
            string[] spl = Regex.Split(room, " : ");
            List<string> conns = Regex.Split(spl[1], ", ").ToList();
            for (int i = conns.Count - 1; i >= 0; i--)
            {
                if (!roomNames.Contains(conns[i])) //if not a value room name, remove
                    conns.RemoveAt(i);
                else if (conns.IndexOf(conns[i]) < i) //if a duplicate connection (the case in one RM room), remove
                    conns.RemoveAt(i);
            }
            connections.Add(spl[0], conns);
        }
        return connections;
    }

    #endregion

    #region File_Writers
    private struct MapConnectionLocation {
        public int roomIdx;
        public int connIdx;
        //public IntVector2 posInRoom;
        public int xPosInRoom;
        public int yPosInRoom;
        public int dir;
    }
    //...for some reason it insists that I make this function private. idk. doesn't matter much
    //ALSO CHANGES EACH ROOM'S LAYER
    private void WriteMapFile(string region, string slugcat, List<Connectible> connectibles, List<AbstractRoom> rooms)
    {
        Logger.LogDebug("Writing map.txt file");
        try
        {
            //step #1: get the map connection data from the current file
            string mapFilePath = LogicalRando.GetRegionMapFile(region, slugcat);

            if (!File.Exists(mapFilePath))
            {
                Logger.LogDebug("Failed to find map file!!!");
                return;
            }

            string[] fileLines = File.ReadAllLines(mapFilePath);

            //Dictionary<string, int> nameToConnIdx = new();
            //for (int i = 0; i < connectibles.Count; i++)
                //nameToConnIdx.Add(connectibles[i].name, i);

            Dictionary<string, string> roomPosDataMiddle = new();
            Dictionary<string, string> roomPosDataLast = new();
            List<MapConnectionLocation> mapConnectionLocations = new();
            foreach (string line in fileLines)
            {
                if (line.Contains('>')) //is a room position line
                {
                    //start the substring at the second ><
                    //so ROOM: CX><CY><DX><DY><L><etc. -> ><DX><DY><L><etc.
                    string roomName = line.Split(':')[0];
                    //cut the line AFTER the second ><
                    roomPosDataMiddle.Add(roomName, line.Substring(line.IndexOf('>', line.IndexOf('>') + 1)));
                    try //try to do so again
                    {
                        //idx of '>' right before L
                        int idx = roomPosDataMiddle[roomName].IndexOf('>', roomPosDataMiddle[roomName].IndexOf('>', 1) + 1);
                        //><DX><DY><L><etc. -> ><etc.
                        roomPosDataLast.Add(roomName, roomPosDataMiddle[roomName].Substring(idx + 3));
                        roomPosDataMiddle[roomName] = roomPosDataMiddle[roomName].Substring(0, idx + 2);
                    } catch (Exception ex)
                    {
                        //failed. Must be no last data or something
                        roomPosDataLast.Add(roomName, "");
                    }
                }
                else if (line.StartsWith("Connection:")) //is a room connection line 
                {
                    string[] connData = Regex.Split(line, ": ")[1].Split(',');
                    if (connData.Length < 8)
                        continue;

                    //find indexes of both rooms (index is relative to rooms)
                    int roomAIdx = -1, roomBIdx = -1;
                    for (int i = 0; i < rooms.Count; i++)
                    {
                        if (rooms[i].name == connData[0])
                            roomAIdx = i;
                        else if (rooms[i].name == connData[1])
                            roomBIdx = i;
                    }
                    if (roomAIdx < 0 || roomBIdx < 0)
                        continue;

                    //find connIdx's for both rooms
                    int connAIdx = Array.IndexOf(rooms[roomAIdx].connections, roomBIdx + firstRoomIdx);
                    int connBIdx = Array.IndexOf(rooms[roomBIdx].connections, roomAIdx + firstRoomIdx);
                    if (connAIdx < 0 || connBIdx < 0)
                        continue;


                    //mirror map connections??
                    int xPosInRoomA = Int32.Parse(connData[2]);
                    int dirA = Int32.Parse(connData[6]);
                    int xPosInRoomB = Int32.Parse(connData[4]);
                    int dirB = Int32.Parse(connData[7]);
                    if (MirroredRooms.Contains(rooms[roomAIdx].name))
                    {
                        //xPosInRoomA = rooms[roomAIdx].size.x - xPosInRoomA - 1;
                        if (dirA == 0)
                            dirA = 2;
                        else if (dirA == 2)
                            dirA = 0;
                    }
                    if (MirroredRooms.Contains(rooms[roomBIdx].name))
                    {
                        //xPosInRoomB = rooms[roomBIdx].size.x - xPosInRoomB - 1;
                        if (dirB == 0)
                            dirB = 2;
                        else if (dirB == 2)
                            dirB = 0;
                    }


                    //make MapConnectionLocations
                    mapConnectionLocations.Add(new MapConnectionLocation { roomIdx = roomAIdx, connIdx = connAIdx, xPosInRoom = xPosInRoomA, yPosInRoom = Int32.Parse(connData[3]), dir = dirA });
                    mapConnectionLocations.Add(new MapConnectionLocation { roomIdx = roomBIdx, connIdx = connBIdx, xPosInRoom = xPosInRoomB, yPosInRoom = Int32.Parse(connData[5]), dir = dirB });

                }
            }

            Logger.LogDebug("mapConnectionLocations: " + mapConnectionLocations.Count);

            //set room layers
            foreach (Connectible c in connectibles)
            {
                //if (!nameToIdx.ContainsKey(c.name))
                    //continue;
                //find closest layer 0, closest layer 1, closest layer 2
                float layer0Dist = float.PositiveInfinity, layer1Dist = float.PositiveInfinity, layer2Dist = float.PositiveInfinity;
                //foreach (string conn in c.connections.Values)
                foreach (Connectible b in connectibles)
                {
                    if (c.name == b.name)
                        continue;
                    //if (nameToConnIdx.TryGetValue(conn, out int ci) && nameToIdx.TryGetValue(conn, out int ri))
                    //if (conn != "" && Int32.TryParse(conn, out int ri) && nameToConnIdx.TryGetValue(conn, out int ci))
                    if (Int32.TryParse(b.name, out int ri))
                    {
                        if (rooms[ri].layer == 0)
                            layer0Dist = Mathf.Min(layer0Dist, (c.position - b.position).magnitude - c.radius - b.radius);
                        else if (rooms[ri].layer == 1)
                            layer1Dist = Mathf.Min(layer1Dist, (c.position - b.position).magnitude - c.radius - b.radius);
                        else
                            layer2Dist = Mathf.Min(layer2Dist, (c.position - b.position).magnitude - c.radius - b.radius);
                    }
                }

                //pick the greatest distance
                if (layer0Dist > layer1Dist) //rule out #1
                {
                    if (layer0Dist > layer2Dist)
                        rooms[Int32.Parse(c.name)].layer = 0;
                    else
                        rooms[Int32.Parse(c.name)].layer = 2;
                }
                else //rule out #0
                {
                    if (layer1Dist > layer2Dist)
                        rooms[Int32.Parse(c.name)].layer = 1;
                    else
                        rooms[Int32.Parse(c.name)].layer = 2;
                }
            }

            //write room position data
            string fileText = "";
            foreach (Connectible c in connectibles)
            {
                AbstractRoom abRoom = rooms[Int32.Parse(c.name)];
                if (roomPosDataMiddle.ContainsKey(abRoom.name))
                {
                    if (roomPosDataLast[abRoom.name] == "")
                        fileText += abRoom.name + ": " + c.position.x + "><" + c.position.y + roomPosDataMiddle[abRoom.name] + "\n";
                    else
                        fileText += abRoom.name + ": " + c.position.x + "><" + c.position.y + roomPosDataMiddle[abRoom.name] + abRoom.layer + roomPosDataLast[abRoom.name] + "\n";
                }
            }

            //write connection data
            foreach (Connectible AConnectible in connectibles)
            {
                int AIdx = Int32.Parse(AConnectible.name);
                AbstractRoom roomA = rooms[AIdx];
                //foreach (int connIdx in abRoom.connections)
                for (int AConnIdx = 0; AConnIdx < roomA.connections.Length; AConnIdx++)
                {
                    if (mapConnectionLocations.Count < 2)
                        break;
                    
                    if (roomA.connections[AConnIdx] < 0)
                        continue;

                    //find this mapConnectionPosition
                    MapConnectionLocation mapPosA = mapConnectionLocations[0];
                    bool mapPosFound = false;
                    foreach (MapConnectionLocation mapLoc in mapConnectionLocations)
                    {
                        if (mapLoc.roomIdx == AIdx && mapLoc.connIdx == AConnIdx)
                        {
                            mapPosA = mapLoc;
                            mapPosFound = true;
                            break;
                        }
                    }
                    if (!mapPosFound)
                        continue;

                    //find the room this connects to AND its mapConnectionLocation
                    //int BIdx = conn - firstRoomIdx;
                    string BIdxStr = AConnectible.connections[AConnIdx.ToString()];
                    if (BIdxStr == "")
                        continue;
                    int BIdx = Int32.Parse(BIdxStr);
                    AbstractRoom roomB = rooms[BIdx];

                    Connectible BConnectible = null;
                    //find BConnectible
                    foreach (Connectible b in connectibles)
                    {
                        if (b.name == BIdxStr)
                        {
                            BConnectible = b;
                            break;
                        }
                    }
                    if (BConnectible == null)
                        continue;

                    //find BconnIdx
                    int BIdxOfA = Array.IndexOf(BConnectible.connections.Values.ToArray(), AConnectible.name);
                    if (BIdxOfA < 0)
                        continue;
                    int BConnIdx = Int32.Parse(BConnectible.connections.Keys.ToArray()[BIdxOfA]);

                    //find B mapConnectionLocation
                    MapConnectionLocation mapPosB = mapConnectionLocations[0];
                    mapPosFound = false;
                    foreach (MapConnectionLocation mapLoc in mapConnectionLocations)
                    {
                        if (mapLoc.roomIdx == BIdx && mapLoc.connIdx == BConnIdx)
                        {
                            mapPosB = mapLoc;
                            mapPosFound = true;
                            break;
                        }
                    }
                    if (!mapPosFound)
                        continue;

                    //finally write the line
                    fileText += "Connection: " + roomA.name + "," + roomB.name + ","
                        + mapPosA.xPosInRoom + "," + mapPosA.yPosInRoom + ","
                        + mapPosB.xPosInRoom + "," + mapPosB.yPosInRoom + ","
                        + mapPosA.dir + "," + mapPosB.dir + "\n";

                    mapConnectionLocations.Remove(mapPosA);
                    mapConnectionLocations.Remove(mapPosB);

                }
            }


            //write the file (finally!!)
            string directoryPath = AssetManager.ResolveDirectory("RoomConnectionRandomizer");
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            File.WriteAllText(GetRandomizerMapFile(region, slugcat), fileText);

            OverwriteMapFile(region, slugcat);

            //make sure the correct directories exist
            /*
            string mergedModsPath = AssetManager.ResolveDirectory("mergedmods");
            string directoryPath = mergedModsPath + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            directoryPath += Path.DirectorySeparatorChar + region;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            //write the file
            string newFilePath = directoryPath + Path.DirectorySeparatorChar + "map_" + region + "-" + slugcat + ".txt";
            File.WriteAllText(newFilePath, fileText);
            */
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }

    }

    public void OverwriteMapFile(string region, string slugcat)
    {
        Logger.LogDebug("Overwriting map.txt file");
        try
        {
            string randomizerFile = GetRandomizerMapFile(region, slugcat);
            if (!File.Exists(randomizerFile))
                return;
            /*
            string mergedModsFile = AssetManager.ResolveFilePath(string.Concat(new string[]
                {
                "mergedmods",
                Path.DirectorySeparatorChar.ToString(),
                "World",
                Path.DirectorySeparatorChar.ToString(),
                region,
                Path.DirectorySeparatorChar.ToString(),
                "map_",
                region,
                "-",
                slugcat,
                ".txt"
                }));
            */
            //make sure the correct directories exist
            string mergedModsPath = AssetManager.ResolveDirectory("mergedmods");
            string directoryPath = mergedModsPath + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            directoryPath += Path.DirectorySeparatorChar + region;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            //write the file
            string newFilePath = directoryPath + Path.DirectorySeparatorChar + "map_" + region + "-" + slugcat + ".txt";
            File.Copy(randomizerFile, newFilePath, true);
        }
        catch (Exception ex) { Logger.LogError(ex); }

    }
    public void OverwriteMapPngFile(string region, string slugcat)
    {
        //overwrite map_XX-SLUG.png
        Logger.LogDebug("Overwriting map.png file");
        try
        {
            string randomizerFile = GetRandomizerMapPngFile(region, slugcat);
            if (!File.Exists(randomizerFile))
                return;
            //make sure the correct directories exist
            string mergedModsPath = AssetManager.ResolveDirectory("mergedmods");
            string directoryPath = mergedModsPath + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            directoryPath += Path.DirectorySeparatorChar + region;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            //write the file
            string newFilePath = directoryPath + Path.DirectorySeparatorChar + "map_" + region + "-" + slugcat + ".png";
            File.Copy(randomizerFile, newFilePath, true);
        }
        catch (Exception ex) { Logger.LogError(ex); }

        //overwrite map_image_XX-SLUG.txt
        Logger.LogDebug("Overwriting map_image.txt file");
        try
        {
            string randomizerFile = GetRandomizerMapImageFile(region, slugcat);
            if (!File.Exists(randomizerFile))
                return;
            //make sure the correct directories exist
            string mergedModsPath = AssetManager.ResolveDirectory("mergedmods");
            string directoryPath = mergedModsPath + Path.DirectorySeparatorChar + "world";
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            directoryPath += Path.DirectorySeparatorChar + region;
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            //write the file
            string newFilePath = directoryPath + Path.DirectorySeparatorChar + "map_image_" + region + "-" + slugcat + ".txt";
            File.Copy(randomizerFile, newFilePath, true);
        }
        catch (Exception ex) { Logger.LogError(ex); }

    }

    public void UpdateRegionMap(World world)
    {
        //blockMapUpdates = true;
        //Task.Run(() => //making this async just causes bad, in-explicable crashes
        //{
            //Task.Delay(1000);

            if (world == null)
            {
                Logger.LogDebug("Error in map generator: null world");
                return;
            }

            CreateCustomMapImage(world);
            /*
            if (Futile.atlasManager.DoesContainAtlas("map_" + world.name))
            {
                Futile.atlasManager.ActuallyUnloadAtlasOrImage("map_" + world.name);
                Logger.LogDebug("Unloaded previous image atlas; hopefully that fixes it?");
            }

            foreach (RoomCamera cam in world.game.cameras)
            {
                cam.hud.ResetMap(new Map.MapData(world.game.world, world.game.rainWorld));
            }
            */
            //try displaying a randomization finished message
            try
            {
                //Room realRoom = self.world.GetAbstractRoom(self.startingRoom).realizedRoom;
                world.game.Players[0].Room.realizedRoom?.NewMessageInRoom("Created map for " + world.name, 0);
            }
            catch (Exception ex) { Logger.LogError(ex); }

            //blockMapUpdates = false;
        //});
    }

    private void WriteRandomizerFiles(WorldLoader wl)
    {
        WriteRandomizerConnectionsFile(wl);
        //WriteRandomizerPositionsFile(wl);
        WriteMirroredRoomsFile(wl);
    }
    //this file keeps track of room connections
    private void WriteRandomizerConnectionsFile(WorldLoader wl)
    {
        string fileText = "";
        Logger.LogDebug("Writing randomizer connections file");

        //plan: go through each room and output its connections. that simple
        foreach (AbstractRoom abRoom in wl.abstractRooms)
        {
            fileText += abRoom.name + ":";
            for (int i = 0; i < abRoom.connections.Length; i++)
            {
                if (i > 0)
                    fileText += ",";
                if (abRoom.connections[i] >= 0)
                    fileText += wl.abstractRooms[abRoom.connections[i] - firstRoomIdx].name;
            }
            fileText += "\n";
        }

        try
        {
            File.WriteAllText(GetRandomizerConnectionsFile(wl.worldName, wl.playerCharacter.value), fileText);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    //this file keeps track of room map positions
    /*private void WriteRandomizerPositionsFile(WorldLoader wl)
    {
        string fileText = "";
        Logger.LogDebug("Writing randomizer positions file");

        //plan: go through each room and output its map position. that simple
        foreach (AbstractRoom abRoom in wl.abstractRooms)
        {
            fileText += abRoom.name + ":" + abRoom.mapPos.x + "," + abRoom.mapPos.y + "\n";
        }

        try
        {
            File.WriteAllText(GetRandomizerPositionsFile(wl.worldName, wl.playerCharacter.value), fileText);
        } catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }*/
    private void WriteMirroredRoomsFile(WorldLoader wl)
    {
        string fileText = "";
        Logger.LogDebug("Writing mirrored rooms file");

        //plan: go through each room and output its map position. that simple
        foreach (string room in MirroredRooms)
        {
            fileText += room + "\n";
        }

        try
        {
            File.WriteAllText(GetMirroredRoomsFile(wl.worldName, wl.playerCharacter.value), fileText);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    private void ClearOldMergedMapFile(string region, string slugcat)
    {
        try
        {
            Logger.LogDebug("Clearing old map.txt file");
            string mapPath = AssetManager.ResolveFilePath(string.Concat(new string[]
            {
                "mergedmods",
                Path.DirectorySeparatorChar.ToString(),
                "world",
                Path.DirectorySeparatorChar.ToString(),
                region,
                Path.DirectorySeparatorChar.ToString(),
                "map_",
                region,
                "-",
                slugcat,
                ".txt"
            }));

            if (File.Exists(mapPath))
                File.Delete(mapPath);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    public void ClearAllRandomizerFiles(string slugcat)
    {
        try
        {
            Logger.LogDebug("Clearing all randomizer files for " + slugcat);

            string dirPath = AssetManager.ResolveDirectory("RoomConnectionRandomizer");
            if (!Directory.Exists(dirPath))
            {
                Logger.LogDebug("Couldn't find RoomConnectionRandomizer directory");
                return;
            }

            string[] files = Directory.GetFiles(dirPath);
            string txtEnd = ("-" + slugcat + ".txt").ToLowerInvariant(), pngEnd = ("-" + slugcat + ".png").ToLowerInvariant();
            foreach (string file in files)
            {
                if (file.EndsWith(txtEnd) || file.EndsWith(pngEnd))
                {
                    try
                    {
                        File.Delete(file);
                    } catch (Exception ex) { Logger.LogError(ex); }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    public void ClearAllRandomizerFiles()
    {
        try
        {
            Logger.LogDebug("Clearing all randomizer files");

            string dirPath = AssetManager.ResolveDirectory("RoomConnectionRandomizer");
            if (!Directory.Exists(dirPath))
            {
                Logger.LogDebug("Couldn't find RoomConnectionRandomizer directory");
                return;
            }

            Directory.Delete(dirPath, true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    #endregion

    #region Randomizer_Logic

    public string LastRandomizedRegion = ""; //used to tell when the "finished randomization" message should be displayed
    public List<string> RandomizedRegions = new(); //used for syncing in Rain Meadow
    public List<long> RandomizationTimes = new(); //^^^

    public string[] MirroredRooms = new string[0];

    public bool IgnoredRegion(string region)
    {
        return Options.ignoredRegions.TryGetValue(region, out Configurable<bool> ignored) && ignored.Value;
    }
    public bool NeedToRandomizeRegion(WorldLoader wl)
    {
        return !File.Exists(GetRandomizerConnectionsFile(wl.worldName, wl.playerCharacter.value))
            || !File.Exists(GetRandomizerMapFile(wl.worldName, wl.playerCharacter.value));
    }

    public void ReadRandomizerFiles(WorldLoader wl)
    {
        firstRoomIdx = wl.world.firstRoomIndex;

        OverwriteMapFile(wl.worldName, wl.playerCharacter.value);
        OverwriteMapPngFile(wl.worldName, wl.playerCharacter.value);

        //do positions first: it's easier
        //ReadRandomizerPositionsFile(wl);
        ReadRandomizerConnectionsFile(wl);
        ReadMirroredRoomsFile(wl);

        //have the randomization data
        if (IsOnline && !RandomizedRegions.Contains(wl.worldName))
        {
            RandomizedRegions.Add(wl.worldName);
            RandomizationTimes.Add(DateTime.Now.Ticks);
            AddOnlineData();
        }

    }
    //The positions file seems redundant since its data is already included entirely in the map file.
    /*private void ReadRandomizerPositionsFile(WorldLoader wl)
    {
        Logger.LogDebug("Reading positions file");
        try
        {
            string positionsFile = GetRandomizerPositionsFile(wl.worldName, wl.playerCharacter.value);
            if (!File.Exists(positionsFile))
                return;

            //first, get a dictionary of room names to indices
            Dictionary<string, int> nameToIdx = new();
            for (int i = 0; i < wl.abstractRooms.Count; i++)
                nameToIdx.Add(wl.abstractRooms[i].name, i);

            //go through each line (if it's valid) and replace the corresponding room's mapPos
            string[] fileLines = File.ReadAllLines(positionsFile);
            foreach (string line in fileLines)
            {
                string[] lineData = line.Split(':');
                if (lineData.Length < 2)
                    continue;

                string name = lineData[0];
                if (!nameToIdx.ContainsKey(name))
                    continue;
                int idx = nameToIdx[name];
                AbstractRoom abRoom = wl.abstractRooms[idx];

                string[] posData = lineData[1].Split(',');
                if (posData.Length < 2)
                    continue;
                abRoom.mapPos.x = float.Parse(posData[0]);
                abRoom.mapPos.y = float.Parse(posData[1]);
            }

            nameToIdx.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }*/
    private void ReadRandomizerConnectionsFile(WorldLoader wl)
    {
        Logger.LogDebug("Reading connections file");
        try
        {
            string connectionsFile = GetRandomizerConnectionsFile(wl.worldName, wl.playerCharacter.value);
            if (!File.Exists(connectionsFile))
            {
                Logger.LogDebug("Couldn't find connections file!!");
                return;
            }

            //first, get a dictionary of room names to indices
            Dictionary<string, int> nameToIdx = new();
            for (int i = 0; i < wl.abstractRooms.Count; i++)
                nameToIdx.Add(wl.abstractRooms[i].name, i);

            //go through each line (if it's valid) and replace the corresponding room's connections
            string[] fileLines = File.ReadAllLines(connectionsFile);
            Logger.LogDebug("Abstract rooms: " + wl.abstractRooms.Count + "; fileLines: " + fileLines.Length);
            foreach (string line in fileLines)
            {
                string[] lineData = line.Split(':');
                if (lineData.Length < 2)
                    continue;

                string name = lineData[0];
                if (!nameToIdx.ContainsKey(name))
                {
                    Logger.LogDebug("Couldn't find " + name + " in wl.abstractRooms");
                    continue;
                }
                int idx = nameToIdx[name];
                AbstractRoom abRoom = wl.abstractRooms[idx];

                string[] connData = lineData[1].Split(',');
                if (abRoom.connections.Length < connData.Length)
                {
                    Logger.LogDebug("Wrong connections length: " + abRoom.connections.Length + " vs. " + connData.Length);
                    abRoom.connections = new int[connData.Length];
                    for (int i = 0; i < abRoom.connections.Length; i++)
                        abRoom.connections[i] = -1;
                }
                for (int i = 0; i < abRoom.connections.Length && i < connData.Length; i++)
                {
                    if (connData[i] == "" || !nameToIdx.ContainsKey(connData[i]))
                        continue;
                    abRoom.connections[i] = nameToIdx[connData[i]] + firstRoomIdx;
                }
            }

            nameToIdx.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    private void ReadMirroredRoomsFile(WorldLoader wl)
    {
        Logger.LogDebug("Reading mirrored rooms file");
        try
        {
            string mirrorFile = GetMirroredRoomsFile(wl.worldName, wl.playerCharacter.value);
            if (!File.Exists(mirrorFile))
                return;

            MirroredRooms = File.ReadAllLines(mirrorFile);
            //this will have one erroneous mirrored room of "", but that shouldn't cause any issues
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    //for map generation
    private int firstRoomIdx = 0;

    public void RandomizeRegion(WorldLoader wl)
    {
        try
        {
            ClearOldMergedMapFile(wl.worldName, wl.playerCharacter.value);

            Logger.LogDebug("Randomizing region " + wl.worldName);

            //list room names, for convenient debugging
            string roomListString = "Rooms: ";
            for (int i = 0; i < wl.abstractRooms.Count; i++)
                roomListString += i + "=" + wl.abstractRooms[i].name + ", ";
            Logger.LogDebug(roomListString);

            //so... map positions aren't added until much later in the process; like, after the worldloader finishes
            //so I'll just add temporary ones now!...?

            firstRoomIdx = wl.world.firstRoomIndex;

            List<string> roomNames = new();
            for (int i = 0; i < wl.abstractRooms.Count; i++)
            {
                //roomOffsets[i] = wl.abstractRooms[i].size.ToVector2() * 10f; //10f = 20f (pixel/tile) * 0.5f (average)
                if (!roomNames.Contains(wl.abstractRooms[i].name))
                    roomNames.Add(wl.abstractRooms[i].name);
            }

            Dictionary<string, Vector2> fileMapPositions = LogicalRando.GetRoomMapPositions(wl.worldName, roomNames, wl.playerCharacter.value);
            for (int k = 0; k < wl.abstractRooms.Count; k++)
            {
                if (fileMapPositions.ContainsKey(wl.abstractRooms[k].name)) //alters room map pos AND makes this pos the center of the room
                    wl.abstractRooms[k].mapPos = fileMapPositions[wl.abstractRooms[k].name];// + wl.abstractRooms[k].size.ToVector2() * 0.5f;
                else
                    Logger.LogDebug("Couldn't find map pos for " + wl.abstractRooms[k].name);
            }
            roomNames.Clear();

            //create connectibles
            List<Connectible> connectibles = new();
            List<string> tempMirroredRooms = new();
            //foreach (AbstractRoom abRoom in wl.abstractRooms)
            for (int k = 0; k < wl.abstractRooms.Count; k++)
            {
                AbstractRoom abRoom = wl.abstractRooms[k];
                Dictionary<string, Vector2> connPositions = new();
                for (int i = 0; i < abRoom.connections.Length; i++) //find each room connection's mapPos
                {
                    if (abRoom.connections[i] >= 0)
                        connPositions.Add(i.ToString(), 0.7f * (wl.abstractRooms[abRoom.connections[i] - wl.world.firstRoomIndex].mapPos - abRoom.mapPos));
                }
                if (connPositions.Count < 1)
                    continue;

                bool notRandomized = (abRoom.gate && !Options.randomizeGates.Value) //don't randomize gates?
                    || ((abRoom.shelter || abRoom.isAncientShelter) && !Options.randomizeShelters.Value) //don't randomize shelters?
                    || UnityEngine.Random.value < (1f - Options.randomizationAmount.Value); //don't randomize a certain percentage?

                //mirror room?
                if (!notRandomized && UnityEngine.Random.value < Options.mirroredRooms.Value)
                {
                    tempMirroredRooms.Add(abRoom.name);
                    string[] conPosArr = connPositions.Keys.ToArray();
                    foreach (string conn in conPosArr)
                        connPositions[conn] = new Vector2(-connPositions[conn].x, connPositions[conn].y);
                }

                connectibles.Add(new Connectible(k.ToString(), connPositions, abRoom.mapPos, notRandomized));
            }

            MirroredRooms = tempMirroredRooms.ToArray();
            tempMirroredRooms.Clear();

            //randomize these connectibles
            connectibles.Shuffle();
            List<Connectible> newConnectibles = LogicalRando.RandomlyConnectConnectibles(connectibles, Options.placementRandomness.Value);

            foreach (Connectible c in connectibles)
                c.Clear();
            connectibles.Clear();

            string debugText = "Connectible Positions: ";
            foreach (Connectible c in newConnectibles)
                debugText += c.name + ":" + c.position.ToString() + ", ";
            Logger.LogDebug(debugText);


            //has to go here before the room connections are changed out
            WriteMapFile(wl.worldName, wl.playerCharacter.value, newConnectibles, wl.abstractRooms);

            //convert these connectibles into a usable format

            foreach (Connectible c in newConnectibles)
            {
                int idx = Int32.Parse(c.name);

                //change map position
                wl.abstractRooms[idx].mapPos = c.position;// - wl.abstractRooms[idx].size.ToVector2() * 0.5f;

                //replace connections
                foreach (var pair in c.connections)
                {
                    if (pair.Value == "") //disconnect not-connected connections
                        wl.abstractRooms[idx].connections[Int32.Parse(pair.Key)] = -1;
                    else
                        wl.abstractRooms[idx].connections[Int32.Parse(pair.Key)] = Int32.Parse(pair.Value) + wl.world.firstRoomIndex;
                }
            }


            //write the randomizer file
            WriteRandomizerFiles(wl);
            //WriteMapFile(wl.worldName, wl.playerCharacter.value, newConnectibles, wl.abstractRooms);

            foreach (Connectible c in newConnectibles)
                c.Clear();
            newConnectibles.Clear();

            LastRandomizedRegion = wl.worldName;
            if (IsOnline && !RandomizedRegions.Contains(wl.worldName))
            {
                RandomizedRegions.Add(wl.worldName);
                RandomizationTimes.Add(DateTime.Now.Ticks);
                AddOnlineData();
            }

            //lastly, try creating a custom map image...
            //CreateCustomMapImage(wl);

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    #endregion

    #region Misc_Tools
    public static void LogSomething(object obj)
    {
        Instance.Logger.LogDebug(obj);
    }

    private void TestConnectibles()
    {
        //test Connectibles
        Dictionary<string, Vector2> conn0conns = new();
        conn0conns.Add("0", new Vector2(-100, 200));
        conn0conns.Add("1", new Vector2(300, 50));
        conn0conns.Add("2", new Vector2(0, -100));
        Connectible conn0 = new Connectible("conn0", conn0conns, new Vector2(0, 0));

        Dictionary<string, Vector2> conn1conns = new();
        conn1conns.Add("0", new Vector2(300, 50));
        conn1conns.Add("1", new Vector2(-50, -100));
        Connectible conn1 = new Connectible("conn1", conn1conns, new Vector2(300, 0));

        Dictionary<string, Vector2> conn2conns = new();
        conn2conns.Add("0", new Vector2(-100, 50));
        conn2conns.Add("1", new Vector2(-150, -50));
        Connectible conn2 = new Connectible("conn2", conn2conns, new Vector2(150, 150));

        Dictionary<string, Vector2> conn3conns = new();
        conn3conns.Add("0", new Vector2(-100, 0));
        Connectible conn3 = new Connectible("conn3", conn3conns, new Vector2(150, -50));

        List<Connectible> connectibles = new(new Connectible[]
        {
            conn0, conn1, conn2, conn3
        });

        List<Connectible> newConns = LogicalRando.RandomlyConnectConnectibles(connectibles, 0.1f);

        foreach (Connectible c in newConns)
        {
            string txt = c.name + ": ";
            txt += "pos=" + c.position.ToString() + "; ";
            txt += "conns: ";
            foreach (var conn in c.connections)
            {
                txt += conn.Key + "-" + conn.Value + ", ";
            }
            Logger.LogDebug(txt);
        }
    }

    #endregion



    #region Custom_Map_Image
    private void CreateCustomMapImage(World world)
    {
        try
        {
            Logger.LogDebug("Creating custom map image");
            //create minimaps...?
            //no, create RoomRepresentations:
            MapObject mapObject = new MapObject(world, true);

            //create textures for rooms that are already realized
            
            foreach (MapObject.RoomRepresentation roomRep in mapObject.roomReps)
            {
                if (roomRep.texture == null && roomRep.room.realizedRoom != null)
                {
                    try { Futile.atlasManager.ActuallyUnloadAtlasOrImage("MapTex_" + roomRep.room.name); }
                    catch (Exception ex) { Logger.LogError(ex); }
                    roomRep.CreateMapTexture(roomRep.room.realizedRoom);
                }
            }
            

            //float mapScale = 1f; //scale should always be 3f
            float mapScale = 3f;
            Vector2 minBounds = new(float.MaxValue, float.MaxValue);
            Vector2 maxBounds = new(float.MinValue, float.MinValue);
            //List<MapRenderDefaultMaterial> list = new List<MapRenderDefaultMaterial>();
            //foreach MiniMap
            //for (int i = 0; i < this.mapPage.subNodes.Count; i++)
            foreach (MapObject.RoomRepresentation roomRep in mapObject.roomReps)
            {
                //if (this.mapPage.subNodes[i] is RoomPanel)
                //{
                /*
                bool flag = true;
                foreach (string text in this.mapPage.map.world.DisabledMapRooms)
                {
                    if (text == (this.mapPage.subNodes[i] as RoomPanel).roomRep.room.name)
                    {
                        flag = false;
                        Custom.Log(new string[] { "MAP SIZER IGNORED HIDDEN ROOM:", text });
                        break;
                    }
                }
                if (flag)
                {
                */
                //MiniMap miniMap = (this.mapPage.subNodes[i] as RoomPanel).miniMap;
                //replaced miniMapSize with miniMapSize
                //Vector2 pos = (this.mapPage.subNodes[i] as RoomPanel).pos;
                if (roomRep.mapTex == null)
                    continue;
                Vector2 miniMapSize = roomRep.mapTex.sourcePixelSize * mapScale;
                //Vector2 miniMapSize = roomRep.room.size.ToVector2();
                //if (roomRep.texture == null)
                    //continue;
                //Vector2 miniMapSize = new Vector2(roomRep.texture.width, roomRep.texture.height) * mapScale;
                Vector2 pos = roomRep.room.mapPos;
                if (pos.x - miniMapSize.x * 0.5f < minBounds.x)
                {
                    minBounds.x = pos.x - miniMapSize.x * 0.5f;
                }
                if (pos.x + miniMapSize.x * 0.5f > maxBounds.x)
                {
                    maxBounds.x = pos.x + miniMapSize.x * 0.5f;
                }
                if (pos.y - miniMapSize.y * 0.5f < minBounds.y)
                {
                    minBounds.y = pos.y - miniMapSize.y * 0.5f;
                }
                if (pos.y + miniMapSize.y * 0.5f > maxBounds.y)
                {
                    maxBounds.y = pos.y + miniMapSize.y * 0.5f;
                }
                //mapScale = miniMap.Scale;
                //}
                //}
                //else if (this.mapPage.subNodes[i] is MapRenderDefaultMaterial)
                //{
                //    list.Add(this.mapPage.subNodes[i] as MapRenderDefaultMaterial);
                //}
            }
            int mapHeight = (int)((maxBounds.y - minBounds.y) / mapScale) + 20;
            Dictionary<string, Rect> imageMeta = new Dictionary<string, Rect>();
            Texture2D mapTexture = new Texture2D((int)((maxBounds.x - minBounds.x) / mapScale) + 20, mapHeight * 3);
            for (int j = 0; j < mapTexture.width; j++)
            {
                for (int k = mapTexture.height - 1; k >= 0; k--)
                {
                    UnityEngine.Color color = new(0f, 1f, 0f);
                    mapTexture.SetPixel(j, k, color);
                }
            }
            //for (int l = 0; l < this.mapPage.subNodes.Count; l++)
            foreach (MapObject.RoomRepresentation roomRep in mapObject.roomReps)
            {
                /*
                if (this.mapPage.subNodes[l] is RoomPanel)
                {
                    bool flag2 = true;
                    foreach (string text2 in this.mapPage.map.world.DisabledMapRooms)
                    {
                        if (text2 == (this.mapPage.subNodes[l] as RoomPanel).roomRep.room.name)
                        {
                            flag2 = false;
                            Custom.Log(new string[] { "MAP RENDER IGNORED HIDDEN ROOM:", text2 });
                            break;
                        }
                    }
                */
                //MiniMap miniMap2 = (this.mapPage.subNodes[l] as RoomPanel).miniMap;
                if (roomRep.mapTex == null)
                    continue;
                Vector2 miniMapSize = roomRep.mapTex.sourcePixelSize * mapScale;
                //Vector2 miniMapSize = roomRep.room.size.ToVector2();
                //if (roomRep.texture == null)
                    //continue;
                //Vector2 miniMapSize = new Vector2(roomRep.texture.width, roomRep.texture.height) * mapScale;
                //IntVector2 intVector = IntVector2.FromVector2(((this.mapPage.subNodes[l] as RoomPanel).pos - minBounds - miniMapSize * 0.5f) / mapScale);
                Vector2 panelPos = roomRep.room.mapPos;
                IntVector2 intVector = IntVector2.FromVector2((panelPos - minBounds - miniMapSize * 0.5f) / mapScale);
                //int num3 = (this.mapPage.subNodes[l] as RoomPanel).layer * num2 + 10;
                int yOffset = roomRep.room.layer * mapHeight + 10;
                int xOffset = 10;
                if (roomRep.texture != null)// && flag2)
                {
                    imageMeta[roomRep.room.name] = new Rect(new Vector2((float)(intVector.x + xOffset), (float)(intVector.y + yOffset)), new Vector2((float)roomRep.texture.width, (float)roomRep.texture.height));
                    for (int m = 0; m < roomRep.texture.width; m++)
                    //for (int j = 0; j < roomRep.texture.width; j++)
                    {
                        //j = m mirrored (if room is mirrored)
                        //j replaces m in all roomRep.texture.GetPixel calls
                        int j = m;
                        if (MirroredRooms.Contains(roomRep.room.name))
                            j = roomRep.texture.width - m - 1;

                        for (int n = 0; n < roomRep.texture.height; n++)
                        {
                            if (intVector.x + m + xOffset >= 0 && intVector.x + m + xOffset < mapTexture.width && intVector.y + n + yOffset >= 0 && intVector.y + n + yOffset < mapTexture.height)
                            {
                                //int num5 = 1;
                                /*
                                for (int num6 = 0; num6 < list.Count; num6++)
                                {
                                    if (list[num6].rect.Vector2Inside((intVector.ToVector2() + new Vector2((float)(m + 10), (float)(n + 10))) * mapScale + minBounds))
                                    {
                                        num5 = (list[num6].materialIsAir ? 2 : 0);
                                    }
                                }
                                */
                                UnityEngine.Color pixel = roomRep.texture.GetPixel(j, n);
                                if (((int)(pixel.r * 255f) == 77 && (int)(pixel.g * 255f) == 77 && (int)(pixel.b * 255f) == 77) || ((int)(pixel.r * 255f) == 54 && (int)(pixel.g * 255f) == 54 && (int)(pixel.b * 255f) == 130))
                                {
                                    //if (num5 != 1 || texture.GetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset) == new UnityEngine.Color(0f, 1f, 0f))
                                    if (mapTexture.GetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset) == new UnityEngine.Color(0f, 1f, 0f))
                                    {
                                        mapTexture.SetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset, new UnityEngine.Color(0f, 0f, 0f));
                                    }
                                }
                                else// if (num5 != 2 || texture.GetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset) == new UnityEngine.Color(0f, 1f, 0f))
                                {
                                    if (((int)(pixel.r * 255f) == 128 && (int)(pixel.g * 255f) == 77 && (int)(pixel.b * 255f) == 77) || ((int)(pixel.r * 255f) == 89 && (int)(pixel.g * 255f) == 54 && (int)(pixel.b * 255f) == 130))
                                    {
                                        mapTexture.SetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset, new UnityEngine.Color(0.6f, 0f, (n <= roomRep.waterLevel) ? 1f : 0f));
                                    }
                                    else
                                    {
                                        mapTexture.SetPixel(intVector.x + m + xOffset, intVector.y + n + yOffset, new UnityEngine.Color(1f, 0f, (n <= roomRep.waterLevel) ? 1f : 0f));
                                    }
                                }
                            }
                        }
                    }
                }
                //}
            }
            mapTexture.wrapMode = TextureWrapMode.Clamp;//1;
            mapTexture.filterMode = 0;
            mapTexture.Apply();
            HeavyTexturesCache.LoadAndCacheAtlasFromTexture("Region_Map_" + world.name, mapTexture, false);
            //this.mapTex = Futile.atlasManager.GetElementWithName("Region_Map_" + wl.world.name);
            FAtlasElement mapTex = Futile.atlasManager.GetElementWithName("Region_Map_" + world.name);
            //Vector2 size = new Vector2(mapTex.sourcePixelSize.x, mapTex.sourcePixelSize.y) + new Vector2(10f, 40f);

            //this.Refresh();

            //now figure out how to write the file!!
            PNGSaver.SaveTextureToFile(mapTexture, GetRandomizerMapPngFile(world.name, world.game.StoryCharacter.value));


            //attempt to abstractize all rooms that do not contain any slugcats
            //RoomsToAbstractize.Clear();
            foreach (MapObject.RoomRepresentation roomRep in mapObject.roomReps)
            {
                if (roomRep.room.realizedRoom == null) //it's already good!
                    continue;
                if (roomRep.room.realizedRoom.PlayersInRoom.Count < 1)
                {
                    try { roomRep.room.Abstractize(); }
                    catch (Exception ex)
                    {
                        Logger.LogDebug("Error abstractizing room " + roomRep.room.name);
                        Logger.LogError(ex);
                        //RoomsToAbstractize.Add(roomRep.room);
                        try
                        {
                            roomRep.room.realizedRoom.Unloaded();
                            roomRep.room.world.activeRooms.Remove(roomRep.room.realizedRoom);
                            roomRep.room.realizedRoom = null;
                        } catch (Exception ex2) { Logger.LogError(ex2); }//RoomsToAbstractize.Add(roomRep.room); }
                    }
                }
                //else
                    //RoomsToAbstractize.Add(roomRep.room);
            }

            //add gate used
            //if (world.game.overWorld != null && world.game.overWorld.reportBackToGate != null)
                //RoomsToAbstractize.Add(world.game.overWorld.reportBackToGate.room.abstractRoom);

            //Logger.LogDebug("Rooms to abstractize: " + RoomsToAbstractize.Count);
            //mapObject.roomReps = null;


            //write map_image.txt
            List<string> imageData = new List<string>();
            foreach (KeyValuePair<string, Rect> keyValuePair in imageMeta)
            {
                imageData.Add(string.Concat(new string[]
                {
                    keyValuePair.Key,
                    ": ",
                    keyValuePair.Value.x.ToString(),
                    ",",
                    keyValuePair.Value.y.ToString(),
                    ",",
                    keyValuePair.Value.width.ToString(),
                    ",",
                    keyValuePair.Value.height.ToString()
                }));
            }
            File.WriteAllLines(GetRandomizerMapImageFile(world.name, world.game.StoryCharacter.value), imageData);

            OverwriteMapPngFile(world.name, world.game.StoryCharacter.value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    #endregion
}
