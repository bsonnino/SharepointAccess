using System.Collections.Generic;
using System.Threading.Tasks;
using SharepointAccess.Model;

namespace SharepointAccess.Repository
{
    public interface IListRepository
    {
        Task<List<Document>> GetDocumentsFromListAsync(string title);
        Task<List<DocumentsList>> GetListsAsync();
    }
}