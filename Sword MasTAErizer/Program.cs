using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using java.nio.charset;
using Newtonsoft.Json;

namespace Sword_MasTAErizer
{
    class Program
    {
        static class Globals
        {
            public static bool replacementExport = true;
            public static List<string> eventListGet = new List<string>();

            public static Dictionary<int, string> taeArgs = new Dictionary<int, string>();
            public static void checkArgs()
            {
                string[] lines = File.ReadAllLines(@".\types\taeArgs.txt", Encoding.UTF8); //taeArgs.txt is like meow's soulsassetspipeline thing but from wish. no name information, no data type information stored in the final JSON but that's unimportant. Would most likely hinder mass editing anyways, and ppl can just look in DSAS

                foreach (string line in lines)
                {
                    Globals.taeArgs.Add(Int32.Parse(line.Split(':')[0]), line.Split(':')[1]);
                }
            }

            public static string taeFilePrefix = "N:\\GR\\data\\INTERROOT_win64\\chr\\c0000\\tae\\a";
            public static string taeFileSuffix = ".tae"; //a complete file name would look like "N:\\GR\\data\\INTERROOT_win64\\chr\\c0000\\tae\\a26.tae"
        }
        public class Variable
        {
            public object Value { get; set; } //dumb lil hack to get miniheader data type as <string, var>
        }

        static byte[] taeEventMaker(int eventType, int paramLength, string[] paramLines)
        {

            byte[] rv = new byte[paramLength];
            int offset = 0;
            int i = 0;

            List<string> currentTaeArgs = Globals.taeArgs[eventType].Split(',').ToList();

            foreach (string dataType in currentTaeArgs)
            {
                switch (dataType)
                {
                    case ("s32"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(Int32.Parse(paramLines[i])), 0, rv, offset, 4);
                        offset += 4;
                        break;
                    case ("s16"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(Int16.Parse(paramLines[i])), 0, rv, offset, 2);
                        offset += 2;
                        break;
                    case ("s8"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(SByte.Parse(paramLines[i])), 0, rv, offset, 1);
                        offset += 1;
                        break;
                    case ("f32"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(Single.Parse(paramLines[i])), 0, rv, offset, 4);
                        offset += 4;
                        break;
                    case ("b"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(Boolean.Parse(paramLines[i])), 0, rv, offset, 1);
                        offset += 1;
                        break;
                    case ("u8"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(Byte.Parse(paramLines[i])), 0, rv, offset, 1);
                        offset += 1;
                        break;
                    case ("u16"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(UInt16.Parse(paramLines[i])), 0, rv, offset, 2);
                        offset += 2;
                        break;
                    case ("u32"):
                        System.Buffer.BlockCopy(BitConverter.GetBytes(UInt32.Parse(paramLines[i])), 0, rv, offset, 4);
                        offset += 4;
                        break;

                    default:
                        //Console.WriteLine("An unknown data type has arisen. The datatype is as follows: " + dataType + "\nEnter to continue");
                        //Console.ReadLine();
                        break;
                }
                i += 1;
            }
            return rv;
        }
        class PlayerExport { //everything is public because apparently the json serializer doesn't write the private stuff. i have an excuse for the poor scoping being done here
            public string FileLocation { get; set; }
            public List<long> taeIDs { get; set; } = new List<long>(); //store a list of the tae IDs gotten in the player export for easy access
            public List<TAEsJSON> taeList { get; set; } = new List<TAEsJSON>();

            public class TAEsJSON { //file types not nested within each other because this project is pretty simple atm.
                public long taeID { get; set; }
                public List<long> animIDs { get; set; } = new List<long>(); //store a list of the anim IDs gotten in the player export for easy access
                public List<AnimsJSON> animList { get; set; } = new List<AnimsJSON>();

            }

            public class AnimsJSON { //should add new variables like: full replacement (delete everything in an animation and start adding things) or slight addition (just adds an event). seek and replace isn't viable due to how weird everything is, and can be done with a full replacement operation anyways, provided that you wrote it proper
                public long animID { get; set; }

                public bool fullReplaceAnim { get; set; } = Globals.replacementExport;

                public Dictionary<string, Variable> miniHeader { get; set; } = new Dictionary<string, Variable> {
                    {"Type", new Variable()},
                    {"AllowDelayLoad", new Variable()},
                    {"ImportFromAnimID", new Variable()},
                    {"ImportHKXSourceAnimID", new Variable()},
                    {"ImportsHKX", new Variable()},
                    {"IsLoopByDefault", new Variable()}
                };
                public List<EventsJSON> events { get; set; } = new List<EventsJSON>();
                
                public void miniHeaderToStrings(TAE.Animation animation) {

                    TAE.Animation.AnimMiniHeader animHeader = animation.MiniHeader; //no inconsistencies with these, everything is fine and dandy about whether the types are a certain way (can always handle discrepancies in the import)
                    
                    this.miniHeader["Type"].Value = animHeader.Type; 
                    this.miniHeader["AllowDelayLoad"].Value = animHeader.AllowDelayLoad;
                    this.miniHeader["ImportFromAnimID"].Value = animHeader.ImportFromAnimID;
                    this.miniHeader["ImportHKXSourceAnimID"].Value = animHeader.ImportHKXSourceAnimID;
                    this.miniHeader["ImportsHKX"].Value = animHeader.ImportsHKX;
                    this.miniHeader["IsLoopByDefault"].Value = animHeader.IsLoopByDefault;

                }

            }
            public class EventsJSON { //groups and group types are not needed, and can be re-written on the import with ease and little time lost.
                public float startingTime { get; set; } //starting time of an event
                public float endingTime { get; set; } //ending time of an event
                public int eventType { get; set; } //the type of event, like 608 being animspeedgradient
                public int paramByteLength { get; set; } = 0; //0 bytes just in case of an event like 16 (no taeArgs) happening
                public bool unkEvent { get; set; } = false;
                public List<string> paramsAsStrings { get; set; } = new List<string>(); //the params of the event, like the f32 f32 of a 1.4x->1.7x speed event being "1.4" "1.7"
                public void evParamsToStrings(TAE.Event eventparams, bool bigEndian) {

                    List<string> paramTypes = new List<string>();
                    if (Globals.taeArgs.ContainsKey(this.eventType))
                    {
                        paramTypes = Globals.taeArgs[this.eventType].Split(',').ToList();
                    }
                    else {
                        byte[] eventBytes = eventparams.GetParameterBytes(bigEndian);
                        paramsAsStrings.Add(Convert.ToBase64String(eventBytes)); //if the event is like 113 where the tae event has not been figured out and the byte format has not been figured out either, then write the bytes as is in string format.
                        unkEvent = true;
                        return;
                    } //add another variable indicating whether an event is a known event or an unknown event
                    int placeKeeper = 0;

                    foreach (string dataType in paramTypes) //iterate through the param type, adding paramType length, as well as the arguments in string form
                    {
                        switch (dataType) //probably should be its own method
                        {
                            case ("s8"):
                                this.paramsAsStrings.Add(((sbyte) eventparams.GetParameterBytes(bigEndian)[placeKeeper]).ToString());
                                placeKeeper += 1;
                                break;
                            case ("s16"):
                                this.paramsAsStrings.Add(BitConverter.ToInt16(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 2;
                                break;
                            case ("s32"):
                                this.paramsAsStrings.Add(BitConverter.ToInt32(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 4;
                                break;
                            case ("f32"):
                                this.paramsAsStrings.Add(BitConverter.ToSingle(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 4;
                                break;
                            case ("b"):
                                this.paramsAsStrings.Add(BitConverter.ToBoolean(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 1;
                                break;
                            case ("u8"):
                                this.paramsAsStrings.Add(eventparams.GetParameterBytes(bigEndian)[placeKeeper].ToString());
                                placeKeeper += 1;
                                break;
                            case ("u16"):
                                this.paramsAsStrings.Add(BitConverter.ToUInt16(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 2;
                                break;
                            case ("u32"):
                                this.paramsAsStrings.Add(BitConverter.ToUInt32(eventparams.GetParameterBytes(bigEndian), placeKeeper).ToString());
                                placeKeeper += 4;
                                break;
                            default:
                                //Console.WriteLine("An unknown data type has arisen. The datatype is as follows: " + dataType.ToString() + "\nEnter to continue");
                                //Console.ReadLine();
                                break;
                        }
                    }

                    this.paramByteLength = placeKeeper;

                }

            }
            public void scanThrough(List<int> taeList, int lowerBound, int upperBound)
            {
                BND4 c0000Main = BND4.Read(this.FileLocation);
                int taeLowerBound = taeList.Min();
                int taeUpperBound = taeList.Max();
                //get rid of this horrible fucking super nest of foreaches

                IEnumerable<BinderFile> taeFiles = c0000Main.Files.Where(x => x.Name.Contains(".tae"));

                /*IEnumerable<BinderFile> taeFilesThing = c0000Main.Files;

                foreach (var filething in taeFilesThing) {
                    Console.WriteLine(filething.Name);
                }*/

                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    int translatedID = taeFile.ID - 5000000; //for some reason, player TAEs are offset by 5 million
                    if (translatedID < taeLowerBound || !taeList.Contains(translatedID))
                    {
                        continue;
                    }
                    if (translatedID > taeUpperBound) {
                        break;
                    }
                    TAE tae = TAE.Read(taeFile.Bytes);
                    TAEsJSON thisTAE = new TAEsJSON();
                    thisTAE.taeID = translatedID;
                    Console.WriteLine("tae: " + tae.ID);

                    foreach (var anim in tae.Animations)
                    {
                        if (anim.ID < lowerBound) {
                            continue;
                        }
                        if (anim.ID > upperBound)
                        {
                            break;
                        }
                        var thisAnim = new AnimsJSON();
                        thisAnim.animID = anim.ID;
                        Console.WriteLine("anim: " + anim.ID);
                        thisAnim.miniHeaderToStrings(anim);

                        foreach (var ev in anim.Events) //for every event in an anim in a tae in the player, do these following things
                        {
                            if (!Globals.replacementExport && !Globals.eventListGet.Contains(ev.Type.ToString())) {
                                continue;
                            }

                            var thisEV = new EventsJSON();

                            thisEV.startingTime = ev.StartTime;
                            thisEV.endingTime = ev.EndTime;
                            thisEV.eventType = ev.Type;

                            thisEV.evParamsToStrings(ev, tae.BigEndian);
                            thisAnim.events.Add(thisEV);
                        }

                        thisTAE.animList.Add(thisAnim);
                        thisTAE.animIDs.Add(thisAnim.animID);
                    }

                    this.taeList.Add(thisTAE);
                    this.taeIDs.Add(thisTAE.taeID);

                }
            }
            
        }

        static string checkPlayerExists() {
            string c0000Location = $"{AppDomain.CurrentDomain.BaseDirectory}\\workstation\\c0000.anibnd.dcx"; //hardcoded location, the workstation folder of this console app being the place to put your anibnd

            if (!File.Exists(c0000Location))
            {
                Console.WriteLine("The c0000 file in the workstation folder of this directory is not found. Press ENTER to exit."); //file not there or not named c0000.anibnd.dcx? tell the person to buzz off
                Console.ReadLine();
                Environment.Exit(0);
            }

            return c0000Location;
        }

        static void exportProcess(XmlDocument doc) {

            XmlNode taesNode = doc.DocumentElement.SelectSingleNode("/configuration/export/taes");
            XmlNode animsNode = doc.DocumentElement.SelectSingleNode("/configuration/export/anims");
            XmlNode modeNode = doc.DocumentElement.SelectSingleNode("/configuration/export/mode");

            string[] taeListStr = taesNode.Attributes["bounds"].Value.Split(',');
            List<int> taeList = new List<int>();
            foreach (string taeBound in taeListStr)
            {
                taeList.Add(Int32.Parse(taeBound));
            }

            int animLowerBound = Int32.Parse(animsNode.Attributes["boundsLower"].Value); //acquires the searching queries
            int animHigherBound = Int32.Parse(animsNode.Attributes["boundsUpper"].Value);

            Globals.replacementExport = Boolean.Parse(modeNode.Attributes["replacementExport"].Value); //changes which mode the exporter goes by, full animations or events
            if (!Globals.replacementExport)
            {
                Globals.eventListGet = modeNode.Attributes["exportEventList"].Value.Split(',').ToList();
            }

            string c0000Location = checkPlayerExists();

            PlayerExport c0000OBJtoJSON = new PlayerExport();
            c0000OBJtoJSON.FileLocation = c0000Location;
            c0000OBJtoJSON.scanThrough(taeList, animLowerBound, animHigherBound);

            string jsonString = System.Text.Json.JsonSerializer.Serialize(c0000OBJtoJSON); //gets the json

            File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\workstation\\c0000.JSON", jsonString); //writes the json to a hardcoded location
        }

        static void importProcess() {

            string c0000Location = checkPlayerExists();
            PlayerExport importInstructionals = new PlayerExport();
            using (StreamReader jsonFile = new StreamReader($"{AppDomain.CurrentDomain.BaseDirectory}\\workstation\\c0000.JSON"))
            {
                string json = jsonFile.ReadToEnd();
                importInstructionals = JsonConvert.DeserializeObject<PlayerExport>(json);
                Console.WriteLine(importInstructionals.FileLocation.ToString());
            }

            BND4 c0000Main = BND4.Read(importInstructionals.FileLocation);

            int taesToEdit = importInstructionals.taeIDs.Count();
            for (int i = 0; i < taesToEdit; i++) //i represents the tae index we're on
            {
                string taeFileName = String.Concat(Globals.taeFilePrefix, importInstructionals.taeIDs[i].ToString(), Globals.taeFileSuffix);
                var taeFileMatch = c0000Main.Files.Where(x => x.Name.Equals(taeFileName));
                Console.WriteLine(taeFileName + " is being edited");


                foreach (var taeFile in taeFileMatch) { //gets the file named a26.tae

                    TAE tae = TAE.Read(taeFile.Bytes); //reads the file and puts every bit of its anims in a dictionary called taeAnimContainer
                    Dictionary<long, TAE.Animation> taeAnimContainer = new Dictionary<long, TAE.Animation>();

                    foreach (var anim in tae.Animations) {
                        taeAnimContainer.Add(anim.ID, anim);
                    }

                    int animListLen = importInstructionals.taeList[i].animIDs.Count();

                    for (int j = 0; j < animListLen; j++) { //j represents anim index in the tae
                        PlayerExport.AnimsJSON animInstruction = importInstructionals.taeList[i].animList[j];

                        TAE.Animation currentAnim = taeAnimContainer[animInstruction.animID]; //changes the miniheader of the animation to match the proper things

                        if ((Int64) animInstruction.miniHeader["Type"].Value == 0) //miniheadertype has to be a type or something
                        {
                            currentAnim.MiniHeader.Type = TAE.Animation.MiniHeaderType.Standard;
                        }
                        else
                        {
                            currentAnim.MiniHeader.Type = TAE.Animation.MiniHeaderType.ImportOtherAnim;
                        }

                        currentAnim.MiniHeader.AllowDelayLoad = (bool) animInstruction.miniHeader["AllowDelayLoad"].Value;
                        currentAnim.MiniHeader.ImportFromAnimID = (int)(long) animInstruction.miniHeader["ImportFromAnimID"].Value;
                        currentAnim.MiniHeader.ImportHKXSourceAnimID = (int)(long) animInstruction.miniHeader["ImportHKXSourceAnimID"].Value;
                        currentAnim.MiniHeader.ImportsHKX = (bool) animInstruction.miniHeader["ImportsHKX"].Value;
                        currentAnim.MiniHeader.IsLoopByDefault = (bool) animInstruction.miniHeader["IsLoopByDefault"].Value;

                        bool isFullReplace = animInstruction.fullReplaceAnim;

                        if (isFullReplace)
                        {
                            currentAnim.EventGroups.Clear();
                            currentAnim.Events.Clear();

                            int eventCount = animInstruction.events.Count();
                            for (int k = 0; k < eventCount; k++)
                            { //k represents event index in the tae

                                PlayerExport.EventsJSON currentEvent = animInstruction.events[k];

                                TAE.Event newEvent;

                                if (currentEvent.unkEvent) //if it's an unknown event, then convert the param string from string base64 to byte array
                                {
                                    newEvent = new TAE.Event(currentEvent.startingTime, currentEvent.endingTime, currentEvent.eventType, 0, Convert.FromBase64String(currentEvent.paramsAsStrings[0]), tae.BigEndian);
                                }
                                else
                                {
                                    newEvent = new TAE.Event(currentEvent.startingTime, currentEvent.endingTime, currentEvent.eventType, 0, taeEventMaker(currentEvent.eventType, currentEvent.paramByteLength, currentEvent.paramsAsStrings.ToArray()), tae.BigEndian);
                                }

                                currentAnim.Events.Add(newEvent);
                                currentAnim.EventGroups.Add(new TAE.EventGroup(currentEvent.eventType));
                            }
                        }
                        else {
                            int eventCount = animInstruction.events.Count();
                            for (int k = 0; k < eventCount; k++)
                            { //k represents event index in the tae

                                PlayerExport.EventsJSON currentEvent = animInstruction.events[k];

                                TAE.Event newEvent;

                                if (currentEvent.unkEvent) //if it's an unknown event, then convert the param string from string base64 to byte array
                                {
                                    newEvent = new TAE.Event(currentEvent.startingTime, currentEvent.endingTime, currentEvent.eventType, 0, Convert.FromBase64String(currentEvent.paramsAsStrings[0]), tae.BigEndian);
                                }
                                else
                                {
                                    newEvent = new TAE.Event(currentEvent.startingTime, currentEvent.endingTime, currentEvent.eventType, 0, taeEventMaker(currentEvent.eventType, currentEvent.paramByteLength, currentEvent.paramsAsStrings.ToArray()), tae.BigEndian);
                                }

                                currentAnim.Events.Add(newEvent);
                                currentAnim.EventGroups.Add(new TAE.EventGroup(currentEvent.eventType));
                            }
                        }

                    }
                    taeFile.Bytes = tae.Write();
                    Console.WriteLine("TAE edited");

                    c0000Main.Write(c0000Location);

                }
            }

        }
        static void Main(string[] args)
        {
            Globals.checkArgs(); //loads in the taeArgs.txt in the global class

            XmlDocument doc = new XmlDocument();
            if (!File.Exists("workstation\\configuration.xml"))
            {
                Console.WriteLine("The configuration file in the workstation folder of this directory is not found. Press ENTER to exit."); //file not there or not named c0000.anibnd.dcx? tell the person to buzz off
                Console.ReadLine();
                Environment.Exit(0);
            }
            doc.Load("workstation\\configuration.xml");

            XmlNode isExport = doc.DocumentElement.SelectSingleNode("/configuration/export");
            XmlNode isImport = doc.DocumentElement.SelectSingleNode("/configuration/import");

            if (Boolean.Parse(isExport.Attributes["isExport"].Value))
            {
                exportProcess(doc);
            }
            else if (Boolean.Parse(isImport.Attributes["isImport"].Value))
            {
                importProcess();
            }


        }
    }
}
