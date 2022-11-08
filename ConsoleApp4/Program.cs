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
        static void extractParamsMany(string directoryHere, string oldDir, List<string> paramTypes, int eventTypeNum)
        {
            
            BND4 bnd = BND4.Read(oldDir + "/" + directoryHere + ".anibnd");
            IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));
            List<string> eventReader = new List<string>();
            eventReader.Add("Animation ID, StartTime, EndTime, " + string.Join(",", paramTypes));
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

                            eventReaderLine.Add(anim.ID.ToString());
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
                        }

                    }
                }
            }
            File.WriteAllLines(oldDir + "/" + directoryHere + "EventsFile.csv", eventReader.Select(x => x.ToString()));
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
            System.Environment.Exit(1);
        }
        
        static void importParamsMany(string oldPath, string newPath)
        {
            string path = oldPath + "\\" + newPath + "EventsFile.csv";
            List<string> lines = System.IO.File.ReadAllLines(path).ToList();
            List<string> header = lines[0].Split(',').ToList();
            int paramLength = 0;
            BND4 bnd = BND4.Read(oldPath + "\\" + newPath + ".anibnd");
            Console.WriteLine("Directory being edited is currently " + oldPath + "\\" + newPath + ".anibnd");
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
                            anim.Events.Add(addedEvent);
                            Console.WriteLine("New event type " + (float.Parse(line.Split(',')[1]).ToString() + " added at " + (float.Parse(line.Split(',')[2])).ToString() + " seconds and ending at " + (float.Parse(line.Split(',')[3])).ToString() + " seconds in in animation " + anim.ID.ToString()));
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    Console.WriteLine("TAE has been written over.");
                }
            }
            bnd.Write(oldPath + "\\" + newPath + ".anibnd", DCX.Type.None);
            Console.WriteLine("File has been written over at " + oldPath + "\\" + newPath + ".anibnd");
        }
        static void importParams(string oldPath) {
            Console.WriteLine("Please give me the file directory for your list of anibnds you wish to import. \n C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+\\importList.txt \nYour import list should have cXXXX on each line, so something like c4310 on the first line and c4290 on the second.");
            string[] lines = System.IO.File.ReadAllLines(Console.ReadLine());
            foreach (string line in lines)
            {
                importParamsMany(oldPath, line);
            }
            System.Environment.Exit(1);
        }
        static void startScraping(string oldPath) {
            
            Console.WriteLine("Do you wish to access the export function? (y/n)");
            if (Console.ReadLine() == "y")
            {
                extractParams(oldPath);
            }
            Console.WriteLine("Do you wish to access the import function? (y/n)");
            if (Console.ReadLine() == "y")
            {
                importParams(oldPath);
            }
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