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
        static class Globals {
            public static Boolean jumpTableEventsTrue;
            public static Boolean blendIncrease;
            public static Boolean addDelays;
            public static Boolean addPerilousJumps;
        }

        static class animSpeedGradients{
            public static byte[] speedOne;
            public static byte[] speedTwo;
            public static byte[] speederGradient;

            public static int delayPercentage;
            public static byte[] speedOneDelay;
            public static byte[] speedTwoDelay;
            public static byte[] speederGradientDelay;
        }
        static class JumpTableStorage {
            public static byte[] jumpTableID;
            public static byte[] f32ArgA;
            public static byte[] s32ArgB;
            public static byte[] u8ArgC;
            public static byte[] u8ArgD;
            public static byte[] s16ArgE;
            public static byte[] jumpTableEvents;
        }

        static class BlendStorage {
            public static float blendLengthIncrease;
        }

        static class spEffectStorage{
            public static byte[] spEffectPerilousID;
            public static float perilousWarningTime;
        }
        static void startScraping(string pathThing, string oldPath) {

            BND4 bnd = BND4.Read(pathThing);
            List<string> animSpeedNums = new List<string>();
            List<string> attackStartTimes = new List<string>();
            Boolean iamacaveman = false;
            int readerlimits = 0;
            int readerlimits2 = 0;
            float startingTime = 0.0f;
            Boolean perilous = false;
            Random rnd = new Random();
            int rndnum = rnd.Next(1, 100);
            List<float> startingTimeList = new List<float>();
            Console.WriteLine("Speed Initial? ");
            animSpeedGradients.speedOne = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
            Console.WriteLine("Speed Final? ");
            animSpeedGradients.speedTwo = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
            animSpeedGradients.speederGradient = new byte[animSpeedGradients.speedOne.Length + animSpeedGradients.speedTwo.Length];
            System.Buffer.BlockCopy(animSpeedGradients.speedOne, 0, animSpeedGradients.speederGradient, 0, animSpeedGradients.speedOne.Length);
            System.Buffer.BlockCopy(animSpeedGradients.speedTwo, 0, animSpeedGradients.speederGradient, animSpeedGradients.speedOne.Length, animSpeedGradients.speedTwo.Length);


            Console.WriteLine("Do you wish to add jumpTable events after attacks (y/n)?");
            
            if (Console.ReadLine() == "y") {
                Globals.jumpTableEventsTrue = true;
                Console.WriteLine("JumpTable ID? ");
                JumpTableStorage.jumpTableID = BitConverter.GetBytes(Int32.Parse(Console.ReadLine()));
                Console.WriteLine("f32 ArgA? ");
                JumpTableStorage.f32ArgA = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
                Console.WriteLine("s32 ArgB? ");
                JumpTableStorage.s32ArgB = BitConverter.GetBytes(Int32.Parse(Console.ReadLine()));
                Console.WriteLine("u8 ArgC? ");
                JumpTableStorage.u8ArgC = BitConverter.GetBytes(Byte.Parse(Console.ReadLine()));
                Console.WriteLine("u8 ArgD? ");
                JumpTableStorage.u8ArgD = BitConverter.GetBytes(Byte.Parse(Console.ReadLine()));
                Console.WriteLine("s16 ArgE? ");
                JumpTableStorage.s16ArgE = BitConverter.GetBytes(short.Parse(Console.ReadLine()));
                byte[] jumpTableEventsLocal = new byte[JumpTableStorage.jumpTableID.Length + JumpTableStorage.f32ArgA.Length + JumpTableStorage.s32ArgB.Length + JumpTableStorage.u8ArgC.Length + JumpTableStorage.u8ArgD.Length + JumpTableStorage.s16ArgE.Length];
                System.Buffer.BlockCopy(JumpTableStorage.jumpTableID, 0, jumpTableEventsLocal, 0, JumpTableStorage.jumpTableID.Length); //I don't want to write a for loop for 5 thingies.
                System.Buffer.BlockCopy(JumpTableStorage.f32ArgA, 0, jumpTableEventsLocal, JumpTableStorage.jumpTableID.Length, JumpTableStorage.f32ArgA.Length);
                System.Buffer.BlockCopy(JumpTableStorage.s32ArgB, 0, jumpTableEventsLocal, JumpTableStorage.jumpTableID.Length + JumpTableStorage.f32ArgA.Length, JumpTableStorage.s32ArgB.Length);
                System.Buffer.BlockCopy(JumpTableStorage.u8ArgC, 0, jumpTableEventsLocal, JumpTableStorage.jumpTableID.Length + JumpTableStorage.f32ArgA.Length + JumpTableStorage.s32ArgB.Length, JumpTableStorage.u8ArgC.Length);
                System.Buffer.BlockCopy(JumpTableStorage.u8ArgD, 0, jumpTableEventsLocal, JumpTableStorage.jumpTableID.Length + JumpTableStorage.f32ArgA.Length + JumpTableStorage.s32ArgB.Length + JumpTableStorage.u8ArgD.Length, JumpTableStorage.u8ArgD.Length);
                System.Buffer.BlockCopy(JumpTableStorage.s16ArgE, 0, jumpTableEventsLocal, JumpTableStorage.jumpTableID.Length + JumpTableStorage.f32ArgA.Length + JumpTableStorage.s32ArgB.Length + JumpTableStorage.u8ArgD.Length + JumpTableStorage.u8ArgD.Length, JumpTableStorage.s16ArgE.Length);
                JumpTableStorage.jumpTableEvents = jumpTableEventsLocal;
            }

            Console.WriteLine("Do you wish to increase blend frames? Preferable for those who choose faster levels of speed gradient. (y/n)? ");
            if (Console.ReadLine() == "y") {
                Globals.blendIncrease = true;
                Console.WriteLine("How many frames longer should each blend be? ");
                BlendStorage.blendLengthIncrease = float.Parse(Console.ReadLine()) * 0.033333333333f;
                Console.WriteLine("Each blend event will now be " + BlendStorage.blendLengthIncrease + " seconds longer.");
            }
            Console.WriteLine("Do you wish to add randomized delayed attacks? (y/n)? ");
            if (Console.ReadLine() == "y")
            {
                Globals.addDelays = true;
                Console.WriteLine("What should the odds of a delayed attack be? (please enter ints of 0-100) ");
                animSpeedGradients.delayPercentage = int.Parse(Console.ReadLine());
                Console.WriteLine("Speed Initial? ");
                animSpeedGradients.speedOneDelay = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
                Console.WriteLine("Speed Final? ");
                animSpeedGradients.speedTwoDelay = BitConverter.GetBytes(float.Parse(Console.ReadLine()));
                animSpeedGradients.speederGradientDelay = new byte[animSpeedGradients.speedOneDelay.Length + animSpeedGradients.speedTwoDelay.Length];
                System.Buffer.BlockCopy(animSpeedGradients.speedOneDelay, 0, animSpeedGradients.speederGradientDelay, 0, animSpeedGradients.speedOneDelay.Length);
                System.Buffer.BlockCopy(animSpeedGradients.speedTwoDelay, 0, animSpeedGradients.speederGradientDelay, animSpeedGradients.speedOneDelay.Length, animSpeedGradients.speedTwoDelay.Length);
                Console.WriteLine("Every attack will now have a " + animSpeedGradients.delayPercentage + " percent chance to delay. \nDelay speed gradients are " + BitConverter.ToSingle(animSpeedGradients.speedOneDelay, 0) + " and " + BitConverter.ToSingle(animSpeedGradients.speedTwoDelay, 0) + ".");
            }
            Console.WriteLine("Do you wish to add perilous attacks on jumping attacks? (y/n)? ");
            if (Console.ReadLine() == "y")
            {
                Globals.addPerilousJumps = true;
                Console.WriteLine("Which spEffect ID is your Perilous attack spEffect? ");
                spEffectStorage.spEffectPerilousID = BitConverter.GetBytes(Int32.Parse(Console.ReadLine()));
                Console.WriteLine("How many frames of advance warning would you like to give? ");
                spEffectStorage.perilousWarningTime = float.Parse(Console.ReadLine()) * 0.033333333333f;
            }

            //foreach (BinderFile file in bnd.Files) //This entire section of foreach loops was stolen from Gomp's RumbleCamID tool
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
                        perilous = false;
                        for (int i = 0; i < anim.Events.Count; i++)
                        {
                            TAE.Event ev = anim.Events[i];
                            if (ev.Type == 16) {
                                if (Globals.blendIncrease) {
                                    ev.EndTime = ev.EndTime + BlendStorage.blendLengthIncrease;
                                }
                                startingTime = ev.EndTime;
                            }
                            if (ev.Type == 0 && (BitConverter.ToInt32(ev.GetParameterBytes(tae.BigEndian), 0) == 27 || BitConverter.ToInt32(ev.GetParameterBytes(tae.BigEndian), 0) == 113) && Globals.addPerilousJumps)
                            {
                                perilous = true;
                            }
                            if (ev.Type == 1) //Have we hit an invokeAttack (event type 1)? If so, then add the animspeedgradients.
                            {

                                attackStartTimes.Add("StartPos " + ev.StartTime + " EndPos " + ev.EndTime + " " + string.Join(" ", startingTimeList) + " " + i1 + " " + i + " \n" + anim.ID);
                                if (ev.StartTime - 0.2f > startingTime) {
                                    if (Globals.addDelays)
                                    {
                                        rndnum = rnd.Next(1, 100);
                                        if (rndnum < animSpeedGradients.delayPercentage)
                                        {
                                            TAE.Event speedAnim = new TAE.Event(startingTime, ev.StartTime - 0.2f, 608, 0, animSpeedGradients.speederGradient, tae.BigEndian);// Write the animspeedgradient from the start to 6 frames before the invokeAttack/Bullet event
                                            anim.Events.Add(speedAnim);
                                            TAE.Event speedAnimDelay = new TAE.Event(ev.StartTime -0.2f, ev.StartTime -0.1f, 608, 0, animSpeedGradients.speederGradientDelay, tae.BigEndian);
                                            anim.Events.Add(speedAnimDelay);
                                        } else
                                        {
                                            TAE.Event speedAnim = new TAE.Event(startingTime, ev.StartTime - 0.1f, 608, 0, animSpeedGradients.speederGradient, tae.BigEndian);// Write the animspeedgradient from the start to 3 frames before the invokeAttack/Bullet event
                                            anim.Events.Add(speedAnim);
                                        }
                                        
                                    } else
                                    {
                                        TAE.Event speedAnim = new TAE.Event(startingTime, ev.StartTime - 0.1f, 608, 0, animSpeedGradients.speederGradient, tae.BigEndian);// Write the animspeedgradient from the start to 3 frames before the invokeAttack/Bullet event
                                        anim.Events.Add(speedAnim);
                                    }
                                    
                                }
                                
                                startingTime = ev.EndTime;
                                startingTimeList.Add(Convert.ToSingle(Math.Round((decimal)ev.StartTime, 3))); //So we can avoid the looping. Unfortunately, as of 10/27/22, the first animspeedgradient gets repeated 5 times.
                                readerlimits += 1;

                                if (Globals.jumpTableEventsTrue)
                                {
                                    TAE.Event jumpTableAnim = new TAE.Event(startingTime + 0.1f, startingTime + 0.2f, 0, 0, JumpTableStorage.jumpTableEvents, tae.BigEndian);
                                    anim.Events.Add(jumpTableAnim);
                                }

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
                        if (perilous)
                        {
                            for (int i = 0; i < anim.Events.Count; i++)
                            {
                                TAE.Event ev = anim.Events[i];
                                if (ev.Type == 1) {
                                    TAE.Event perilousSpEffect = new TAE.Event(ev.StartTime - spEffectStorage.perilousWarningTime, ev.EndTime + 0.1f, 67, 0, spEffectStorage.spEffectPerilousID, tae.BigEndian);// Write the animspeedgradient from the start to 3 frames before the invokeAttack/Bullet event
                                    anim.Events.Add(perilousSpEffect);
                                }
                                
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

                //if (iamacaveman)
                //{
                //    break;
                //}
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
