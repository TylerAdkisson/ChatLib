using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatLib.Twitch;


namespace ChatLib
{
    public static class ChatServiceFactory
    {
        private static Dictionary<string, Type> _serviceDict;

        static ChatServiceFactory()
        {
            _serviceDict = new Dictionary<string, Type>();
        }


        /// <summary>
        /// Registers a chat service to the specified identifier
        /// </summary>
        /// <typeparam name="T">The class implementing <see cref="IChatService"/> to register</typeparam>
        /// <param name="id">The identifier to register the service to</param>
        public static void RegisterService<T>(string id) where T : IChatService
        {
            _serviceDict[id] = typeof(T);
        }
		
		/// <summary>
        /// Registers all built-in chat services
        /// </summary>
		public static void RegisterBuiltinServices()
		{
			RegisterService<TwitchIrcService>("irc_twitch");
		}
		
		/// <summary>
        /// Returns a list of all registered chat service IDs
        /// </summary>
        /// <returns>An array of strings containing all registered chat service IDs</returns>
		public static string[] GetRegisteredServices()
		{
			string[] ids = new string[_serviceDict.Keys.Count];
			_serviceDict.Keys.CopyTo(ids, 0);
			
			return ids;
		}

        /// <summary>
        /// Creates an instance of the service by the specified identifier
        /// </summary>
        /// <param name="id">The identifier of the service to create</param>
        /// <returns>A new instance of the specified chat service</returns>
        public static IChatService CreateServiceInstance(string id)
        {
            Type serviceType;
            if (!_serviceDict.TryGetValue(id, out serviceType))
                throw new ArgumentException("The specified ID could not be found.");

            return (IChatService)Activator.CreateInstance(serviceType);
        }
    }
}
