using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    /// <summary>
    /// Least efficient, most straight forward bf interpreter.
    /// </summary>
    /// <remarks>Mandelbrot: 20 minutes</remarks>
    public class ReferenceInterpreter : IBfInterpreter
    {
        public void Run(FileStream source)
        {
            var pointer = 0;
            var memory = new byte[1024*1024];
            var cr = new CharReader(source);

            while(cr.HasCharacters())
            {
                var c = cr.GetChar();
                switch(c)
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
                        }
                        break;
                    case ']':
                        nesting = 0;
                        if (memory[pointer] != 0)
                        {
                            while (true)
                            {
                                cr.Back();
                                var tempChar = cr.GetChar();
                                if (tempChar == '[' && nesting == 0) { cr.GetChar(); break; }
                                if (tempChar == ']') { nesting++; }
                                if (tempChar == '[') { nesting--; }
                            }
                        }
                        break;
                }
                cr.Forward();
            }
        }
    }
}
