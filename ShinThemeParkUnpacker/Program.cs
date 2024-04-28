using BinaryMemory;

namespace ShinThemeParkUnpacker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                Pause();
                return;
            }

            try
            {
                foreach (var path in args)
                {
                    if (File.Exists(path))
                    {
                        Unpack(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Repack(path);
                    }
                    else
                    {
                        throw new InvalidDataException($"Could not find a file or folder for: {path}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"{ex.Message}\n" +
                    $"{ex.StackTrace}\n\n" +
                    $"An error has occurred.");
                Pause();
            }
        }

        static void Unpack(string headerPath)
        {
            if (!headerPath.EndsWith(".HDT", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidDataException($"Provided HDT file does not end with the HDT extension: {headerPath}");
            }

            string dataPath = Path.ChangeExtension(headerPath, "BIN");
            string? outputFolder = Path.GetDirectoryName(headerPath);
            outputFolder ??= Path.GetPathRoot(headerPath);
            if (string.IsNullOrEmpty(outputFolder))
            {
                throw new DirectoryNotFoundException("Cannot find folder to place unpacked files into.");
            }

            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException($"BIN file does not exist: {dataPath}");
            }

            var headerReader = new BinaryMemoryReader(File.ReadAllBytes(headerPath), false);
            int headerByteLength = headerReader.Length;
            if (headerByteLength % 4 != 0)
            {
                throw new InvalidDataException($"Provided HDT byte length not divisible by 4, file is invalid: {headerPath}");
            }

            using var dataStream = File.OpenRead(dataPath);
            string unpackFolder = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(dataPath));
            Directory.CreateDirectory(unpackFolder);

            var offsetCount = headerByteLength / 4;
            var offsets = headerReader.ReadSpan<int>(offsetCount);
            int lengthsCount = offsetCount - 1;
            
            for (int i = 0; i < lengthsCount; i++)
            {
                int length = offsets[i + 1] - offsets[i];
                byte[] bytes = new byte[length];
                int readCount = dataStream.Read(bytes, 0, length);
                if (readCount < length)
                {
                    throw new InvalidDataException($"Could not read the requested number of bytes for file {i}: Read: {readCount}; Expected: {length}");
                }

                File.WriteAllBytes(Path.Combine(unpackFolder, i.ToString()), bytes);
            }
        }

        static void Repack(string folder)
        {
            if (!Directory.Exists(folder))
            {
                throw new DirectoryNotFoundException($"Folder to repack does not exist: {folder}");
            }

            string headerPath = $"{folder}.HDT";
            string dataPath = $"{folder}.BIN";

            string backupHeaderPath = $"{headerPath}.BAK";
            if (File.Exists(headerPath) && !File.Exists(backupHeaderPath))
            {
                File.Move(headerPath, backupHeaderPath);
            }

            string backupDataPath = $"{dataPath}.BAK";
            if (File.Exists(dataPath) && !File.Exists(backupDataPath))
            {
                File.Move(dataPath, backupDataPath);
            }

            using var headerStream = File.OpenWrite(headerPath);
            using var headerWriter = new BinaryWriter(headerStream);
            using var dataStream = File.OpenWrite(dataPath);

            var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                headerWriter.Write((int)dataStream.Position);
                byte[] bytes = File.ReadAllBytes(file);
                dataStream.Write(bytes, 0, bytes.Length);
            }
        }

        static void PrintUsage() => Console.WriteLine("Provide an HDT file to the unpacker by drag and dropping or passing its file path as an argument." +
            "Make sure the BIN data file is in the same folder under the same name before the extension.");

        static void Pause() => Console.ReadKey();
    }
}
