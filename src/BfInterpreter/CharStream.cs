using System;
using System.IO;

namespace BfInterpreter
{
    public class CharReader
    {
        private char[] _buffer;

        private int _position = 0;

        public int Position
        {
            get { return _position; }
            set
            {
                if (value < 0 || value >= _buffer.Length)
                {
                    new IndexOutOfRangeException();
                }

                _position = value;
            }
        }

        public char this[int index]
        {
            get
            {
                return _buffer[index];
            }
        }

        public CharReader(Stream stream)
        {
            _buffer = new char[stream.Length];
            int data = stream.ReadByte();

            int i = 0;
            while (data != -1)
            {
                _buffer[i++] = (char)data;
                data = stream.ReadByte();
            }

            stream.Seek(0, SeekOrigin.Begin);
        }

        public void Back()
        {
            if (_position <= 0)
            {
                throw new InvalidOperationException("Attempt to read before beginning of stream.");
            }

            _position--;
        }

        public void Forward()
        {
            if (_position == _buffer.Length)
            {
                throw new InvalidOperationException("No bytes left.");
            }

            _position++;
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public char GetChar()
        {
            return _buffer[_position];
        }

        public bool HasCharacters()
        {
            if(_position == _buffer.Length)
            {
                return false;
            }
            return true;
        }
    }
}
