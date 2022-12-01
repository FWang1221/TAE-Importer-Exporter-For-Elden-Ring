using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ConsoleApp4 //ConsoleApp4, the TAE Importer/Exporter, as well as the Hemalurgical Spike, resides here
{
    class Program
    {
        static class Globals
        {
            public static bool isSeed = false;

            public static Dictionary<int, string> taeParams = new Dictionary<int, string>(); //dictionary of all events used by the player character's TAE, excluding event 113
            public static Dictionary<int, string> animIDType = new Dictionary<int, string>(); //dictionary of all animation IDs and their intended functionality in a vanilla animation file. For easy sorting when going through a Hemalurgical Spike.

            public static bool hemalurgicalSpike = false; //whether or not I'm using Hemalurgical Spike or just a normal export/import
        }
        static void extractParamsMany(string directoryHere, string oldDir, int eventTypeNum) //the actual .tae extracting method
        {
            
            BND4 bnd = BND4.Read(oldDir + "/" + directoryHere + ".anibnd.dcx"); //new BND4 bnd is created by reading the file specified at the directory
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae")); //taeFiles is where the bnd files contain .tae ending
            List<string> eventReader = new List<string>(); //eventReader is the List<string> of lines that I use for exporting all the things the program reads to a file
            List<string> paramTypes = new List<string>(); //paramTypes is a list of parameter data types, s32 is int32, b is boolean, etc etc
            paramTypes.Clear();
            paramTypes = Globals.taeParams[eventTypeNum].Split().ToList(); //reference the parameters of an event, like event 608 would have data types of float float (f32 f32), event 16 wouldn't have data types, and event 1 would have data types of int32 int32 int32 byte byte int16 (s32 s32 s32 u8 u8 s16)
            if (directoryHere == "c0000") //c0000 is different from all other anibnds because of the sheer amount of animations inside and their IDs. Enemy attacks are stored in 30XX, like 3000, 3038, 3020, etc..., whereas player attacks are stored in 3XXXX, 4XXXX, and others. Enemy TAEs are also different.
                //Enemies have different TAEs, most enemies store their animations in the default TAE, so their attacks have IDs of 3000, 3002, etc etc, while an alternative offset animation ID (which can be seen in NPCParam, there are only 5 available, so 6 total variants per enemy) 
                //just adds an offset to replace 3000, like 1003000, 1003002 replaces 3002 if the alternative offset animation ID is 1, 2003020 replaces 3020 if the alternative offset animation ID is 2, etc etc. If an animation is present like 3003028 but the default animation does not contain 3028, then it is not loaded.
                //if the offset animation does not contain X0030XX, then the default 30XX is used.
            {
                if (!Globals.hemalurgicalSpike) //these are the headers for the .csv files I export, depending on whether we're exporting player animations or not, and whether the player animations we export are event type specific or Hemalurgical Spikes
                {
                    eventReader.Add("Name (for filtering purpose), TAE, Animation ID, StartTime, EndTime, " + string.Join(",", paramTypes)); //player export but not a Hemalurgical Spike
                } else
                {
                    eventReader.Add("Name, Flavor Text, TAE, Animation ID, Animation ID Description, AllowDelayLoad (Grab Attacks(?)), Imports HKX?, Is Loop By Default?, Import HKX Source Animation ID, MiniHeader Type , StartTime, EndTime, EventType"); //player export but with a Hemalurgical Spike
                }
                
            } else
            {
                eventReader.Add("Name (for filtering purpose), Animation ID, StartTime, EndTime, " + string.Join(",", paramTypes)); //enemy export
            }
            
            
            List<string> eventReaderLine = new List<string>();


            foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0)) //iterates through tae files where the size is larger than 0 bytes.
            {
                TAE tae = TAE.Read(taeFile.Bytes); //each tae file (3000, 3001, 3002... 3038, etc etc) is split into many taes (3000), (3002), (3030)
                for (int i1 = 0; i1 < tae.Animations.Count; i1++) //iterates through the tae files
                {
                    TAE.Animation anim = tae.Animations[i1];
                    
                    bool mayPass = false; //checks whether the tae contains the events we are looking for
                    for (int i = 0; i < anim.Events.Count; i++)
                    {
                        TAE.Event ev = anim.Events[i];
                        if ((!Globals.hemalurgicalSpike && ev.Type == eventTypeNum) || (Globals.hemalurgicalSpike && ev.Type == 10138))
                        {
                            mayPass = true;
                            break;
                        }

                    }
                    for (int i = 0; i < anim.Events.Count; i++) //iterates through the events in a tae file
                    {
                        TAE.Event ev = anim.Events[i];
                        
                        if (mayPass) //if we pass, we run through this section of shitty nested if statements, if not, then we break
                        {


                            int placeKeeper = 0; //placeKeeper keeps track of where we are in the byte array for each event

                            eventReaderLine.Clear(); //eventReaderLine is each line of the eventReader

                            eventReaderLine.Add(directoryHere); //adds the character ID of whatever is being exported to the line. Very helpful when filtering a bunch of .csv file exports that have been merged to create a seed

                            if (directoryHere == "c0000")
                            {
                                if (Globals.hemalurgicalSpike)
                                {
                                    eventReaderLine.Add("Flavor Text Goes Here");
                                }
                                eventReaderLine.Add(tae.ID.ToString()); //regardless of Hemalurgical Spiking, if a player is being read, then the tae ID will be stored
                            }

                            eventReaderLine.Add(anim.ID.ToString()); //this is the actual animation ID, such as 3002 (for enemies), 1003010 (for enemies), 32000 (for players), etc etc

                            if (directoryHere == "c0000")
                            {
                                if (Globals.animIDType.ContainsKey((int)anim.ID)) //for players only, if the animIDType dictionary contains the ID of the animation, like 32000, which is the first two-handed attack, then it will say that as the descriptioin
                                {
                                    eventReaderLine.Add(Globals.animIDType[(int)anim.ID]);
                                } else
                                {
                                    string animIDStr = anim.ID.ToString("D6");
                                    
                                    switch(animIDStr.Substring(0, 3))
                                    {
                                        case "004":
                                            eventReaderLine.Add("Unknown Guess - Falling");
                                            break;
                                        case "005":
                                            eventReaderLine.Add("Unknown Guess - Stagger");
                                            break;
                                        case "006":
                                            eventReaderLine.Add("Unknown Guess - Retching(?)");
                                            break;
                                        case "010":
                                            eventReaderLine.Add("Unknown Guess - Light Stagger");
                                            break;
                                        case "011":
                                            eventReaderLine.Add("Unknown Guess - A Pose");
                                            break;
                                        case "013":
                                            eventReaderLine.Add("Unknown Guess - Stagger");
                                            break;
                                        case "017":
                                            eventReaderLine.Add("Unknown Guess - Death Animation");
                                            break;
                                        case "018":
                                            eventReaderLine.Add("Unknown Guess - Post Death Animation");
                                            break;
                                        case "019":
                                            eventReaderLine.Add("Unknown Guess - Guarding");
                                            break;
                                        case "020":
                                            eventReaderLine.Add("Unknown Guess - Movement");
                                            break;
                                        case "022":
                                            eventReaderLine.Add("Unknown Guess - Movement Braking");
                                            break;
                                        case "026":
                                            eventReaderLine.Add("Unknown Guess - Sharp Rotation(?)");
                                            break;
                                        case "027":
                                            eventReaderLine.Add("Unknown Guess - Roll");
                                            break;
                                        case "028":
                                            eventReaderLine.Add("Unknown Guess - Ladder");
                                            break;
                                        case "029":
                                            eventReaderLine.Add("Unknown Guess - Weapon Swapping");
                                            break;
                                        case "030":
                                            eventReaderLine.Add("Unknown Guess - 1H");
                                            break;
                                        case "031":
                                            eventReaderLine.Add("Unknown Guess - 1H");
                                            break;
                                        case "032":
                                            eventReaderLine.Add("Unknown Guess - 2H");
                                            break;
                                        case "033":
                                            eventReaderLine.Add("Unknown Guess - 2H");
                                            break;
                                        case "034":
                                            eventReaderLine.Add("Unknown Guess - PS");
                                            break;
                                        case "035":
                                            eventReaderLine.Add("Unknown Guess - LH");
                                            break;
                                        case "038":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Right");
                                            break;
                                        case "039":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Left");
                                            break;
                                        case "040":
                                            eventReaderLine.Add("Unknown Guess - Skill");
                                            break;
                                        case "041":
                                            eventReaderLine.Add("Unknown Guess - Skill");
                                            break;
                                        case "042":
                                            eventReaderLine.Add("Unknown Guess - Skill");
                                            break;
                                        case "043":
                                            eventReaderLine.Add("Unknown Guess - Skill");
                                            break;
                                        case "045":
                                            eventReaderLine.Add("Unknown Guess - Spell");
                                            break;
                                        case "046":
                                            eventReaderLine.Add("Unknown Guess - Spell");
                                            break;
                                        case "049":
                                            eventReaderLine.Add("Unknown Guess - Spell Failure");
                                            break;
                                        case "050":
                                            eventReaderLine.Add("Unknown Guess - Consume Goods");
                                            break;
                                        case "051":
                                            eventReaderLine.Add("Unknown Guess - Ladder Consume Goods Failure");
                                            break;
                                        case "052":
                                            eventReaderLine.Add("Unknown Guess - Ladder Consume Goods");
                                            break;
                                        case "055":
                                            eventReaderLine.Add("Unknown Guess - Running Consume Goods");
                                            break;
                                        case "060":
                                            eventReaderLine.Add("Unknown Guess - World Interaction");
                                            break;
                                        case "063":
                                            eventReaderLine.Add("Unknown Guess - World Interaction");
                                            break;
                                        case "065":
                                            eventReaderLine.Add("Unknown Guess - World Interaction");
                                            break;
                                        case "067":
                                            eventReaderLine.Add("Unknown Guess - World Interaction");
                                            break;
                                        case "068":
                                            eventReaderLine.Add("Unknown Guess - Gestures");
                                            break;
                                        case "069":
                                            eventReaderLine.Add("Unknown Guess - Gestures");
                                            break;
                                        case "070":
                                            eventReaderLine.Add("Unknown Guess - Post Riposte");
                                            break;
                                        case "075":
                                            eventReaderLine.Add("Unknown Guess - Falling Post Uppercut");
                                            break;
                                        case "080":
                                            eventReaderLine.Add("Unknown Guess - Gestures");
                                            break;
                                        case "081":
                                            eventReaderLine.Add("Unknown Guess - Falling Post Uppercut");
                                            break;
                                        case "090":
                                            eventReaderLine.Add("Unknown Guess - Player Type NPC Animations");
                                            break;
                                        case "098":
                                            eventReaderLine.Add("Unknown Guess - A Pose");
                                            break;
                                        case "099":
                                            eventReaderLine.Add("Unknown Guess - Posing");
                                            break;
                                        case "101":
                                            eventReaderLine.Add("Unknown Guess - Horseriding");
                                            break;
                                        case "104":
                                            eventReaderLine.Add("Unknown Guess - Horseriding");
                                            break;
                                        case "105":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Stagger");
                                            break;
                                        case "109":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Fall");
                                            break;
                                        case "111":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Stagger");
                                            break;
                                        case "117":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Death Animation");
                                            break;
                                        case "118":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Post Death Animation");
                                            break;
                                        case "119":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Stagger");
                                            break;
                                        case "120":
                                            eventReaderLine.Add("Unknown Guess - Horseriding");
                                            break;
                                        case "122":
                                            eventReaderLine.Add("Unknown Guess - Horseriding");
                                            break;
                                        case "126":
                                            eventReaderLine.Add("Unknown Guess - Horseriding");
                                            break;
                                        case "129":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Weapon Swaps(?)");
                                            break;
                                        case "140":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Spell");
                                            break;
                                        case "145":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Spell");
                                            break;
                                        case "146":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Spell");
                                            break;
                                        case "150":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Consume Goods");
                                            break;
                                        case "155":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Running Consume Goods");
                                            break;
                                        case "160":
                                            eventReaderLine.Add("Unknown Guess - Horseriding Acquire Item(?)");
                                            break;
                                        case "202":
                                            eventReaderLine.Add("Unknown Guess - Jumping");
                                            break;
                                        case "320":
                                            eventReaderLine.Add("Unknown Guess - Crouching Movement");
                                            break;
                                        case "322":
                                            eventReaderLine.Add("Unknown Guess - Crouching Movement Braking");
                                            break;
                                        case "326":
                                            eventReaderLine.Add("Unknown Guess - Crouching Unknown");
                                            break;
                                        case "327":
                                            eventReaderLine.Add("Unknown Guess - Crouching Dodging");
                                            break;
                                        case "329":
                                            eventReaderLine.Add("Unknown Guess - Crouching Weapon Swaps");
                                            break;
                                        case "350":
                                            eventReaderLine.Add("Unknown Guess - Crouching Consume Goods");
                                            break;
                                        case "360":
                                            eventReaderLine.Add("Unknown Guess - Crouching Acquire Item(?)");
                                            break;
                                        case "380":
                                            eventReaderLine.Add("Unknown Guess - Crouching Gestures");
                                            break;
                                        case "390":
                                            eventReaderLine.Add("Unknown Guess - Crouching Activate/Deactivate");
                                            break;

                                        default:
                                            eventReaderLine.Add("Unknown");
                                            break;
                                    }
                                    //for players only, if the animIDType dictionary does not contain the ID of the animation, then it will say that it is unknown. A guesser will be implemented in the future based on the first two numbers of the ID.
                                }
                                eventReaderLine.Add(anim.MiniHeader.AllowDelayLoad.ToString());
                                eventReaderLine.Add(anim.MiniHeader.ImportsHKX.ToString());
                                eventReaderLine.Add(anim.MiniHeader.IsLoopByDefault.ToString());
                                eventReaderLine.Add(anim.MiniHeader.ImportHKXSourceAnimID.ToString());
                                eventReaderLine.Add(anim.MiniHeader.Type.ToString()); //for players only, it adds the actual animation (like bones and joints and stuff) file being referenced, the .hkt files. animation ID does not always reference its corresponding .hkt file, especially in mods.
                                                                        //When merging mods it is important to look for when an animation ID does not reference its own .hkt file. For example, Clever's Moveset Modpack changes the .hkt files of things like the fist (a042_XXXXXX.hkt animations),
                                                                        //but other mods like Moveset Animation Remix's references said a042_XXXXXX.hkt files, except the taes for MAR are based on vanilla .hkt files, leading to awkward situations where the events in some animations are totally different to what the character is actually doing.
                            }
                            eventReaderLine.Add(ev.StartTime.ToString());//adds the starting time to the line
                            if (ev.EndTime > 40)
                            {
                                eventReaderLine.Add("40"); //all animations in the game are shorter than 40 seconds long. However, there's a strange issue where FROM really like putting ludicrous, like 10^38 seconds long events for animations. Naturally, this causes some problems when importing, so it's rounded down to 40

                            } else
                            {
                                eventReaderLine.Add(ev.EndTime.ToString()); //adds the event end time to the line
                            }
                            
                            if (Globals.hemalurgicalSpike) //for Hemalurgical Spike exportation, where the event type is constantly changing
                            {
                                eventReaderLine.Add(ev.Type.ToString()); //adds the event type to the string
                                paramTypes.Clear();
                                if (!Globals.taeParams.ContainsKey(ev.Type)) //Skips exporting an event if it's not in the taeParams. Event 113 is not in, therefore it is skipped. Everything else is though.
                                {
                                    continue;
                                }
                                paramTypes = Globals.taeParams[ev.Type].Split().ToList(); //changes paramTypes to the current data types of the event
                            }
                            foreach (string dataType in paramTypes) //for each of the data types listed in paramTypes, so for an event with event type 608, it would iterate twice, for f32 and f32
                            {
                                if (dataType == "s32") //if the dataType matches a certain string, then do the following
                                {
                                    if (Globals.hemalurgicalSpike) //if we're exporting a Hemalurgical Spike, add the data type to the line
                                    {
                                        eventReaderLine.Add("s32");
                                    }
                                    eventReaderLine.Add(BitConverter.ToInt32(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString()); //regardless of Hemalurgical Spike or player animation, we add the converted bytes at whichever place we are to the line
                                    placeKeeper += 4; //shifts our place up by however long our data type is
                                }
                                if (dataType == "s16")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("s16");
                                    }
                                    eventReaderLine.Add(BitConverter.ToInt16(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 2;
                                }
                                if (dataType == "f32")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("f32");
                                    }
                                    eventReaderLine.Add(BitConverter.ToSingle(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 4;
                                }
                                if (dataType == "b")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("b");
                                    }
                                    eventReaderLine.Add(BitConverter.ToBoolean(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 1;
                                }
                                if (dataType == "u8")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("u8");
                                    }
                                    eventReaderLine.Add(ev.GetParameterBytes(tae.BigEndian)[placeKeeper].ToString());
                                    placeKeeper += 1;
                                }
                                if (dataType == "u16")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("u16");
                                    }
                                    eventReaderLine.Add(BitConverter.ToUInt16(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 2;
                                }
                                if (dataType == "u32")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("u32");
                                    }
                                    eventReaderLine.Add(BitConverter.ToUInt32(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 4;
                                }
                                if (dataType == "s8")
                                {
                                    if (Globals.hemalurgicalSpike)
                                    {
                                        eventReaderLine.Add("s8");
                                    }
                                    eventReaderLine.Add(((sbyte) ev.GetParameterBytes(tae.BigEndian)[placeKeeper]).ToString());
                                    placeKeeper += 1;
                                }
                            }
                            eventReader.Add(string.Join(",", eventReaderLine)); //adds the joined together line to a string, so we can have our .csv file
                        } else
                        {
                            break;
                        }

                    }
                }
            }
            if (!Globals.hemalurgicalSpike)
            {
                eventReader.Add(directoryHere + "," + directoryHere); //at the end of it, it writes two of our character IDs together, indicating that we're done, if we ever merge files to a seed
            }
            
            File.WriteAllLines(oldDir + "/" + directoryHere + "EventsFile.csv", eventReader.Select(x => x.ToString())); //writes all the lines to our character ID + EventsFile .csv file
            //File.WriteAllLines(oldDir + "/" + directoryHere + "EventGroupsFile.csv", eventGroupReader.Select(x => x.ToString()));
        }
        static void extractParams(string oldPath)
        {
            List<string> paramTypes = new List<string>();
            Console.WriteLine("Which event type do you wish to extract the information of? \nFor example, 0 is JumpTable, 1 is invokeAttack, 608 is animspeedgradient, etc... \n To access Hemalurgical Spiking of c0000 files, enter Ruin's number (11) in the console instead.");
            string nextLine = Console.ReadLine();
            int eventTypeNum;
            if (nextLine != "11")
            {
                eventTypeNum = Convert.ToInt32(nextLine);
            } else
            {
                eventTypeNum = 0;
                Globals.hemalurgicalSpike = true;
            }
            
            //Console.WriteLine("Click that event in DSAS. See them numbers on the lower right side? s16, u8, s32, f32, all that? \nWell just input them, like for JumpTable, event 0, input s32 f32 s32 u8 u8 s16");
            //paramTypes = Console.ReadLine().Split().ToList();
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to export. \nFor example, C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game\\mod\\chr\\importExportList.txt");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            foreach (string line in lines)
            {
                extractParamsMany(line, oldPath, eventTypeNum);
            }
            Console.WriteLine("Operation completed. Have a nice day!");
            System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }
        
        static void importParamsMany(string oldPath, string newPath)
        {
            string path = oldPath + "\\" + newPath + "EventsFile.csv";
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            List<string> header = lines[0].Split(',').ToList();
            int paramLength = 0;
            BND4 bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd.dcx");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            foreach (string column in header.Skip(4)) {
                if (column == "s32")
                {
                    paramLength += 4;
                }
                if (column == "s16")
                {
                    paramLength += 2;
                }
                if (column == "f32")
                {
                    paramLength += 4;
                }
                if (column == "b")
                {
                    paramLength += 1;
                }
                if (column == "u8")
                {
                    paramLength += 1;
                }
                if (column == "u16")
                {
                    paramLength += 2;
                }
                if (column == "s8")
                {
                    paramLength += 1;
                }
                if (column == "u32")
                {
                    paramLength += 4;
                }
            }
            foreach (string line in lines)
            {
                if (line[0] == 'c')
                {
                    newPath = line.Replace(",", "");
                    break;
                }
                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    TAE tae = TAE.Read(taeFile.Bytes);
                    for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                    {
                        TAE.Animation anim = tae.Animations[i1];
                        if (anim.ID.ToString() == line.Split(',')[0])
                        {
                            string[] paramLines = line.Split(',').ToArray();
                            byte[] rv = new byte[paramLength];
                            int offset = 0;
                            for (int i = 4; i < header.Count(); i++)
                            {
                                if (header[i] == "s32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Int32.Parse(paramLines[i])), 0, rv, offset, 4);
                                    offset += 4;
                                }
                                if (header[i] == "s16")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Int16.Parse(paramLines[i])), 0, rv, offset, 2);
                                    offset += 2;
                                }
                                if (header[i] == "f32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Parse(paramLines[i])), 0, rv, offset, 4);
                                    offset += 4;
                                }
                                if (header[i] == "b")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Boolean.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u8")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Byte.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u16")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 4;
                                }
                                if (header[i] == "s8")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(SByte.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                            }
                            TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[2]), float.Parse(line.Split(',')[3]), int.Parse(line.Split(',')[1]), 0, rv, tae.BigEndian);
                            addedEvent.Group = new TAE.EventGroup();
                            addedEvent.Group.GroupType = long.Parse(line.Split(',')[1]);
                            anim.Events.Add(addedEvent);
                            Console.WriteLine("New event type " + (float.Parse(line.Split(',')[1]).ToString() + " added at " + float.Parse(line.Split(',')[2]) + " seconds and ending at " + float.Parse(line.Split(',')[3]) + " seconds in animation " + anim.ID.ToString()));
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    Console.WriteLine("TAE has been written over.");
                }
            }
            bnd.Write(oldPath + "\\" + newPath + ".anibnd.dcx", DCX.Type.DCX_KRAK);
            Console.WriteLine("File has been written over at " + oldPath + "\\" + newPath + ".anibnd.dcx");
        }

        static void importParamsSeed(string oldPath, string newPath)
        {
            string path = oldPath + "\\SwordMasSeed.csv";
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            List<string> header = lines[0].Split(',').ToList();
            int paramLength = 0;
            BND4 bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd.dcx");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            foreach (string column in header.Skip(4))
            {
                if (column == "s32")
                {
                    paramLength += 4;
                }
                if (column == "s16")
                {
                    paramLength += 2;
                }
                if (column == "f32")
                {
                    paramLength += 4;
                }
                if (column == "b")
                {
                    paramLength += 1;
                }
                if (column == "u8")
                {
                    paramLength += 1;
                }
                if (column == "u16")
                {
                    paramLength += 2;
                }
                if (column == "s8")
                {
                    paramLength += 1;
                }
                if (column == "u32")
                {
                    paramLength += 4;
                }
            }
            foreach (string line in lines)
            {
                if (line[0] == 'c')
                {
                    newPath = line.Replace(",", "").Substring(0, 5);
                    bnd.Write(oldPath + "\\" + newPath + ".anibnd.dcx", DCX.Type.DCX_KRAK);
                    Console.WriteLine("File has been written over at " + oldPath + "\\" + newPath + ".anibnd.dcx");

                    newPath = line.Replace(",", "").Substring(5, 5);
                    if (newPath == "cXXXX")
                    {
                        System.Threading.Thread.Sleep(10000);
                        System.Environment.Exit(1);
                    }
                    bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd.dcx");
                    Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd.dcx");
                    taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));

                    
                }
                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    TAE tae = TAE.Read(taeFile.Bytes);
                    for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                    {
                        TAE.Animation anim = tae.Animations[i1];
                        if (anim.ID.ToString() == line.Split(',')[0])
                        {
                            string[] paramLines = line.Split(',').ToArray();
                            byte[] rv = new byte[paramLength];
                            int offset = 0;
                            for (int i = 4; i < header.Count(); i++)
                            {
                                if (header[i] == "s32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Int32.Parse(paramLines[i])), 0, rv, offset, 4);
                                    offset += 4;
                                }
                                if (header[i] == "s16")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Int16.Parse(paramLines[i])), 0, rv, offset, 2);
                                    offset += 2;
                                }
                                if (header[i] == "f32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Parse(paramLines[i])), 0, rv, offset, 4);
                                    offset += 4;
                                }
                                if (header[i] == "b")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Boolean.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u8")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(Byte.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u16")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                                if (header[i] == "u32")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 4;
                                }
                                if (header[i] == "s8")
                                {
                                    System.Buffer.BlockCopy(BitConverter.GetBytes(SByte.Parse(paramLines[i])), 0, rv, offset, 1);
                                    offset += 1;
                                }
                            }
                            TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[2]), float.Parse(line.Split(',')[3]), int.Parse(line.Split(',')[1]), 0, rv, tae.BigEndian);
                            addedEvent.Group = new TAE.EventGroup();
                            addedEvent.Group.GroupType = long.Parse(line.Split(',')[1]);
                            anim.Events.Add(addedEvent);
                            Console.WriteLine("New event type " + (float.Parse(line.Split(',')[1]).ToString() + " added at " + float.Parse(line.Split(',')[2]) + " seconds and ending at " + float.Parse(line.Split(',')[3]) + " seconds in animation " + anim.ID.ToString()));
                            break;
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    Console.WriteLine("TAE has been written over.");
                }
            }
            
        }

        static void importParamsPlayer(string oldPath, string newPath)
        {
            string path = oldPath + "\\" + newPath + "EventsFile.csv";
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            List<string> header = lines[0].Split(',').ToList();
            int paramLength = 0;
            BND4 bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd.dcx");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));

            Dictionary<string, BinderFile> taeDictionary = new Dictionary<string, BinderFile>();
            foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
            {
                taeDictionary.Add((taeFile.ID - 5000000 + 2000).ToString(), taeFile);
            }

            foreach (string column in header.Skip(5))
            {
                if (column == "s32")
                {
                    paramLength += 4;
                }
                if (column == "s16")
                {
                    paramLength += 2;
                }
                if (column == "f32")
                {
                    paramLength += 4;
                }
                if (column == "b")
                {
                    paramLength += 1;
                }
                if (column == "u8")
                {
                    paramLength += 1;
                }
                if (column == "u16")
                {
                    paramLength += 2;
                }
                if (column == "s8")
                {
                    paramLength += 1;
                }
                if (column == "u32")
                {
                    paramLength += 4;
                }
            }
            foreach (string line in lines.Skip(1))
            {
                if (line[0] == 'c')
                {
                    newPath = line.Replace(",", "");
                    break;
                }

                
                TAE tae = TAE.Read(taeDictionary[line.Split(',')[0]].Bytes);
                if (tae.ID.ToString() != line.Split(',')[0])
                {
                    continue;
                }
                for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                {
                    if (tae.ID.ToString() != line.Split(',')[0])
                    {
                        break;
                    }
                    TAE.Animation anim = tae.Animations[i1];
                    if (anim.ID.ToString() == line.Split(',')[1] && tae.ID.ToString() == line.Split(',')[0])
                    {
                        string[] paramLines = line.Split(',').ToArray();
                        byte[] rv = new byte[paramLength];
                        int offset = 0;
                        for (int i = 5; i < header.Count(); i++)
                        {
                            if (header[i] == "s32")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Int32.Parse(paramLines[i])), 0, rv, offset, 4);
                                offset += 4;
                            }
                            if (header[i] == "s16")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Int16.Parse(paramLines[i])), 0, rv, offset, 2);
                                offset += 2;
                            }
                            if (header[i] == "f32")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Parse(paramLines[i])), 0, rv, offset, 4);
                                offset += 4;
                            }
                            if (header[i] == "b")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Boolean.Parse(paramLines[i])), 0, rv, offset, 1);
                                offset += 1;
                            }
                            if (header[i] == "u8")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Byte.Parse(paramLines[i])), 0, rv, offset, 1);
                                offset += 1;
                            }
                            if (header[i] == "u16")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                offset += 1;
                            }
                            if (header[i] == "u32")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 1);
                                offset += 4;
                            }
                            if (header[i] == "s8")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(SByte.Parse(paramLines[i])), 0, rv, offset, 1);
                                offset += 1;
                            }
                        }
                        TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[3]), float.Parse(line.Split(',')[4]), int.Parse(line.Split(',')[2]), 0, rv, tae.BigEndian);
                        addedEvent.Group = new TAE.EventGroup();
                        addedEvent.Group.GroupType = long.Parse(line.Split(',')[2]);
                        anim.Events.Add(addedEvent);
                        Console.WriteLine("New event type " + (float.Parse(line.Split(',')[2]).ToString() + " added at " + float.Parse(line.Split(',')[3]) + " seconds and ending at " + float.Parse(line.Split(',')[4]) + " seconds in animation " + anim.ID.ToString() + " in TAE " + tae.ID.ToString()));
                            
                    }
                }
                taeDictionary[line.Split(',')[0]].Bytes = tae.Write();
                Console.WriteLine("TAE has been written over.");
                
            }
            bnd.Write(oldPath + "\\" + newPath + ".anibnd.dcx", DCX.Type.DCX_KRAK);
            Console.WriteLine("File has been written over at " + oldPath + "\\" + newPath + ".anibnd.dcx");
        }

        static void importParamsSpike(string oldPath, string newPath)
        {
            string path = oldPath + "\\" + newPath + "EventsFile.csv";
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            List<string> header = lines[0].Split(',').ToList();


            BND4 bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd.dcx");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));

            Dictionary<string, BinderFile> taeDictionary = new Dictionary<string, BinderFile>();
            foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
            {
                taeDictionary.Add((taeFile.ID - 5000000 + 2000).ToString(), taeFile);
            }

            foreach (string line in lines.Skip(2))
            {
                
                TAE tae = TAE.Read(taeDictionary[line.Split(',')[2]].Bytes);
                if (tae.ID.ToString() != line.Split(',')[0])
                {
                    continue;
                }
                for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                {
                    if (tae.ID.ToString() != line.Split(',')[2])
                    {
                        break;
                    }
                    TAE.Animation anim = tae.Animations[i1];
                    if (anim.ID.ToString() == line.Split(',')[3] && tae.ID.ToString() == line.Split(',')[2])
                    {
                        anim.Events.Clear();
                        anim.MiniHeader.ImportsHKX = bool.Parse(line.Split(',')[6]);
                        anim.MiniHeader.IsLoopByDefault = bool.Parse(line.Split(',')[7]);
                        anim.MiniHeader.AllowDelayLoad = bool.Parse(line.Split(',')[5]);
                        anim.MiniHeader.ImportHKXSourceAnimID = int.Parse(line.Split(',')[8]);
                        int paramLength = 0;
                        byte[] rv = new byte[paramLength];
                        for (int i = 13; i < line.Split(',').Length; i += 2)
                        {
                            if (line.Split(',')[i] == "s32")
                            {                                
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Int32.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 4);
                                paramLength += 4;
                            }
                            if (line.Split(',')[i] == "s16")
                            {                                
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Int16.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 2);
                                paramLength += 2;
                            }
                            if (line.Split(',')[i] == "f32")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 4);
                                paramLength += 4;
                            }
                            if (line.Split(',')[i] == "b")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Boolean.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 1);
                                paramLength += 1;
                            }
                            if (line.Split(',')[i] == "u8")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(Byte.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 1);
                                paramLength += 1;
                            }
                            if (line.Split(',')[i] == "u16")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 1);
                                paramLength += 2;
                            }
                            if (line.Split(',')[i] == "s8")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(SByte.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 1);
                                paramLength += 1;
                            }
                            if (line.Split(',')[i] == "u32")
                            {
                                System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(line.Split(',')[i + 1])), 0, rv, paramLength, 1);
                                paramLength += 4;
                            }
                        }
                        TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[10]), float.Parse(line.Split(',')[11]), int.Parse(line.Split(',')[12]), 0, rv, tae.BigEndian);
                        addedEvent.Group = new TAE.EventGroup();
                        addedEvent.Group.GroupType = long.Parse(line.Split(',')[12]);
                        anim.Events.Add(addedEvent);
                    }
                    
                }

                taeDictionary[line.Split(',')[2]].Bytes = tae.Write();
                Console.WriteLine("TAE has been written over.");
            }
            bnd.Write(oldPath + "\\" + newPath + ".anibnd.dcx", DCX.Type.DCX_KRAK);
            Console.WriteLine("File has been written over at " + oldPath + "\\" + newPath + ".anibnd.dcx");
            System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }

        static void importParams(string oldPath) {
            if (Globals.isSeed)
            {
                importParamsSeed(oldPath, "c2010");
            }
            if (Globals.hemalurgicalSpike)
            {
                importParamsSpike(oldPath, "c0000");
            }
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to import. \n C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+\\importList.txt \nYour import list should have cXXXX on each line, so something like c4310 on the first line and c4290 on the second.");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            if (!Globals.isSeed)
            {
                foreach (string line in lines)
                {
                    if (line != "c0000")
                    {
                        importParamsMany(oldPath, line);
                    } else
                    {
                        importParamsPlayer(oldPath, line);
                    }
                }
            }

            Console.WriteLine("Operation completed. Have a nice day!");
            System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }
        static void startScraping(string oldPath) {
            
            Console.WriteLine("Do you wish to access the export function? \n(This exports your file to the directory as cXXXXEventFiles.csv) (y/n)");
            if (Console.ReadLine() == "y")
            {
                extractParams(oldPath);
            }
            Console.WriteLine("Do you wish to access the import function? (y/n/s/r)\nImporting will check the animation files reader to import the files. Files must follow the example template. \ns is for 'seed', your file in the directory must be named 'SwordMasSeed.csv' and should follow the formatting as seen on the example. \n r is for Hemalurgical Spike, where your animation file will be merged.");
            string answer = Console.ReadLine();
            if (answer == "y")
            {
                importParams(oldPath);
            } else if (answer == "s")
            {
                Globals.isSeed = true;
                importParams(oldPath);
            }
            else if (answer == "r")
            {
                Globals.hemalurgicalSpike = true;
                importParams(oldPath);
            }

            System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }
        static void Main(string[] args)
        {
            Globals.taeParams.Add(0, "s32 f32 s32 u8 u8 s16");
            Globals.taeParams.Add(1, "s32 s32 s32 u8 u8 s16");
            Globals.taeParams.Add(2, "s32 s32 s32 u8 b s16 s16 u8 b");
            Globals.taeParams.Add(5, "s32 s32");
            Globals.taeParams.Add(14, "u8 u8 u8 f32 f32 f32 f32 f32 f32");
            Globals.taeParams.Add(16, "N");
            Globals.taeParams.Add(17, "N");
            Globals.taeParams.Add(24, "s32 s32 s32 s32");
            Globals.taeParams.Add(32, "s32");
            Globals.taeParams.Add(33, "s32");
            Globals.taeParams.Add(34, "s32");
            Globals.taeParams.Add(35, "s32");
            Globals.taeParams.Add(64, "s32 b u8 s16 u8 u8 s16 s8 s8");
            Globals.taeParams.Add(65, "s32 u8 u8 u16 b u8");
            Globals.taeParams.Add(66, "s32");
            Globals.taeParams.Add(67, "s32");
            Globals.taeParams.Add(95, "s32 s32 s32 b b b u8 s16 s16 s32");
            Globals.taeParams.Add(96, "s32 s32 s32 b b b u8 s16");
            Globals.taeParams.Add(104, "s32 s32 s32 b");
            Globals.taeParams.Add(110, "s32");
            Globals.taeParams.Add(112, "s32 s16 s16 s32");
            Globals.taeParams.Add(114, "s32 s16 s16 s32 b u8 u8 b b u8");
            Globals.taeParams.Add(115, "s32 s16 s16 s32 b u8 u8 b b u8");
            Globals.taeParams.Add(116, "s32 s32 s32 b u8 u8 u8");
            Globals.taeParams.Add(117, "s32 s32 s32 b b b u8");
            Globals.taeParams.Add(118, "s32 s16 s16 s16 s16");
            Globals.taeParams.Add(119, "s32 s16 s16 s32 b");
            Globals.taeParams.Add(120, "s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 b");
            Globals.taeParams.Add(121, "s32 s16 b u8");
            Globals.taeParams.Add(122, "s32 s16 s16 u8 b s16 u8");
            Globals.taeParams.Add(123, "s32 s32 u8 u8 s16 u8 b u8 u8");
            Globals.taeParams.Add(128, "s32 s32");
            Globals.taeParams.Add(129, "s32 s32 s32 s32 s16 b u8 s32 s32 s32");
            Globals.taeParams.Add(132, "s32 s32 s32 s32");
            Globals.taeParams.Add(133, "s32 s32 s32 u8 u8");
            Globals.taeParams.Add(134, "s32 s32 s32 f32 f32");
            Globals.taeParams.Add(137, "s32");
            Globals.taeParams.Add(138, "s32 s32");
            Globals.taeParams.Add(139, "s32 u16 u16 u16 u16 u16 u16");
            Globals.taeParams.Add(144, "s16 u16 f32 f32");
            Globals.taeParams.Add(145, "s16 u16 s16");
            Globals.taeParams.Add(150, "s32");
            Globals.taeParams.Add(151, "s32");
            Globals.taeParams.Add(152, "f32 f32 f32 f32 u8 b");
            Globals.taeParams.Add(153, "s32 s32 s32 u8 u8");
            Globals.taeParams.Add(160, "N");
            Globals.taeParams.Add(161, "N");
            Globals.taeParams.Add(192, "f32");
            Globals.taeParams.Add(193, "f32 f32 u8");
            Globals.taeParams.Add(196, "s32 s32 s32 s32 b");
            Globals.taeParams.Add(197, "f32 f32");
            Globals.taeParams.Add(198, "s16 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32 s32");
            Globals.taeParams.Add(199, "s16");
            Globals.taeParams.Add(200, "s16");
            Globals.taeParams.Add(224, "f32 b u8");
            Globals.taeParams.Add(225, "u8");
            Globals.taeParams.Add(226, "u8");
            Globals.taeParams.Add(227, "s32");
            Globals.taeParams.Add(228, "f32 f32");
            Globals.taeParams.Add(229, "s32");
            Globals.taeParams.Add(230, "u8");
            Globals.taeParams.Add(231, "s32");
            Globals.taeParams.Add(232, "u8 u8 u8 u8");
            Globals.taeParams.Add(233, "u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8 u8");
            Globals.taeParams.Add(234, "s32");
            Globals.taeParams.Add(235, "b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b");
            Globals.taeParams.Add(236, "f32 f32 u8");
            Globals.taeParams.Add(237, "s32");
            Globals.taeParams.Add(238, "f32 s32 s32 s32");
            Globals.taeParams.Add(300, "s16 s16 f32 f32 s16 s16");
            Globals.taeParams.Add(301, "s32 s32 s32 s32");
            Globals.taeParams.Add(302, "s32");
            Globals.taeParams.Add(303, "s32");
            Globals.taeParams.Add(304, "u16 u8 u8 s32");
            Globals.taeParams.Add(307, "u16 u16 s32 s32");
            Globals.taeParams.Add(310, "u8 u8");
            Globals.taeParams.Add(311, "u8 u8 u8");
            Globals.taeParams.Add(312, "b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b");
            Globals.taeParams.Add(320, "b b b b b b b");
            Globals.taeParams.Add(330, "N");
            Globals.taeParams.Add(331, "s32 s32");
            Globals.taeParams.Add(332, "N");
            Globals.taeParams.Add(339, "s32 s32");
            Globals.taeParams.Add(340, "s32 u8 u8 u8");
            Globals.taeParams.Add(341, "N");
            Globals.taeParams.Add(342, "u16");
            Globals.taeParams.Add(343, "s32 f32 f32 f32");
            Globals.taeParams.Add(344, "f32 f32 f32 f32");
            Globals.taeParams.Add(401, "s32");
            Globals.taeParams.Add(500, "u8 u8");
            Globals.taeParams.Add(511, "u8");
            Globals.taeParams.Add(522, "f32 s32 s32 s32");
            Globals.taeParams.Add(600, "s32");
            Globals.taeParams.Add(601, "s32 f32 f32 s32");
            Globals.taeParams.Add(602, "s32 f32 f32");
            Globals.taeParams.Add(603, "u32");
            Globals.taeParams.Add(604, "s32 s32 s32");
            Globals.taeParams.Add(605, "b s32 f32 f32");
            Globals.taeParams.Add(606, "u8 u8 u8 u8 u16 u16");
            Globals.taeParams.Add(607, "s32 f32 f32 s32");
            Globals.taeParams.Add(608, "f32 f32");
            Globals.taeParams.Add(609, "u16 u16 f32 f32 s32");
            Globals.taeParams.Add(700, "f32 f32 f32 f32 s32 u8 u8 u8 u8 f32 f32 f32 f32");
            Globals.taeParams.Add(702, "f32 s32");
            Globals.taeParams.Add(703, "b");
            Globals.taeParams.Add(704, "f32 f32 f32");
            Globals.taeParams.Add(705, "f32");
            Globals.taeParams.Add(706, "f32 s32");
            Globals.taeParams.Add(707, "N");
            Globals.taeParams.Add(710, "b b b b");
            Globals.taeParams.Add(711, "b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b");
            Globals.taeParams.Add(712, "u8 s8 u8 s8 u8 s8 u8 s8 u8 s8 u8 s8 u8 s8 u8 s8 u8 u8 u8");
            Globals.taeParams.Add(713, "b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b b");
            Globals.taeParams.Add(714, "u8");
            Globals.taeParams.Add(715, "u8 s32 s32 s32 s32 s32 s32");
            Globals.taeParams.Add(716, "s32 s32 s32 s32");
            Globals.taeParams.Add(717, "f32 b u8");
            Globals.taeParams.Add(718, "N");
            Globals.taeParams.Add(730, "f32 f32");
            Globals.taeParams.Add(731, "s32");
            Globals.taeParams.Add(740, "N");
            Globals.taeParams.Add(760, "b f32 f32 f32 f32 f32");
            Globals.taeParams.Add(761, "N");
            Globals.taeParams.Add(770, "s32 f32 u8");
            Globals.taeParams.Add(771, "u8");
            Globals.taeParams.Add(781, "u8");
            Globals.taeParams.Add(782, "N");
            Globals.taeParams.Add(785, "f32 s32 s32 b u8");
            Globals.taeParams.Add(787, "f32 f32 f32 f32");
            Globals.taeParams.Add(788, "N");
            Globals.taeParams.Add(789, "N");
            Globals.taeParams.Add(790, "N");
            Globals.taeParams.Add(791, "N");
            Globals.taeParams.Add(792, "u16 s32 s32 b b b u8");
            Globals.taeParams.Add(795, "u8 u8 f32");
            Globals.taeParams.Add(800, "f32 f32 f32");
            Globals.taeParams.Add(900, "u8 u8");
            Globals.taeParams.Add(901, "u8");
            Globals.taeParams.Add(902, "s32 s32 u8");
            Globals.taeParams.Add(903, "s32 b");
            Globals.taeParams.Add(904, "s32");
            Globals.taeParams.Add(905, "N");
            Globals.taeParams.Add(906, "N");
            Globals.taeParams.Add(907, "f32 f32 f32 f32");
            Globals.taeParams.Add(908, "f32 f32 f32");
            Globals.taeParams.Add(910, "s32 s32 s32");
            Globals.taeParams.Add(911, "s32 s32");
            Globals.taeParams.Add(10096, "s32 s32 s32 s32");
            Globals.taeParams.Add(10130, "s32 s32 s32 s32");
            Globals.taeParams.Add(10137, "s32 s32 s32 s32");
            Globals.taeParams.Add(10138, "s32 s32 s32 s32");

            Globals.animIDType.Add(0, "Idle");
            Globals.animIDType.Add(9, "A Pose");
            Globals.animIDType.Add(100000, "Horseriding Idle");
            Globals.animIDType.Add(300000, "Crouching Idle");

            Globals.animIDType.Add(30000, "1H Light 1");
            Globals.animIDType.Add(30010, "1H Light 2");
            Globals.animIDType.Add(30020, "1H Light 3");
            Globals.animIDType.Add(30030, "1H Light 4");
            Globals.animIDType.Add(30040, "1H Light 5");
            Globals.animIDType.Add(30050, "1H Light 6");
            Globals.animIDType.Add(30060, "1H Light 7");
            Globals.animIDType.Add(30200, "1H Light Running");
            Globals.animIDType.Add(30210, "1H Heavy Running");
            Globals.animIDType.Add(30300, "1H Dodge or Crouch");
            Globals.animIDType.Add(30400, "1H Backstep");
            Globals.animIDType.Add(30500, "1H Heavy Charged 1");
            Globals.animIDType.Add(30501, "1H Heavy Windup");
            Globals.animIDType.Add(30505, "1H Heavy Uncharged 1");
            Globals.animIDType.Add(30510, "1H Heavy Charged 2");
            Globals.animIDType.Add(30515, "1H Heavy Uncharged 2");
            Globals.animIDType.Add(30600, "1H BR Heavy Charged 1");
            Globals.animIDType.Add(30601, "1H BR Heavy Windup");
            Globals.animIDType.Add(30605, "1H BR Heavy Uncharged 1");
            Globals.animIDType.Add(30610, "1H BR Heavy Charged 2");
            Globals.animIDType.Add(30615, "1H BR Heavy Uncharged 2");
            Globals.animIDType.Add(30620, "1H WC Heavy Charged 1");
            Globals.animIDType.Add(30621, "1H WC Heavy Windup");
            Globals.animIDType.Add(30625, "1H WC Heavy Uncharged 1");
            Globals.animIDType.Add(30630, "1H WC Heavy Charged 2");
            Globals.animIDType.Add(30635, "1H WC Heavy Uncharged 2");
            Globals.animIDType.Add(30700, "1H Guard Counter");
            Globals.animIDType.Add(31030, "1H Jumping Light");
            Globals.animIDType.Add(31040, "1H Jumping Light");
            Globals.animIDType.Add(31050, "1H Jumping Light");
            Globals.animIDType.Add(31060, "1H Jumping Light Hangtime");
            Globals.animIDType.Add(31070, "1H Jumping Light Landing");
            Globals.animIDType.Add(31071, "1H Jumping Light Landing");
            Globals.animIDType.Add(31072, "1H Jumping Light Landing");
            Globals.animIDType.Add(31081, "1H Jumping Light Landing");
            Globals.animIDType.Add(31082, "1H Jumping Light Landing");
            Globals.animIDType.Add(31230, "1H Jumping Heavy");
            Globals.animIDType.Add(31240, "1H Jumping Heavy");
            Globals.animIDType.Add(31250, "1H Jumping Heavy");
            Globals.animIDType.Add(31260, "1H Jumping Heavy Hangtime");
            Globals.animIDType.Add(31270, "1H Jumping Heavy Landing");
            Globals.animIDType.Add(31271, "1H Jumping Heavy Landing");
            Globals.animIDType.Add(31272, "1H Jumping Heavy Landing");
            Globals.animIDType.Add(31281, "1H Jumping Heavy Landing");
            Globals.animIDType.Add(31282, "1H Jumping Heavy Landing");
            Globals.animIDType.Add(31900, "1H Bounceoff Small");
            Globals.animIDType.Add(31910, "1H Bounceoff Medium");
            Globals.animIDType.Add(31920, "1H Bounceoff Large");

            Globals.animIDType.Add(30900, "1H Shieldpoke Medium");
            Globals.animIDType.Add(30901, "1H Shieldpoke Great");
            Globals.animIDType.Add(30902, "1H Shieldpoke Small");
            Globals.animIDType.Add(30903, "1H Shieldpoke Torch");
            Globals.animIDType.Add(30950, "1H Heavy Dodge Cancel 1");
            Globals.animIDType.Add(30955, "1H Heavy Dodge Cancel 2");
            Globals.animIDType.Add(32950, "2H Heavy Dodge Cancel 1");
            Globals.animIDType.Add(32955, "2H Heavy Dodge Cancel 2");

            Globals.animIDType.Add(32000, "2H Light 1");
            Globals.animIDType.Add(32010, "2H Light 2");
            Globals.animIDType.Add(32020, "2H Light 3");
            Globals.animIDType.Add(32030, "2H Light 4");
            Globals.animIDType.Add(32040, "2H Light 5");
            Globals.animIDType.Add(32050, "2H Light 6");
            Globals.animIDType.Add(32060, "2H Light 7");
            Globals.animIDType.Add(32200, "2H Light Running");
            Globals.animIDType.Add(32210, "2H Heavy Running");
            Globals.animIDType.Add(32300, "2H Dodge or Crouch");
            Globals.animIDType.Add(32400, "2H Backstep");
            Globals.animIDType.Add(32500, "2H Heavy Charged 1");
            Globals.animIDType.Add(32501, "2H Heavy Windup");
            Globals.animIDType.Add(32505, "2H Heavy Uncharged 1");
            Globals.animIDType.Add(32510, "2H Heavy Charged 2");
            Globals.animIDType.Add(32515, "2H Heavy Uncharged 2");
            Globals.animIDType.Add(32600, "2H BR Heavy Charged 1");
            Globals.animIDType.Add(32601, "2H BR Heavy Windup");
            Globals.animIDType.Add(32605, "2H BR Heavy Uncharged 1");
            Globals.animIDType.Add(32610, "2H BR Heavy Charged 2");
            Globals.animIDType.Add(32615, "2H BR Heavy Uncharged 2");
            Globals.animIDType.Add(32620, "2H WC Heavy Charged 1");
            Globals.animIDType.Add(32621, "2H WC Heavy Windup");
            Globals.animIDType.Add(32625, "2H WC Heavy Uncharged 1");
            Globals.animIDType.Add(32630, "2H WC Heavy Charged 2");
            Globals.animIDType.Add(32635, "2H WC Heavy Uncharged 2");
            Globals.animIDType.Add(32700, "2H Guard Counter");
            Globals.animIDType.Add(33030, "2H Jumping Light");
            Globals.animIDType.Add(33040, "2H Jumping Light");
            Globals.animIDType.Add(33050, "2H Jumping Light");
            Globals.animIDType.Add(33060, "2H Jumping Light Hangtime");
            Globals.animIDType.Add(33070, "2H Jumping Light Landing");
            Globals.animIDType.Add(33071, "2H Jumping Light Landing");
            Globals.animIDType.Add(33072, "2H Jumping Light Landing");
            Globals.animIDType.Add(33081, "2H Jumping Light Landing");
            Globals.animIDType.Add(33082, "2H Jumping Light Landing");
            Globals.animIDType.Add(33230, "2H Jumping Heavy");
            Globals.animIDType.Add(33240, "2H Jumping Heavy");
            Globals.animIDType.Add(33250, "2H Jumping Heavy");
            Globals.animIDType.Add(33260, "2H Jumping Heavy Hangtime");
            Globals.animIDType.Add(33270, "2H Jumping Heavy Landing");
            Globals.animIDType.Add(33271, "2H Jumping Heavy Landing");
            Globals.animIDType.Add(33272, "2H Jumping Heavy Landing");
            Globals.animIDType.Add(33281, "2H Jumping Heavy Landing");
            Globals.animIDType.Add(33282, "2H Jumping Heavy Landing");
            Globals.animIDType.Add(32900, "2H Bounceoff Small");
            Globals.animIDType.Add(32910, "2H Bounceoff Medium");
            Globals.animIDType.Add(32920, "2H Bounceoff Large");

            Globals.animIDType.Add(34000, "PS 1");
            Globals.animIDType.Add(34010, "PS 2");
            Globals.animIDType.Add(34020, "PS 3");
            Globals.animIDType.Add(34030, "PS 4");
            Globals.animIDType.Add(34040, "PS 5");
            Globals.animIDType.Add(34050, "PS 6");
            Globals.animIDType.Add(34200, "PS Running/Sprinting");
            Globals.animIDType.Add(34300, "PS Dodge or Crouch");
            Globals.animIDType.Add(34400, "PS Backstep");
            Globals.animIDType.Add(34530, "PS Jumping");
            Globals.animIDType.Add(34540, "PS Jumping");
            Globals.animIDType.Add(34550, "PS Jumping");
            Globals.animIDType.Add(34560, "PS Jumping Hangtime");
            Globals.animIDType.Add(34570, "PS Jumping Landing");
            Globals.animIDType.Add(34571, "PS Jumping Landing");
            Globals.animIDType.Add(34572, "PS Jumping Landing");
            Globals.animIDType.Add(34580, "PS Jumping Landing");
            Globals.animIDType.Add(34581, "PS Jumping Landing");
            Globals.animIDType.Add(34582, "PS Jumping Landing");

            Globals.animIDType.Add(35000, "LH Light 1");
            Globals.animIDType.Add(35010, "LH Light 2");
            Globals.animIDType.Add(35020, "LH Light 3");
            Globals.animIDType.Add(35030, "LH Light 4");
            Globals.animIDType.Add(35040, "LH Light 5");
            Globals.animIDType.Add(35050, "LH Light 6");
            Globals.animIDType.Add(35900, "LH Bounceoff Small");
            Globals.animIDType.Add(35910, "LH Bounceoff Medium");
            Globals.animIDType.Add(35920, "LH Bounceoff Large");

            Globals.animIDType.Add(38000, "Horseriding Right Light 1");
            Globals.animIDType.Add(38010, "Horseriding Right Light 2");
            Globals.animIDType.Add(38020, "Horseriding Right Light 3");
            Globals.animIDType.Add(38030, "Horseriding Right Light 4");
            Globals.animIDType.Add(38040, "Horseriding Right Light 5");
            Globals.animIDType.Add(38050, "Horseriding Right Light 6");
            Globals.animIDType.Add(38100, "Horseriding Right Charged 1");
            Globals.animIDType.Add(38110, "Horseriding Right Uncharged 1");
            Globals.animIDType.Add(38200, "Horseriding Right Light 1 Dupe(?)");
            Globals.animIDType.Add(38300, "Horseriding Right Uncharged Dupe(?)");
            Globals.animIDType.Add(38900, "Horseriding Right Bounceoff Small");
            Globals.animIDType.Add(38910, "Horseriding Right Bounceoff Medium");
            Globals.animIDType.Add(38920, "Horseriding Right Bounceoff Large");

            Globals.animIDType.Add(39000, "Horseriding Left Light 1");
            Globals.animIDType.Add(39010, "Horseriding Left Light 2");
            Globals.animIDType.Add(39020, "Horseriding Left Light 3");
            Globals.animIDType.Add(39030, "Horseriding Left Light 4");
            Globals.animIDType.Add(39040, "Horseriding Left Light 5");
            Globals.animIDType.Add(39050, "Horseriding Left Light 6");
            Globals.animIDType.Add(39100, "Horseriding Left Charged 1");
            Globals.animIDType.Add(39110, "Horseriding Left Uncharged 1");
            Globals.animIDType.Add(39200, "Horseriding Left Light 1 Dupe(?)");
            Globals.animIDType.Add(39300, "Horseriding Left Uncharged Dupe(?)");
            Globals.animIDType.Add(39900, "Horseriding Left Bounceoff Small");
            Globals.animIDType.Add(39910, "Horseriding Left Bounceoff Medium");
            Globals.animIDType.Add(39920, "Horseriding Left Bounceoff Large");

            Globals.animIDType.Add(27000, "Backstep Light");
            Globals.animIDType.Add(27010, "Backstep Medium");
            Globals.animIDType.Add(27020, "Backstep Heavy");
            Globals.animIDType.Add(27030, "Backstep Overencumbered");
            Globals.animIDType.Add(27100, "Forwards Dodge Light");
            Globals.animIDType.Add(27101, "Backwards Dodge Light");
            Globals.animIDType.Add(27102, "Leftwards Dodge Light");
            Globals.animIDType.Add(27103, "Rightwards Dodge Light");
            Globals.animIDType.Add(27104, "Blending(?) Dodge Light");
            Globals.animIDType.Add(27105, "Blending(?) Dodge Light");
            Globals.animIDType.Add(27106, "Blending(?) Dodge Light");
            Globals.animIDType.Add(27107, "Blending(?) Dodge Light");
            Globals.animIDType.Add(27110, "Forwards Dodge Medium");
            Globals.animIDType.Add(27111, "Backwards Dodge Medium");
            Globals.animIDType.Add(27112, "Leftwards Dodge Medium");
            Globals.animIDType.Add(27113, "Rightwards Dodge Medium");
            Globals.animIDType.Add(27114, "Blending(?) Dodge Medium");
            Globals.animIDType.Add(27115, "Blending(?) Dodge Medium");
            Globals.animIDType.Add(27116, "Blending(?) Dodge Medium");
            Globals.animIDType.Add(27117, "Blending(?) Dodge Medium");
            Globals.animIDType.Add(27120, "Forwards Dodge Heavy");
            Globals.animIDType.Add(27121, "Backwards Dodge Heavy");
            Globals.animIDType.Add(27122, "Leftwards Dodge Heavy");
            Globals.animIDType.Add(27123, "Rightwards Dodge Heavy");
            Globals.animIDType.Add(27124, "Blending(?) Dodge Heavy");
            Globals.animIDType.Add(27125, "Blending(?) Dodge Heavy");
            Globals.animIDType.Add(27126, "Blending(?) Dodge Heavy");
            Globals.animIDType.Add(27127, "Blending(?) Dodge Heavy");
            Globals.animIDType.Add(27130, "Forwards Dodge Overencumbered");
            Globals.animIDType.Add(27131, "Backwards Dodge Overencumbered");
            Globals.animIDType.Add(27132, "Leftwards Dodge Overencumbered");
            Globals.animIDType.Add(27133, "Rightwards Dodge Overencumbered");
            Globals.animIDType.Add(27134, "Blending(?) Dodge Overencumbered");
            Globals.animIDType.Add(27135, "Blending(?) Dodge Overencumbered");
            Globals.animIDType.Add(27136, "Blending(?) Dodge Overencumbered");
            Globals.animIDType.Add(27137, "Blending(?) Dodge Overencumbered");
            Globals.animIDType.Add(27140, "Forwards Dodge DWGR (Unused)");
            Globals.animIDType.Add(27141, "Backwards Dodge DWGR (Unused)");
            Globals.animIDType.Add(27142, "Leftwards Dodge DWGR (Unused)");
            Globals.animIDType.Add(27143, "Rightwards Dodge DWGR (Unused)");

            Globals.animIDType.Add(100, "Blocking Idle Normal/Medium Shield");
            Globals.animIDType.Add(110, "Blocking Idle Great Shield");
            Globals.animIDType.Add(120, "Blocking Idle Small Shield");
            Globals.animIDType.Add(30, "Blocking Idle Torch 1");
            Globals.animIDType.Add(40, "Blocking Idle Torch 2");
            Globals.animIDType.Add(50, "Blocking Idle Torch 3");
            Globals.animIDType.Add(19000, "Blocking Init From Idle Normal/Medium Shield");
            Globals.animIDType.Add(19001, "Blocking Init From Motion Normal/Medium Shield");
            Globals.animIDType.Add(19002, "Blocking Release from Motion Normal/Medium Shield");
            Globals.animIDType.Add(19004, "Blocking Init From Idle Normal/Medium Shield");
            Globals.animIDType.Add(19100, "Blocking Release from Idle Normal/Medium Shield");
            Globals.animIDType.Add(19010, "Blocking Init From Idle Great Shield");
            Globals.animIDType.Add(19011, "Blocking Init From Motion Great Shield");
            Globals.animIDType.Add(19012, "Blocking Release from Motion Great Shield");
            Globals.animIDType.Add(19014, "Blocking Init From Idle Great Shield");
            Globals.animIDType.Add(19110, "Blocking Release from Idle Great Shield");
            Globals.animIDType.Add(19020, "Blocking Init From Idle Small Shield");
            Globals.animIDType.Add(19021, "Blocking Init From Motion Small Shield");
            Globals.animIDType.Add(19022, "Blocking Release from Small Shield");
            Globals.animIDType.Add(19024, "Blocking Init From Idle Small Shield");
            Globals.animIDType.Add(19120, "Blocking Release from Idle Small Shield");
            Globals.animIDType.Add(19030, "Blocking Init From Idle Torch 1");
            Globals.animIDType.Add(19031, "Blocking Init From Motion Torch 1");
            Globals.animIDType.Add(19032, "Blocking Release from Torch 1");
            Globals.animIDType.Add(19034, "Blocking Init From Idle Torch 1");
            Globals.animIDType.Add(19130, "Blocking Release from Idle Torch 1");
            Globals.animIDType.Add(19040, "Blocking Init From Idle Torch 2");
            Globals.animIDType.Add(19041, "Blocking Init From Motion Torch 2");
            Globals.animIDType.Add(19042, "Blocking Release from Torch 2");
            Globals.animIDType.Add(19044, "Blocking Init From Idle Torch 2");
            Globals.animIDType.Add(19140, "Blocking Release from Idle Torch 2");
            Globals.animIDType.Add(19050, "Blocking Init From Idle Torch 3");
            Globals.animIDType.Add(19051, "Blocking Init From Motion Torch 3");
            Globals.animIDType.Add(19052, "Blocking Release from Torch 3");
            Globals.animIDType.Add(19054, "Blocking Init From Idle Torch 3");
            Globals.animIDType.Add(19150, "Blocking Release from Idle Torch 3");
            Globals.animIDType.Add(19200, "Blocking Post-Hit Animation Light Normal/Medium Shield");
            Globals.animIDType.Add(19210, "Blocking Post-Hit Animation Medium Normal/Medium Shield");
            Globals.animIDType.Add(19220, "Blocking Post-Hit Animation Heavy Normal/Medium Shield");
            Globals.animIDType.Add(19250, "Blocking Post-Hit Animation Light Small Shield");
            Globals.animIDType.Add(19260, "Blocking Post-Hit Animation Medium Small Shield");
            Globals.animIDType.Add(19270, "Blocking Post-Hit Animation Heavy Small Shield");
            Globals.animIDType.Add(19300, "Blocking Post-Hit Animation Light Great Shield");
            Globals.animIDType.Add(19310, "Blocking Post-Hit Animation Medium Great Shield");
            Globals.animIDType.Add(19320, "Blocking Post-Hit Animation Heavy Great Shield");
            Globals.animIDType.Add(19400, "Blocking Post-Hit Animation Light Torch");
            Globals.animIDType.Add(19410, "Blocking Post-Hit Animation Medium Torch");
            Globals.animIDType.Add(19420, "Blocking Post-Hit Animation Heavy Torch");
            Globals.animIDType.Add(19500, "Blocking Post-Hit Animation Guardbreak Normal/Medium Shield");
            Globals.animIDType.Add(19510, "Blocking Post-Hit Animation Guardbreak Great Shield");
            Globals.animIDType.Add(19520, "Blocking Post-Hit Animation Guardbreak Small Shield");
            Globals.animIDType.Add(19530, "Blocking Post-Hit Animation Guardbreak Torch"); // add in spells/horseback/powerstance/whatever tomorrow
            Globals.animIDType.Add(19600, "Blocking Backstep Normal/Medium Shield Light");
            Globals.animIDType.Add(19601, "Blocking Backstep Normal/Medium Shield Medium");
            Globals.animIDType.Add(19602, "Blocking Backstep Normal/Medium Shield Heavy");
            Globals.animIDType.Add(19610, "Blocking Backstep Great Shield Light");
            Globals.animIDType.Add(19611, "Blocking Backstep Great Shield Medium");
            Globals.animIDType.Add(19612, "Blocking Backstep Great Shield Heavy");
            Globals.animIDType.Add(19620, "Blocking Backstep Small Shield Light");
            Globals.animIDType.Add(19621, "Blocking Backstep Small Shield Medium");
            Globals.animIDType.Add(19622, "Blocking Backstep Small Shield Heavy");
            Globals.animIDType.Add(19630, "Blocking Backstep Small Torch 1 Light");
            Globals.animIDType.Add(19631, "Blocking Backstep Small Torch 1 Medium");
            Globals.animIDType.Add(19632, "Blocking Backstep Small Torch 1 Heavy");
            Globals.animIDType.Add(19640, "Blocking Backstep Small Torch 2 Light");
            Globals.animIDType.Add(19641, "Blocking Backstep Small Torch 2 Medium");
            Globals.animIDType.Add(19642, "Blocking Backstep Small Torch 2 Heavy");
            Globals.animIDType.Add(19650, "Blocking Backstep Small Torch 3 Light");
            Globals.animIDType.Add(19651, "Blocking Backstep Small Torch 3 Medium");
            Globals.animIDType.Add(19652, "Blocking Backstep Small Torch 3 Heavy");

            Globals.animIDType.Add(20000, "Walking Forwards Light");
            Globals.animIDType.Add(20001, "Walking Backwards Light");
            Globals.animIDType.Add(20002, "Walking Leftwards Light");
            Globals.animIDType.Add(20003, "Walking Rightwards Light");
            Globals.animIDType.Add(20010, "Walking Forwards Medium");
            Globals.animIDType.Add(20011, "Walking Backwards Medium");
            Globals.animIDType.Add(20012, "Walking Leftwards Medium");
            Globals.animIDType.Add(20013, "Walking Rightwards Medium");
            Globals.animIDType.Add(20020, "Walking Forwards Heavy");
            Globals.animIDType.Add(20021, "Walking Backwards Heavy");
            Globals.animIDType.Add(20022, "Walking Leftwards Heavy");
            Globals.animIDType.Add(20023, "Walking Rightwards Heavy");
            Globals.animIDType.Add(20100, "Running Forwards Light");
            Globals.animIDType.Add(20101, "Running Backwards Light");
            Globals.animIDType.Add(20102, "Running Leftwards Light");
            Globals.animIDType.Add(20103, "Running Rightwards Light");
            Globals.animIDType.Add(20110, "Running Forwards Medium");
            Globals.animIDType.Add(20111, "Running Backwards Medium");
            Globals.animIDType.Add(20112, "Running Leftwards Medium");
            Globals.animIDType.Add(20113, "Running Rightwards Medium");
            Globals.animIDType.Add(20120, "Running Forwards Heavy");
            Globals.animIDType.Add(20121, "Running Backwards Heavy");
            Globals.animIDType.Add(20122, "Running Leftwards Heavy");
            Globals.animIDType.Add(20123, "Running Rightwards Heavy");
            Globals.animIDType.Add(20200, "Sprinting Forwards Light");
            Globals.animIDType.Add(20210, "Sprinting Forwards Light");
            Globals.animIDType.Add(20220, "Sprinting Forwards Light");

            Globals.animIDType.Add(23000, "Blocking Walking Forwards Normal/Medium Shield");
            Globals.animIDType.Add(23010, "Blocking Walking Forwards Great Shield");
            Globals.animIDType.Add(23020, "Blocking Walking Forwards Small Shield");
            Globals.animIDType.Add(23030, "Blocking Walking Forwards Torch 1");
            Globals.animIDType.Add(23040, "Blocking Walking Forwards Torch 2");
            Globals.animIDType.Add(23050, "Blocking Walking Forwards Torch 3");
            Globals.animIDType.Add(23100, "Blocking Running Forwards Normal/Medium Shield");
            Globals.animIDType.Add(23110, "Blocking Running Forwards Great Shield");
            Globals.animIDType.Add(23120, "Blocking Running Forwards Small Shield");
            Globals.animIDType.Add(23130, "Blocking Running Forwards Torch 1");
            Globals.animIDType.Add(23140, "Blocking Running Forwards Torch 2");
            Globals.animIDType.Add(23150, "Blocking Running Forwards Torch 3");
            Globals.animIDType.Add(23200, "Blocking Sprinting Forwards Normal/Medium Shield");
            Globals.animIDType.Add(23210, "Blocking Sprinting Forwards Great Shield");
            Globals.animIDType.Add(23220, "Blocking Sprinting Forwards Small Shield");
            Globals.animIDType.Add(23230, "Blocking Sprinting Forwards Torch 1");
            Globals.animIDType.Add(23240, "Blocking Sprinting Forwards Torch 2");
            Globals.animIDType.Add(23250, "Blocking Sprinting Forwards Torch 3");

            Globals.animIDType.Add(40000, "Skill Default");
            Globals.animIDType.Add(40005, "Skill Default - No FP");
            Globals.animIDType.Add(40200, "Skill Default Over The Shoulder Idle Type");
            Globals.animIDType.Add(40205, "Skill Default Over The Shoulder Idle Type - No FP");
            Globals.animIDType.Add(40300, "Skill Default Greatspear Idle Type ");
            Globals.animIDType.Add(40305, "Skill Default Greatspear Idle Type- No FP");
            Globals.animIDType.Add(44200, "Skill Default Fist Type");
            Globals.animIDType.Add(44205, "Skill Default Fist Type - No FP");

            Globals.animIDType.Add(40010, "Skill Second");
            Globals.animIDType.Add(40015, "Skill Second - No FP");
            Globals.animIDType.Add(40210, "Skill Second Over The Shoulder Idle Type");
            Globals.animIDType.Add(40215, "Skill Second Over The Shoulder Idle Type - No FP");
            Globals.animIDType.Add(40310, "Skill Second Greatspear Idle Type ");
            Globals.animIDType.Add(40315, "Skill Second Greatspear Idle Type - No FP");
            Globals.animIDType.Add(42410, "Skill Second Twinblade (?) Idle Type ");
            Globals.animIDType.Add(42415, "Skill Second Twinblade (?) Idle Type - No FP");

            Globals.animIDType.Add(40001, "Skill Default Uncharged");
            Globals.animIDType.Add(40006, "Skill Default Uncharged - No FP");

            Globals.animIDType.Add(40050, "Skill Default Windup");
            Globals.animIDType.Add(40051, "Skill Default Continued Hold");
            Globals.animIDType.Add(40052, "Skill Default Continued Hold Moving");
            Globals.animIDType.Add(40053, "Skill Default Release");
            Globals.animIDType.Add(40056, "Skill Default Continued Hold - No FP");
            Globals.animIDType.Add(40057, "Skill Default Continued Hold Moving - No FP");
            Globals.animIDType.Add(40060, "Skill Default Continued Hold + R1");
            Globals.animIDType.Add(40062, "Skill Default Continued Hold + R1 Over The Shoulder Idle Type");
            Globals.animIDType.Add(40063, "Skill Default Continued Hold + R1 Greatspear Idle Type");
            Globals.animIDType.Add(40065, "Skill Default Continued Hold + R1 - No FP");
            Globals.animIDType.Add(40067, "Skill Default Continued Hold + R1 Over The Shoulder Idle Type - No FP");
            Globals.animIDType.Add(40068, "Skill Default Continued Hold + R1 Greatspear Idle Type - No FP");
            Globals.animIDType.Add(40070, "Skill Default Continued Hold + R2");
            Globals.animIDType.Add(40072, "Skill Default Continued Hold + R2 Over The Shoulder Idle Type");
            Globals.animIDType.Add(40073, "Skill Default Continued Hold + R2 Greatspear Idle Type");
            Globals.animIDType.Add(40075, "Skill Default Continued Hold + R2 - No FP");
            Globals.animIDType.Add(40077, "Skill Default Continued Hold + R2 Over The Shoulder Idle Type - No FP");
            Globals.animIDType.Add(40078, "Skill Default Continued Hold + R2 Greatspear Idle Type - No FP");




            Console.WriteLine("Enter the directory for your chr files? \nExample, C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game\\mod\\chr ");
            string path = Console.ReadLine();
            startScraping(path);

        }
    }
}
