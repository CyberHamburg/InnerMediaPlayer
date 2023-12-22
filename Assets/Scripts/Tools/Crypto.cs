using System;
using System.Security.Cryptography;
using System.Text;
using LitJson;

namespace InnerMediaPlayer.Tools
{
    internal class Crypto
    {
        internal readonly byte[] iv;
        internal readonly byte[] firstKey;
        internal readonly byte[] lastKey;
        internal string _encSecKey;
        internal const string LastKeyString = "YpG1fbxNTQXjldXE";

        private readonly StringBuilder _md5StringBuilder;

        internal Crypto()
        {
            _md5StringBuilder = new StringBuilder(40);
            iv = Encoding.UTF8.GetBytes("0102030405060708");
            firstKey = Encoding.UTF8.GetBytes("0CoJUm6Qyw8W8jud");
            lastKey = Encoding.UTF8.GetBytes(LastKeyString);
            _encSecKey =
                "d569d0d55e4864407c738f3cc0d1921cee0259f56ac84a2239d274a6342756ed0dd5614731718e51aa5f94dc109b8ca203e2824070cc4644b8cc25ed1cd66cc8b21bc74cdbf102c9aa4b17d1762d3decccce939199b46ed6119a64a92e51da92424fd1ea1fe51d5a9ac2fcf5144697be0c7c98a214bf30fa6f39224f7f15efd6";
        }

        internal string Encrypt<T>(T data) where T : class
        {
            string json = JsonMapper.ToJson(data);
            string encryptOne = AesEncrypt(json, iv, firstKey);
            string encryptTwo = AesEncrypt(encryptOne, iv, lastKey);
            return encryptTwo;
        }

        internal string Encrypt(string json)
        {
            string encryptOne = AesEncrypt(json, iv, firstKey);
            string encryptTwo = AesEncrypt(encryptOne, iv, lastKey);
            return encryptTwo;
        }

        internal string AesEncrypt(string content, byte[] iv, byte[] key)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(content);
            using SymmetricAlgorithm aes = Aes.Create();
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

            using ICryptoTransform enCrypt = aes.CreateEncryptor();
            byte[] result = enCrypt.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
            aes.Clear();
            return Convert.ToBase64String(result);
        }

        internal bool Md5Verify(string md5, byte[] data)
        {
            MD5 computeMD5 = MD5.Create();
            byte[] result = computeMD5.ComputeHash(data);
            _md5StringBuilder.Clear();
            foreach (byte hash in result)
            {
                _md5StringBuilder.Append(hash.ToString("x2"));
            }
            return _md5StringBuilder.ToString().Equals(md5);
        }
    }

}