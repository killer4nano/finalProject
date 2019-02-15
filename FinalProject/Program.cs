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
                        Console.WriteLine("Program stopped, Press P to starttt !");
                        data = false;
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
                    } else
                    {

                        column = readFromTag("ScurrentBottomX");
                        row = readFromTag("ScurrentBottomY");

                    }


                    if (colour == 0)
                    {
                        Scolour = "black";
                    } else if (colour == 1)
                    {
                        Scolour = "white";
                    } else if (colour == 2)
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
                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }
                    Console.WriteLine("colour is " + Scolour + " \r\nThe piece is a " + (isTop == 1 ? "Top." : "Bottom.") + "\r\nIn Column: " + column + "\r\nRow: " + row);
                    writeToTag("dataReceived", 1);


                } else if (dataAvailable == 2) {
                    int orderAvailable = 0;
                    try
                    {
                        while (orderAvailable != 2) {
                            orderAvailable = readFromTag("orderAvailable");
                        }
                        conn = new MySqlConnection("server=buildabox.online;user id=faraway;password=killer12;database=finalproject;connection timeout=4;");
                        conn.Open();
                        Console.WriteLine("Connection Success!");
                        MySqlCommand command = conn.CreateCommand();
                        command.CommandText = "select * from orders;";
                        bool read = false;
                        MySqlDataReader reader = null;
                        Console.WriteLine("waiting for order....");
                        while (!read)
                        {
                            reader = command.ExecuteReader();
                            read = reader.Read();
                            if (!read) {
                                reader.Close();
                            }
                        }
                        Console.WriteLine("Order found.");
                        Console.WriteLine("Waiting for ASRS...");
                        while(readFromTag("ASRS_task") != 0)
                        {

                        }
                        int yDesired = (int)reader["bottomsPositionR"];
                        int xDesired = (int)reader["bottomsPositionC"];
                        writeToTag("ASRS_yDesired", yDesired);
                        Console.WriteLine("Y:"+yDesired);
                        writeToTag("ASRS_xDesired", xDesired);
                        Console.WriteLine("X:"+xDesired);
                        Console.WriteLine("Cordinates programmed to pickup of bottom.");
               
                        Console.WriteLine("Going to pickup bottom!");
                        writeToTag("orderAvailable", 1);
                        Console.WriteLine("Waiting for part to be dropped off to press...");
                        while(readFromTag("asrsGrabTop") == 0)
                        {
                            
                        }
                        Console.WriteLine("Waiting for ASRS....");
                        while (readFromTag("ASRS_task") != 0)
                        {

                        }
                        yDesired = (int)reader["topsPositionR"];
                        xDesired = (int)reader["topsPositionC"];
                        writeToTag("ASRS_yDesired", yDesired);
                        Console.WriteLine("Y:" + yDesired);
                        writeToTag("ASRS_xDesired", xDesired);
                        Console.WriteLine("X:" + xDesired);
                        Console.WriteLine("Cordinates for pickup of top have been programmed.");
                     
                        Console.WriteLine("Going to pickup top!");
                        writeToTag("asrsGrabTop", 2);
                        while (readFromTag("PP_Pickup") == 0)
                        {

                        }
                        writeToTag("asrsGrabTop", 0);
                        writeToTag("orderAvailable", 0);
                        reader.Close();
                        command.CommandText = "delete from finalproject.orders where topsPositionR = "+yDesired+" and topsPositionC = "+xDesired+";";
                        if (command.ExecuteNonQuery() > 0)
                        {
                            Console.WriteLine("Order deleted!");
                        }else
                        {
                            Console.WriteLine("Error deleting order!");
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }

                    
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







