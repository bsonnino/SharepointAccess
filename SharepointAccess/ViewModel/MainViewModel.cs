using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using SharepointAccess.Model;
using SharepointAccess.Repository;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Input;

namespace SharepointAccess.ViewModel
{
    public enum ApiSelection
    {
        NetApi,
        Rest,
        Soap
    };

    public class MainViewModel : ViewModelBase
    {
        private IListRepository _listRepository;
        private List<DocumentsList> _documentsLists;
        private string _listTiming;
        private string itemTiming;
        private string address;
        private DocumentsList _selectedList;
        private List<Document> documents;
        private Document _selectedDocument;
        private ApiSelection _selectedApi;
        private ICommand _apiSelectCommand;


        public MainViewModel()
        {
            Address = ConfigurationManager.AppSettings["WebSite"];
            _selectedApi = ApiSelection.NetApi;
            GoToAddress();
        }

        public List<DocumentsList> DocumentsLists
        {
            get => _documentsLists;
            set
            {
                _documentsLists = value;
                RaisePropertyChanged();
            }
        }
        public string ListTiming
        {
            get => _listTiming;
            set
            {
                _listTiming = value;
                RaisePropertyChanged();
            }
        }

        public List<Document> Documents
        {
            get => documents;
            set
            {
                documents = value;
                RaisePropertyChanged();
            }
        }

        public string ItemTiming
        {
            get => itemTiming;
            set
            {
                itemTiming = value;
                RaisePropertyChanged();
            }
        }
        public string Address
        {
            get => address;
            set
            {
                address = value;
                RaisePropertyChanged();
            }
        }

        public DocumentsList SelectedList
        {
            get => _selectedList;
            set
            {
                if (_selectedList == value)
                    return;
                _selectedList = value;
                GetDocumentsForList(value);

                RaisePropertyChanged();
            }
        }

        public Document SelectedDocument
        {
            get => _selectedDocument;
            set
            {
                if (_selectedDocument == value)
                    return;
                _selectedDocument = value;
                RaisePropertyChanged();
                RaisePropertyChanged("Fields");
            }
        }

        public Dictionary<string, object> Fields => _selectedDocument?.Fields;

        public ICommand ApiSelectCommand =>
            _apiSelectCommand ?? (_apiSelectCommand = new RelayCommand<string>(s => SelectApi(s)));

        private void SelectApi(string s)
        {
            _selectedApi = (ApiSelection)Enum.Parse(typeof(ApiSelection), s, true);
            GoToAddress();
        }

        private async void GoToAddress()
        {
            var sw = new Stopwatch();
            sw.Start();
            _listRepository = _selectedApi == ApiSelection.Rest ?
                (IListRepository)new RestListRepository(Address) :
                _selectedApi == ApiSelection.NetApi ?
                (IListRepository)new CsomListRepository(Address) :
                new SoapListRepository(Address);

            DocumentsLists = await _listRepository.GetListsAsync();
            ListTiming = $"Time to get lists: {sw.ElapsedMilliseconds}";
            ItemTiming = "";
        }

        private async void GetDocumentsForList(DocumentsList list)
        {
            var sw = new Stopwatch();
            sw.Start();
            if (list != null)
            {
                Documents = await _listRepository.GetDocumentsFromListAsync(list.Title);
                ItemTiming = $"Time to get items: {sw.ElapsedMilliseconds}";
            }
            else
            {
                Documents = null;
                ItemTiming = "";
            }
        }

    }
}