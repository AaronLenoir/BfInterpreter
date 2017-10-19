using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    class Program
    {
        static void Main(string[] args)
        {
            var interpreters = new IBfInterpreter[] {
                //new ReferenceInterpreter(),
                //new Optimization01()
                //new Optimization02()
                //new Optimization04()
                //new Optimization05()
                //new Optimization06()
                //new Optimization07()
                //new Optimization08()
                //new Optimization09()
                //new Optimization10()
                //new Optimization11()
                new Optimization12()
            };
            using (FileStream source = File.OpenRead("mandelbrot.bf"))
            {
                Benchmark(interpreters, source);
            }
        }

        static void Benchmark(IEnumerable<IBfInterpreter> interpreters, FileStream source)
        {
            foreach(var interpreter in interpreters)
            {
                var benchmark = new Benchmark(interpreter);
                var result = benchmark.Run(source, 1);
                Console.WriteLine(result);
            }
        }
    }
}