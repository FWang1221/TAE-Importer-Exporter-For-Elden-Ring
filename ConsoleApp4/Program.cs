using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System.IO;

namespace ConsoleApp4 //Sorry to anyone who wants to read code written by a fucking monkey.
{
    class Program
    {
        static class Globals
        {
            public static bool isSeed = false;
        }
        static void extractParamsMany(string directoryHere, string oldDir, List<string> paramTypes, int eventTypeNum)
        {
            
            BND4 bnd = BND4.Read(oldDir + "/" + directoryHere + ".anibnd.dcx");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            List<string> eventReader = new List<string>();
            eventReader.Add("Animation ID, StartTime, EndTime, " + string.Join(",", paramTypes));
            //List<string> eventGroupReader = new List<string>();
            //eventGroupReader.Add("GroupType, GroupData.Area, GroupData.Block, GroupData.CutsceneEntityIDPart1, GroupData.CutsceneEntityIDPart2, GroupData.CutsceneEntityType, GroupData.DataType");
            List<string> eventReaderLine = new List<string>();
            //List<string> eventGroupReaderLine = new List<string>();
            foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
            {
                TAE tae = TAE.Read(taeFile.Bytes);
                for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                {
                    TAE.Animation anim = tae.Animations[i1];
                    /*for (int i = 0; i < anim.EventGroups.Count; i++)
                    {
                        eventGroupReaderLine.Clear();
                        TAE.EventGroup evg = anim.EventGroups[i];
                        eventGroupReaderLine.Add(evg.GroupType.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.Area.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.Block.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.CutsceneEntityIDPart1.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.CutsceneEntityIDPart2.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.CutsceneEntityType.ToString());
                        eventGroupReaderLine.Add(evg.GroupData.DataType.ToString());
                        eventGroupReader.Add(string.Join(",", eventGroupReaderLine));
                    }*/
                    for (int i = 0; i < anim.Events.Count; i++)
                    {
                        TAE.Event ev = anim.Events[i];

                        if (ev.Type == eventTypeNum)
                        {

                            int placeKeeper = 0;

                            eventReaderLine.Clear();

                            eventReaderLine.Add(anim.ID.ToString());
                            eventReaderLine.Add(ev.StartTime.ToString());
                            eventReaderLine.Add(ev.EndTime.ToString());
                            //eventReaderLine.Add((Math.Floor(Math.Round(ev.StartTime * 30) / 3) * 3).ToString());
                            //eventReaderLine.Add((Math.Floor(Math.Round(ev.EndTime * 30) / 3) * 3).ToString());

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
                        }

                    }
                }
            }
            eventReader.Add(directoryHere);
            File.WriteAllLines(oldDir + "/" + directoryHere + "EventsFile.csv", eventReader.Select(x => x.ToString()));
            //File.WriteAllLines(oldDir + "/" + directoryHere + "EventGroupsFile.csv", eventGroupReader.Select(x => x.ToString()));
        }
        static void extractParams(string oldPath)
        {
            List<string> paramTypes = new List<string>();
            Console.WriteLine("Which event type do you wish to extract the information of? \nFor example, 0 is JumpTable, 1 is invokeAttack, 608 is animspeedgradient, etc...");
            int eventTypeNum = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Click that event in DSAS. See them numbers on the lower right side? s16, u8, s32, f32, all that? \nWell just input them, like for JumpTable, event 0, input s32 f32 s32 u8 u8 s16");
            paramTypes = Console.ReadLine().Split().ToList();
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to export.");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            foreach (string line in lines)
            {
                extractParamsMany(line, oldPath, paramTypes, eventTypeNum);
            }
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
                            }
                            TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[2]), float.Parse(line.Split(',')[3]), int.Parse(line.Split(',')[1]), 0, rv, tae.BigEndian);
                            addedEvent.Group = new TAE.EventGroup();
                            addedEvent.Group.GroupType = long.Parse(line.Split(',')[1]);
                            anim.Events.Add(addedEvent);
                            Console.WriteLine("New event type " + (float.Parse(line.Split(',')[1]).ToString() + " added at " + float.Parse(line.Split(',')[2]) + " seconds and ending at " + float.Parse(line.Split(',')[3]) + " seconds in in animation " + anim.ID.ToString()));
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
                            }
                            TAE.Event addedEvent = new TAE.Event(float.Parse(line.Split(',')[2]), float.Parse(line.Split(',')[3]), int.Parse(line.Split(',')[1]), 0, rv, tae.BigEndian);
                            addedEvent.Group = new TAE.EventGroup();
                            addedEvent.Group.GroupType = long.Parse(line.Split(',')[1]);
                            anim.Events.Add(addedEvent);
                            Console.WriteLine("New event type " + (float.Parse(line.Split(',')[1]).ToString() + " added at " + float.Parse(line.Split(',')[2]) + " seconds and ending at " + float.Parse(line.Split(',')[3]) + " seconds in in animation " + anim.ID.ToString()));
                            break;
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    Console.WriteLine("TAE has been written over.");
                }
            }
            
        }

        static void importParams(string oldPath) {
            if (Globals.isSeed)
            {
                importParamsSeed(oldPath, "c2010");
            }
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to import. \n C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+\\importList.txt \nYour import list should have cXXXX on each line, so something like c4310 on the first line and c4290 on the second.");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            if (!Globals.isSeed)
            {
                foreach (string line in lines)
                {
                    importParamsMany(oldPath, line);
                }
            }
            
            
            System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }
        static void startScraping(string oldPath) {
            
            Console.WriteLine("Do you wish to access the export function? (y/n)");
            if (Console.ReadLine() == "y")
            {
                extractParams(oldPath);
            }
            Console.WriteLine("Do you wish to access the import function? (y/n/s)");
            string answer = Console.ReadLine();
            if (answer == "y")
            {
                importParams(oldPath);
            } else if (answer == "s")
            {
                Globals.isSeed = true;
                importParams(oldPath);
            }

                System.Threading.Thread.Sleep(10000);
            System.Environment.Exit(1);
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Directory for your yabber please? \nExample, C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+ \nYou must have opened your anibnd.dcx file with Yabber first before you run the program on your file. \nThen after the program is done, you must recompress it in Yabber, plop it back to your directory, and you are set.");
            string path = Console.ReadLine();
            startScraping(path);

        }
    }
}
