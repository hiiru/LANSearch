using System;

namespace LANSearch.Data.Feedback
{
    public class Feedback
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Location { get; set; }

        public string Text { get; set; }

        public DateTime Created { get; set; }

        public bool Deleted { get; set; }

        public bool Read { get; set; }
    }
}