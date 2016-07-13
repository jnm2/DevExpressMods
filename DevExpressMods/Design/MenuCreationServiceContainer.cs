using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using DevExpress.XtraReports.Design;
using DevExpress.XtraReports.UserDesigner;

namespace DevExpressMods.Design
{
    public class MenuCreationServiceContainer : IMenuCreationService
    {
        private readonly List<IMenuCreationService> services = new List<IMenuCreationService>();

        private MenuCreationServiceContainer()
        {
        }

        public static MenuCreationServiceContainer Get(IServiceContainer serviceContainer)
        {
            var current = serviceContainer.GetService<IMenuCreationService>();
            var r = current as MenuCreationServiceContainer;
            if (r == null)
            {
                r = new MenuCreationServiceContainer();
                if (current != null)
                {
                    r.Add(current);
                    serviceContainer.RemoveService(typeof(IMenuCreationService));
                }
                serviceContainer.AddService(typeof(IMenuCreationService), r);
            }
            return r;
        }

        public void Add(IMenuCreationService instance)
        {
            services.Add(instance);
        }
        public void Remove(IMenuCreationService instance)
        {
            services.Remove(instance);
        }

        MenuCommandDescription[] IMenuCreationService.GetCustomMenuCommands()
        {
            return services.SelectMany(s => s.GetCustomMenuCommands()).ToArray();
        }

        void IMenuCreationService.ProcessMenuItems(MenuKind menuKind, MenuItemDescriptionCollection items)
        {
            foreach (var s in services)
                s.ProcessMenuItems(menuKind, items);
        }
    }
}
