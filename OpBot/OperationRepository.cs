using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace OpBot
{
    internal class OperationRepository
    {
        private string _filename;

        public OperationRepository(string fileName)
        {
            _filename = fileName;
        }

        public Operation Get()
        {
            if (!File.Exists(_filename))
                return null;

            Operation op;
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                op = (Operation)formatter.Deserialize(stream);
            return op;
        }

        public void Save(Operation op)
        {
            if (op != null)
            {
                lock (op)
                {
                    IFormatter formatter = new BinaryFormatter();
                    using (Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.Write, FileShare.None))
                        formatter.Serialize(stream, op);
                }
            }
            else
            {
                if (File.Exists(_filename))
                    File.Delete(_filename);
            }
        }

    }
}
