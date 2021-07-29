using System;
using System.Collections.Generic;
using System.Text;

namespace ClassLibrary_Entity
{
    [Serializable]
    public class E_BD_PageViews
    {
        //public List<E_UPLOADED_FILE> UPLOADED_FILES { get; set; }
        public List<string> UPLOADED_FILES { get; set; }

        public List<E_AllHours> SUMMARY_VIEWS { get; set; }

        public List<string> INVALID_LINES { get; set; }

        public E_BD_PageViews()
        {
            this.SUMMARY_VIEWS = new List<E_AllHours>();
            //this.UPLOADED_FILES = new List<E_UPLOADED_FILE>();
            this.UPLOADED_FILES = new List<string>();
        }
    }
}
