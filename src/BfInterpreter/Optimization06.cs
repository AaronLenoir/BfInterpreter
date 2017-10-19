using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // 00:09
    class Optimization06 : IBfInterpreter
    {
        private enum InstructionType
        {
            None = 0,
            Add, Subtract, ShiftLeft, ShiftRight, Print, Read, BeginLoop, EndLoop,
            SetToZero
        }

        private class Instruction
        {
            public InstructionType Type { get; private set; }

            public int Parameter { get; set; }

            public Instruction(InstructionType type) : this(type, 0)
            { }

            public Instruction(InstructionType type, int parameter)
            {
                Type = type;
                Parameter = parameter;
            }
        }

        private List<char> _legalCharacters = new List<char>() { '+', '-', '>', '<', '.', ',', '[', ']' };

        public void Run(FileStream source)
        {
            var strippedSource = Strip(source);

            var instructions = BuildInstructions(strippedSource);

            Execute(instructions);
        }

        private void Execute(Instruction[] instructions)
        {
            var instructionPointer = 0;

            var pointer = 0;
            var memory = new byte[1024 * 1024];

            while (instructionPointer < instructions.Length)
            {
                var instruction = instructions[instructionPointer];

                switch(instruction.Type)
                {
                    case InstructionType.Add:
                        memory[pointer] += (byte)instruction.Parameter;
                        break;
                    case InstructionType.Subtract:
                        memory[pointer] -= (byte)instruction.Parameter;
                        break;
                    case InstructionType.ShiftRight:
                        pointer += instruction.Parameter;
                        break;
                    case InstructionType.ShiftLeft:
                        pointer -= instruction.Parameter;
                        break;
                    case InstructionType.Print:
                        Console.Write((char)memory[pointer]);
                        break;
                    case InstructionType.Read:
                        var newChar = Console.Read();
                        memory[pointer] = (byte)newChar;
                        break;
                    case InstructionType.BeginLoop:
                        if (memory[pointer] == 0)
                        {
                            instructionPointer = instruction.Parameter;
                        }
                        break;
                    case InstructionType.EndLoop:
                        if (memory[pointer] != 0)
                        {
                            instructionPointer = instruction.Parameter;
                        }
                        break;
                    case InstructionType.SetToZero:
                        memory[pointer] = 0;
                        break;
                }

                instructionPointer++;
            }
        }

        private Instruction[] BuildInstructions(Stream source)
        {
            var instructions = new List<Instruction>();
            var reader = new CharReader(source);
            var jumpTable = new Stack<int>();

            while(reader.HasCharacters())
            {
                if (IsSetToZero(reader))
                {
                    instructions.Add(new Instruction(InstructionType.SetToZero));
                }

                var c = reader.GetChar();
                
                switch(c)
                {
                    case '+':
                        instructions.Add(new Instruction(InstructionType.Add, CountSeries(reader)));
                        break;
                    case '-':
                        instructions.Add(new Instruction(InstructionType.Subtract, CountSeries(reader)));
                        break;
                    case '>':
                        instructions.Add(new Instruction(InstructionType.ShiftRight, CountSeries(reader)));
                        break;
                    case '<':
                        instructions.Add(new Instruction(InstructionType.ShiftLeft, CountSeries(reader)));
                        break;
                    case '.':
                        instructions.Add(new Instruction(InstructionType.Print));
                        break;
                    case ',':
                        instructions.Add(new Instruction(InstructionType.Read));
                        break;
                    case '[':
                        instructions.Add(new Instruction(InstructionType.BeginLoop));
                        jumpTable.Push(instructions.Count - 1);
                        break;
                    case ']':
                        var beginPosition = jumpTable.Pop();
                        var beginInstruction = instructions[beginPosition];
                        instructions.Add(new Instruction(InstructionType.EndLoop, beginPosition));
                        beginInstruction.Parameter = instructions.Count - 1;
                        break;
                }

                reader.Forward();
            }

            return instructions.ToArray();
        }

        private bool IsSetToZero(CharReader cr)
        {
            var i = cr.Position;
            if (cr.Position <= cr.Length - 3)
            {
                if (cr[i] == '[' &&
                    cr[i + 1] == '-' &&
                    cr[i + 2] == ']')
                {
                    cr.Position += 3;
                    return true;
                }
            }

            return false;
        }

        private int CountSeries(CharReader cr)
        {
            var count = 0;
            var currentChar = cr.GetChar();
            while (cr.HasCharacters() && cr.GetChar() == currentChar)
            {
                count++;
                cr.Forward();
            }
            cr.Back();
            return count;
        }

        private Stream Strip(Stream source)
        {
            var newStream = new MemoryStream();
            var data = source.ReadByte();
            while(data != -1)
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
