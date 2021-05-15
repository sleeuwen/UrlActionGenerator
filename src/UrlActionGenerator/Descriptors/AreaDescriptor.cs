namespace UrlActionGenerator.Descriptors
{
    public class AreaDescriptor
    {
        public AreaDescriptor(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
            Controllers = new KeyedCollection<ControllerDescriptor>(controller => controller.Name);
        }

        public string Name { get; }

        public KeyedCollection<ControllerDescriptor> Controllers { get; }
    }
}
