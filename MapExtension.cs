using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;

namespace ClassLibrary.Mapping
{
    [ReflectionPermission(SecurityAction.Assert, MemberAccess = true)]
    public static class Map
    {

        /// <summary>
        /// Mapeia os campos do DataTable para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dt">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this DataTable dt) where T : class, new()
        {
            var dr = dt.CreateDataReader();

            return dr.MapToEntities<T>();
        }

        /// <summary>
        /// Mapeia os campos do dataReader para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dr">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this IDataReader dr) where T : class ,new()
        {
            try
            {
                var objetos = new List<T>();
                var dadosObjeto = RetornaMapObjeto<T>(dr);

                while (dr.Read())
                {
                    var objetoAlvo = new T();

                    foreach (var p in dadosObjeto)
                    {
                        if (!dr.HasColumn(p.Name))
                        {
                            continue;
                        }

                        var valorDr = dr[p.Name];

                        if (valorDr != DBNull.Value)
                        {
                            if (valorDr is TimeSpan && p.PropertyType == typeof(DateTime))
                            {
                                p.SetValue(objetoAlvo, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (valorDr is byte[] && p.PropertyType == typeof(string))
                            {
                                p.SetValue(objetoAlvo, Convert.ChangeType(System.Text.Encoding.Default.GetString((byte[])valorDr), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (valorDr is Guid && p.PropertyType == typeof(string))
                            {
                                p.SetValue(objetoAlvo, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && p.PropertyType.GetGenericArguments()[0].IsEnum)
                            {
                                p.SetValue(objetoAlvo, Enum.Parse(p.PropertyType.GetGenericArguments()[0], valorDr.ToString()), null);
                            }
                            else if (!p.PropertyType.IsEnum)
                            {
                                p.SetValue(objetoAlvo, Nullable.GetUnderlyingType(p.PropertyType) != null
                                        ? Convert.ChangeType(valorDr, Nullable.GetUnderlyingType(p.PropertyType), System.Globalization.CultureInfo.InvariantCulture)
                                        : Convert.ChangeType(valorDr, p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (p.PropertyType.IsEnum)
                            {
                                if (valorDr is string)
                                {
                                    p.SetValue(objetoAlvo, Enum.Parse(p.PropertyType, valorDr.ToString()), null);
                                }
                                else
                                {
                                    p.SetValue(objetoAlvo, Enum.ToObject(p.PropertyType, valorDr), null);
                                }
                            }
                        }
                        else
                        {
                            if ((p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                            {
                                p.SetValue(objetoAlvo, null, null);
                            }
                            else
                            {
                                p.SetValue(objetoAlvo, !p.PropertyType.IsValueType
                                    ? null
                                    : Activator.CreateInstance(p.PropertyType), null);
                            }
                        }
                    }
                    objetos.Add(objetoAlvo);
                }

                return objetos;

            }
            catch
            {
                throw;
            }
        }

        private static List<PropertyInfo> RetornaMapObjeto<T>(IDataRecord dr) where T : new()
        {
            if (dr != null)
            {
                return typeof (T).GetProperties().ToList();
            }
            
            throw new ArgumentNullException("dr","DataReader não pode ser nulo");
        }

        private static bool HasColumn(this IDataRecord dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
