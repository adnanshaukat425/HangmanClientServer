using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HangmanServer
{
    class Words
    {
        string file;
        string[] word;
        string correctWord;
        string sendedWord;

        public Words()
        {
            file = File.ReadAllText(@"D:\Adnan\UniversityWork\Fall' 17\NP\Project\Project\words.txt");
            word = file.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string[] GetWords()
        {
            Random rand = new Random();
            correctWord = word[rand.Next(0, word.Length)];
            sendedWord = correctWord;
            char[] charArray = sendedWord.ToCharArray();
        label:
            charArray = GetDashes(charArray);
            sendedWord = GetSpace(charArray);
            if (!sendedWord.Contains("_"))
            {
                goto label;
            }
            string[] finalWords = new string[2];
            finalWords[0] = correctWord;
            finalWords[1] = sendedWord;
            return finalWords;
        }

        public char[] GetDashes(char[] charArray)
        {
            int b = -1;
            Random rand = new Random();
            int limit = rand.Next(0, correctWord.Length - 1);

            for (int i = 0; i < limit; i++)
            {
                int a = rand.Next(0, correctWord.Length - 1);
                if (a != b)
                {
                    charArray[a] = '_';
                }
                b = a;
            }
            return charArray;
        }

        public string GetSpace(char[] charArray)
        {
            sendedWord = "";
            for (int i = 0; i < charArray.Length; i++)
            {
                sendedWord += charArray[i].ToString() + " ";
            }
            return sendedWord;
        }

        public string RemoveSpace(char[] charArray)
        {
            string temp = "";
            for (int i = 0; i < charArray.Length; i++)
            {
                if (charArray[i].ToString() != " ")
                {
                    temp += charArray[i];
                }
            }
            return temp;
        }
    }
}
