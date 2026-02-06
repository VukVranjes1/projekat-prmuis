using Domain;
using static Domain.Enumeratori;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Server
{
    public class Program
    {
        static Dictionary<int, TcpClient> tcpClients = new Dictionary<int, TcpClient>();

        static void Main(string[] args)
        {
            Console.WriteLine("POKRETANJE SCADA SERVERA");

         
            Socket udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpServer.Bind(new IPEndPoint(IPAddress.Any, 9000));

           
            TcpListener tcpListener = new TcpListener(IPAddress.Any, 10000);
            tcpListener.Start();

            new Thread(() => AcceptTCPClients(tcpListener)) { IsBackground = true }.Start();

            

           
            new Thread(() =>
            {
                int nextID = 2;
                while (true)
                {

                    Thread.Sleep(15000);
                    PokreniKlijenta(nextID, 11000 + nextID, $"127.0.0.{nextID}");
                    nextID++;
                    if (nextID == 4) break;
                }
            }).Start();

         
            while (true)
            {
                foreach (var kvp in tcpClients)
                {
                    try
                    {
                        TcpClient client = kvp.Value;
                        NetworkStream ns = client.GetStream();
                        BinaryFormatter bf = new BinaryFormatter();

                        Random R1 = new Random();
                        int random1 =   R1.Next(0, 50);
                        Poruka zahtev;
                        if (random1<36)
                        {
                             zahtev = new Poruka()
                            {
                                Tip = PorukaTip.ZAHTEV,
                                Komanda = "READ"
                            };
                        }
                        else
                        {
                            zahtev = new Poruka()
                            {
                                Tip = PorukaTip.ZAHTEV,
                                Komanda = "WRITE"
                            };
                        }



                       bf.Serialize(ns, zahtev);

                        Poruka odgovor = (Poruka)bf.Deserialize(ns);

                        Console.WriteLine($"Uredjaj {odgovor.Uredjaj.ID_uredjaja} | Tip: {odgovor.Uredjaj.tip_uredjaja} | Min: {odgovor.Uredjaj.min_vrednost} | Max: {odgovor.Uredjaj.max_vrednost} | Status: {odgovor.Uredjaj.status}");
                        if (odgovor.Uredjaj.status == STATUS.ALARMNO_STANJE)
                        {
                            Console.WriteLine($"ALARMNO STANJE UREĐAJA {odgovor.Uredjaj.ID_uredjaja}!");
                            Console.WriteLine("POPRAVAK ALARMNOG STANJA....");
                           

                            Poruka repair = new Poruka()
                            {
                                Tip = PorukaTip.ZAHTEV,
                                Komanda = "WRITE",
                                Uredjaj = odgovor.Uredjaj
                            };

                            bf.Serialize(ns, repair);

                            Console.WriteLine($"Uredjaj {odgovor.Uredjaj.ID_uredjaja} popravljen.");


                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error polling device {kvp.Key}: {ex.Message}");
                    }
                }

                Thread.Sleep(5000);
            }
        }

        static void AcceptTCPClients(TcpListener tcpListener)
        {
            while (true)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    NetworkStream ns = client.GetStream();

                    BinaryFormatter bf = new BinaryFormatter();
                    Poruka p = (Poruka)bf.Deserialize(ns);

                    int id = p.Uredjaj.ID_uredjaja;

                    tcpClients.Add(id, client); //!!!!
                    Console.WriteLine($"TCP povezan uređaj {id} | IP: {p.Uredjaj.ip_adresa} | Port: {p.Uredjaj.port}");
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("AcceptTCPClients exception: " + ex.Message);
                }
            }
        }

       

        static void PokreniKlijenta(int id, int port, string ip)
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = @"Klijent.exe";
                p.StartInfo.Arguments = $"{id} {port} {ip}";
                p.Start();

                Console.WriteLine($"Pokrenut uređaj ID: {id} | IP: {ip} | Port: {port}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}