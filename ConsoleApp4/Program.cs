using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SoulsFormats;
using SoulsAssetPipeline.Animation;
using System.IO;

namespace ConsoleApp4
{
    class Program
    {
        static void startScraping(string pathThing, string oldPath) {

            BND4 bnd = BND4.Read(pathThing);
            List<string> animSpeedNums = new List<string>();
            List<string> attackStartTimes = new List<string>();
            Boolean iamacaveman = false;
            int readerlimits = 0;
            int readerlimits2 = 0;
            float startingTime = 0.0f;
            List<float> startingTimeList = new List<float>();
            Console.WriteLine("Speed Initial? ");
            byte[] speedOne = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
            Console.WriteLine("Speed Final? ");
            byte[] speedTwo = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
            byte[] speederGradient = new byte[speedOne.Length + speedTwo.Length];
            System.Buffer.BlockCopy(speedOne, 0, speederGradient, 0, speedOne.Length);
            System.Buffer.BlockCopy(speedTwo, 0, speederGradient, speedOne.Length, speedTwo.Length);

            foreach (BinderFile file in bnd.Files) //This entire section of foreach loops was stolen from Gomp's RumbleCamID tool
            {
                IEnumerable<BinderFile> taeFiles = bnd.Files.Where(x => x.Name.Contains(".tae"));

                foreach (var taeFile in taeFiles.Where(x => x.Bytes.Length > 0))
                {
                    TAE tae = TAE.Read(taeFile.Bytes);
                    for (int i1 = 0; i1 < tae.Animations.Count; i1++)
                    {
                        TAE.Animation anim = tae.Animations[i1];
                        startingTime = 0.0f;
                        startingTimeList.Clear();
                        for (int i = 0; i < anim.Events.Count; i++)
                        {
                            TAE.Event ev = anim.Events[i];

                            if ((ev.Type == 1 || ev.Type == 2)) //Have we hit an invokeAttack (event type 1) or an invokeBullet (event type 2)? If so, then add the animspeedgradients. Also avoids repeating the animspeedgradient events but somehow it doesn't work.
                            {

                                attackStartTimes.Add("StartPos " + ev.StartTime + " EndPos " + ev.EndTime + " " + string.Join(" ", startingTimeList) + " " + i1 + " " + i + " \n" + anim.ID);
                                TAE.Event speedAnim = new TAE.Event(startingTime, ev.StartTime, 608, 0, speederGradient, tae.BigEndian);// Write the animspeedgradient from the start to 2 frames before the invokeAttack/Bullet event
                                anim.Events.Add(speedAnim);
                                startingTime = ev.EndTime;
                                startingTimeList.Add(Convert.ToSingle(Math.Round((decimal)ev.StartTime, 3))); //So we can avoid the looping. Unfortunately, as of 10/27/22, the first animspeedgradient gets repeated 5 times.
                                readerlimits += 1;
                                if (readerlimits > 300)
                                {
                                    iamacaveman = true; //It's simple, it's effective. It's dumb. Still breaks me out of infinite loops though so that's a boon.
                                    break;
                                }
                                readerlimits2 += 1;
                                if (readerlimits2 > 10)
                                {
                                    break;
                                }
                            }

                            if (iamacaveman)
                            {
                                break;
                            }
                        }
                        if (iamacaveman)
                        {
                            break;
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    if (iamacaveman)
                    {
                        break;
                    }

                }

                if (iamacaveman)
                {
                    break;
                }
            }
            bnd.Write(pathThing, DCX.Type.None);
            Console.WriteLine("Finished with file at " + pathThing + "\nWould you like to choose a new directory (d), a new file (f), or quit (q)?");
            string answer = Console.ReadLine();

            if (answer == "d")
            {
                Console.WriteLine("Directory for your yabber please? \nExample, C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+ \nYou must have opened your anibnd.dcx file with Yabber first before you run the program on your file. Then after the program is done, you must recompress it in Yabber, plop it back to your directory, and you are set.");
                string path = Console.ReadLine();
                Console.WriteLine("Which anibnd do you want to modify? \nExample, c4290.anibnd");
                string thePath = path + "\\" + Console.ReadLine();
                startScraping(thePath, path);
            }
            else if (answer == "f")
            {
                Console.WriteLine("Which anibnd do you want to modify? \nExample, c2500.anibnd");
                string thePath2 = oldPath + "\\" + Console.ReadLine();
                startScraping(thePath2, oldPath);
            }
            else {
                Console.WriteLine("Bye bye!");
                System.Threading.Thread.Sleep(1000);
                System.Environment.Exit(1);
            }
            //File.WriteAllLines($"C:/Program Files (x86)/Steam/steamapps/common/ELDEN RING/Game/mod/chr/animLines.txt", animSpeedNums.Select(x => x.ToString()));
            //File.WriteAllLines($"C:/Program Files (x86)/Steam/steamapps/common/ELDEN RING/Game/mod/chr/animAttackTimes.txt", attackStartTimes.Select(x => x.ToString()));

        }
        static void Main(string[] args)
        {
            Console.WriteLine("Directory for your yabber please? \nExample, C:\\Users\\Francis Wang\\Downloads\\Yabber+\\Yabber+ \nYou must have opened your anibnd.dcx file with Yabber first before you run the program on your file. \nThen after the program is done, you must recompress it in Yabber, plop it back to your directory, and you are set.");
            string path = Console.ReadLine();
            Console.WriteLine("Which anibnd do you want to modify? \nExample, c4290.anibnd");
            string thePath = path + "\\" + Console.ReadLine();
            startScraping(thePath, path);

        }
    }
}
