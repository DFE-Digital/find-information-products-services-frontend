//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace find_information_products_services_tests.utilities
//{
//    internal class Program
//    {
//        public static void Main(string[] args)
//        {
//            string originalString = "abc@edu.uk";

//            Console.WriteLine("Original String: " + originalString);

//            // --- ENCODING ---
//            // 1. Convert the string to a byte array.
//            byte[] originalBytes = Encoding.UTF8.GetBytes(originalString);

//            // 2. Convert the byte array to a Base64 string.
//            string base64EncodedString = Convert.ToBase64String(originalBytes);

//            Console.WriteLine("Encoded String (Base64): " + base64EncodedString);

//            // --- DECODING ---
//            try
//            {
//                // 1. Convert the Base64 string to a byte array.
//                byte[] decodedBytes = Convert.FromBase64String(base64EncodedString);

//                // 2. Convert the byte array back to a string.
//                string decodedString = Encoding.UTF8.GetString(decodedBytes);

//                Console.WriteLine("Decoded String: " + decodedString);
//            }
//            catch (FormatException ex)
//            {
//                Console.WriteLine("Error decoding Base64 string: " + ex.Message);
//            }
//        }
//    }
//}
