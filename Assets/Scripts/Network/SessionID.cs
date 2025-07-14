using System;
using UnityEngine;
using Random = System.Random;

namespace TDM
{
    [Tooltip("Manager that stores a token per user to help reassign input authority in Client Host Mode during host migration and reconnects.")]
    public class SessionID : MonoBehaviour
    {
        public static long ID { get; private set; }

        public static byte[] ByteID { get; private set; } = new byte[8];

        private void Awake()
        {
            new Random().NextBytes(ByteID);
            ID = ConvertID(ByteID);
        }

        public static long ConvertID(byte[] id)
        {
            return BitConverter.ToInt64(id, 0);
        }
    }
}