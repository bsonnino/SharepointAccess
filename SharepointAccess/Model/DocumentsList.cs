namespace SharepointAccess.Model
{
    public class DocumentsList
    {
        public DocumentsList(string title, string description)
        {
            Title = title;
            Description = description;
        }

        public string Title { get; }
        public string Description { get; }
    }
}