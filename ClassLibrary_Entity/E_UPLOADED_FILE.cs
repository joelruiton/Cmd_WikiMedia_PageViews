using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary_Entity
{
    [Serializable]
    public class E_UPLOADED_FILE
    {
        public string NAME { get; set; }

        public string URL { get; set; }

        public DateTime date { get; set; }

        public DateTime Upload_date { get; set; }
    }
}
