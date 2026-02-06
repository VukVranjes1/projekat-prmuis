using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Domain.Enumeratori;



namespace Domain
{

    [Serializable]


   
    public class Poruka  // Klasa za poruke koje se salju između servera i klijenata, lakse mi je imati samo jednu klasu koja sluzi za komunikaciju preko MemoryStreama
    {
        public PorukaTip Tip { get; set; }
        public Uredjaji Uredjaj { get; set; }
        public string Komanda { get; set; }
        public string Tekst { get; set; }
    }


    [Serializable]
    public class Uredjaji
    {

        public int ID_uredjaja { get; set; }
        public TIP_UREDJAJA tip_uredjaja { get; set; }
        public FIZICKA_VELICINA fizicka_velicina { get; set; }
        public double min_vrednost { get; set; }
        public double max_vrednost { get; set; }
        public IO ulaz_izlaz { get; set; }
        public string ip_adresa { get; set; }
        public int port { get; set; }
        public STATUS status { get; set; }







    }
}
