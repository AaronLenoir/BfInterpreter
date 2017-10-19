using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    //Optimization02: 1:20 
    // Slower than Optimization01
    class Optimization02 : IBfInterpreter
    {
        private byte[] _seriesCache;

        public void Run(FileStream source)
        {
            var pointer = 0;
            var memory = new byte[1024 * 1024];
            var cr = new CharReader(source);
            _seriesCache = new byte[cr.Length];
            var jumpTable = new Stack<int>();

            while (cr.HasCharacters())
            {
                var c = cr.GetChar();
                switch (c)
                {
                    case '>':
                        pointer++;
                        break;
                    case '<':
                        pointer--;
                        break;
                    case '+':
                        memory[pointer] += GetSumFromSeries(cr, '+');
                        break;
                    case '-':
                        memory[pointer] -= GetSumFromSeries(cr, '-');
                        break;
                    case '.':
                        Console.Write((char)memory[pointer]);
                        break;
                    case ',':
                        var newChar = Console.Read();
                        memory[pointer] = (byte)newChar;
                        break;
                    case '[':
                        var nesting = 0;
                        if (memory[pointer] == 0)
                        {
                            while (cr.HasCharacters())
                            {
                                cr.Forward();
                                var tempChar = cr.GetChar();
                                if (tempChar == ']' && nesting == 0) { break; }
                                if (tempChar == '[') { nesting++; }
                                if (tempChar == ']') { nesting--; }
                            }
                        } else
                        {
                            jumpTable.Push(cr.Position);
                        }
                        break;
                    case ']':
                        nesting = 0;
                        if (memory[pointer] != 0)
                        {
                            cr.Position = jumpTable.Peek();
                        } else
                        {
                            jumpTable.Pop();
                        }
                        break;
                }
                cr.Forward();
            }

            Console.WriteLine("Cache hits: " + _cacheHits);
            Console.WriteLine("Cache misses: " + (_hits - _cacheHits));
        }

        private int _cacheHits = 0;

        private int _hits = 0;

        public byte GetSumFromSeries(CharReader cr, Char lookupChar)
        {
            _hits++;
            var startPosition = cr.Position;
            var cachedCount = _seriesCache[startPosition];

            if (cachedCount != 0)
            {
                cr.Position += cachedCount - 1;
                if (cachedCount > 10) { _cacheHits++; }
                return cachedCount;
            }

            byte count = 1;

            cr.Forward();
            while (cr.GetChar() == lookupChar) { count += 1; cr.Forward(); }
            cr.Back();

            _seriesCache[startPosition] = count;

            return count;
        }
    }
}
