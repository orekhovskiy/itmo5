using System;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace ShiftCo.ITMO.CA.Lab_2
{
    class IOManager
    {
        #region Messages
        const string invalidArgumentsMessage = "ERROR: Program takes 1 required argument and 1 optional." +
                                               "\nPossible required arguments are:\n" +
                                               "1. --help OR -h,\n" +
                                               "2. name of the input file.\n" +
                                               "Optional argument is --tree OR -t, to print Function Expression Tree.";
        const string helpMessage = "Computer Algebra course, Laboratory work #2.\n" +
                                   "Authors: ShiftCo.\n" +
                                   "Program takes 1 required argument amd 1 optional." +
                                   "\nPossible required arguments are:\n" +
                                   "1. --help OR -h,\n" +
                                   "2. name of the input file.\n" +
                                   "Optional argument is --tree OR -t, to print Function Expression Tree." +
                                   "Input file must be an .xml file formatted by rules of MathML standard and\n" +
                                   "containing a math expression consisting of monomials, polynomials, brackets and\n" +
                                   "applications of positive integer powers to those elements.";
        const string fileNotFoundError = "ERROR: File not found.";
        #endregion

        static Simplifier Simplifier = new Simplifier();

        static void ShowMessage(string message, int exitCode)
        {
            Console.Write(message);
            System.Environment.Exit(exitCode);
        }

        static void Main(string[] args)
        {
            // Работа с аргументами
            if (args.Length == 0 || args.Length >= 3)
            {
                ShowMessage(invalidArgumentsMessage, 1);
            }
            if (args.Length == 2 && args[1] != "--tree" && args[1] != "-t")
            {
                ShowMessage(invalidArgumentsMessage, 1);
            }
            if (args[0] == "--help" || args[0] == "-h")
            {
                ShowMessage(helpMessage, 0);
            }
            if (args[0] != "--help" && args[0] != "-h")
            {
                string inputFileName = args[0];
                string workFileName = "result_" + inputFileName;
                if (inputFileName.Length < ".xml".Length || inputFileName.Substring(inputFileName.Length - 4) != ".xml")
                {
                    inputFileName += ".xml";
                    workFileName += ".xml";
                }
                if (!File.Exists(inputFileName))
                {
                    ShowMessage(fileNotFoundError, 2);
                }
                if (File.Exists(workFileName))
                {
                    File.Delete(workFileName);
                }
                
                // Отсчет времени
                Stopwatch Rolex = new Stopwatch();
                Rolex.Start();

                // Работа с документом и упрощение выражения
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(inputFileName);
                Simplifier.SimplifyThis(ref xDoc);

                // Сохранение полученного упрощенного выражения в формате MathML
                xDoc.Save(workFileName);
                Console.WriteLine("Result file created: {0}", workFileName);

                // Вывод Function Expression Tree
                if (args.Length == 2)
                {
                    if (args[1] == "--tree" || args[1] == "-t")
                    {
                        Console.WriteLine("Function Expression Tree: \n{0}", TreeConverter.ExpressionToTree(xDoc.LastChild));
                    }
                }

                // Вывод времени работы программы
                Rolex.Stop();
                TimeSpan TimeSpan = Rolex.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                                     TimeSpan.Hours, TimeSpan.Minutes, TimeSpan.Seconds,
                                     TimeSpan.Milliseconds / 10);
                Console.Write("Time elapsed (h:m:s:ms): " + elapsedTime);
            }
        }
    }
}
