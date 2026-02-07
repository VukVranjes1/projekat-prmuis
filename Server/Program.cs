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

            
            new Thread(() => {
                while (true)
                {
                    byte[] buffer = new byte[4096];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                    
                    int rec = udpServer.ReceiveFrom(buffer, ref remoteEP);

                    Console.WriteLine("\nPrimljen zahtev za dodavanje novog uređaja!");

                   
                    udpServer.SendTo(buffer, 0, rec, SocketFlags.None, remoteEP);
                }
            }).Start();


            TcpListener tcpListener = new TcpListener(IPAddress.Any, 10000);
            tcpListener.Start();

            new Thread(() => AcceptTCPClients(tcpListener)) { IsBackground = true }.Start();

            

           
            new Thread(() =>
            {
                int nextID = 2;
                while (true)
                {

                    Thread.Sleep(2000);
                    PokreniKlijenta(nextID, 11000 + nextID, $"127.0.0.{nextID}");
                    nextID++;
                    if (nextID > 4) break;
                }
            }).Start();

            List<Uredjaji> uredjaji = new List<Uredjaji>();

            while (true)
            {

                
                if (Console.KeyAvailable)
                {
                    
                    ConsoleKeyInfo taster = Console.ReadKey(true);

                    
                    if (taster.Key == ConsoleKey.U)
                    {
                        
                        Console.Write("\nUnesite ID uređaja za pregled: ");

                       
                        string unos = Console.ReadLine();

                        if (int.TryParse(unos, out int trazeniID))
                        {
                            Uredjaji pronadjen = uredjaji.Find(x => x.ID_uredjaja == trazeniID);

                            if (pronadjen != null)
                            {
                                Console.WriteLine($"\n>>> PODACI ZA ID {trazeniID}:");
                                Console.WriteLine($"> Tip: {pronadjen.tip_uredjaja}");
                                Console.WriteLine($"> Status: {pronadjen.status}");
                                Console.WriteLine($"> Opseg: {pronadjen.min_vrednost} - {pronadjen.max_vrednost}");
                                Console.WriteLine("----------------------------------");
                            }
                            else
                            {
                                Console.WriteLine(">>> Uređaj nije u listi.");
                            }
                        }

                        Console.WriteLine("Pritisnite bilo koji taster za nastavak polling-a...");
                        Console.ReadKey(true); 
                    }
                }

            

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

                        uredjaji.RemoveAll(x => x.ID_uredjaja == odgovor.Uredjaj.ID_uredjaja);
                        uredjaji.Add(odgovor.Uredjaj);

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

                for (int i = 0; i < 50; i++)
                {
                    if (Console.KeyAvailable) break; 
                    Thread.Sleep(100);
                }
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

                    tcpClients.Add(id, client);
                    string tip = (id == 1) ? "Ručno pokrenut" : "Automatski";

                    Console.WriteLine($"TCP povezan uređaj {id} [{tip}] | IP: {p.Uredjaj.ip_adresa}");

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
                p.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
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