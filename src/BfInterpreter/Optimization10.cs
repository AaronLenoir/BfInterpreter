using System;
using System.Collections.Generic;
using System.IO;

namespace BfInterpreter
{
    // Instruction list converted to an array to support pointer access (oh boy)
    // All this effort, currently no win, even some loss: 00:00:08.5670000
    // Changed "while (pi - pInstructions < instructionLength)" to "while (pi < maxPi)" and got 600 ms speedup: 00:00:07.8270346
    class Optimization10 : IBfInterpreter
    {
        private enum InstructionType
        {
            None = 0,
            Print = 1, Read = 2, BeginLoop = 3, EndLoop = 4,
            Combo = 5
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

                    if (instruction.Type == InstructionType.Combo)
                    {
                        data.Add(instruction.DeltaDictionary.Count);
                        foreach(var delta in instruction.DeltaDictionary)
                        {
                            data.Add(delta.Key);
                            data.Add(delta.Value);
                        }
                        data.Add(instruction._currentKey);
                    }
                }

                return data.ToArray();
            }
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
            var instructionArray = instructions.ConvertToArray();
            var instructionLength = instructionArray.Length;

            var memory = new byte[1024 * 1024];

            fixed (byte* pMemory = memory)
            {
                fixed (int* pInstructions = instructionArray)
                {
                    byte* pm = pMemory;
                    int* pi = pInstructions;

                    var maxPi = pInstructions + instructionLength;
                    while (pi < maxPi)
                    {
                        var instructionType = *(pi++);
                        var parameter = *(pi++);

                        switch (instructionType)
                        {
                            case 5:
                                var deltaLength = *(pi++);
                                for(var i = 0; i < deltaLength; i++)
                                {
                                    var key = *(pi++);
                                    var value = *(pi++);

                                    *(pm + key) += (byte)value;
                                }

                                pm += *(pi++);

                                break;
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
