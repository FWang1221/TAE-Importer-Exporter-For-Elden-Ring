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
        static void Main(string[] args)
        {
            Console.WriteLine("Directory for your animation please? \nExample, C:\\Program Files (x86)\\Steam\\steamapps\\common\\ELDEN RING\\Game\\mod\\chr\\c4290.anibnd.dcx \nYou must have opened your animation file with DSAS first and saved it properly before you run the program on your file.");
            string path = Console.ReadLine();
            BND4 bnd = BND4.Read(path);
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

                            if ((ev.Type == 1 || ev.Type == 2) && !startingTimeList.Contains(Convert.ToSingle(Math.Round((decimal)ev.StartTime, 3)))) //Have we hit an invokeAttack (event type 1) or an invokeBullet (event type 2)? If so, then add the animspeedgradients. Also avoids repeating the animspeedgradient events but somehow it doesn't work.
                            {
                                
                                //attackStartTimes.Add("StartPos " + ev.StartTime + " EndPos " + ev.EndTime + " " + string.Join(" ", startingTimeList) + " " + i1 + " " + i);
                                TAE.Event speedAnim = new TAE.Event(startingTime, ev.StartTime - (2/30), 608, 0, speederGradient, tae.BigEndian);// Write the animspeedgradient from the start to 2 frames before the invokeAttack/Bullet event
                                anim.Events.Add(speedAnim);
                                startingTime = ev.EndTime + (2/30);
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
                            if (iamacaveman) { 
                                break; 
                            }
                        }
                        if (iamacaveman) {
                            break;
                        }
                    }
                    taeFile.Bytes = tae.Write();
                    if (iamacaveman)
                    {
                        break;
                    }
                                       
                }
                if (iamacaveman) {
                    break;
                }
            }
            bnd.Write(path, DCX.Type.None);
            //File.WriteAllLines($"C:/Program Files (x86)/Steam/steamapps/common/ELDEN RING/Game/mod/chr/animLines.txt", animSpeedNums.Select(x => x.ToString()));
            //File.WriteAllLines($"C:/Program Files (x86)/Steam/steamapps/common/ELDEN RING/Game/mod/chr/animAttackTimes.txt", attackStartTimes.Select(x => x.ToString()));
        }
    }
}
