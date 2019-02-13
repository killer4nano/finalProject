using LibplctagWrapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FinalProject
{
    class Program
    {

        private const String PLC_IP = "10.116.99.170";
        private static int retry;

        static void Main(string[] args) 
        {
            int dataAvailable;
            bool data = false;
            bool running = false;
            bool toggle = false;
            MySqlConnection conn = null;
      

            while (true)
            {
                dataAvailable = 0;
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.P)
                {
                    running = !running;
                }
                if (running)
                {
                    dataAvailable = readFromTag("dataAvailable");
                    toggle = false;
                }else
                {
                    if (!toggle)
                    {
                        toggle = true;
                        Console.WriteLine("Program stopped, Press P to start!");
                    }
                }
                if (dataAvailable == 1)
                {
                    data = false;
                    String Scolour = "";
                    int colour = readFromTag("pieceColour");
                    int isTop = readFromTag("isPieceTop");
                    int column;
                    int row;
                    if (isTop == 1)
                    {
                        column = readFromTag("ScurrentTopX");
                        row = readFromTag("ScurrentTopY");
                    }else
                    {

                        column = readFromTag("ScurrentBottomX");
                        row = readFromTag("ScurrentBottomY");

                    }


                    if (colour == 0)
                    {
                        Scolour = "black";
                    }else if (colour == 1)
                    {
                        Scolour = "white";
                    }else if (colour == 2)
                    {
                        Scolour = "silver";
                    }

                    try
                    {

                        conn = new MySqlConnection("server=buildabox.online;user id=faraway;password=killer12;database=finalproject;connection timeout=4;");
                        conn.Open();
                        Console.WriteLine("Connection Success!");
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "INSERT INTO inventory (rowN,columnN,color, orientation) VALUES (" + row + "," + column + ",'" + Scolour + "'," + isTop + ")";
                        command.ExecuteNonQuery();
                        conn.Close();
                    }catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    Console.WriteLine("colour is " + Scolour + " \r\nThe piece is a " + (isTop == 1 ? "Top." : "Bottom.") + "\r\nIn Column: "+ column +"\r\nRow: " +row);
                    writeToTag("dataReceived", 1);


                } else {
                    if (!data && running)
                    {
                        Console.WriteLine("No data available!");
                        data = true;
                    }
                }
            }

        }




        static int readFromTag(String tagName)
         {
                int val = 0;


                var tag = new Tag(PLC_IP, "1, 0", CpuType.LGX, tagName, DataType.Int32, 1);


                var client = new Libplctag();

                client.AddTag(tag);


                while (client.GetStatus(tag) == Libplctag.PLCTAG_STATUS_PENDING)
                {
                    Thread.Sleep(100);
                }

                if (client.GetStatus(tag) != Libplctag.PLCTAG_STATUS_OK)
                {
                    Console.WriteLine($"Error setting up tag internal state. Error{ client.DecodeError(client.GetStatus(tag))}\n");
                    return -1;
                }

                var result = client.ReadTag(tag, 2000);

                // Check the read operation result
                if (result != Libplctag.PLCTAG_STATUS_OK)
                {
                    Console.WriteLine($"ERROR: Unable to read the data! Got error code {result}: {client.DecodeError(result)}\n");
                    return -1;
                }
                // Convert the data
                val = client.GetInt32Value(tag, 0 * tag.ElementSize);

                return val;

        }




        static void writeToTag(String tagName, int value)
        {
            var tag = new Tag(PLC_IP, "1, 0", CpuType.LGX, tagName, DataType.Int32, 1);
            var client = new Libplctag();

            client.AddTag(tag);


            while (client.GetStatus(tag) == Libplctag.PLCTAG_STATUS_PENDING)
            {
                Thread.Sleep(100);
            }

            if (client.GetStatus(tag) != Libplctag.PLCTAG_STATUS_OK)
            {
                Console.WriteLine($"Error setting up tag internal state. Error{ client.DecodeError(client.GetStatus(tag))}\n");
                return;
            }
            client.SetInt32Value(tag, 0 * tag.ElementSize, value);
            var result = client.WriteTag(tag, 2000);
            if (result != Libplctag.PLCTAG_STATUS_OK)
            {
                if (retry < 2)
                {
                    Console.WriteLine("ERROR!");
                    Console.WriteLine("Retrying....");
                    Thread.Sleep(500);
                    retry++;
                    writeToTag(tagName, value);
                }
                else
                {
                    Console.WriteLine("Retry Failed! Start over!");
                    retry = 0;
                    return;
                }
            }
            else
            {
                retry = 0;
            }

            if (value == 1 && tagName.Contains("SOL"))
            {
                writeToTag(tagName, 0);
            }


        }
    }



}







