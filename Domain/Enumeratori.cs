using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{
    public class Enumeratori
    {

        [Serializable]
        public enum PorukaTip
        {
            KONFIG,       // za inicijalnu konfiguraciju uredjaja
            ZAHTEV,      // zahtev servera za komunikaciju sa uredjajem
            ODGOVOR,     // odgovor klijenta
            ALARM         // alarmno stanje
        }

        public enum TIP_UREDJAJA
        {
            VETROGENERATOR,
            SOLARNI_PANEL,
            BATERIJA,
            POTROSAC

        }

        public enum FIZICKA_VELICINA
        {
            P,
            V,
            C,
            T
        }

        public enum IO
        {
            ULAZ,
            IZLAZ
        }

        public enum STATUS
        {
            PROIZVODNJA,
            POTROSNJA,
            ALARMNO_STANJE
        }
    }
}
