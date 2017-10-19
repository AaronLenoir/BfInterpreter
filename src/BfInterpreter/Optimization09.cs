using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // Same as optimization 08 but with direct memory access for the arrays.
    // With memory access now with pointer Down to about 00:00:08.1694197 
    // Shaved about 400 ms from the run time.
    // Instructions still has a complex structure so direct memory access seems harder
    class Optimization09 : IBfInterpreter
    {
        private enum InstructionType
        {
            None = 0,
            Print, Read, BeginLoop, EndLoop,
            SetToZero, Combo
        }

        private class Instruction
        {
            public InstructionType Type;

            public int Parameter;

            public Instruction(InstructionType type) : this(type, 0)
            { }

            public Instruction(InstructionType type, int parameter)
            {
                Type = type;
                Parameter = parameter;
            }

            public int _currentKey;

            private Dictionary<int, byte> _deltas = new Dictionary<int, byte>();

            public int[] Deltas;

            public void Complete()
            {
                Deltas = new int[_deltas.Count * 2];

                var i = 0;
                foreach (var delta in _deltas)
                {
                    Deltas[i] = delta.Key;
                    Deltas[i + 1] = delta.Value;

                    i += 2;
                }
            }

            public void Add()
            {
                if (!_deltas.ContainsKey(_currentKey))
                {
                    _deltas.Add(_currentKey, 0);
                }
                _deltas[_currentKey]++;
            }

            public void Subtract()
            {
                if (!_deltas.ContainsKey(_currentKey))
                {
                    _deltas.Add(_currentKey, 0);
                }
                _deltas[_currentKey]--;
            }

            public void ShiftLeft()
            {
                _currentKey -= 1;
            }

            public void ShiftRight()
            {
                _currentKey += 1;
            }
        }

        private List<char> _legalCharacters = new List<char>() { '+', '-', '>', '<', '.', ',', '[', ']' };

        public void Run(FileStream source)
        {
            var strippedSource = Strip(source);

            var instructions = BuildInstructions(strippedSource);

            Execute(instructions);
        }

        private unsafe void Execute(Instruction[] instructions)
        {
            var instructionPointer = 0;

            var memory = new byte[1024 * 1024];

            fixed (byte* pMemory = memory)
            {
                byte* pm = pMemory;

                while (instructionPointer < instructions.Length)
                {
                    var instruction = instructions[instructionPointer];

                    switch (instruction.Type)
                    {
                        case InstructionType.Combo:

                            for (var i = 0; i < instruction.Deltas.Length; i += 2)
                            {
                                *(pm + instruction.Deltas[i]) += (byte)instruction.Deltas[i + 1];
                            }

                            pm += instruction._currentKey;

                            break;
                        case InstructionType.Print:
                            Console.Write((char)(*pm));
                            break;
                        case InstructionType.Read:
                            var newChar = Console.Read();
                            (*pm) = (byte)newChar;
                            break;
                        case InstructionType.BeginLoop:
                            if ((*pm) == 0)
                            {
                                instructionPointer = instruction.Parameter;
                            }
                            break;
                        case InstructionType.EndLoop:
                            if ((*pm) != 0)
                            {
                                instructionPointer = instruction.Parameter;
                            }
                            break;
                    }

                    instructionPointer++;
                }

            }
        }

        private Instruction[] BuildInstructions(Stream source)
        {
            var instructions = new List<Instruction>();
            var reader = new CharReader(source);
            var jumpTable = new Stack<int>();

            while(reader.HasCharacters())
            {
                var c = reader.GetChar();
                
                switch(c)
                {
                    case '+':
                        instructions.Add(GetComboInstruction(reader));
                        break;
                    case '-':
                        instructions.Add(GetComboInstruction(reader));
                        break;
                    case '>':
                        instructions.Add(GetComboInstruction(reader));
                        break;
                    case '<':
                        instructions.Add(GetComboInstruction(reader));
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

        private Instruction GetComboInstruction(CharReader cr)
        {
            var newInstruction = new Instruction(InstructionType.Combo);

            while(cr.HasCharacters())
            {
                var c = cr.GetChar();

                if (c == '[' || c == ']' || c == '.' || c == ',') { break; }

                if (c == '+') { newInstruction.Add(); }
                if (c == '-') { newInstruction.Subtract(); }
                if (c == '<') { newInstruction.ShiftLeft(); }
                if (c == '>') { newInstruction.ShiftRight(); }

                cr.Forward();
            }

            cr.Back();

            newInstruction.Complete();

            return newInstruction;
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
