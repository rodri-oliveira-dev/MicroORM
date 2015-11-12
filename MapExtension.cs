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
        /// Mapeia os campos do DataView para o objeto alvo
        /// </summary>
        /// <typeparam name="T">Objeto a ser mapeado</typeparam>
        /// <param name="dv">dados a serem mapeados</param>
        /// <returns>Lista de objetos mapeados</returns>
        public static List<T> MapToEntities<T>(this DataView dv) where T : class, new()
        {
            var dt = dv.ToTable("Tabela");

            return dt.MapToEntities<T>();
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
                var listaNovosObjetos = new List<T>();
                var camposValidos = RetornaMapObjeto<T>(dr);

                while (dr.Read())
                {
                    var novoObjeto = new T();

                    foreach (var p in camposValidos)
                    {
                        var valorDr = dr[p.Name];

                        if (valorDr != DBNull.Value)
                        {
                            if (valorDr is TimeSpan && p.PropertyType == typeof(DateTime))
                            {
                                p.SetValue(novoObjeto, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (valorDr is byte[] && p.PropertyType == typeof(string))
                            {
                                p.SetValue(novoObjeto, Convert.ChangeType(System.Text.Encoding.Default.GetString((byte[])valorDr), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (valorDr is Guid && p.PropertyType == typeof(string))
                            {
                                p.SetValue(novoObjeto, Convert.ChangeType(valorDr.ToString(), p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && p.PropertyType.GetGenericArguments()[0].IsEnum)
                            {
                                p.SetValue(novoObjeto, Enum.Parse(p.PropertyType.GetGenericArguments()[0], valorDr.ToString()), null);
                            }
                            else if (!p.PropertyType.IsEnum)
                            {
                                p.SetValue(novoObjeto, Nullable.GetUnderlyingType(p.PropertyType) != null
                                        ? Convert.ChangeType(valorDr, Nullable.GetUnderlyingType(p.PropertyType), System.Globalization.CultureInfo.InvariantCulture)
                                        : Convert.ChangeType(valorDr, p.PropertyType, System.Globalization.CultureInfo.InvariantCulture), null);
                            }
                            else if (p.PropertyType.IsEnum)
                            {
                                if (valorDr is string)
                                {
                                    p.SetValue(novoObjeto, Enum.Parse(p.PropertyType, valorDr.ToString()), null);
                                }
                                else
                                {
                                    p.SetValue(novoObjeto, Enum.ToObject(p.PropertyType, valorDr), null);
                                }
                            }
                        }
                        else
                        {
                            if ((p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                            {
                                p.SetValue(novoObjeto, null, null);
                            }
                            else
                            {
                                p.SetValue(novoObjeto, !p.PropertyType.IsValueType
                                    ? null
                                    : Activator.CreateInstance(p.PropertyType), null);
                            }
                        }
                    }
                    listaNovosObjetos.Add(novoObjeto);
                }

                return listaNovosObjetos;

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
                return typeof(T).GetProperties().Where(p => p.CanWrite && dr.HasColumn(p.Name)).ToList();
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
