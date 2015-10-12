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
        public string Subtitle { get; set; }
        public string Image { get; set; }
        public string Movie { get; set; }
        public string LocalMovie { get; set; }
        public DateTime PubDate { get; set; }

        public FeedItem () { }

        public FeedItem (string title, string subtitle, string image, string movie, DateTime pubdate)
        {
            Title = title;
            Subtitle = subtitle;
            Image = image;
            Movie = movie;
            PubDate = pubdate;
        }

        public override string ToString ()
        {
            return String.Format ("{0} {1}", Title.Substring (0, Math.Min (Title.Length, 20)), PubDate);
        }

    }
}
