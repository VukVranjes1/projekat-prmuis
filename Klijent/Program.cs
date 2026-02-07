using Domain;
using static Domain.Enumeratori;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace Klijent
{
    public class Program
    {
        static void Main(string[] args)
        {
            int clientID = 1;
            int port = 11001;
            string ip = "127.0.0.1";
            string tip_pokretanja = "Rucno pokrenut:";

            if (args.Length >= 3)
            {
                int.TryParse(args[0], out clientID);
                int.TryParse(args[1], out port);
                ip = args[2];
                tip_pokretanja = "Automatski pokrenut!";
            }

            Console.WriteLine($"POKRETANJE KLIJENTA | {tip_pokretanja} ID: {clientID} | IP: {ip} | Port: {port}");

            Random R = new Random();

            TIP_UREDJAJA tip;
            int mod = clientID % 4;
            if (mod == 1) tip = TIP_UREDJAJA.SOLARNI_PANEL;
            else if (mod == 2) tip = TIP_UREDJAJA.BATERIJA;
            else if (mod == 3) tip = TIP_UREDJAJA.POTROSAC;
            else tip = TIP_UREDJAJA.VETROGENERATOR;

            Uredjaji uredjaj = new Uredjaji()
            {
                ID_uredjaja = clientID,
                tip_uredjaja = tip,
                fizicka_velicina = FIZICKA_VELICINA.P,
                min_vrednost = 30,
                max_vrednost = 50,
                ulaz_izlaz = IO.IZLAZ,
                ip_adresa = ip,
                port = port,
                status = STATUS.PROIZVODNJA
            };

          
            Socket udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Poruka cfg = new Poruka() { Tip = PorukaTip.KONFIG, Uredjaj = uredjaj };
            udp.SendTo(Serialize(cfg), new IPEndPoint(IPAddress.Loopback, 9000));
            byte[] buffer = new byte[4096];
            EndPoint serverEP = new IPEndPoint(IPAddress.Loopback, 9000);
            udp.ReceiveFrom(buffer, ref serverEP);
            Console.WriteLine($"Konfiguracija poslata za uredjaj {clientID}");

            Thread.Sleep(2000);

         
            TcpClient tcp = new TcpClient();
            tcp.Connect("127.0.0.1", 10000);
            NetworkStream ns = tcp.GetStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ns, cfg);
            Console.WriteLine($"TCP povezan uredjaj {clientID} | IP: {ip} | Port: {port}");

            while (true)
            {
                try
                {
                    Poruka zahtev = (Poruka)bf.Deserialize(ns);

                    if (zahtev.Komanda == "WRITE" && uredjaj.ulaz_izlaz==IO.IZLAZ && uredjaj.status!=STATUS.ALARMNO_STANJE)
                    {
                        uredjaj.min_vrednost = R.Next(10, 90);
                        uredjaj.max_vrednost = R.Next(20, 100);

                        if (uredjaj.min_vrednost > uredjaj.max_vrednost)
                        {
                            uredjaj.min_vrednost = uredjaj.max_vrednost;
                        }

                        if (uredjaj.min_vrednost < 20 || uredjaj.max_vrednost > 80)
                            uredjaj.status = STATUS.ALARMNO_STANJE;
                        else
                            uredjaj.status = (uredjaj.tip_uredjaja == TIP_UREDJAJA.POTROSAC) ? STATUS.POTROSNJA : STATUS.PROIZVODNJA;

                        Poruka odgovor = new Poruka()
                        {
                            Tip = PorukaTip.ODGOVOR,
                            Uredjaj = uredjaj
                        };

                        bf.Serialize(ns, odgovor);

                        Console.WriteLine($"Uredjaj {clientID} | Tip: {tip} | Min: {uredjaj.min_vrednost} | Max: {uredjaj.max_vrednost} | Status: {uredjaj.status}");
                    }
                    else if(zahtev.Komanda == "WRITE" && uredjaj.ulaz_izlaz == IO.IZLAZ && uredjaj.status == STATUS.ALARMNO_STANJE)
                    {
                        if (uredjaj.min_vrednost <20 ) uredjaj.min_vrednost = 20;
                        if (uredjaj.max_vrednost > 80) uredjaj.max_vrednost = 80;
                        Console.WriteLine("Uredjaj je popravljen!");
                        if (uredjaj.tip_uredjaja == TIP_UREDJAJA.POTROSAC)
                            uredjaj.status = STATUS.POTROSNJA;
                        else
                        uredjaj.status = STATUS.PROIZVODNJA;
                    }

                    else if (zahtev.Komanda == "WRITE" && uredjaj.ulaz_izlaz == IO.ULAZ)
                    {
                        Console.WriteLine("Uslov ne moze se izvrsiti jer je uredjaj ulazni");
                    }
                    else if (zahtev.Komanda == "READ")
                    {
                        Poruka odgovor = new Poruka()
                        {
                            Tip = PorukaTip.ODGOVOR,
                            Uredjaj = uredjaj
                        };
                        bf.Serialize(ns, odgovor);

                        Console.WriteLine($"Uredjaj {clientID} | Tip: {tip} | Min: {uredjaj.min_vrednost} | Max: {uredjaj.max_vrednost} | Status: {uredjaj.status}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client {clientID} exception: {ex.Message}");
                    break;
                }

                Thread.Sleep(500);
            }
        }

        static byte[] Serialize(Poruka p)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, p);
                return ms.ToArray();
            }
        }
    }
}