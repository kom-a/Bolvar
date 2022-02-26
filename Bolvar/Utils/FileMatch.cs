using System;
using System.Collections.Generic;
using System.Text;

namespace Bolvar
{
    public class Match
    { 
        public int Line { get; set; } // Line of file where occurance happened
        public int Position { get; set; } // Start position in line where occurance happened
    }


    public class FileMatch
    {
        public string Filename { get; set; }
        public string Pattern { get; set; } // string that has been found
        public List<Match> Matches { get; set; }
    }
}
