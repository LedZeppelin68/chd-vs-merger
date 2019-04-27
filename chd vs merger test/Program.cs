using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace chd_vs_merger_test
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                string test_file = arg;

                Dictionary<string, int> dupes = new Dictionary<string, int>();

                FileStream fs_for_ds = new FileStream(test_file + ".ds.mrg", FileMode.Create);
                FileStream gs_for_gs = new FileStream(test_file + ".gs.mrg", FileMode.Create);

                DeflateStream ds = new DeflateStream(fs_for_ds, CompressionLevel.Optimal);
                GZipStream gs = new GZipStream(gs_for_gs, CompressionLevel.Optimal);

                using (BinaryReader br = new BinaryReader(new FileStream(test_file, FileMode.Open)))
                {
                    while (br.BaseStream.Position != br.BaseStream.Length)
                    {
                        byte[] temp_block = br.ReadBytes(512);

                        string temp_block_md5 = CalcMD5(temp_block);

                        if (dupes.ContainsKey(temp_block_md5))
                        {

                        }
                        else
                        {
                            MemoryStream block = new MemoryStream(temp_block);
                            block.CopyTo(ds);
                            block.Position = 0;
                            block.CopyTo(gs);
                        }
                    }
                }

                ds.Dispose();
                gs.Dispose();

                fs_for_ds.Dispose();
                gs_for_gs.Dispose();
            }
        }

        private static string CalcMD5(byte[] temp_block)
        {
            string md5 = string.Empty;
            using (MD5 hash = MD5.Create())
            {
                md5 = BitConverter.ToString(hash.ComputeHash(temp_block)).Replace("-", "").ToLower();
            }
            return md5;
        }
    }
}