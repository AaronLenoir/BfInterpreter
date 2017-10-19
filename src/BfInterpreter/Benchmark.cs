using System;
using System.Diagnostics;
using System.IO;

namespace BfInterpreter
{
    public class BenchmarkResults
    {
        public string InterpreterName { get; set; }
        public int Iterations { get; set; }
        public TimeSpan TotalElapsedTime { get; set; }
        public TimeSpan AverageTime { get; set; }

        public override string ToString()
        {
            return $"{InterpreterName} ran {Iterations} time(s):\r\n\tTotal {TotalElapsedTime}\r\n\tAverage: {AverageTime}";
        }
    }

    public class Benchmark
    {
        private readonly IBfInterpreter _interpreter;

        public string InterpreterName
        {
            get
            {
                return _interpreter.GetType().FullName;
            }
        }

        public Benchmark(IBfInterpreter interpreter)
        {
            _interpreter = interpreter;
        }

        public BenchmarkResults Run(FileStream source, int iterations)
        {
            var results = new BenchmarkResults();
            var stopWatch = new Stopwatch();

            for(var i = 0; i < iterations; i++)
            {
                stopWatch.Start();
                _interpreter.Run(source);
                stopWatch.Stop();

                source.Seek(0, SeekOrigin.Begin);
            }

            results.Iterations = iterations;
            results.TotalElapsedTime = stopWatch.Elapsed;
            results.AverageTime = TimeSpan.FromMilliseconds(stopWatch.Elapsed.TotalMilliseconds / (double)iterations);
            results.InterpreterName = InterpreterName;

            return results;
        }
    }
}
