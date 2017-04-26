using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace OpBot
{
    internal class OperationRepository
    {
        private string _filename;

        public OperationRepository(string fileName)
        {
            _filename = fileName;
        }

        public OperationManager Get()
        {
            OperationManager ops = new OperationManager();
            if (!File.Exists(_filename))
                return ops;

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                ops = (OperationManager)formatter.Deserialize(stream);
            ops.WireUp();
            return ops;
        }

        public Task SaveAsync(OperationManager ops)
        {
            return Task.Run(() => Save(ops));
        }

        public void Save(OperationManager ops)
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
