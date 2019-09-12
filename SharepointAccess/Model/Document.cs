using System.Collections.Generic;

namespace SharepointAccess.Model
{
    public class Document
    {
        public Document(string id, string title, 
            Dictionary<string, object> fields,
            string url)
        {
            Id = id;
            Title = title;
            Fields = fields;
            Url = url;
        }
        public string Id { get; }
        public string Title { get; }
        public string Url { get; }
        public Dictionary<string, object> Fields { get; }
    }
}