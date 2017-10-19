using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    //Optimization01: 1:05
    class Optimization01 : IBfInterpreter
    {
        public void Run(FileStream source)
        {
            var pointer = 0;
            var memory = new byte[1024 * 1024];
            var cr = new CharReader(source);
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
        }
    }
}
