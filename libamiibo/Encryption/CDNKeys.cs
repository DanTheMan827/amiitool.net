﻿/*
 * Copyright (C) 2015 Marcos Vives Del Sol
 * Copyright (C) 2016 Benjamin Krämer
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using LibAmiibo.Helper;
using System.IO;
using System.Security.Cryptography;

namespace LibAmiibo.Encryption;

public class CDNKeys
{
    private byte[] aesIV;           // 16 bytes
    private List<byte[]> aesKeys;   // 16 bytes each

    internal static CDNKeys Unserialize(BinaryReader reader)
    {
        byte[] aesIV = reader.ReadBytes(16);
        List<byte[]> aesKeys = new List<byte[]>();
        while (reader.PeekChar() != -1)
            aesKeys.Add(reader.ReadBytes(16));

        return new CDNKeys
        {
            aesIV = aesIV,
            aesKeys = aesKeys
        };
    }

    public static CDNKeys LoadKeys()
    {
        using (var reader = new BinaryReader(new MemoryStream(KeyTables.CDN)))
        {
            var result = Unserialize(reader);

            if (result.aesKeys.Count < 4)
            {
                Console.Error.WriteLine("AES count missmatch");
                return null;
            }

            return result;
        }
    }

    public byte[] DecryptIcon(byte[] encrypted, int keyId)
    {
        byte[] result;

        using (var aesManaged = new AesManaged())
        {
            aesManaged.Key = aesKeys[keyId];
            aesManaged.IV = aesIV;
            aesManaged.Padding = PaddingMode.None;
            aesManaged.Mode = CipherMode.CBC;

            var transform = aesManaged.CreateDecryptor();

            using (var memoryStream = new MemoryStream())
            {
                using (var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(encrypted, 0, encrypted.Length);
                }

                result = memoryStream.ToArray();
            }
        }

        return result;
    }
}