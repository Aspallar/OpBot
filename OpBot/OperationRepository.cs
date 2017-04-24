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

        public OperationCollection Get()
        {
            OperationCollection ops = new OperationCollection();
            if (!File.Exists(_filename))
                return ops;

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                ops = (OperationCollection)formatter.Deserialize(stream);
            ops.WireUp();
            return ops;
        }

        public void Save(OperationCollection ops)
        {
            lock (ops)
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new FileStream(_filename, FileMode.Create, FileAccess.Write, FileShare.None))
                    formatter.Serialize(stream, ops);
            }
        }

    }
}
