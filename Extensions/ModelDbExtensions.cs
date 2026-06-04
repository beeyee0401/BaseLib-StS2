using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ModelDbExtensions
{
    //Will require language version set to 14 to use.
    extension(ModelDb)
    {
        /// <summary>
        /// Obtains a new instance of a CardModifier. Requires language version 14+.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CardModifier<T>() where T : CardModifier
        {
            return ModelDb.Get<T>();
        }
    }
}