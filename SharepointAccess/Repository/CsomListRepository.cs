using Microsoft.SharePoint.Client;
using SharepointAccess.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharepointAccess.Repository
{
    public class CsomListRepository : IListRepository
    {
        private string _sharepointSite;

        public CsomListRepository(string sharepointSite)
        {
            _sharepointSite = sharepointSite;
        }
        public Task<List<DocumentsList>> GetListsAsync()
        {
            return Task.Run(() =>
            {
                using (var context = new ClientContext(_sharepointSite))
                {
                    var web = context.Web;
                    context.Load(web, w => w.Title, w => w.Description);

                    var query = web.Lists.Include(l => l.Title, l => l.Description)
                         .Where(l => !l.Hidden && l.ItemCount > 0);

                    var lists = context.LoadQuery(query);
                    context.ExecuteQuery();

                    return lists.Select(l => new DocumentsList(l.Title, l.Description)).ToList();
                }
            });
        }

        public Task<List<Document>> GetDocumentsFromListAsync(string listTitle)
        {
            return Task.Run(() =>
            {
                using (var context = new ClientContext(_sharepointSite))
                {
                    var web = context.Web;
                    var list = web.Lists.GetByTitle(listTitle);
                    var query = new CamlQuery();

                    query.ViewXml = "<View />";
                    var items = list.GetItems(query);
                    context.Load(list,
                        l => l.Title);
                    context.Load(items, l => l.IncludeWithDefaultProperties(i => i.Folder, i => i.File, i => i.DisplayName));
                    context.ExecuteQuery();

                    return items
                        .Where(i => i["Title"] != null)
                        .Select(i => new Document(i["ID"].ToString(), i["Title"].ToString(), i.FieldValues, i["FileRef"].ToString()))
                        .ToList();
                }
            });
        }
    }
}
