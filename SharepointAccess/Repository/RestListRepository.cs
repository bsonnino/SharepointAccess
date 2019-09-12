using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SharepointAccess.Model;

namespace SharepointAccess.Repository
{
    public class RestListRepository : IListRepository
    {
        public RestListRepository(string sharepointSite)
        {
            _sharepointSite = sharepointSite + "_api/";
        }
        public async Task<List<Document>> GetDocumentsFromListAsync(string title)
        {
            var doc = await GetResponseDocumentAsync(_sharepointSite + $"Lists/GetByTitle('{title}')/Items");
            if (doc == null)
                return null;
            var entries = doc.Element(ns + "feed").Descendants(ns + "entry");
            var documents = await Task.WhenAll(entries.Select(async e => await GetDocumentFromElementAsync(e)));
            return documents.Where(d => !string.IsNullOrEmpty(d.Title)).ToList(); ;
        }

        private XNamespace ns = "http://www.w3.org/2005/Atom";
        public async Task<List<DocumentsList>> GetListsAsync()
        {
            var doc = await GetResponseDocumentAsync(_sharepointSite + "Lists");
            if (doc == null)
                return null;

            var entries = doc.Element(ns + "feed").Descendants(ns + "entry");
            return entries.Select(GetDocumentsListFromElement)
                .Where(d => !string.IsNullOrEmpty(d.Title)).ToList();
        }
        public async Task<XDocument> GetResponseDocumentAsync(string url)
        {
            var handler = new HttpClientHandler
            {
                UseDefaultCredentials = true
            };
            HttpClient httpClient = new HttpClient(handler);
            var headers = httpClient.DefaultRequestHeaders;
            var header = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/76.0.3809.100 Safari/537.36";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }
            Uri requestUri = new Uri(url);

            try
            {
                var httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                return XDocument.Parse(httpResponseBody);
            }
            catch
            {
                return null;
            }
        }

        private XNamespace mns = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private XNamespace dns = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private string _sharepointSite;

        private DocumentsList GetDocumentsListFromElement(XElement e)
        {
            var element = e.Element(ns + "content")?.Element(mns + "properties");
            if (element == null)
                return new DocumentsList("", "");
            bool.TryParse(element.Element(dns + "Hidden")?.Value ?? "true", out bool isHidden);
            int.TryParse(element.Element(dns + "ItemCount")?.Value ?? "0", out int ItemCount);
            return !isHidden && ItemCount > 0 ?
              new DocumentsList(element.Element(dns + "Title")?.Value ?? "",
                element.Element(dns + "Description")?.Value ?? "") :
              new DocumentsList("", "");
        }

        private async Task<Document> GetDocumentFromElementAsync(XElement e)
        {
            var element = e.Element(ns + "content")?.Element(mns + "properties");
            if (element == null)
                return new Document("", "", null, "");
            var id = element.Element(dns + "Id")?.Value ?? "";
            var title = element.Element(dns + "Title")?.Value ?? "";
            var description = element.Element(dns + "Description")?.Value ?? "";
            var fields = element.Descendants().ToDictionary(el => el.Name.LocalName, el => (object)el.Value);
            int.TryParse(element.Element(dns + "FileSystemObjectType")?.Value ?? "-1", out int fileType);
            string docUrl = "";

            var url = GetUrlFromTitle(e, fileType == 0 ? "File" : "Folder");
            if (url != null)
            {
                var fileDoc = await GetResponseDocumentAsync(_sharepointSite + url);
                docUrl = fileDoc.Element(ns + "entry")?.
                    Element(ns + "content")?.
                    Element(mns + "properties")?.
                    Element(dns + "ServerRelativeUrl")?.
                    Value;
            }

            return new Document(id, title, fields, docUrl);
        }

        private string GetUrlFromTitle(XElement element, string title)
        {
            return element.Descendants(ns + "link")
                    ?.FirstOrDefault(e1 => e1.Attribute("title")?.Value == title)
                    ?.Attribute("href")?.Value;
        }
    }
}
