using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // 00:42
    class Optimization04 : IBfInterpreter
    {
        private int[] _jumpTableForward;

        private byte[] _seriesCache;

        public void Run(FileStream source)
        {
            var pointer = 0;
            var memory = new byte[1024 * 1024];
            var cr = new CharReader(Strip(source));
            _jumpTableForward = new int[cr.Length];
            _seriesCache = BuildSeriesCache(cr);

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
                        if (_seriesCache[cr.Position] != 0)
                        {
                            memory[pointer] += _seriesCache[cr.Position];
                            cr.Position += _seriesCache[cr.Position] - 1;
                        }
                        else
                        {
                            memory[pointer]++;
                        }
                        break;
                    case '-':
                        if (_seriesCache[cr.Position] != 0)
                        {
                            memory[pointer] -= _seriesCache[cr.Position];
                            cr.Position += _seriesCache[cr.Position] - 1;
                        }
                        else
                        {
                            memory[pointer]--;
                        }
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
                            var startPosition = cr.Position;
                            if (_jumpTableForward[startPosition] > 0)
                            {
                                cr.Position = _jumpTableForward[startPosition] - 1;
                            }
                            else
                            {
                                while (cr.HasCharacters())
                                {
                                    cr.Forward();
                                    var tempChar = cr.GetChar();
                                    if (tempChar == ']' && nesting == 0)
                                    {
                                        _jumpTableForward[startPosition] = cr.Position + 1;
                                        break;
                                    }
                                    if (tempChar == '[') { nesting++; }
                                    if (tempChar == ']') { nesting--; }
                                }
                            }
                        }
                        else
                        {
                            jumpTable.Push(cr.Position);
                        }
                        break;
                    case ']':
                        nesting = 0;
                        if (memory[pointer] != 0)
                        {
                            cr.Position = jumpTable.Peek();
                        }
                        else
                        {
                            jumpTable.Pop();
                        }
                        break;
                }
                cr.Forward();
            }
        }

        private byte[] BuildSeriesCache(CharReader cr)
        {
            var result = new byte[cr.Length];

            var previous = (char)0;
            byte sameCount = 0;

            while (cr.HasCharacters())
            {
                var currentCharacter = cr.GetChar();

                if (currentCharacter == '+' ||
                    currentCharacter == '-' ||
                    currentCharacter == '[' ||
                    currentCharacter == ']' ||
                    currentCharacter == '<' ||
                    currentCharacter == '>' ||
                    currentCharacter == '.' ||
                    currentCharacter == ',')
                {
                    if (previous == 0) { previous = currentCharacter; sameCount = 1; cr.Forward(); continue; }

                    if (previous == currentCharacter)
                    {
                        sameCount++;
                    }
                    else
                    {
                        result[cr.Position - sameCount] = sameCount;
                        sameCount = 1;
                        previous = currentCharacter;
                    }
                }

                cr.Forward();
            }

            cr.Position = 0;

            return result;
        }

        private List<char> _legalCharacters = new List<char>() { '+', '-', '>', '<', '.', ',', '[', ']' };

        public Stream Strip(Stream source)
        {
            var newStream = new MemoryStream();
            var data = source.ReadByte();
            while (data != -1)
            {
                var readChar = (char)data;
                if (_legalCharacters.Contains(readChar)) { newStream.WriteByte((byte)readChar); }
                data = source.ReadByte();
            }
            newStream.Seek(0, SeekOrigin.Begin);
            return newStream;
        }
    }
}
