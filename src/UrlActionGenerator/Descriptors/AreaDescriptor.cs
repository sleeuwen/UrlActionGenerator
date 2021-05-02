namespace UrlActionGenerator.Descriptors
{
    public class AreaDescriptor
    {
        public AreaDescriptor(string name)
        {
            Name = name;
            Controllers = new KeyedCollection<ControllerDescriptor>(controller => controller.Name);
        }

        public string Name { get; }

        public KeyedCollection<ControllerDescriptor> Controllers { get; }
    }
}
