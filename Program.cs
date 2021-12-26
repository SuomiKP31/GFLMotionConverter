using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MotionConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please Make Sure you put this program inside a folder where:");
            Console.WriteLine("1. An empty folder, named dst ");
            Console.WriteLine("2. A folder holds UABE export motion files, named src");
            Console.WriteLine("3. A folder holds UABE export physics files, named phy - exists");
            Console.WriteLine("Press Any Key...");
            Console.ReadKey();
            
            if (Directory.Exists("dst"))
            {
                string[] existFiles = Directory.GetFiles("dst");
                foreach (var name in existFiles)
                {
                    File.Delete(name);
                }
            }
            else
            {
                Directory.CreateDirectory("dst");
            }
            
            string[] fileNames = Directory.GetFiles("src");
            
            #region motionConvert
            
            MotionDataConverter converter = new MotionDataConverter();
            foreach (var name in fileNames)
            {
                Console.WriteLine($"Converting {name}...");
                try
                {
                    // For Motions with Stepped segment, there will be raw data that gives 1.#INF as inSlope Value, which will fail JSON reader.
                    // How to Handle: Find 1.#INF in file string in a preprocess state, then give it some attention when converting segments.
                    var fileString = File.ReadAllText(name);
                    fileString = fileString.Replace("1.#INF", "\"1.#INF\"");
                    File.WriteAllText("dst/" + Path.GetFileName(name), converter.Convert(JObject.Parse(fileString)).ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Failed to convert Motion");
                }
            }
            
            RenameMacro();
            

            #endregion

            #region PhysicsConvert
            fileNames = Directory.GetFiles("phy");
            PhysicsDataConverter phyConverter = new PhysicsDataConverter();
            int i = 0;
            foreach (var name in fileNames)
            {
                i++;
                Console.WriteLine($"Converting {name}...");
                try
                {
                    File.WriteAllText("dst/" + Path.GetFileName($"output{i}.physics3.json"),
                        phyConverter.Convert(JObject.Parse(File.ReadAllText(name))).ToString());
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Failed to convert Physics");
                }
            }
            
            #endregion



            Console.WriteLine("Complete. Press any key to terminate.");
            Console.ReadKey();
        }

        static void RenameMacro()
        {
            Console.WriteLine("Renaming Motions...");
            string[] names = Directory.GetFiles("dst");
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i];
                var index = name.IndexOf(".fade", StringComparison.Ordinal);
                if(index == -1) continue;
                name = name.Remove(index);
                name += ".motion3.json";
                File.Move(names[i], name);
            }
            return;
        }
        
    }
}