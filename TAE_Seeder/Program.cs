using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System.IO;

namespace TAE_Seeder //functional based program woot woot
{
    class Program
    {
        static class Globals
        {
            public static bool isSeed = false;
            public static int paramLength = 0;
            public static List<string> header;
            public static List<string> headerDataTypes;

            public static Dictionary<int, string> taeArgs = new Dictionary<int, string>();
            public static List<string> eventSeedMaker = new List<string>();

            public static void checkArgs()
            {
                string[] lines = File.ReadAllLines(@".\types\taeArgs.txt", Encoding.UTF8);

                foreach (string line in lines)
                {
                    Globals.taeArgs.Add(Int32.Parse(line.Split(':')[0]), line.Split(':')[1]);
                }
            }
        }
        static void extractParamsMany(string directoryHere, string oldDir, List<string> paramTypes, int eventTypeNum)
        {

            BND4 bnd = BND4.Read(oldDir + "/" + directoryHere + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            List<string> eventReader = new List<string>();
            eventReader.Add("Name, Animation ID, EventType, StartTime, EndTime, " + string.Join(",", paramTypes));
            
            List<string> eventReaderLine = new List<string>();
            foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
            {
                TAE tae = TAE.Read(taeFile.Bytes);
                for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                {
                    TAE.Animation anim = tae.Animations[i1];
                    for (int i = 0; i < anim.Events.Count; i++)
                    {
                        TAE.Event ev = anim.Events[i];

                        if (ev.Type == eventTypeNum)
                        {

                            int placeKeeper = 0;

                            eventReaderLine.Clear();

                            eventReaderLine.Add(directoryHere);

                            eventReaderLine.Add(anim.ID.ToString());
                            eventReaderLine.Add(ev.Type.ToString());
                            eventReaderLine.Add(ev.StartTime.ToString());
                            eventReaderLine.Add(ev.EndTime.ToString());

                            foreach (string dataType in paramTypes)
                            {
                                if (dataType == "s32")
                                {
                                    eventReaderLine.Add(BitConverter.ToInt32(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 4;
                                }
                                if (dataType == "s16")
                                {
                                    eventReaderLine.Add(BitConverter.ToInt16(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 2;
                                }
                                if (dataType == "f32")
                                {
                                    eventReaderLine.Add(BitConverter.ToSingle(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 4;
                                }
                                if (dataType == "b")
                                {
                                    eventReaderLine.Add(BitConverter.ToBoolean(ev.GetParameterBytes(tae.BigEndian), placeKeeper).ToString());
                                    placeKeeper += 1;
                                }
                                if (dataType == "u8")
                                {
                                    eventReaderLine.Add(ev.GetParameterBytes(tae.BigEndian)[placeKeeper].ToString());
                                    placeKeeper += 1;
                                }
                            }
                            eventReader.Add(string.Join(",", eventReaderLine));
                            Globals.eventSeedMaker.Add(string.Join(",", eventReaderLine));
                        }

                    }
                }
            }
            File.WriteAllLines(oldDir + "/" + directoryHere + "EventsFile.csv", eventReader.Select(x => x.ToString()));
            Console.WriteLine("Done");
        }


        static void extractParams(string oldPath)
        {
            List<string> paramTypes = new List<string>();
            Console.WriteLine("Which event type do you wish to extract the information of? \nFor example, 0 is JumpTable, 1 is invokeAttack, 608 is animspeedgradient, etc...");
            int eventTypeNum = Convert.ToInt32(Console.ReadLine());
            Globals.checkArgs();
            paramTypes = Globals.taeArgs[eventTypeNum].Split(',').ToList();
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to export.");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            Globals.eventSeedMaker.Add("Name, Animation ID, EventType, StartTime, EndTime, " + string.Join(",", paramTypes));
            foreach (string line in lines)
            {
                extractParamsMany(line, oldPath, paramTypes, eventTypeNum);
            }
            File.WriteAllLines(oldPath + "\\ExportedSeeds.csv", Globals.eventSeedMaker.Select(x => x.ToString()));
            Console.WriteLine("Seeded");
            Console.WriteLine("ENTER to exit.");
            Console.ReadLine();
            System.Environment.Exit(1);
        }
        static void prepTheTAEs(List<string> header) { //now that i'm older and a fair bit smarter (hopefully) i realize that i coulda just changed the defs of my soulsformats to be quicker about this. SMH.

            Globals.paramLength = 0;
            
            foreach (string column in header)
            {
                switch (column) {
                    case ("s8"):
                        Globals.paramLength += 1;
                        break;
                    case ("s16"):
                        Globals.paramLength += 2;
                        break;
                    case ("s32"):
                        Globals.paramLength += 4;
                        break;
                    case ("f8"):
                        Globals.paramLength += 1;
                        break;
                    case ("f16"):
                        Globals.paramLength += 2;
                        break;
                    case ("f32"):
                        Globals.paramLength += 4;
                        break;
                    case ("b"):
                        Globals.paramLength += 1;
                        break;
                    case ("u8"):
                        Globals.paramLength += 1;
                        break;
                    case ("u16"):
                        Globals.paramLength += 2;
                        break;
                    case ("u32"):
                        Globals.paramLength += 4;
                        break;

                    default:
                        Console.WriteLine("An unknown data type has arisen. The datatype is as follows: " + column + "\nEnter to continue");
                        Console.ReadLine();
                        break;

                }

            }

        }

        static byte[] taeEventMaker(string line) {
            string[] paramLines = line.Split(',').Skip(5).ToArray();
            byte[] rv = new byte[Globals.paramLength];
            int offset = 0;
            int i = 0;

            foreach (string dataType in Globals.headerDataTypes)
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
                        Console.WriteLine("An unknown data type has arisen. The datatype is as follows: " + dataType + "\nEnter to continue");
                        Console.ReadLine();
                        break;
                }
                i += 1;
            }
            return rv;
        }

        static void importParamsSeed(string oldPath)
        {
            Console.WriteLine("What is the seed's name? Example: SwordMasSeed");
            string seedName = Console.ReadLine() + ".csv";
            string path = oldPath + "\\" + seedName;
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            
            Globals.header = lines[0].Split(',').ToList();
            Globals.headerDataTypes = Globals.header.Skip(5).ToList();
            prepTheTAEs(Globals.headerDataTypes);
            String prevLine = lines[1].Substring(0, 5);

            lines.Add("c2010,0,0,0,0" + String.Concat(Enumerable.Repeat(",0", Globals.headerDataTypes.Count()))); //off by 1 error fix

            BND4 bnd = BND4.Read(oldPath + "\\" + prevLine + ".anibnd.dcx");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + prevLine + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            
            
            int lineIterator = 0;
            foreach (string line in lines)
            {
                if (lineIterator == 0) { //skips doing something stupid
                    lineIterator += 1;
                    continue;
                }
                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    TAE tae = TAE.Read(taeFile.Bytes);
                    string[] dataLine = line.Split(',');
                    foreach (TAE.Animation anim in tae.Animations)
                    {
                        if (anim.ID.ToString() == dataLine[1])
                        {
                            TAE.Event addedEvent = new TAE.Event(float.Parse(dataLine[3]), float.Parse(dataLine[4]), int.Parse(dataLine[2]), 0, taeEventMaker(line), tae.BigEndian);
                            addedEvent.Group = new TAE.EventGroup();
                            addedEvent.Group.GroupType = long.Parse(dataLine[2]);
                            anim.Events.Add(addedEvent);
                            break;
                        }
                    }
                    if (lineIterator + 1 >= lines.Count())
                    {
                        taeFile.Bytes = tae.Write();
                        Console.WriteLine("TAE has been written over.");
                    }
                    else if (lines[lineIterator + 1] != line) {
                        taeFile.Bytes = tae.Write();
                        Console.WriteLine("TAE has been written over.");
                    }
                    
                }
                if (line.Substring(0, 5) != prevLine && line[0] == 'c') //logic that determines which file is up next
                {
                    bnd.Write(oldPath + "\\" + prevLine + ".anibnd.dcx", DCX.Type.DCX_KRAK);
                    Console.WriteLine("File has been written over at " + oldPath + "\\" + prevLine + ".anibnd.dcx");

                    prevLine = line.Substring(0, 5);
                    bnd = BND4.Read(oldPath + "\\" + prevLine + ".anibnd.dcx");
                    taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
                    Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + prevLine + ".anibnd.dcx");
                    
                }
                lineIterator += 1;
            }
            
        }

        static void importParams(string oldPath) {
            if (Globals.isSeed)
            {
                importParamsSeed(oldPath);
            }

            Console.WriteLine("Operation completed. Have a nice day! ENTER to exit.");
            Console.ReadLine();
            System.Environment.Exit(1);
        }


        static void startScraping(string oldPath) {
            

            Console.WriteLine("There are 2 functions available. Export (exports all info about a certain event for a list of anibnds) and Seed (imports in a seed file, usually generated off of an exported seed.) \n (e/s)");
            string answer = Console.ReadLine();
            if (answer.ToLower() == "s")
            {
                Globals.isSeed = true;
                importParams(oldPath);
            }
            if (answer.ToLower() == "e")
            {
                extractParams(oldPath);
            }

        }
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the directory for your chr files? \nExample, C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game\\mod\\chr ");
            string path = Console.ReadLine();
            startScraping(path);

        }
    }
}