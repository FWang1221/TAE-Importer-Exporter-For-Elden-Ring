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

namespace Sword_MasTAErizer
{
    class Program
    {
        static class Globals
        {
            public static Dictionary<int, string> taeArgs = new Dictionary<int, string>();

            public static void checkArgs()
            {
                string[] lines = File.ReadAllLines(@".\types\taeArgs.txt", Encoding.UTF8); //taeArgs.txt is like meow's soulsassetspipeline thing but from wish. no name information, no data type information stored in the final JSON but that's unimportant. Would most likely hinder mass editing anyways, and ppl can just look in DSAS

                foreach (string line in lines)
                {
                    Globals.taeArgs.Add(Int32.Parse(line.Split(':')[0]), line.Split(':')[1]);
                }
            }
        }
        class PlayerExport { //everything is public because apparently the json serializer doesn't write the private stuff. i have an excuse for the poor scoping being done here
            public string FileLocation { get; set; }
            public List<TAEsJSON> taeList { get; set; } = new List<TAEsJSON>();

            public class TAEsJSON { //file types not nested within each other because this project is pretty simple atm.
                public long taeID { get; set; }
                public List<AnimsJSON> animList { get; set; } = new List<AnimsJSON>();

            }

            public class AnimsJSON { //should add new variables like: full replacement (delete everything in an animation and start adding things) or slight addition (just adds an event). seek and replace isn't viable due to how weird everything is, and can be done with a full replacement operation anyways, provided that you wrote it proper
                public long animID { get; set; }

                public Dictionary<string, string> miniHeader { get; set; } = new Dictionary<string, string>();
                public List<EventsJSON> events { get; set; } = new List<EventsJSON>();
                
                public void miniHeaderToStrings(TAE.Animation animation) {

                    TAE.Animation.AnimMiniHeader animHeader = animation.MiniHeader; //no inconsistencies with these, everything is fine and dandy about whether the types are a certain way (can always handle discrepancies in the import)
                    
                    this.miniHeader.Add("Type", animHeader.Type.ToString()); 
                    this.miniHeader.Add("AllowDelayLoad", animHeader.AllowDelayLoad.ToString());
                    this.miniHeader.Add("ImportFromAnimID", animHeader.ImportFromAnimID.ToString());
                    this.miniHeader.Add("ImportHKXSourceAnimID", animHeader.ImportHKXSourceAnimID.ToString());
                    this.miniHeader.Add("ImportsHKX", animHeader.ImportsHKX.ToString());
                    this.miniHeader.Add("IsLoopByDefault", animHeader.IsLoopByDefault.ToString());

                }

            }
            public class EventsJSON { //groups and group types are not needed, and can be re-written on the import with ease and little time lost.
                public float startingTime { get; set; } //starting time of an event
                public float endingTime { get; set; } //ending time of an event
                public int eventType { get; set; } //the type of event, like 608 being animspeedgradient
                public int paramByteLength { get; set; } = 0; //0 bytes just in case of an event like 16 (no taeArgs) happening
                public List<string> paramsAsStrings { get; set; } = new List<string>(); //the params of the event, like the f32 f32 of a 1.4x->1.7x speed event being "1.4" "1.7"
                public void evParamsToStrings(TAE.Event eventparams, bool bigEndian) {

                    List<string> paramTypes = new List<string>();
                    if (Globals.taeArgs.ContainsKey(this.eventType))
                    {
                        paramTypes = Globals.taeArgs[this.eventType].Split(',').ToList();
                    }
                    else {
                        paramsAsStrings.Add(eventparams.GetParameterBytes(bigEndian).ToString()); //if the event is like 113 where the tae event has not been figured out and the byte format has not been figured out either, then write the bytes as is in string format.
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

                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    int translatedID = taeFile.ID - 5000000;
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
                            var thisEV = new EventsJSON();

                            thisEV.startingTime = ev.StartTime;
                            thisEV.endingTime = ev.EndTime;
                            thisEV.eventType = ev.Type;

                            thisEV.evParamsToStrings(ev, tae.BigEndian);
                            thisAnim.events.Add(thisEV);
                        }

                        thisTAE.animList.Add(thisAnim);
                    }

                    this.taeList.Add(thisTAE);

                }
            }
            
        }
        static void Main(string[] args)
        {
            string c0000Location = $"{AppDomain.CurrentDomain.BaseDirectory}\\workstation\\c0000.anibnd.dcx"; //hardcoded location, the workstation folder of this console app being the place to put your anibnd

            if (!File.Exists(c0000Location))
            {
                Console.WriteLine("The c0000 file in the workstation folder of this directory is not found. Press ENTER to exit."); //file not there or not named c0000.anibnd.dcx? tell the person to buzz off
                Console.ReadLine();
                Environment.Exit(0);
            }

            Globals.checkArgs(); //loads in the taeArgs.txt in the global class

            XmlDocument doc = new XmlDocument();
            doc.Load("workstation\\configuration.xml");
            XmlNode taesNode = doc.DocumentElement.SelectSingleNode("/configuration/taes");
            XmlNode animsNode = doc.DocumentElement.SelectSingleNode("/configuration/anims");

            string[] taeListStr = taesNode.Attributes["bounds"].Value.Split(',');
            List<int> taeList = new List<int>();
            foreach (string taeBound in taeListStr) {
                taeList.Add(Int32.Parse(taeBound));
            }

            int animLowerBound = Int32.Parse(animsNode.Attributes["boundsLower"].Value); //acquires the searching queries
            int animHigherBound = Int32.Parse(animsNode.Attributes["boundsUpper"].Value);

            PlayerExport c0000OBJtoJSON = new PlayerExport();
            c0000OBJtoJSON.FileLocation = c0000Location;
            c0000OBJtoJSON.scanThrough(taeList, animLowerBound, animHigherBound);

            string jsonString = JsonSerializer.Serialize(c0000OBJtoJSON); //gets the json

            File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\workstation\\c0000.JSON", jsonString); //writes the json to a hardcoded location

        }
    }
}
