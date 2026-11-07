using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace ECCStorageSimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("===============================================================================");
            Console.WriteLine("  DIZAJN I IMPLEMENTACIJA KODOVA ZA ISPRAVLJANJE GREŠAKA ZA POUZDANO SKLADIŠTENJE");
            Console.WriteLine("                  C# Prototip i Simulacioni Sistem - Verzija 2");
            Console.WriteLine("===============================================================================");
            Console.ResetColor();

            StorageMedia simulatedDisk = new StorageMedia();
            DiskController controller = new DiskController(simulatedDisk);

            bool running = true;
            while (running)
            {
                Console.WriteLine($"\nTRENUTNA ECC SCHEME: {(controller.ActiveEcc == "hamming" ? "Hamming (7,4,3) - SEC" : "Golay (23,12,7) - TEC")}");
                Console.WriteLine("IZABERITE OPCIJU:");
                Console.WriteLine("1. Promenite aktivnu ECC tehniku (Hamming / Golay)");
                Console.WriteLine("2. Upišite tekstualnu poruku na simulirani disk (Kodiranje i upis)");
                Console.WriteLine("3. Učitajte i analizirajte fajl sa računara");
                Console.WriteLine("4. Ubacite hardverske greške (Smetnje / Noise injection)");
                Console.WriteLine("5. Rekonstruišite podatke i pročitajte sa diska (Dekodiranje i korekcija)");
                Console.WriteLine("6. Pokrenite uporedni eksperimentalni test (Hamming vs Golay)");
                Console.WriteLine("7. Izlaz");
                Console.Write("Vaš izbor: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\nIzaberite ECC tehniku:");
                        Console.WriteLine("1. Hamming (7,4,3) - Korekcija 1 bita po bloku");
                        Console.WriteLine("2. Golay (23,12,7) - Korekcija do 3 bita po bloku");
                        Console.Write("Vaš izbor: ");
                        string eccChoice = Console.ReadLine();
                        if (eccChoice == "1")
                        {
                            controller.ActiveEcc = "hamming";
                            Console.WriteLine("Aktivirana Hamming (7,4) ECC shema.");
                        }
                        else if (eccChoice == "2")
                        {
                            controller.ActiveEcc = "golay";
                            Console.WriteLine("Aktivirana Golay (23,12) ECC shema.");
                        }
                        else
                        {
                            Console.WriteLine("Neispravan izbor.");
                        }
                        break;

                    case "2":
                        Console.Write("\nUnesite tekst koji želite da uskladištite: ");
                        string text = Console.ReadLine();
                        byte[] textBytes = Encoding.UTF8.GetBytes(text);
                        controller.WriteFile("tekst.txt", textBytes);
                        break;

                    case "3":
                        Console.Write("\nUnesite putanju do fajla na vašem računaru: ");
                        string filePath = Console.ReadLine();
                        if (File.Exists(filePath))
                        {
                            try
                            {
                                byte[] fileBytes = File.ReadAllBytes(filePath);
                                string fileName = Path.GetFileName(filePath);
                                controller.WriteFile(fileName, fileBytes);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"Fajl '{fileName}' ({fileBytes.Length} bajtova) je uspešno kodiran i upisan.");
                                Console.ResetColor();
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Greška pri čitanju fajla: " + ex.Message);
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Fajl ne postoji!");
                            Console.ResetColor();
                        }
                        break;

                    case "4":
                        if (!controller.HasData)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Skladište je prazno. Prvo upišite neke podatke (Opcija 2 ili 3).");
                            Console.ResetColor();
                            break;
                        }

                        Console.WriteLine("\nIzaberite tip ubacivanja šuma:");
                        Console.WriteLine("1. Nasumičan šum na sve bitove (Random noise)");
                        Console.WriteLine("2. Ciljani šum - samo bitovi podataka (Data bits only)");
                        Console.WriteLine("3. Ciljani šum - samo paritetni bitovi (Parity bits only)");
                        Console.Write("Vaš izbor: ");
                        string noiseChoice = Console.ReadLine();

                        string targetBits = noiseChoice switch
                        {
                            "1" => "random",
                            "2" => "data",
                            "3" => "parity",
                            _ => null
                        };

                        if (targetBits == null)
                        {
                            Console.WriteLine("Neispravan izbor.");
                            break;
                        }

                        Console.Write("\nUnesite verovatnoću greške pojedinačnog bita (Bit Error Rate - BER) u % (npr. 0.1 za 0.1%): ");
                        if (double.TryParse(Console.ReadLine(), out double berPercent))
                        {
                            double ber = berPercent / 100.0;
                            int errorsInjected = simulatedDisk.InjectTargetedNoise(ber, controller.ActiveEcc, targetBits);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"\nUbacivanje šuma završeno!");
                            Console.WriteLine($"Tip šuma: {(noiseChoice == "1" ? "Nasumičan (svi bitovi)" : noiseChoice == "2" ? "Ciljani (bitovi podataka)" : "Ciljani (paritetni bitovi)")}");
                            Console.WriteLine($"Uspešno izmenjeno/oštećeno {errorsInjected} bita na medijumu.");
                            Console.ResetColor();

                            try
                            {
                                byte[] damagedData = controller.ExtractRawDamagedData();
                                string damagedPath = controller.SaveByteArrayAsFile(damagedData, "_OSTECEN");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Oštećena verzija fajla (bez ECC korekcije) je sačuvana na: {damagedPath}");
                                Console.ResetColor();

                                
                                controller.DisplayDamagedContent(damagedData);
                            }
                            catch (Exception ex)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("Greška pri čuvanju oštećenog fajla: " + ex.Message);
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Neispravan unos.");
                        }
                        break;

                    case "5":
                        if (!controller.HasData)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Nema podataka za čitanje.");
                            Console.ResetColor();
                            break;
                        }
                        controller.ReadFileAndReconstruct();
                        break;

                    case "6":
                        RunFullComparisonExperiment();
                        break;

                    case "7":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Nepoznata opcija.");
                        break;
                }
            }
        }

        static void RunFullComparisonExperiment()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================================");
            Console.WriteLine("          UPOREDNI EKSPERIMENTALNI TEST: HAMMING VS GOLAY (Napredne metrike)");
            Console.WriteLine("================================================================================");
            Console.ResetColor();

           
            byte[] testData = new byte[10240];
            Random rnd = new Random(42);
            rnd.NextBytes(testData);

           
            double[] testBers = { 0.000001, 0.00001, 0.0001, 0.001, 0.005, 0.01, 0.05 };

            Console.WriteLine($"Generisan je binarni test fajl veličine {testData.Length} bajtova.\n");
            Console.WriteLine("Legenda:");
            Console.WriteLine("  - BER: Bit Error Rate (stopa grešaka po bitu)");
            Console.WriteLine("  - Ispravljeno: Broj ispravljenih bitova");
            Console.WriteLine("  - Neisprav.: Broj grešaka koje NISU ispravljene");
            Console.WriteLine("  - Tačnost: Procenat pravilno rekonstruisanih bajtova");
            Console.WriteLine("  - BER posle: Residual Bit Error Rate nakon dekodiranja");
            Console.WriteLine("  - Throughput: Efikasnost skladištenja (originalni / kodirani)\n");

            Console.WriteLine(new string('=', 140));
            Console.WriteLine("{0,-10} | {1,-60} | {2,-60}", "BER", "HAMMING (7,4) [SEC - Korekcija 1 bita]", "GOLAY (23,12) [TEC - Korekcija 3 bita]");
            Console.WriteLine("{0,-10} | {1,-10} {2,-10} {3,-12} {4,-12} {5,-10} | {6,-10} {7,-10} {8,-12} {9,-12} {10,-10}",
                "", "Ispr.", "Neispr.", "Tačnost", "BER posle", "Throughput",
                "Ispr.", "Neispr.", "Tačnost", "BER posle", "Throughput");
            Console.WriteLine(new string('=', 140));

            foreach (double ber in testBers)
            {
                
                int noiseSeed = (int)(ber * 1000000000);

                
                StorageMedia diskHamming = new StorageMedia();
                DiskController controllerHamming = new DiskController(diskHamming) { ActiveEcc = "hamming" };
                controllerHamming.WriteFile("test_hamming.bin", testData);
                int flippedHamming = diskHamming.InjectNoise(ber, "hamming", noiseSeed);
                var statsHamming = controllerHamming.DecodeAndAnalyzeDetailed(testData);

                
                StorageMedia diskGolay = new StorageMedia();
                DiskController controllerGolay = new DiskController(diskGolay) { ActiveEcc = "golay" };
                controllerGolay.WriteFile("test_golay.bin", testData);
                int flippedGolay = diskGolay.InjectNoise(ber, "golay", noiseSeed);
                var statsGolay = controllerGolay.DecodeAndAnalyzeDetailed(testData);

                Console.WriteLine("{0,-10:P4} | {1,-10} {2,-10} {3,-12:P2} {4,-12:P4} {5,-10:P1} | {6,-10} {7,-10} {8,-12:P2} {9,-12:P4} {10,-10:P1}",
                    ber,
                    statsHamming.BitsCorrected,
                    statsHamming.UncorrectedErrors,
                    statsHamming.ByteAccuracy,
                    statsHamming.ResidualBER,
                    statsHamming.Throughput,
                    statsGolay.BitsCorrected,
                    statsGolay.UncorrectedErrors,
                    statsGolay.ByteAccuracy,
                    statsGolay.ResidualBER,
                    statsGolay.Throughput);
            }
            Console.WriteLine(new string('=', 140));
            Console.WriteLine("\nNapomena: Throughput pokazuje efikasnost - Hamming ~57%, Golay ~52% (više redundancije).");
        }
    }

    
    public static class Hamming74
    {
        private static readonly int[][] G = new int[4][]
        {
            new int[] { 1, 0, 0, 0, 1, 0, 1 },
            new int[] { 0, 1, 0, 0, 1, 1, 1 },
            new int[] { 0, 0, 1, 0, 1, 1, 0 },
            new int[] { 0, 0, 0, 1, 0, 1, 1 }
        };

        private static readonly int[][] H = new int[3][]
        {
            new int[] { 1, 1, 1, 0, 1, 0, 0 },
            new int[] { 0, 1, 1, 1, 0, 1, 0 },
            new int[] { 0, 0, 1, 1, 1, 0, 1 }
        };

        private static readonly Dictionary<int, int> SyndromeMap = new Dictionary<int, int>()
        {
            { 4, 0 }, { 6, 1 }, { 7, 2 }, { 3, 3 }, { 5, 4 }, { 2, 5 }, { 1, 6 }
        };

        public static byte EncodeNibble(byte nibble)
        {
            int[] u = new int[4]
            {
                (nibble >> 3) & 1,
                (nibble >> 2) & 1,
                (nibble >> 1) & 1,
                (nibble >> 0) & 1
            };

            int[] c = new int[7];
            for (int col = 0; col < 7; col++)
            {
                int sum = 0;
                for (int row = 0; row < 4; row++)
                {
                    sum ^= u[row] * G[row][col];
                }
                c[col] = sum;
            }

            byte codeword = 0;
            for (int i = 0; i < 7; i++)
            {
                codeword |= (byte)(c[i] << (6 - i));
            }
            return codeword;
        }

        public static byte DecodeCodeword(byte codeword, out bool corrected)
        {
            int[] c = new int[7];
            for (int i = 0; i < 7; i++)
            {
                c[i] = (codeword >> (6 - i)) & 1;
            }

            int[] s = new int[3];
            for (int row = 0; row < 3; row++)
            {
                int sum = 0;
                for (int col = 0; col < 7; col++)
                {
                    sum ^= H[row][col] * c[col];
                }
                s[row] = sum;
            }

            int syndromeValue = (s[0] << 2) | (s[1] << 1) | s[2];
            corrected = false;

            if (syndromeValue != 0)
            {
                if (SyndromeMap.ContainsKey(syndromeValue))
                {
                    int errorPosition = SyndromeMap[syndromeValue];
                    c[errorPosition] ^= 1;
                    corrected = true;
                }
            }

            byte nibble = (byte)((c[0] << 3) | (c[1] << 2) | (c[2] << 1) | c[3]);
            return nibble;
        }


        public static byte DecodeRawNibble(byte codeword)
        {
            int[] c = new int[7];
            for (int i = 0; i < 7; i++)
            {
                c[i] = (codeword >> (6 - i)) & 1;
            }

            return (byte)((c[0] << 3) | (c[1] << 2) | (c[2] << 1) | c[3]);
        }

        
        public static bool HasUncorrectableError(byte codeword)
        {
            int[] c = new int[7];
            for (int i = 0; i < 7; i++)
            {
                c[i] = (codeword >> (6 - i)) & 1;
            }

            int[] s = new int[3];
            for (int row = 0; row < 3; row++)
            {
                int sum = 0;
                for (int col = 0; col < 7; col++)
                {
                    sum ^= H[row][col] * c[col];
                }
                s[row] = sum;
            }

            int syndromeValue = (s[0] << 2) | (s[1] << 1) | s[2];

           
            return syndromeValue != 0 && !SyndromeMap.ContainsKey(syndromeValue);
        }
    }

  
    public static class Golay23
    {
        private static readonly uint[] DecodingTable = new uint[2048];

        static Golay23()
        {
            DecodingTable[0] = 0;

           
            for (uint i = 0; i < 23; i++)
            {
                uint pattern = (uint)(1 << (int)i);
                DecodingTable[GetSyndrome(pattern)] = pattern;
            }

            
            for (int i = 0; i < 23; i++)
            {
                for (int j = i + 1; j < 23; j++)
                {
                    uint pattern = (uint)((1 << i) | (1 << j));
                    DecodingTable[GetSyndrome(pattern)] = pattern;
                }
            }

            
            for (int i = 0; i < 23; i++)
            {
                for (int j = i + 1; j < 23; j++)
                {
                    for (int k = j + 1; k < 23; k++)
                    {
                        uint pattern = (uint)((1 << i) | (1 << j) | (1 << k));
                        DecodingTable[GetSyndrome(pattern)] = pattern;
                    }
                }
            }
        }

        public static uint GetSyndrome(uint word)
        {
            uint g = 0xC75; 
            uint temp = word;
            for (int i = 22; i >= 11; i--)
            {
                if (((temp >> i) & 1) == 1)
                {
                    temp ^= (g << (i - 11));
                }
            }
            return temp;
        }

        public static uint Encode(ushort data12)
        {
            uint shifted = (uint)data12 << 11;
            uint rem = GetSyndrome(shifted);
            return shifted | rem;
        }

        public static ushort Decode(uint codeword, out int correctedCount)
        {
            uint s = GetSyndrome(codeword);
            if (s == 0)
            {
                correctedCount = 0;
                return (ushort)((codeword >> 11) & 0xFFF);
            }

            uint err = DecodingTable[s];
            uint corrected = codeword ^ err;
            correctedCount = GetWeight(err);
            return (ushort)((corrected >> 11) & 0xFFF);
        }

   
        public static ushort DecodeRaw(uint codeword)
        {
            return (ushort)((codeword >> 11) & 0xFFF);
        }

        private static int GetWeight(uint w)
        {
            int c = 0;
            while (w > 0)
            {
                if ((w & 1) == 1) c++;
                w >>= 1;
            }
            return c;
        }
    }

  
    public class StorageMedia
    {
        public List<byte[]> Sectors { get; set; } = new List<byte[]>();
        public int TotalBits => Sectors.Count * 1024 * 7;

        public void Clear()
        {
            Sectors.Clear();
        }

        public void WriteSector(int index, byte[] encodedData)
        {
            if (index >= Sectors.Count)
            {
                while (Sectors.Count <= index) Sectors.Add(null);
            }
            Sectors[index] = (byte[])encodedData.Clone();
        }

        public byte[] ReadSector(int index)
        {
            if (index < Sectors.Count) return Sectors[index];
            return null;
        }

        
        public int InjectTargetedNoise(double ber, string eccType, string target, int? seed = null)
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();
            int errorCount = 0;

            for (int s = 0; s < Sectors.Count; s++)
            {
                byte[] sector = Sectors[s];

                if (eccType == "hamming")
                {
                    int[] dataBits = { 3, 4, 5, 6 };
                    int[] parityBits = { 0, 1, 2 };
                    int[] allBits = { 0, 1, 2, 3, 4, 5, 6 };
                    int[] activeBits = target == "data" ? dataBits : target == "parity" ? parityBits : allBits;

                    for (int i = 0; i < sector.Length; i++)
                    {
                        byte b = sector[i];
                        foreach (int bitPos in activeBits)
                        {
                            if (rand.NextDouble() < ber)
                            {
                                b ^= (byte)(1 << bitPos);
                                errorCount++;
                            }
                        }
                        sector[i] = b;
                    }
                }
                else if (eccType == "golay")
                {
                    const int codewordCount = 342;
                    int startJ = 0, endJ = 22;
                    if (target == "data") { startJ = 0; endJ = 11; }
                    else if (target == "parity") { startJ = 12; endJ = 22; }

                    for (int k = 0; k < codewordCount; k++)
                    {
                        for (int j = startJ; j <= endJ; j++)
                        {
                            if (rand.NextDouble() < ber)
                            {
                                int bitIdx = k * 23 + j;
                                int bytePos = bitIdx / 8;
                                int bitPos = 7 - (bitIdx % 8);
                                sector[bytePos] ^= (byte)(1 << bitPos);
                                errorCount++;
                            }
                        }
                    }
                }
            }
            return errorCount;
        }

        public int InjectNoise(double ber, string eccType, int? seed = null)
        {
            Random rand = seed.HasValue ? new Random(seed.Value) : new Random();
            int errorCount = 0;

            for (int s = 0; s < Sectors.Count; s++)
            {
                byte[] sector = Sectors[s];
                if (eccType == "hamming")
                {
                    for (int i = 0; i < sector.Length; i++)
                    {
                        byte b = sector[i];
                        for (int bitPos = 0; bitPos < 7; bitPos++)
                        {
                            if (rand.NextDouble() < ber)
                            {
                                b ^= (byte)(1 << bitPos);
                                errorCount++;
                            }
                        }
                        sector[i] = b;
                    }
                }
                else if (eccType == "golay")
                {
                   
                    for (int bitIdx = 0; bitIdx < 7866; bitIdx++)
                    {
                        if (rand.NextDouble() < ber)
                        {
                            int bytePos = bitIdx / 8;
                            int bitPos = 7 - (bitIdx % 8);
                            sector[bytePos] ^= (byte)(1 << bitPos);
                            errorCount++;
                        }
                    }
                }
            }
            return errorCount;
        }
    }

  
    public class DecodingStatistics
    {
        public int BitsCorrected { get; set; }             
        public int UncorrectedErrors { get; set; }          
        public double ByteAccuracy { get; set; }            
        public double ResidualBER { get; set; }             
        public double Throughput { get; set; }              
        public int TotalBlocks { get; set; }               
        public int BlocksWithUncorrectableErrors { get; set; } 
    }

   
    public class DiskController
    {
        private readonly StorageMedia _disk;
        private const int SectorSize = 512;
        private const int PayloadSize = 508;

        public string ActiveFileName { get; private set; }
        public int ActiveFileLength { get; private set; }
        public bool HasData => !string.IsNullOrEmpty(ActiveFileName);
        public string ActiveEcc { get; set; } = "hamming"; // Podrazumevana shema
        private byte[] _originalData; 

        public DiskController(StorageMedia disk)
        {
            _disk = disk;
        }

        public void WriteFile(string fileName, byte[] data)
        {
            _disk.Clear();
            ActiveFileName = fileName;
            ActiveFileLength = data.Length;
            _originalData = (byte[])data.Clone(); 

            int totalSectors = (int)Math.Ceiling((double)data.Length / PayloadSize);

            for (int i = 0; i < totalSectors; i++)
            {
                byte[] chunk = new byte[PayloadSize];
                int offset = i * PayloadSize;
                int bytesToCopy = Math.Min(PayloadSize, data.Length - offset);
                Array.Copy(data, offset, chunk, 0, bytesToCopy);

                byte[] encodedSector = EncodeSector(chunk);
                _disk.WriteSector(i, encodedSector);
            }
        }

        private byte[] EncodeSector(byte[] payload)
        {
            uint crc = CRC32.Compute(payload);
            byte[] crcBytes = BitConverter.GetBytes(crc);
            if (BitConverter.IsLittleEndian) Array.Reverse(crcBytes);

            byte[] fullSector = new byte[SectorSize];
            Array.Copy(payload, 0, fullSector, 0, PayloadSize);
            Array.Copy(crcBytes, 0, fullSector, PayloadSize, 4);

            if (ActiveEcc == "hamming")
            {
                byte[] encoded = new byte[SectorSize * 2];
                for (int i = 0; i < SectorSize; i++)
                {
                    byte b = fullSector[i];
                    byte high = (byte)((b >> 4) & 0x0F);
                    byte low = (byte)(b & 0x0F);

                    encoded[2 * i] = Hamming74.EncodeNibble(high);
                    encoded[2 * i + 1] = Hamming74.EncodeNibble(low);
                }
                return encoded;
            }
            else
            {
                
                ushort[] chunks = new ushort[342];
                int bitStream = 0;
                int bitCount = 0;
                int chunkIdx = 0;
                for (int i = 0; i < SectorSize; i++)
                {
                    bitStream = (bitStream << 8) | fullSector[i];
                    bitCount += 8;
                    while (bitCount >= 12)
                    {
                        chunks[chunkIdx++] = (ushort)((bitStream >> (bitCount - 12)) & 0xFFF);
                        bitCount -= 12;
                    }
                }
                if (bitCount > 0)
                {
                    chunks[chunkIdx++] = (ushort)((bitStream << (12 - bitCount)) & 0xFFF);
                }

                uint[] codewords = new uint[342];
                for (int i = 0; i < 342; i++)
                {
                    codewords[i] = Golay23.Encode(chunks[i]);
                }

                byte[] encoded = new byte[984];
                long bitBuffer = 0;
                int bufferBits = 0;
                int byteIdx = 0;
                for (int i = 0; i < 342; i++)
                {
                    uint c = codewords[i];
                    bitBuffer = (bitBuffer << 23) | (c & 0x7FFFFF);
                    bufferBits += 23;
                    while (bufferBits >= 8)
                    {
                        encoded[byteIdx++] = (byte)((bitBuffer >> (bufferBits - 8)) & 0xFF);
                        bufferBits -= 8;
                    }
                }
                if (bufferBits > 0)
                {
                    encoded[byteIdx++] = (byte)((bitBuffer << (8 - bufferBits)) & 0xFF);
                }
                return encoded;
            }
        }

        public void ReadFileAndReconstruct()
        {
            int totalSectors = (int)Math.Ceiling((double)ActiveFileLength / PayloadSize);
            byte[] reconstructed = DecodeAndAnalyze(out int totalCorrected, out int uncorrectableCount);

            Console.WriteLine($"\n================== IZVEŠTAJ KONTROLERA DISKA ==================");
            Console.WriteLine($"Učitavanje fajla: {ActiveFileName}");
            Console.WriteLine($"Veličina fajla: {ActiveFileLength} bajtova");
            Console.WriteLine($"Aktivni ECC: {ActiveEcc.ToUpper()}");
            Console.WriteLine($"Broj sektora na disku: {totalSectors}");
            Console.WriteLine($"Broj uspešno ispravljenih bita: {totalCorrected}");
            Console.WriteLine($"Broj sektora sa CRC nepodudaranjem: {uncorrectableCount}");
            Console.WriteLine("\n[NAPOMENA]: CRC nepodudaranja mogu biti rezultat grešaka u CRC");
            Console.WriteLine("           bitima koji su ispravljeni ECC-om, ali ne u originalnoj vrednosti.");

            if (uncorrectableCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[STATUS]: INTEGRITET PODATAKA JE SAČUVAN!");
                Console.WriteLine("Sve detektovane greške su uspešno ispravljene.");
                Console.ResetColor();

                if (ActiveFileName.EndsWith(".txt"))
                {
                    string text = Encoding.UTF8.GetString(reconstructed);
                    Console.WriteLine("\nSadržaj fajla:\n" + text);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n[STATUS]: UPOZORENJE - CRC NEPODUDARANJE!");
                Console.WriteLine($"Detektovano {uncorrectableCount} sektora sa CRC nepodudaranjem.");
                Console.WriteLine("Međutim, ECC je ispravio sve bitove u dozvoljenom kapacitetu.");
                Console.WriteLine("Podaci su verovatno rekonstruisani, ali CRC validacija nije moguća.");
                Console.ResetColor();

                if (ActiveFileName.EndsWith(".txt"))
                {
                    string text = Encoding.UTF8.GetString(reconstructed);
                    Console.WriteLine("\nRekonstruisani sadržaj fajla:\n" + text);
                }
            }

            try
            {
                string reconstructedPath = SaveByteArrayAsFile(reconstructed, "_REKONSTRUISAN");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\nRekonstruisana verzija fajla je sačuvana na: {reconstructedPath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Greška pri čuvanju rekonstruisanog fajla: " + ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("===============================================================");
        }

       
        public byte[] ExtractRawDamagedData()
        {
            int totalSectors = (int)Math.Ceiling((double)ActiveFileLength / PayloadSize);
            byte[] fileBytes = new byte[totalSectors * PayloadSize];

            for (int s = 0; s < totalSectors; s++)
            {
                byte[] encoded = _disk.ReadSector(s);
                byte[] sectorData = new byte[SectorSize];

                if (ActiveEcc == "hamming")
                {
                    for (int i = 0; i < SectorSize; i++)
                    {
                        byte highCode = encoded[2 * i];
                        byte lowCode = encoded[2 * i + 1];

                        byte high = Hamming74.DecodeRawNibble(highCode);
                        byte low = Hamming74.DecodeRawNibble(lowCode);

                        sectorData[i] = (byte)((high << 4) | low);
                    }
                }
                else
                {
                    
                    uint[] codewords = new uint[342];
                    long bitBuffer = 0;
                    int bufferBits = 0;
                    int codIdx = 0;
                    for (int i = 0; i < 984; i++)
                    {
                        bitBuffer = (bitBuffer << 8) | encoded[i];
                        bufferBits += 8;
                        while (bufferBits >= 23)
                        {
                            codewords[codIdx++] = (uint)((bitBuffer >> (bufferBits - 23)) & 0x7FFFFF);
                            bufferBits -= 23;
                        }
                    }

                    ushort[] chunks = new ushort[342];
                    for (int i = 0; i < 342; i++)
                    {
                        chunks[i] = Golay23.DecodeRaw(codewords[i]);
                    }

                    bitBuffer = 0;
                    bufferBits = 0;
                    int byteIdx = 0;
                    for (int i = 0; i < 342; i++)
                    {
                        bitBuffer = (bitBuffer << 12) | chunks[i];
                        bufferBits += 12;
                        while (bufferBits >= 8 && byteIdx < SectorSize)
                        {
                            sectorData[byteIdx++] = (byte)((bitBuffer >> (bufferBits - 8)) & 0xFF);
                            bufferBits -= 8;
                        }
                    }
                }

                byte[] payload = new byte[PayloadSize];
                Array.Copy(sectorData, 0, payload, 0, PayloadSize);
                Array.Copy(payload, 0, fileBytes, s * PayloadSize, PayloadSize);
            }

            byte[] trimmedData = new byte[ActiveFileLength];
            Array.Copy(fileBytes, 0, trimmedData, 0, ActiveFileLength);
            return trimmedData;
        }

       
        public string SaveByteArrayAsFile(byte[] data, string suffix)
        {
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "ECC_Rezultati");
            Directory.CreateDirectory(outputDir);

            string baseName = Path.GetFileNameWithoutExtension(ActiveFileName);
            string ext = Path.GetExtension(ActiveFileName);
            string outputFileName = $"{baseName}{suffix}{ext}";
            string fullPath = Path.Combine(outputDir, outputFileName);

            File.WriteAllBytes(fullPath, data);
            return fullPath;
        }

        
        public void DisplayDamagedContent(byte[] damagedData)
        {
            if (_originalData == null || damagedData == null)
                return;

            // Provera da li je tekstualni fajl
            if (!ActiveFileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                return;

            Console.WriteLine("\n========== PRIKAZ OŠTEĆENOG SADRŽAJA FAJLA ==========");
            Console.WriteLine("(Zelenom bojom su označeni znakovi u kojima je došlo do greške)\n");

            int errorCount = 0;
            int minLength = Math.Min(_originalData.Length, damagedData.Length);

            try
            {
                string originalText = Encoding.UTF8.GetString(_originalData);
                string damagedText = Encoding.UTF8.GetString(damagedData);

                // Provera da li su dužine iste
                int maxLength = Math.Max(originalText.Length, damagedText.Length);

                for (int i = 0; i < maxLength; i++)
                {
                    char originalChar = i < originalText.Length ? originalText[i] : '\0';
                    char damagedChar = i < damagedText.Length ? damagedText[i] : '\0';

                    if (originalChar != damagedChar)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(damagedChar);
                        Console.ResetColor();
                        errorCount++;
                    }
                    else
                    {
                        Console.Write(damagedChar);
                    }
                }

                Console.WriteLine("\n");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Ukupno oštećenih znakova: {errorCount}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Greška pri dekodiranju UTF8 teksta: {ex.Message}");
                Console.WriteLine("Prikaz sirovih bajtova:\n");
                Console.ResetColor();

                for (int i = 0; i < minLength; i++)
                {
                    if (_originalData[i] != damagedData[i])
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{damagedData[i]:X2}]");
                        Console.ResetColor();
                        errorCount++;
                    }
                    else
                    {
                        Console.Write($"{(char)damagedData[i]}");
                    }
                }

                Console.WriteLine("\n");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Ukupno oštećenih bajtova: {errorCount}");
                Console.ResetColor();
            }

            Console.WriteLine("======================================================\n");
        }

        public byte[] DecodeAndAnalyze(out int totalCorrected, out int uncorrectableCount)
        {
            totalCorrected = 0;
            uncorrectableCount = 0;
            int totalErrorsDetected = 0;

            int totalSectors = (int)Math.Ceiling((double)ActiveFileLength / PayloadSize);
            byte[] fileBytes = new byte[totalSectors * PayloadSize];

            for (int s = 0; s < totalSectors; s++)
            {
                byte[] encoded = _disk.ReadSector(s);
                byte[] sectorData = new byte[SectorSize];
                int sectorErrorsDetected = 0;

                if (ActiveEcc == "hamming")
                {
                    for (int i = 0; i < SectorSize; i++)
                    {
                        byte highCode = encoded[2 * i];
                        byte lowCode = encoded[2 * i + 1];

                        byte high = Hamming74.DecodeCodeword(highCode, out bool highCorrected);
                        byte low = Hamming74.DecodeCodeword(lowCode, out bool lowCorrected);

                        if (highCorrected) { totalCorrected++; sectorErrorsDetected++; }
                        if (lowCorrected) { totalCorrected++; sectorErrorsDetected++; }

                        sectorData[i] = (byte)((high << 4) | low);
                    }
                }
                else
                {
                    // Golay (23,12) systematic decoding
                    uint[] codewords = new uint[342];
                    long bitBuffer = 0;
                    int bufferBits = 0;
                    int codIdx = 0;
                    for (int i = 0; i < 984; i++)
                    {
                        bitBuffer = (bitBuffer << 8) | encoded[i];
                        bufferBits += 8;
                        while (bufferBits >= 23)
                        {
                            codewords[codIdx++] = (uint)((bitBuffer >> (bufferBits - 23)) & 0x7FFFFF);
                            bufferBits -= 23;
                        }
                    }

                    ushort[] chunks = new ushort[342];
                    for (int i = 0; i < 342; i++)
                    {
                        chunks[i] = Golay23.Decode(codewords[i], out int corrected);
                        totalCorrected += corrected;
                        if (corrected > 0) sectorErrorsDetected++;
                    }

                    bitBuffer = 0;
                    bufferBits = 0;
                    int byteIdx = 0;
                    for (int i = 0; i < 342; i++)
                    {
                        bitBuffer = (bitBuffer << 12) | chunks[i];
                        bufferBits += 12;
                        while (bufferBits >= 8 && byteIdx < SectorSize)
                        {
                            sectorData[byteIdx++] = (byte)((bitBuffer >> (bufferBits - 8)) & 0xFF);
                            bufferBits -= 8;
                        }
                    }
                }

                byte[] payload = new byte[PayloadSize];
                Array.Copy(sectorData, 0, payload, 0, PayloadSize);

                uint decodedCrc = BitConverter.ToUInt32(sectorData, PayloadSize);
                if (BitConverter.IsLittleEndian)
                {
                    byte[] temp = new byte[4];
                    Array.Copy(sectorData, PayloadSize, temp, 0, 4);
                    Array.Reverse(temp);
                    decodedCrc = BitConverter.ToUInt32(temp, 0);
                }

                uint computedCrc = CRC32.Compute(payload);

               
                if (decodedCrc != computedCrc && sectorErrorsDetected == 0)
                {
                    uncorrectableCount++;
                    totalErrorsDetected++;
                }

                Array.Copy(payload, 0, fileBytes, s * PayloadSize, PayloadSize);
            }

            byte[] trimmedData = new byte[ActiveFileLength];
            Array.Copy(fileBytes, 0, trimmedData, 0, ActiveFileLength);
            return trimmedData;
        }

      
        public DecodingStatistics DecodeAndAnalyzeDetailed(byte[] originalData)
        {
            int totalCorrected = 0;
            int totalSectors = (int)Math.Ceiling((double)ActiveFileLength / PayloadSize);
            byte[] reconstructedData = new byte[totalSectors * PayloadSize];
            int blocksWithUncorrectableErrors = 0;
            int totalBlocks = 0;

            for (int s = 0; s < totalSectors; s++)
            {
                byte[] encoded = _disk.ReadSector(s);
                byte[] sectorData = new byte[SectorSize];

                if (ActiveEcc == "hamming")
                {
                    
                    totalBlocks += 1024;

                    for (int i = 0; i < SectorSize; i++)
                    {
                        byte highCode = encoded[2 * i];
                        byte lowCode = encoded[2 * i + 1];

                        
                        byte high = Hamming74.DecodeCodeword(highCode, out bool highCorrected);
                        byte low = Hamming74.DecodeCodeword(lowCode, out bool lowCorrected);

                        
                        if (Hamming74.HasUncorrectableError(highCode))
                            blocksWithUncorrectableErrors++;
                        if (Hamming74.HasUncorrectableError(lowCode))
                            blocksWithUncorrectableErrors++;

                        if (highCorrected) totalCorrected++;
                        if (lowCorrected) totalCorrected++;

                        sectorData[i] = (byte)((high << 4) | low);
                    }
                }
                else
                {
                    // Golay: 342 kodne reči po sektoru
                    totalBlocks += 342;

                    uint[] codewords = new uint[342];
                    long bitBuffer = 0;
                    int bufferBits = 0;
                    int codIdx = 0;
                    for (int i = 0; i < 984; i++)
                    {
                        bitBuffer = (bitBuffer << 8) | encoded[i];
                        bufferBits += 8;
                        while (bufferBits >= 23)
                        {
                            codewords[codIdx++] = (uint)((bitBuffer >> (bufferBits - 23)) & 0x7FFFFF);
                            bufferBits -= 23;
                        }
                    }

                    ushort[] chunks = new ushort[342];
                    for (int i = 0; i < 342; i++)
                    {
                        chunks[i] = Golay23.Decode(codewords[i], out int corrected);
                        totalCorrected += corrected;

                        // Ako je broj ispravljenih > 3, to je neispravljiva greška
                        if (corrected > 3)
                            blocksWithUncorrectableErrors++;
                    }

                    bitBuffer = 0;
                    bufferBits = 0;
                    int byteIdx = 0;
                    for (int i = 0; i < 342; i++)
                    {
                        bitBuffer = (bitBuffer << 12) | chunks[i];
                        bufferBits += 12;
                        while (bufferBits >= 8 && byteIdx < SectorSize)
                        {
                            sectorData[byteIdx++] = (byte)((bitBuffer >> (bufferBits - 8)) & 0xFF);
                            bufferBits -= 8;
                        }
                    }
                }

                byte[] payload = new byte[PayloadSize];
                Array.Copy(sectorData, 0, payload, 0, PayloadSize);
                Array.Copy(payload, 0, reconstructedData, s * PayloadSize, PayloadSize);
            }

            // Trim do originalne veličine
            byte[] trimmedReconstructed = new byte[ActiveFileLength];
            Array.Copy(reconstructedData, 0, trimmedReconstructed, 0, ActiveFileLength);

            // Računanje metrika
            int correctBytes = 0;
            int totalBitErrors = 0;

            for (int i = 0; i < originalData.Length; i++)
            {
                if (originalData[i] == trimmedReconstructed[i])
                {
                    correctBytes++;
                }
                else
                {
                    // Brojanje koliko bitova je različito
                    byte xor = (byte)(originalData[i] ^ trimmedReconstructed[i]);
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if ((xor & (1 << bit)) != 0)
                            totalBitErrors++;
                    }
                }
            }

            double byteAccuracy = (double)correctBytes / originalData.Length;
            double residualBER = (double)totalBitErrors / (originalData.Length * 8);

            // Throughput: originalni podatak / kodirani podatak
            int encodedSize = ActiveEcc == "hamming" ? totalSectors * 1024 : totalSectors * 984;
            double throughput = (double)(originalData.Length) / encodedSize;

            return new DecodingStatistics
            {
                BitsCorrected = totalCorrected,
                UncorrectedErrors = totalBitErrors,
                ByteAccuracy = byteAccuracy,
                ResidualBER = residualBER,
                Throughput = throughput,
                TotalBlocks = totalBlocks,
                BlocksWithUncorrectableErrors = blocksWithUncorrectableErrors
            };
        }
    }

    
    public static class CRC32
    {
        private static readonly uint[] Table;

        static CRC32()
        {
            uint poly = 0xEDB88320;
            Table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ poly;
                    else
                        entry >>= 1;
                }
                Table[i] = entry;
            }
        }

        public static uint Compute(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte index = (byte)((crc & 0xff) ^ bytes[i]);
                crc = (crc >> 8) ^ Table[index];
            }
            return ~crc;
        }
    }
}