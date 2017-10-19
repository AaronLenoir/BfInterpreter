using System.IO;

namespace BfInterpreter
{
    public interface IBfInterpreter
    {
        void Run(FileStream sources);
    }
}
