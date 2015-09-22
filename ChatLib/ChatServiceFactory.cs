using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib
{
    public static class ChatServiceFactory
    {
        private static Dictionary<string, Type> _serviceDict;

        static ChatServiceFactory()
        {
            _serviceDict = new Dictionary<string, Type>();
        }


        public static void RegisterService<T>(string id) where T : IChatService
        {
            _serviceDict[id] = typeof(T);
        }

        public static IChatService CreateServiceInstance(string id)
        {
            Type serviceType;
            if (!_serviceDict.TryGetValue(id, out serviceType))
                throw new ArgumentException("The specified ID could not be found.");

            return (IChatService)Activator.CreateInstance(serviceType);
        }
    }
}
