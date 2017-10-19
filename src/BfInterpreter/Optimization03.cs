using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // 00:42
    class Optimization03 : IBfInterpreter
    {
        private int[] _jumpTableForward;

        public void Run(FileStream source)
        {
            var pointer = 0;
            var memory = new byte[1024 * 1024];
            var cr = new CharReader(source);
            _jumpTableForward = new int[cr.Length];
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
                        memory[pointer]++;
                        break;
                    case '-':
                        memory[pointer]--;
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
                            } else
                            {
                                while (cr.HasCharacters())
                                {
                                    cr.Forward();
                                    var tempChar = cr.GetChar();
                                    if (tempChar == ']' && nesting == 0) {
                                        _jumpTableForward[startPosition] = cr.Position + 1;
                                        break;
                                    }
                                    if (tempChar == '[') { nesting++; }
                                    if (tempChar == ']') { nesting--; }
                                }
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
        }
    }
}
