namespace CharacterSelectBackgroundPlugin.Windows.Tabs
{
    internal interface ITab
    {
        public abstract string Title { get; }
        void Draw();

    }
}
