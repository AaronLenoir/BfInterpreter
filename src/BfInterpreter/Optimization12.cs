using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // Same as 11 with some micro optimization attempts
    // Unchecked has No effect: Total 00:00:07.6438988
    // Interesting: replacing the switch statement with if else if statements
    // adds almost 2 seconds to the run time: Total 00:00:09.0056142
    // Also useful, Console.WriteLine itself already takes ~1 second.
    //    Running without Console.WriteLine: Total 00:00:06.8609440
    // Returned "SetToZero": Total 00:00:07.6544805
    // Next excercise: find other patterns like SetToZero.
    class Optimization12 : IBfInterpreter
    {
        private enum InstructionType
        {
            None = 0,
            Print = 1, Read = 2, BeginLoop = 3, EndLoop = 4,
            Combo = 5, Add = 6, Shift = 7, SetZero = 8
        }

        private class Instructions : List<Instruction>
        {
            public int[] ConvertToArray()
            {
                var data = new List<int>(); // first use a list, convert to array in the end.
                var jumpTable = new Stack<int>();
                foreach (var instruction in this)
                {
                    // instruction
                    // parameter
                    // [deltacount] (if instruction is combo)
                    // deltaKey
                    // deltaValue
                    // finalShift
                    data.Add((int)instruction.Type);

                    // We must adjust the jump indexes for the loops
                    // because we now index an instruction in the flat array
                    switch (instruction.Type)
                    {
                        case InstructionType.Add:
                            data.Add(instruction.Parameter);
                            data.Add(instruction.Offset);
                            break;
                        case InstructionType.BeginLoop:
                            jumpTable.Push(data.Count + 1);
                            data.Add(0);
                            break;
                        case InstructionType.EndLoop:
                            var beginPosition = jumpTable.Pop();
                            data[beginPosition - 1] = data.Count + 1;
                            data.Add(beginPosition);
                            break;
                        default:
                            data.Add(instruction.Parameter);
                            break;
                    }
                }

                return data.ToArray();
            }
        }

        private class Instruction
        {
            public InstructionType Type;

            public int Parameter;

            public int Offset;

            public Instruction(InstructionType type) : this(type, 0)
            { }

            public Instruction(InstructionType type, int parameter)
            {
                Type = type;
                Parameter = parameter;
            }

            public int _currentKey;

            private Dictionary<int, byte> _deltas = new Dictionary<int, byte>();

            public Dictionary<int, byte> DeltaDictionary { get { return _deltas; } }

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

        private unsafe void Execute(Instructions instructions)
        {
            Console.WriteLine($"{instructions.Count} instructions.");
            var instructionArray = instructions.ConvertToArray();
            var instructionLength = instructionArray.Length;

            using(var sw = new BinaryWriter(File.OpenWrite(@"C:\temp\mandelbrot.bfb")))
            {
                foreach (int value in instructionArray)
                {
                    sw.Write(value);
                }
            }

            var memory = new byte[1024 * 1024];

            fixed (byte* pMemory = memory)
            {
                fixed (int* pInstructions = instructionArray)
                {
                    byte* pm = pMemory;
                    int* pi = pInstructions;

                    var maxPi = pInstructions + instructionLength;

                    unchecked
                    {
                        while (pi < maxPi)
                        {
                            
                            var instructionType = *(pi++);
                            var parameter = *(pi++);

                            switch (instructionType)
                            {
                                case 1:
                                    Console.Write((char)(*pm));
                                    break;
                                case 2:
                                    var newChar = Console.Read();
                                    (*pm) = (byte)newChar;
                                    break;
                                case 3:
                                    if ((*pm) == 0)
                                    {
                                        pi = pInstructions + parameter;
                                    }
                                    break;
                                case 4:
                                    if ((*pm) != 0)
                                    {
                                        pi = pInstructions + parameter;
                                    }
                                    break;
                                case 6:
                                    *(pm + *(pi++)) += (byte)parameter;
                                    break;
                                case 7:
                                    pm += parameter;
                                    break;
                                case 8:
                                    *(pm) = 0;
                                    break;
                            }

                            // Replacing the switch with this code makes it almost 2 second slower in total:
                            //if (instructionType == 1)
                            //{
                            //    Console.Write((char)(*pm));
                            //}
                            //else if (instructionType == 2)
                            //{
                            //    var newChar = Console.Read();
                            //    (*pm) = (byte)newChar;
                            //}
                            //else if (instructionType == 3)
                            //{
                            //    if ((*pm) == 0)
                            //    {
                            //        pi = pInstructions + parameter;
                            //    }
                            //}
                            //else if (instructionType == 4)
                            //{
                            //    if ((*pm) != 0)
                            //    {
                            //        pi = pInstructions + parameter;
                            //    }
                            //}
                            //else if (instructionType == 6)
                            //{
                            //    var offset = *(pi++);
                            //    *(pm + offset) += (byte)parameter;
                            //}
                            //else if (instructionType == 7)
                            //{
                            //    pm += parameter;
                            //}
                        }
                    }
                }
            }
        }

        private Instructions BuildInstructions(Stream source)
        {
            var instructions = new Instructions();
            var reader = new CharReader(source);
            var jumpTable = new Stack<int>();

            while(reader.HasCharacters())
            {
                if (IsSetToZero(reader))
                {
                    instructions.Add(new Instruction(InstructionType.SetZero));
                }

                var c = reader.GetChar();
                
                switch(c)
                {
                    case '+':
                        instructions.AddRange(DeflateComboInstruction(GetComboInstruction(reader)));
                        break;
                    case '-':
                        instructions.AddRange(DeflateComboInstruction(GetComboInstruction(reader)));
                        break;
                    case '>':
                        instructions.AddRange(DeflateComboInstruction(GetComboInstruction(reader)));
                        break;
                    case '<':
                        instructions.AddRange(DeflateComboInstruction(GetComboInstruction(reader)));
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

            return instructions;
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

        private List<Instruction> DeflateComboInstruction(Instruction combo)
        {
            var result = new List<Instruction>();

            foreach (var delta in combo.DeltaDictionary)
            {
                // This attempt at optimization costs a second?
                //if (delta.Value != 0)
                //{
                    var addInstruction = new Instruction(InstructionType.Add, delta.Value);
                    addInstruction.Offset = delta.Key;
                    result.Add(addInstruction);
                //}
            }

            if (combo._currentKey != 0)
            {
                result.Add(new Instruction(InstructionType.Shift, combo._currentKey));
            }

            return result;
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
