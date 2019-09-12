using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using SharepointAccess.ListsReference;
using SharepointAccess.Model;

namespace SharepointAccess.Repository
{
    public class SoapListRepository : IListRepository
    {
        private string _address;

        public SoapListRepository(string address)
        {
            if (!address.EndsWith("/"))
                address += "/";
            _address = address + "_vti_bin/Lists.asmx";
        }

        XNamespace rs = "urn:schemas-microsoft-com:rowset";
        XNamespace z = "#RowsetSchema";

        public async Task<List<Document>> GetDocumentsFromListAsync(string title)
        {
            var tcs = new TaskCompletionSource<XmlNode>();
            _proxy = new Lists
            {
                Url = _address,
                UseDefaultCredentials = true
            };
            _proxy.GetListItemsCompleted += ProxyGetListItemsCompleted;
            _proxy.GetListItemsAsync(title, "", null, null, "", null, "", tcs);
            XmlNode response;
            try
            {
                response = await tcs.Task;
            }
            finally
            {
                _proxy.GetListItemsCompleted -= ProxyGetListItemsCompleted;
            }

            var list = XElement.Parse(response.OuterXml);

            var result = list?.Element(rs + "data").Descendants(z + "row")
                ?.Select(e => new Document(e.Attribute("ows_ID")?.Value,
                e.Attribute("ows_LinkFilename")?.Value, AttributesToDictionary(e),
                e.Attribute("ows_FileRef")?.Value)).ToList();
            return result;
        }

        private Dictionary<string, object> AttributesToDictionary(XElement e)
        {
            return e.Attributes().ToDictionary(a => a.Name.ToString().Replace("ows_", ""), a => (object)a.Value);
        }

        private void ProxyGetListItemsCompleted(object sender, GetListItemsCompletedEventArgs e)
        {
            var tcs = (TaskCompletionSource<XmlNode>)e.UserState;
            if (e.Cancelled)
            {
                tcs.TrySetCanceled();
            }
            else if (e.Error != null)
            {
                tcs.TrySetException(e.Error);
            }
            else
            {
                tcs.TrySetResult(e.Result);
            }
        }

        XNamespace ns = "http://schemas.microsoft.com/sharepoint/soap/";
        private Lists _proxy;

        public async Task<List<DocumentsList>> GetListsAsync()
        {
            var tcs = new TaskCompletionSource<XmlNode>();
            _proxy = new Lists
            {
                Url = _address,
                UseDefaultCredentials = true
            };
            _proxy.GetListCollectionCompleted += ProxyGetListCollectionCompleted;
            _proxy.GetListCollectionAsync(tcs);
            XmlNode response;
            try
            {
                response = await tcs.Task;
            }
            finally
            {
                _proxy.GetListCollectionCompleted -= ProxyGetListCollectionCompleted;
            }

            var list = XElement.Parse(response.OuterXml);
            var result = list?.Descendants(ns + "List")
                ?.Where(e => e.Attribute("Hidden").Value == "False")
                ?.Select(e => new DocumentsList(e.Attribute("Title").Value,
                e.Attribute("Description").Value)).ToList();
            return result;
        }

        private void ProxyGetListCollectionCompleted(object sender, GetListCollectionCompletedEventArgs e)
        {
            var tcs = (TaskCompletionSource<XmlNode>)e.UserState;
            if (e.Cancelled)
            {
                tcs.TrySetCanceled();
            }
            else if (e.Error != null)
            {
                tcs.TrySetException(e.Error);
            }
            else
            {
                tcs.TrySetResult(e.Result);
            }
        }
    }
}
