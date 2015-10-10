using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TastyTradeReader
{
    public class Feed : List<FeedItem>
    { }

    public class FeedItem
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public string Movie { get; set; }
        public string PubDate { get; set; }

        public override string ToString ()
        {
            return String.Format ("{0} {1}", Title.Substring (0, 20), PubDate);
        }

    }
}
