using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace InfoTrack.NaqelAPI
{
    public class TransferFile
    {
        public TransferFile() { }

        public string WriteBinarFile(byte[] fs, string path, string fileName)
        {
            try
            {
                MemoryStream memoryStream = new MemoryStream(fs);
                FileStream fileStream = new FileStream(path + fileName, FileMode.Create);
                memoryStream.WriteTo(fileStream);
                memoryStream.Close();
                fileStream.Close();
                fileStream = null;
                memoryStream = null;
                return "File has already uploaded successfully。";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public byte[] ReadBinaryFile(string path, string fileName)
        {
            if (File.Exists(path + fileName))
            {
                try
                {
                    ///Open and read a file。
                    FileStream fileStream = File.OpenRead(path + fileName);
                    return GlobalVar.GV.ConvertStreamToByteBuffer(fileStream);
                }
                catch
                {
                    return new byte[0];
                }
            }
            else
            {
                return new byte[0];
            }
        }

        //public byte[] ConvertStreamToByteBuffer1(System.IO.Stream theStream)
        //{
        //    int b1;
        //    System.IO.MemoryStream tempStream = new System.IO.MemoryStream();
        //    while ((b1 = theStream.ReadByte()) != -1)
        //    {
        //        tempStream.WriteByte(((byte)b1));
        //    }
        //    return tempStream.ToArray();
        //}
    }
}