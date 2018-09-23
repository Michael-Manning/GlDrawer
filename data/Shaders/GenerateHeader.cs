using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace PackShaders
{
    class Program
    {
        static string writePath = @"../../OpenGL3";
        static void Main(string[] args)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "PackShaders.exe";
            p.Start();

            string[] readFiles = Directory.GetFiles("../", "*.*", SearchOption.AllDirectories).Where(s => s.Contains(".glsl")).ToArray();
            string[][] glslFiles =  new string[readFiles.Length][];
            for (int i = 0; i < readFiles.Length; i++)
                glslFiles[i] = File.ReadAllLines(readFiles[i]);

            using (StreamWriter outputFile = new StreamWriter(Path.Combine(writePath, "shaders.h")))
            {
                Console.WriteLine("Packing shaders:");
                outputFile.WriteLine("//This file was automatically generated with a batch file and C# script with .glsl files as a source");
                outputFile.WriteLine("//Both the script file and shaders used can be found in the data/Shaders folder of the projects working directory");
                outputFile.WriteLine("#pragma once");
                for (int i = 0; i < glslFiles.Length; i++)
                {
                    string s = readFiles[i].Substring(readFiles[i].LastIndexOf('\\') + 1);
                    Console.WriteLine(s + "...");
                    outputFile.WriteLine("const char * " + s.Remove(s.Length - 5, 5) + "=");
                    for (int j = 0; j < glslFiles[i].Length; j++)
                        outputFile.WriteLine("\"" + glslFiles[i][j] + "\\n\"");
					outputFile.Write(";");
                }
            }
            Console.WriteLine("Shaders packed to " + writePath + "\n");
            p.Kill();
        }
    }
}
