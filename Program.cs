using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

/// Name: Brennan Reed <@bmr8777@rit.edu>
/// Date: 2/3/20
/// Class: CSCI.251.01-02
/// Assignment: Disk Usage Project

namespace DiskUsage
{
    /// <summary>
    ///     Class that holds the core logic and attributes of the DiskUsage program
    /// </summary>
    class Program
    {

        /// <summary>
        ///     Enums for the various modes of the Program
        /// </summary>
        public enum ProgramMode
        {
            SingleMode,
            ParallelMode,
            BothMode
        }

        private long singleThreadFolderCount; // Total number of folders for the Sequential calculation
        private long singleThreadFileCount; // Total number of files for the Sequential calculation
        private long singleThreadByteCount; // Total number of bytes for the Sequential calculation
        private long parallelThreadFolderCount; // Total number of folders for the Parallel calculation
        private long parallelThreadFileCount; // Total number of files for the Parallel calculation
        private long parallelThreadByteCount; // Total number of bytes for the Parallel calculation
        private string path; // The root directory to be traversed
        private ProgramMode mode; // The mode which the program is running in

        System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();

        /// <summary>
        ///     Constructor for the Program Class
        /// </summary>
        /// <param name="mode_">
        ///     The mode which the program is going to run in
        /// </param>
        /// <param name="path_">
        ///     The root directory
        /// </param>
        /// <returns>
        ///     An instance of the Program Class
        /// </returns>

        Program(ProgramMode mode_, string path_)
        {
            path = path_;
            mode = mode_;
            singleThreadFolderCount = 0;
            singleThreadFileCount = 0;
            singleThreadByteCount = 0;
            parallelThreadFolderCount = 0;
            parallelThreadFileCount = 0;
            parallelThreadByteCount = 0;
        }

        /// <summary>
        ///     Method that makes use of ForEach to sequentially navigate through a specified directory
        /// </summary>
        /// <param name="path">
        ///     The path to the root directory
        /// </param>

        void SingleThreaded(string path)
        {
            try
            {
                SingleCalculation(path);
            }
            catch (UnauthorizedAccessException e)
            {
                log.Add(e.Message);
            }
            catch (System.IO.DirectoryNotFoundException e)
            {
                log.Add(e.Message);
            }
            catch (IOException e)
            {
                log.Add(e.Message);
            }
        }

        /// <summary>
        ///     Method that makes use of Parallel.ForEach() to navigate through a specified directory
        /// </summary>
        /// <param name="path">
        ///     The path to the root directory
        /// </param>

        void ParallelCalculation(string path)
        {
            ConcurrentStack<string> dirs = new ConcurrentStack<string>(); // Holds the directories that need to be traversed
            int procCount = Environment.ProcessorCount;

            dirs.Push(path);
            parallelThreadFolderCount++;
            while (dirs.Count > 0)
            {
                string currentDir; 
                dirs.TryPop(out currentDir);
                string[] subDirs = { };
                string[] files = { };
                try {
                    try
                    {
                        subDirs = Directory.GetDirectories(currentDir);
                        if (subDirs != null)
                        {
                            foreach (string s in subDirs)
                                dirs.Push(s);
                            Interlocked.Add(ref parallelThreadFolderCount, subDirs.Length);
                        }
                        files = Directory.GetFiles(currentDir);
                        if (files != null)
                        {
                            try 
                            {
                                long tempFile = 0;
                                foreach (string f in files)
                                {
                                    FileInfo fi = new FileInfo(f);
                                    tempFile += fi.Length;
                                }
                                Interlocked.Add(ref parallelThreadByteCount, tempFile);
                                Interlocked.Add(ref parallelThreadFileCount, files.Length);
                            }
                            catch (UnauthorizedAccessException){}
                            catch (System.IO.DirectoryNotFoundException) {}
                        }
                    }
                    catch (UnauthorizedAccessException){continue;}
                    catch (System.IO.DirectoryNotFoundException) {continue;}
                    Parallel.ForEach<string, long>(dirs, () => 0, (dir, loopstate, returnValue) =>
                    {
                        try
                        {
                            string tempDir;
                            string[] temp = {};
                            string[] tempFiles = {};
                            dirs.TryPop(out tempDir);

                            try 
                            {
                                temp = Directory.GetDirectories(tempDir);
                                if (temp != null)
                                {
                                    Interlocked.Add(ref parallelThreadFolderCount, temp.Length);
                                    foreach (string s in temp)
                                        dirs.Push(s);
                                }
                            }
                            catch (UnauthorizedAccessException) {}
                            catch (IOException) {}
                            try 
                            {
                                tempFiles = Directory.GetFiles(tempDir);
                                if (tempFiles != null)
                                {
                                    Interlocked.Add(ref parallelThreadFileCount, tempFiles.Length);
                                    foreach (string f in tempFiles)
                                    {
                                        try 
                                        {
                                            FileInfo fi = new FileInfo(f);
                                            returnValue += fi.Length;
                                        }
                                        catch (UnauthorizedAccessException) {continue;}
                                        catch (IOException) {continue;}
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException) {}
                            catch (IOException) {}
                        }
                        catch (UnauthorizedAccessException) {}
                        catch (IOException) {}
                        return returnValue;
                    },
                    (c) => 
                    {
                        Interlocked.Add(ref parallelThreadByteCount, c);
                    });
                    }
                    catch (UnauthorizedAccessException) {}
                    catch (IOException) {}
                }
        }


        /// <summary>
        ///     Calculation using Sequential ForEach 
        /// </summary>
        /// <param name="path">
        ///     The path to the root directory
        /// </param>
        void SingleCalculation(string path)
        {
            Stack<string> dirs = new Stack<string>(); // Holds the subFolders to be traversed

            dirs.Push(path);
            singleThreadFolderCount++;
            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs = { };
                string[] files = { };
                long fs;

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                    files = Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException)
                {
                    continue;
                }
                if (subDirs != null)
                {
                    singleThreadFolderCount += subDirs.Length;
                    foreach (string di in subDirs)
                        dirs.Push(di);
                }
                if (files != null)
                {
                    singleThreadFileCount += files.Length;
                    fs = 0;
                    foreach (string file in files)
                    {
                        try 
                        {
                            FileInfo fi = new FileInfo(file);
                            fs += fi.Length;
                        }
                        catch (UnauthorizedAccessException) {}
                        catch (IOException) {}
                    }
                    singleThreadByteCount += fs;
                }
            }
        }

        /// <summary>
        ///     Formats, and performs the Console Output for the sequential calculation
        /// </summary>
        /// <param name="timeSpan">
        ///     The TimeSpan returned by the StopWatch for the sequential calculation
        /// </param>

        void SingleThreadedOutput(TimeSpan timeSpan)
        {
            Console.WriteLine("Sequential Calculated in: " + timeSpan.TotalSeconds + "s");
            Console.WriteLine(OutputFormat(singleThreadFolderCount) + " folders, " + OutputFormat(singleThreadFileCount) + " files, " + OutputFormat(singleThreadByteCount) + " bytes");
        }

        /// <summary>
		///     Returns a formatted String to match the desired program output
		/// </summary>
		/// <param name="value">
		///     The value to be formatted
		/// </param>
		/// <returns>
        ///     Formatted version of the string
        /// </returns>

        string OutputFormat(long value)
        {
            return String.Format("{0:n0}", value);
        }

        /// <summary>
        ///     Formats, and performs the Console Output for the parallel calculation
        /// </summary>
        /// <param name="timeSpan">
        ///     The TimeSpan returned by the StopWatch for the parallel calculation
        /// </param>

        void ParallelOutput(TimeSpan timeSpan)
        {
            Console.WriteLine("Parallel Calculated in: " + timeSpan.TotalSeconds + "s");
            Console.WriteLine(OutputFormat(parallelThreadFolderCount) + " folders, " + OutputFormat(parallelThreadFileCount) + " files, " + OutputFormat(parallelThreadByteCount) + " bytes");
        }

        /// <summary>
        ///     Outputs the program usage information to the
        /// </summary>

        static void printUsage()
        {
            Console.WriteLine("Usage: du [-s] [-p] [-b] <path>");                
            Console.WriteLine("Summarize disk usage of the set of FILES, recursively for directories.");
            Console.WriteLine("You MUST specify one of the parameters, -s, -p, or -b");
            Console.WriteLine("-s\tRun in single threaded mode");
            Console.WriteLine("-p\tRun in parallel mode (uses all available processors");
            Console.WriteLine("-b\tRun in both parallel and single threaded mode.\n\tRuns in parallel followed by sequential mode");
        }

        /// <summary>
        ///     Main method which handles user input and controls the core logic of the program
        /// </summary>
        /// <param name="args">
        ///     Command line arguments
        /// </param>

        static void Main(string[] args)
        {
            if (args.Length != 2) // Invalid number of command line arguments
            {
                printUsage();
                System.Environment.Exit(0);
            }
            Stopwatch stopWatchSingle = new Stopwatch();
            Stopwatch stopWatchParallel = new Stopwatch();
            string mode = args[0];
            string path = args[1];
            Program program = null;
            if (!System.IO.Directory.Exists(path)) // The given directory does not exist
            {
              Console.WriteLine("Invalid Directory Path.");
              printUsage();
              System.Environment.Exit(0);
            }
            switch (mode)
            {
                case "-s":
                    Console.WriteLine("Directory '" + path + "':\n"); // Validate the path and call single-threaded mode
                    program = new Program(ProgramMode.SingleMode, path);
                    stopWatchSingle.Start();
                    program.SingleThreaded(path);
                    stopWatchSingle.Stop();
                    program.SingleThreadedOutput(stopWatchSingle.Elapsed);
                    break;
                case "-p":
                    Console.WriteLine("Directory '" + path + "':\n"); // Validate the path and call single-threaded mode
                    program = new Program(ProgramMode.ParallelMode, path);
                    stopWatchParallel.Start();
                    program.ParallelCalculation(path); // Call the ParallelCalculation method
                    stopWatchParallel.Stop();
                    program.ParallelOutput(stopWatchParallel.Elapsed);
                    break;
                case "-b":
                    Console.WriteLine("Directory '" + path + "':\n"); // Validate the path and call single-threaded mode
                    program = new Program(ProgramMode.BothMode, path);
                    stopWatchParallel.Start();
                    program.ParallelCalculation(path); // Call the ParallelCalculation method
                    stopWatchParallel.Stop();
                    stopWatchSingle.Start();
                    program.SingleThreaded(path); // Call the SingleThreaded method
                    stopWatchSingle.Stop();
                    program.ParallelOutput(stopWatchParallel.Elapsed);
                    Console.WriteLine();
                    program.SingleThreadedOutput(stopWatchSingle.Elapsed);
                    break;
                default:
                    printUsage();
                    break;
            }
        }
    }
}
